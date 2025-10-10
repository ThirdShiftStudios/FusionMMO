using System;
using Fusion;

namespace TPSBR
{
        [Serializable]
        public class PlayerInventorySaveData
        {
                public PlayerInventoryItemData[] InventorySlots;
                public PlayerHotbarSlotData[]    HotbarSlots;
                public byte                      CurrentWeaponSlot;
        }

        [Serializable]
        public struct PlayerInventoryItemData
        {
                public int    ItemDefinitionId;
                public byte   Quantity;
                public string ConfigurationHash;
        }

        [Serializable]
        public struct PlayerHotbarSlotData
        {
                public int    WeaponDefinitionId;
                public string ConfigurationHash;
        }

        public sealed class PlayerCloudSaveService : IGlobalService
        {
                // CONSTANTS

                private const string StorageKeyPrefix = "PlayerInventory-";

                // PRIVATE MEMBERS

                private Inventory               _trackedInventory;
                private Inventory               _pendingRegistration;
                private Inventory               _pendingRestoreInventory;
                private PlayerInventorySaveData _cachedData;
                private string                  _storageKey;
                private bool                    _isInitialized;
                private bool                    _pendingSave;
                private bool                    _suppressTracking;

                // PUBLIC PROPERTIES

                public bool IsInitialized => _isInitialized;

                // IGlobalService INTERFACE

                void IGlobalService.Initialize()
                {
                        var pendingInventory = _pendingRegistration;

                        _trackedInventory        = null;
                        _pendingRestoreInventory = null;
                        _pendingSave             = false;
                        _suppressTracking        = false;

                        ResolveStorageKey();

                        _isInitialized = true;

                        if (pendingInventory != null)
                        {
                                _pendingRegistration = null;
                                RegisterInventoryAndRestore(pendingInventory);
                        }
                }

                void IGlobalService.Tick()
                {
                        if (_isInitialized == false)
                        {
                                return;
                        }

                        ProcessDeferredInventory();

                        if (_pendingSave == false)
                                return;

                        CaptureAndStoreSnapshot(false);
                }

                void IGlobalService.Deinitialize()
                {
                        CaptureAndStoreSnapshot(true);
                        DetachInventory();

                        _storageKey             = null;
                        _cachedData             = null;
                        _pendingRegistration    = null;
                        _pendingRestoreInventory = null;
                        _isInitialized          = false;
                        _pendingSave            = false;
                }

                // PUBLIC METHODS

                public void RegisterInventory(Inventory inventory)
                {
                        if (IsInventoryEligible(inventory) == false)
                                return;

                        if (_isInitialized == false)
                        {
                                _pendingRegistration = inventory;
                                return;
                        }

                        AttachInventory(inventory);
                }

                public bool RegisterInventoryAndRestore(Inventory inventory)
                {
                        if (IsInventoryEligible(inventory) == false)
                                return false;

                        if (_isInitialized == false)
                        {
                                _pendingRegistration     = inventory;
                                _pendingRestoreInventory = inventory;
                                return false;
                        }

                        AttachInventory(inventory);

                        bool restored = TryRestoreInventory(inventory);

                        if (restored == false && _cachedData != null)
                        {
                                _pendingRestoreInventory = inventory;
                        }
                        else if (restored == true && ReferenceEquals(_pendingRestoreInventory, inventory) == true)
                        {
                                _pendingRestoreInventory = null;
                        }

                        return restored;
                }

                public void UnregisterInventory(Inventory inventory)
                {
                        if (_pendingRegistration == inventory)
                        {
                                _pendingRegistration = null;
                        }

                        if (_pendingRestoreInventory == inventory)
                        {
                                _pendingRestoreInventory = null;
                        }

                        if (_trackedInventory != inventory)
                                return;

                        CaptureAndStoreSnapshot(true);
                        DetachInventory();
                }

                public bool TryRestoreInventory(Inventory inventory)
                {
                        if (_cachedData == null)
                                return false;

                        if (inventory == null || inventory.HasStateAuthority == false)
                                return false;

                        _suppressTracking = true;
                        inventory.ApplySaveData(_cachedData);
                        _suppressTracking = false;

                        if (ReferenceEquals(_pendingRestoreInventory, inventory) == true)
                        {
                                _pendingRestoreInventory = null;
                        }

                        return true;
                }

                // PRIVATE METHODS

