using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using UnityEngine;

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
                private bool                    _cloudSaveReady;
                private Task                    _saveTask;

                // PUBLIC PROPERTIES

                public bool IsInitialized => _isInitialized;

                // IGlobalService INTERFACE

                void IGlobalService.Initialize()
                {
                        var pendingInventory = _pendingRegistration;

                        _trackedInventory         = null;
                        _pendingRestoreInventory  = null;
                        _pendingSave              = false;
                        _suppressTracking         = false;
                        _cachedData               = null;
                        _storageKey               = null;
                        _cloudSaveReady           = false;
                        _saveTask                 = null;
                        _isInitialized            = false;

                        _pendingRegistration    = pendingInventory;
                        _pendingRestoreInventory = pendingInventory;

                        _ = InitializeAsync();
                }

                void IGlobalService.Tick()
                {
                        if (_isInitialized == false)
                        {
                                return;
                        }

                        if (_saveTask != null && _saveTask.IsCompleted == true)
                        {
                                _saveTask = null;
                        }

                        ProcessDeferredInventory();

                        if (_pendingSave == false)
                                return;

                        if (_saveTask != null)
                                return;

                        _saveTask = CaptureAndStoreSnapshotAsync(false);
                }

                void IGlobalService.Deinitialize()
                {
                        var saveTask = CaptureAndStoreSnapshotAsync(true);

                        if (saveTask.IsCompleted == false)
                        {
                                try
                                {
                                        saveTask.GetAwaiter().GetResult();
                                }
                                catch (Exception exception)
                                {
                                        Debug.LogException(exception);
                                }
                        }

                        DetachInventory();

                        _storageKey              = null;
                        _cachedData              = null;
                        _pendingRegistration     = null;
                        _pendingRestoreInventory = null;
                        _saveTask                = null;
                        _isInitialized           = false;
                        _pendingSave             = false;
                        _cloudSaveReady          = false;
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

                        var saveTask = CaptureAndStoreSnapshotAsync(true);

                        if (saveTask.IsCompleted == false)
                        {
                                try
                                {
                                        saveTask.GetAwaiter().GetResult();
                                }
                                catch (Exception exception)
                                {
                                        Debug.LogException(exception);
                                }
                        }

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
                                return;
                        }

                        var playerData = Global.PlayerService?.PlayerData;
                        if (playerData != null && playerData.UserID.HasValue() == true)
                        {
                                _storageKey = StorageKeyPrefix + playerData.UserID;
                        }
                        else
                        {
                                _storageKey = null;
                        }
                }

                private async Task InitializeAsync()
                {
                        try
                        {
                                await Global.UnityServicesInitialization;
                                await EnsurePlayerSignedInAsync();

                                _cloudSaveReady = AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn == true;

                                ResolveStorageKey();

                                if (_cloudSaveReady == true && _storageKey.HasValue() == true)
                                {
                                        await LoadCachedDataAsync();
                                }
                        }
                        catch (Exception exception)
                        {
                                _cloudSaveReady = false;
                                _cachedData = null;
                                Debug.LogWarning("Failed to initialize Unity Cloud Save for player inventory persistence.");
                                Debug.LogException(exception);
                        }
                        finally
                        {
                                _isInitialized = true;
                        }
                }

                private async Task EnsurePlayerSignedInAsync()
                {
                        var desiredProfile = Global.PlayerService?.PlayerData?.UserID;
                        var sanitizedProfile = SanitizeProfileName(desiredProfile);

                        if (sanitizedProfile.HasValue() == true && AuthenticationService.Instance.Profile != sanitizedProfile)
                        {
                                AuthenticationService.Instance.SwitchProfile(sanitizedProfile);
                        }

                        if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn == true)
                                return;

                        await AuthenticationService.Instance.SignInAnonymouslyAsync(new SignInOptions { CreateAccount = true });
                }

                private static string SanitizeProfileName(string profileName)
                {
                        if (string.IsNullOrEmpty(profileName) == true)
                                return null;

                        Span<char> buffer = stackalloc char[Math.Min(profileName.Length, 30)];
                        int index = 0;

                        for (int i = 0; i < profileName.Length && index < buffer.Length; i++)
                        {
                                char character = profileName[i];

                                if (char.IsLetterOrDigit(character) == false && character != '-' && character != '_')
                                        continue;

                                buffer[index++] = character;
                        }

                        if (index == 0)
                                return null;

                        return new string(buffer.Slice(0, index));
                }

                private async Task LoadCachedDataAsync()
                {
                        _cachedData = null;

                        try
                        {
                                var keys = new HashSet<string> { _storageKey };
                                var result = await CloudSaveService.Instance.Data.LoadAsync(keys);

                                if (result != null && result.TryGetValue(_storageKey, out string json) == true)
                                {
                                        if (string.IsNullOrEmpty(json) == false)
                                        {
                                                _cachedData = JsonUtility.FromJson<PlayerInventorySaveData>(json);
                                        }
                                }
                        }
                        catch (Exception exception)
                        {
                                _cachedData = null;
                                Debug.LogWarning($"Failed to load inventory from Unity Cloud Save using key {_storageKey}.");
                                Debug.LogException(exception);
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

                private async Task CaptureAndStoreSnapshotAsync(bool forceSave)
                {
                        _ = forceSave;
                        _pendingSave = false;

                        if (_cloudSaveReady == false || _storageKey.HasValue() == false)
                                return;

                        if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn == false)
                                return;

                        if (_trackedInventory != null && _trackedInventory.HasStateAuthority == true)
                        {
                                _suppressTracking = true;
                                _cachedData = _trackedInventory.CreateSaveData();
                                _suppressTracking = false;
                        }

                        if (_cachedData == null)
                                return;

                        try
                        {
                                string payload = JsonUtility.ToJson(_cachedData);
                                var data = new Dictionary<string, object>
                                {
                                        { _storageKey, payload }
                                };

                                await CloudSaveService.Instance.Data.ForceSaveAsync(data);
                        }
                        catch (Exception exception)
                        {
                                Debug.LogWarning($"Failed to save inventory to Unity Cloud Save using key {_storageKey}.");
                                Debug.LogException(exception);
                        }
                }
        }
}
