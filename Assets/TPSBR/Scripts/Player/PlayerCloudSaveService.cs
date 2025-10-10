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
                        _trackedInventory = null;
                        _pendingSave      = false;
                        _suppressTracking = false;

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

                        _isInitialized = true;
                }

                void IGlobalService.Tick()
                {
                        if (_pendingSave == false)
                                return;

                        CaptureAndStoreSnapshot(false);
                }

                void IGlobalService.Deinitialize()
                {
                        CaptureAndStoreSnapshot(true);
                        DetachInventory();

                        _storageKey    = null;
                        _cachedData    = null;
                        _isInitialized = false;
                        _pendingSave   = false;
                }

                // PUBLIC METHODS

                public void RegisterInventory(Inventory inventory)
                {
                        if (IsInventoryEligible(inventory) == false)
                                return;

                        if (_trackedInventory == inventory)
                                return;

                        DetachInventory();

                        _trackedInventory = inventory;
                        _trackedInventory.ItemSlotChanged += OnItemSlotChanged;
                        _trackedInventory.HotbarSlotChanged += OnHotbarSlotChanged;
                }

                public void UnregisterInventory(Inventory inventory)
                {
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

                private void DetachInventory()
                {
                        if (_trackedInventory == null)
                                return;

                        _trackedInventory.ItemSlotChanged -= OnItemSlotChanged;
                        _trackedInventory.HotbarSlotChanged -= OnHotbarSlotChanged;
                        _trackedInventory = null;
                }

                private void OnItemSlotChanged(int index, InventorySlot slot)
                {
                        if (_suppressTracking == true)
                                return;

                        if (_trackedInventory == null || _trackedInventory.HasStateAuthority == false)
                                return;

                        _pendingSave = true;
                }

                private void OnHotbarSlotChanged(int index, Weapon weapon)
                {
                        if (_suppressTracking == true)
                                return;

                        if (_trackedInventory == null || _trackedInventory.HasStateAuthority == false)
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