                private bool IsInventoryEligible(Inventory inventory)
                {
                        if (inventory == null)
                                return false;

                        if (inventory.Object == null)
                                return false;

                        var runner = inventory.Runner;
                        if (runner == null)
                                return false;

                        return runner.LocalPlayer == inventory.Object.InputAuthority;
                }

                private void AttachInventory(Inventory inventory)
                {
                        if (_trackedInventory == inventory)
                                return;

                        DetachInventory();

                        _trackedInventory = inventory;
                        if (_trackedInventory != null)
                        {
                                _trackedInventory.ItemSlotChanged += OnItemSlotChanged;
                                _trackedInventory.HotbarSlotChanged += OnHotbarSlotChanged;
                        }
                }

                private void DetachInventory()
                {
                        if (_trackedInventory == null)
                                return;

                        _trackedInventory.ItemSlotChanged -= OnItemSlotChanged;
                        _trackedInventory.HotbarSlotChanged -= OnHotbarSlotChanged;
                        _trackedInventory = null;
                }

                private void ResolveStorageKey()
                {
                        var authenticationService = Global.PlayerAuthenticationService;
                        if (authenticationService != null && authenticationService.IsInitialized == true && authenticationService.IsAuthenticated == true)
                        {
                                _storageKey = StorageKeyPrefix + authenticationService.PlayerId;
                                _cachedData = PersistentStorage.GetObject<PlayerInventorySaveData>(_storageKey);
                                return;
                        }

                        var playerData = Global.PlayerService?.PlayerData;
                        if (playerData != null && playerData.UserID.HasValue() == true)
                        {
                                _storageKey = StorageKeyPrefix + playerData.UserID;
                                _cachedData = PersistentStorage.GetObject<PlayerInventorySaveData>(_storageKey);
                        }
                        else
                        {
                                _storageKey = null;
                                _cachedData = null;
                        }
                }

                private void ProcessDeferredInventory()
                {
                        if (_pendingRegistration != null)
                        {
                                var inventory = _pendingRegistration;
                                _pendingRegistration = null;

                                if (IsInventoryEligible(inventory) == true)
                                {
                                        AttachInventory(inventory);

                                        if (_cachedData != null)
                                        {
                                                bool restored = TryRestoreInventory(inventory);
                                                if (restored == false)
                                                {
                                                        _pendingRestoreInventory = inventory;
                                                }
                                        }
                                }
                        }

                        if (_pendingRestoreInventory != null)
                        {
                                var inventory = _pendingRestoreInventory;

                                if (IsInventoryEligible(inventory) == false)
                                {
                                        _pendingRestoreInventory = null;
                                }
                                else if (_cachedData == null)
                                {
                                        _pendingRestoreInventory = null;
                                }
                                else if (inventory.HasStateAuthority == true)
                                {
                                        if (TryRestoreInventory(inventory) == true)
                                        {
                                                _pendingRestoreInventory = null;
                                        }
                                }
                        }
                }

                private void OnItemSlotChanged(int index, InventorySlot slot)
                {
                        if (_suppressTracking == true)
                                return;

                        if (_trackedInventory == null || _trackedInventory.HasStateAuthority == false)
                                return;

                        if (ReferenceEquals(_pendingRestoreInventory, _trackedInventory) == true)
                                return;

                        _pendingSave = true;
                }

                private void OnHotbarSlotChanged(int index, Weapon weapon)
                {
                        if (_suppressTracking == true)
                                return;

                        if (_trackedInventory == null || _trackedInventory.HasStateAuthority == false)
                                return;

                        if (ReferenceEquals(_pendingRestoreInventory, _trackedInventory) == true)
                                return;

                        _pendingSave = true;
                }

                private void CaptureAndStoreSnapshot(bool forceSave)
                {
                        if (_storageKey.HasValue() == false)
                        {
                                _pendingSave = false;
                                return;
                        }

                        if (_trackedInventory != null && _trackedInventory.HasStateAuthority == true)
                        {
                                _suppressTracking = true;
                                _cachedData = _trackedInventory.CreateSaveData();
                                _suppressTracking = false;
                        }

                        if (_cachedData == null)
                        {
                                PersistentStorage.Delete(_storageKey, saveImmediately: forceSave);
                        }
                        else
                        {
                                PersistentStorage.SetObject(_storageKey, _cachedData, saveImmeditely: forceSave);
                        }

                        if (forceSave == true)
                        {
                                PersistentStorage.Save();
                        }

                        _pendingSave = false;
                }
        }
}
