using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using TSS.Data;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace TPSBR
{
    [Serializable]
    public class PlayerInventorySaveData
    {
        public PlayerInventoryItemData[] InventorySlots;
        public PlayerHotbarSlotData[] HotbarSlots;
        public byte CurrentWeaponSlot;
        public PlayerInventoryItemData PickaxeSlot;
        public PlayerInventoryItemData WoodAxeSlot;
        public int Gold;
        public PlayerCharacterSaveData[] Characters;
        public string ActiveCharacterId;
        public PlayerCharacterInventorySaveData[] CharacterInventories;
    }

    [Serializable]
    public struct PlayerInventoryItemData
    {
        public int ItemDefinitionId;
        public byte Quantity;
        public string ConfigurationHash;
    }

    [Serializable]
    public struct PlayerHotbarSlotData
    {
        public int WeaponDefinitionId;
        public string ConfigurationHash;
    }

    [Serializable]
    public class PlayerCharacterSaveData
    {
        public string CharacterId;
        public string CharacterName;
        public string CharacterDefinitionCode;
        public long CreatedAtUtc;
    }

    [Serializable]
    public class PlayerCharacterInventorySaveData
    {
        public string CharacterId;
        public PlayerInventoryItemData[] InventorySlots;
        public PlayerHotbarSlotData[] HotbarSlots;
        public byte CurrentWeaponSlot;
        public PlayerInventoryItemData PickaxeSlot;
        public PlayerInventoryItemData WoodAxeSlot;
        public int Gold;
    }

    public sealed class PlayerCloudSaveService : IGlobalService
    {
        // CONSTANTS

        private const string StorageKeyPrefix = "PlayerInventory-";

        // PRIVATE MEMBERS

        private Inventory _trackedInventory;
        private Inventory _pendingRegistration;
        private Inventory _pendingRestoreInventory;
        private PlayerInventorySaveData _cachedData;
        private string _storageKey;
        private bool _isInitialized;
        private bool _pendingSave;
        private bool _suppressTracking;
        private bool _cloudSaveReady;
        private Task _saveTask;
        private bool _initialLoadComplete;
        private readonly List<PlayerCharacterSaveData> _characters = new List<PlayerCharacterSaveData>();
        private readonly List<PlayerCharacterInventorySaveData> _characterInventories = new List<PlayerCharacterInventorySaveData>();
        private string _activeCharacterId;

        // PUBLIC PROPERTIES

        public bool IsInitialized => _isInitialized;
        public IReadOnlyList<PlayerCharacterSaveData> Characters => _characters;
        public string ActiveCharacterId => _activeCharacterId;

        public event Action CharactersChanged;
        public event Action<string> ActiveCharacterChanged;

        // IGlobalService INTERFACE

        void IGlobalService.Initialize()
        {
            var pendingInventory = _pendingRegistration;

            _trackedInventory = null;
            _pendingRestoreInventory = null;
            _pendingSave = false;
            _suppressTracking = false;
            _cachedData = null;
            _storageKey = null;
            _cloudSaveReady = false;
            _saveTask = null;
            _initialLoadComplete = false;
            _characters.Clear();
            _characterInventories.Clear();
            _activeCharacterId = null;
            _isInitialized = false;

            _pendingRegistration = pendingInventory;
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
            var activeSaveTask = _saveTask;
            _saveTask = null;
            ObserveTask(activeSaveTask);

            var saveTask = CaptureAndStoreSnapshotAsync(true);
            ObserveTask(saveTask);

            DetachInventory();

            _storageKey = null;
            _cachedData = null;
            _pendingRegistration = null;
            _pendingRestoreInventory = null;
            _saveTask = null;
            _isInitialized = false;
            _pendingSave = false;
            _cloudSaveReady = false;
            _initialLoadComplete = false;
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
                _pendingRegistration = inventory;
                _pendingRestoreInventory = inventory;
                return false;
            }

            AttachInventory(inventory);

            bool restored = TryRestoreInventory(inventory);

            if (restored == true && ReferenceEquals(_pendingRestoreInventory, inventory) == true)
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

            var activeSaveTask = _saveTask;
            _saveTask = null;
            ObserveTask(activeSaveTask);

            var saveTask = CaptureAndStoreSnapshotAsync(true);
            ObserveTask(saveTask);

            DetachInventory();
        }

        public IReadOnlyList<PlayerCharacterSaveData> GetCharacters()
        {
            return _characters;
        }

        public PlayerCharacterSaveData GetCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId) == true)
                return null;

            for (int i = 0; i < _characters.Count; i++)
            {
                var character = _characters[i];
                if (character != null && string.Equals(character.CharacterId, characterId, StringComparison.Ordinal) == true)
                {
                    return character;
                }
            }

            return null;
        }

        public PlayerCharacterSaveData CreateCharacter(string name, CharacterDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrWhiteSpace(name) == true)
                return null;

            string trimmedName = name.Trim();

            if (trimmedName.HasValue() == false)
                return null;

            if (IsCharacterNameAvailable(trimmedName) == false)
                return null;

            var record = new PlayerCharacterSaveData
            {
                CharacterId = Guid.NewGuid().ToString("N"),
                CharacterName = trimmedName,
                CharacterDefinitionCode = definition.StringCode,
                CreatedAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };

            _characters.Add(record);
            EnsureCharacterInventoryData(record.CharacterId);
            _activeCharacterId = record.CharacterId;

            ApplyCharacterDataToCachedData();
            UpdatePlayerActiveCharacter();
            NotifyCharactersChanged();
            NotifyActiveCharacterChanged();
            MarkDirty();

            return record;
        }

        public bool SelectCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId) == true)
                return false;

            if (string.Equals(_activeCharacterId, characterId, StringComparison.Ordinal) == true)
                return false;

            if (GetCharacter(characterId) == null)
                return false;

            _activeCharacterId = characterId;
            EnsureCharacterInventoryData(_activeCharacterId);

            ApplyCharacterDataToCachedData();
            UpdatePlayerActiveCharacter();
            NotifyActiveCharacterChanged();
            MarkDirty();

            return true;
        }

        public bool IsCharacterNameAvailable(string name)
        {
            if (string.IsNullOrEmpty(name) == true)
                return false;

            for (int i = 0; i < _characters.Count; i++)
            {
                var character = _characters[i];
                if (character != null && string.Equals(character.CharacterName, name, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryRestoreInventory(Inventory inventory)
        {
            if (inventory == null || inventory.HasStateAuthority == false)
                return false;

            string characterId = _activeCharacterId;
            PlayerCharacterInventorySaveData inventoryData = null;

            if (characterId.HasValue() == true)
            {
                inventoryData = GetCharacterInventoryData(characterId);

                if (inventoryData == null)
                {
                    EnsureCharacterInventoryData(characterId);
                }
            }

            bool hasInventoryData = inventoryData != null;

            if (hasInventoryData == true)
            {
                _suppressTracking = true;
                inventory.ApplySaveData(inventoryData);
                _suppressTracking = false;
            }

            if (ReferenceEquals(_pendingRestoreInventory, inventory) == true)
            {
                _pendingRestoreInventory = null;
            }

            return hasInventoryData;
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
                _trackedInventory.GoldChanged += OnGoldChanged;
            }
        }

        private void DetachInventory()
        {
            if (_trackedInventory == null)
                return;

            _trackedInventory.ItemSlotChanged -= OnItemSlotChanged;
            _trackedInventory.HotbarSlotChanged -= OnHotbarSlotChanged;
            _trackedInventory.GoldChanged -= OnGoldChanged;
            _trackedInventory = null;
        }

        private PlayerCharacterInventorySaveData GetCharacterInventoryData(string characterId)
        {
            if (string.IsNullOrEmpty(characterId) == true)
                return null;

            for (int i = 0; i < _characterInventories.Count; i++)
            {
                var inventory = _characterInventories[i];
                if (inventory == null)
                    continue;

                if (string.Equals(inventory.CharacterId, characterId, StringComparison.Ordinal) == true)
                {
                    return inventory;
                }
            }

            return null;
        }

        private PlayerCharacterInventorySaveData EnsureCharacterInventoryData(string characterId, bool markDirty = true)
        {
            if (string.IsNullOrEmpty(characterId) == true)
                return null;

            var inventory = GetCharacterInventoryData(characterId);
            if (inventory != null)
                return inventory;

            inventory = new PlayerCharacterInventorySaveData
            {
                CharacterId = characterId,
                PickaxeSlot = default,
                WoodAxeSlot = default,
                CurrentWeaponSlot = 0,
                Gold = 0,
            };

            _characterInventories.Add(inventory);
            if (markDirty == true)
            {
                MarkDirty();
            }
            return inventory;
        }

        private void StoreCharacterInventorySnapshot(PlayerCharacterInventorySaveData snapshot)
        {
            if (snapshot == null || snapshot.CharacterId.HasValue() == false)
                return;

            for (int i = 0; i < _characterInventories.Count; i++)
            {
                var inventory = _characterInventories[i];
                if (inventory != null && string.Equals(inventory.CharacterId, snapshot.CharacterId, StringComparison.Ordinal) == true)
                {
                    _characterInventories[i] = snapshot;
                    return;
                }
            }

            _characterInventories.Add(snapshot);
        }

        private void ResolveStorageKey()
        {
            var authenticationService = Global.PlayerAuthenticationService;
            if (authenticationService != null && authenticationService.IsInitialized == true &&
                authenticationService.IsAuthenticated == true)
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

                _cloudSaveReady = AuthenticationService.Instance != null &&
                                  AuthenticationService.Instance.IsSignedIn == true;

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
                _initialLoadComplete = true;
                _isInitialized = true;
            }
        }

        private async Task EnsurePlayerSignedInAsync()
        {
            var authenticationInstance = AuthenticationService.Instance;

            if (authenticationInstance == null)
                return;

            string desiredProfile = Global.PlayerAuthenticationService?.UnityProfileName;

            if (desiredProfile.HasValue() == false)
            {
                desiredProfile = Global.PlayerService?.PlayerData?.UserID;
            }

            var sanitizedProfile = SanitizeProfileName(desiredProfile);

            if (sanitizedProfile.HasValue() == true && authenticationInstance.Profile != sanitizedProfile)
            {
                if (authenticationInstance.IsSignedIn == true)
                    authenticationInstance.SignOut();

                authenticationInstance.SwitchProfile(sanitizedProfile);
            }

            if (authenticationInstance.IsSignedIn == true)
                return;

            await authenticationInstance.SignInAnonymouslyAsync(new SignInOptions { CreateAccount = true });
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

                if (result != null && result.TryGetValue(_storageKey, out var item) == true)
                {
                    string json = item.ToString();

                    if (string.IsNullOrEmpty(json) == false &&
                        string.Equals(json, "null", StringComparison.OrdinalIgnoreCase) == false)
                    {
                        _cachedData = JsonUtility.FromJson<PlayerInventorySaveData>(json);
                    }
                }

                if (_cachedData != null)
                {
                    if (_trackedInventory != null)
                    {
                        if (_trackedInventory.HasStateAuthority == true)
                        {
                            _pendingRestoreInventory = _trackedInventory;
                        }
                        else if (ReferenceEquals(_pendingRestoreInventory, _trackedInventory) == false)
                        {
                            _pendingRestoreInventory = _trackedInventory;
                        }
                    }
                    else if (_pendingRegistration != null)
                    {
                        _pendingRestoreInventory = _pendingRegistration;
                    }
                }
            }
            catch (Exception exception)
            {
                _cachedData = null;
                Debug.LogWarning($"Failed to load inventory from Unity Cloud Save using key {_storageKey}.");
                Debug.LogException(exception);
            }

            SyncCharactersFromCachedData(true);
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
                    if (_initialLoadComplete == true)
                    {
                        _pendingRestoreInventory = null;
                    }
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

        private void OnGoldChanged(int value)
        {
            if (_suppressTracking == true)
                return;

            if (_trackedInventory == null || _trackedInventory.HasStateAuthority == false)
                return;

            if (ReferenceEquals(_pendingRestoreInventory, _trackedInventory) == true)
                return;

            _pendingSave = true;
        }

        private void MarkDirty()
        {
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
                var snapshot = _trackedInventory.CreateSaveData();
                _suppressTracking = false;

                if (snapshot != null && _activeCharacterId.HasValue() == true)
                {
                    snapshot.CharacterId = _activeCharacterId;
                    StoreCharacterInventorySnapshot(snapshot);
                }
            }

            ApplyCharacterDataToCachedData();

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

        private static void ObserveTask(Task task)
        {
            if (task == null)
                return;

            if (task.IsCompleted == true)
            {
                if (task.IsFaulted == true && task.Exception != null)
                {
                    Debug.LogException(task.Exception);
                }

                return;
            }

            task.ContinueWith(static t =>
            {
                if (t.Exception != null)
                {
                    Debug.LogException(t.Exception);
                }
            }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
        }

        private void ApplyCharacterDataToCachedData()
        {
            EnsureCachedData();

            if (_cachedData == null)
                return;

            if (_characters.Count > 0)
            {
                _cachedData.Characters = _characters.ToArray();
            }
            else
            {
                _cachedData.Characters = null;
            }

            if (_characterInventories.Count > 0)
            {
                _cachedData.CharacterInventories = _characterInventories.ToArray();
            }
            else
            {
                _cachedData.CharacterInventories = null;
            }

            _cachedData.InventorySlots = null;
            _cachedData.HotbarSlots = null;
            _cachedData.CurrentWeaponSlot = 0;
            _cachedData.PickaxeSlot = default;
            _cachedData.WoodAxeSlot = default;
            _cachedData.Gold = 0;

            _cachedData.ActiveCharacterId = _activeCharacterId;
        }

        private void EnsureCachedData()
        {
            if (_cachedData == null)
            {
                _cachedData = new PlayerInventorySaveData();
            }
        }

        private void NotifyCharactersChanged()
        {
            CharactersChanged?.Invoke();
        }

        private void NotifyActiveCharacterChanged()
        {
            ActiveCharacterChanged?.Invoke(_activeCharacterId);
        }

        private void UpdatePlayerActiveCharacter()
        {
            var playerData = Global.PlayerService?.PlayerData;
            if (playerData == null)
                return;

            playerData.ActiveCharacterId = _activeCharacterId;

            if (_activeCharacterId.HasValue() == true)
            {
                var character = GetCharacter(_activeCharacterId);
                if (character != null)
                {
                    playerData.ActiveCharacterName = character.CharacterName;
                    playerData.ActiveCharacterDefinitionCode = character.CharacterDefinitionCode;
                }
                else
                {
                    playerData.ActiveCharacterName = null;
                    playerData.ActiveCharacterDefinitionCode = null;
                }
            }
            else
            {
                playerData.ActiveCharacterName = null;
                playerData.ActiveCharacterDefinitionCode = null;
            }
        }

        private void SyncCharactersFromCachedData(bool notify)
        {
            _characters.Clear();
            _characterInventories.Clear();
            _activeCharacterId = null;

            if (_cachedData != null)
            {
                if (_cachedData.Characters != null && _cachedData.Characters.Length > 0)
                {
                    for (int i = 0; i < _cachedData.Characters.Length; i++)
                    {
                        var character = _cachedData.Characters[i];
                        if (character == null)
                            continue;

                        if (string.IsNullOrEmpty(character.CharacterId) == true)
                            continue;

                        _characters.Add(character);
                    }
                }

                if (_cachedData.CharacterInventories != null && _cachedData.CharacterInventories.Length > 0)
                {
                    for (int i = 0; i < _cachedData.CharacterInventories.Length; i++)
                    {
                        var inventory = _cachedData.CharacterInventories[i];
                        if (inventory == null)
                            continue;

                        if (string.IsNullOrEmpty(inventory.CharacterId) == true)
                            continue;

                        _characterInventories.Add(inventory);
                    }
                }
                else if ((_cachedData.InventorySlots != null && _cachedData.InventorySlots.Length > 0) ||
                         (_cachedData.HotbarSlots != null && _cachedData.HotbarSlots.Length > 0) ||
                         _cachedData.PickaxeSlot.ItemDefinitionId != 0 ||
                         _cachedData.WoodAxeSlot.ItemDefinitionId != 0 ||
                         _cachedData.Gold != 0)
                {
                    string legacyCharacterId = _cachedData.ActiveCharacterId;
                    if (legacyCharacterId.HasValue() == false && _cachedData.Characters != null && _cachedData.Characters.Length > 0)
                    {
                        for (int i = 0; i < _cachedData.Characters.Length; i++)
                        {
                            var character = _cachedData.Characters[i];
                            if (character != null && character.CharacterId.HasValue() == true)
                            {
                                legacyCharacterId = character.CharacterId;
                                break;
                            }
                        }
                    }

                    if (legacyCharacterId.HasValue() == true)
                    {
                        var legacy = new PlayerCharacterInventorySaveData
                        {
                            CharacterId = legacyCharacterId,
                            InventorySlots = _cachedData.InventorySlots,
                            HotbarSlots = _cachedData.HotbarSlots,
                            CurrentWeaponSlot = _cachedData.CurrentWeaponSlot,
                            PickaxeSlot = _cachedData.PickaxeSlot,
                            WoodAxeSlot = _cachedData.WoodAxeSlot,
                            Gold = _cachedData.Gold,
                        };

                        _characterInventories.Add(legacy);
                        MarkDirty();
                    }
                }

                if (_cachedData.ActiveCharacterId.HasValue() == true)
                {
                    _activeCharacterId = _cachedData.ActiveCharacterId;
                }
            }

            for (int i = 0; i < _characters.Count; i++)
            {
                var character = _characters[i];
                if (character == null)
                    continue;

                EnsureCharacterInventoryData(character.CharacterId, false);
            }

            if (_activeCharacterId.HasValue() == false && _characters.Count > 0)
            {
                _activeCharacterId = _characters[0].CharacterId;
            }

            UpdatePlayerActiveCharacter();

            if (notify == true)
            {
                NotifyCharactersChanged();
                NotifyActiveCharacterChanged();
            }
        }
    }
}
