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
        public PlayerCharacterProfessionSaveData[] CharacterProfessions;
        public PlayerCharacterStatsSaveData[] CharacterStats;
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

    [Serializable]
    public class PlayerCharacterProfessionSaveData
    {
        public string CharacterId;
        public PlayerProfessionSaveData[] Professions;
    }

    [Serializable]
    public class PlayerCharacterStatsSaveData
    {
        public string CharacterId;
        public PlayerStatSaveData[] Stats;
    }

    [Serializable]
    public struct PlayerProfessionSaveData
    {
        public string ProfessionCode;
        public byte Level;
        public int Experience;
    }

    public sealed class PlayerCloudSaveService : IGlobalService
    {
        // CONSTANTS

        private const string StorageKeyPrefix = "PlayerInventory-";

        // PRIVATE MEMBERS

        private Inventory _trackedInventory;
        private Inventory _pendingRegistration;
        private Inventory _pendingRestoreInventory;
        private Professions _trackedProfessions;
        private Professions _pendingProfessionsRegistration;
        private Professions _pendingRestoreProfessions;
        private Stats _trackedStats;
        private Stats _pendingStatsRegistration;
        private Stats _pendingRestoreStats;
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
        private readonly List<PlayerCharacterProfessionSaveData> _characterProfessions = new List<PlayerCharacterProfessionSaveData>();
        private readonly List<PlayerCharacterStatsSaveData> _characterStats = new List<PlayerCharacterStatsSaveData>();
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
            var pendingProfessions = _pendingProfessionsRegistration;
            var pendingStats = _pendingStatsRegistration;

            _trackedInventory = null;
            _pendingRestoreInventory = null;
            _trackedProfessions = null;
            _pendingRestoreProfessions = null;
            _trackedStats = null;
            _pendingRestoreStats = null;
            _pendingSave = false;
            _suppressTracking = false;
            _cachedData = null;
            _storageKey = null;
            _cloudSaveReady = false;
            _saveTask = null;
            _initialLoadComplete = false;
            _characters.Clear();
            _characterInventories.Clear();
            _characterProfessions.Clear();
            _characterStats.Clear();
            _activeCharacterId = null;
            _isInitialized = false;

            _pendingRegistration = pendingInventory;
            _pendingRestoreInventory = pendingInventory;
            _pendingProfessionsRegistration = pendingProfessions;
            _pendingRestoreProfessions = pendingProfessions;
            _pendingStatsRegistration = pendingStats;
            _pendingRestoreStats = pendingStats;

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

            ProcessDeferredComponents();

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
            DetachProfessions();
            DetachStats();

            _storageKey = null;
            _cachedData = null;
            _pendingRegistration = null;
            _pendingRestoreInventory = null;
            _pendingProfessionsRegistration = null;
            _pendingRestoreProfessions = null;
            _pendingStatsRegistration = null;
            _pendingRestoreStats = null;
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

        public void RegisterProfessions(Professions professions)
        {
            if (IsProfessionsEligible(professions) == false)
                return;

            if (_isInitialized == false)
            {
                _pendingProfessionsRegistration = professions;
                return;
            }

            AttachProfessions(professions);
        }

        public bool RegisterProfessionsAndRestore(Professions professions)
        {
            if (IsProfessionsEligible(professions) == false)
                return false;

            if (_isInitialized == false)
            {
                _pendingProfessionsRegistration = professions;
                _pendingRestoreProfessions = professions;
                return false;
            }

            AttachProfessions(professions);

            bool restored = TryRestoreProfessions(professions);

            if (restored == true && ReferenceEquals(_pendingRestoreProfessions, professions) == true)
            {
                _pendingRestoreProfessions = null;
            }

            return restored;
        }

        public void UnregisterProfessions(Professions professions)
        {
            if (_pendingProfessionsRegistration == professions)
            {
                _pendingProfessionsRegistration = null;
            }

            if (_pendingRestoreProfessions == professions)
            {
                _pendingRestoreProfessions = null;
            }

            if (_trackedProfessions != professions)
                return;

            DetachProfessions();
        }

        public void RegisterStats(Stats stats)
        {
            if (IsStatsEligible(stats) == false)
                return;

            if (_isInitialized == false)
            {
                _pendingStatsRegistration = stats;
                return;
            }

            AttachStats(stats);
        }

        public bool RegisterStatsAndRestore(Stats stats)
        {
            if (IsStatsEligible(stats) == false)
                return false;

            if (_isInitialized == false)
            {
                _pendingStatsRegistration = stats;
                _pendingRestoreStats = stats;
                return false;
            }

            AttachStats(stats);

            bool restored = TryRestoreStats(stats);

            if (restored == true && ReferenceEquals(_pendingRestoreStats, stats) == true)
            {
                _pendingRestoreStats = null;
            }

            return restored;
        }

        public void UnregisterStats(Stats stats)
        {
            if (_pendingStatsRegistration == stats)
            {
                _pendingStatsRegistration = null;
            }

            if (_pendingRestoreStats == stats)
            {
                _pendingRestoreStats = null;
            }

            if (_trackedStats != stats)
                return;

            DetachStats();
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
            EnsureCharacterProfessionData(record.CharacterId);
            EnsureCharacterStatsData(record.CharacterId);
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
            EnsureCharacterProfessionData(_activeCharacterId);
            EnsureCharacterStatsData(_activeCharacterId);

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

        private bool TryRestoreProfessions(Professions professions)
        {
            if (professions == null || professions.HasStateAuthority == false)
                return false;

            string characterId = _activeCharacterId;
            PlayerCharacterProfessionSaveData professionData = null;

            if (characterId.HasValue() == true)
            {
                professionData = GetCharacterProfessionData(characterId);

                if (professionData == null)
                {
                    EnsureCharacterProfessionData(characterId);
                }
            }

            bool hasProfessionData = professionData != null &&
                                     professionData.Professions != null &&
                                     professionData.Professions.Length > 0;

            if (hasProfessionData == true)
            {
                _suppressTracking = true;
                professions.ApplySaveData(professionData.Professions);
                _suppressTracking = false;
            }

            if (ReferenceEquals(_pendingRestoreProfessions, professions) == true)
            {
                _pendingRestoreProfessions = null;
            }

            return hasProfessionData;
        }

        private bool TryRestoreStats(Stats stats)
        {
            if (stats == null || stats.HasStateAuthority == false)
                return false;

            string characterId = _activeCharacterId;
            PlayerCharacterStatsSaveData statData = null;

            if (characterId.HasValue() == true)
            {
                statData = GetCharacterStatsData(characterId);

                if (statData == null)
                {
                    EnsureCharacterStatsData(characterId);
                }
            }

            bool hasStatData = statData != null &&
                                statData.Stats != null &&
                                statData.Stats.Length > 0;

            if (hasStatData == true)
            {
                _suppressTracking = true;
                stats.ApplySaveData(statData.Stats);
                _suppressTracking = false;
            }

            if (ReferenceEquals(_pendingRestoreStats, stats) == true)
            {
                _pendingRestoreStats = null;
            }

            return hasStatData;
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

        private bool IsProfessionsEligible(Professions professions)
        {
            if (professions == null)
                return false;

            if (professions.Object == null)
                return false;

            var runner = professions.Runner;
            if (runner == null)
                return false;

            return runner.LocalPlayer == professions.Object.InputAuthority;
        }

        private bool IsStatsEligible(Stats stats)
        {
            if (stats == null)
                return false;

            if (stats.Object == null)
                return false;

            var runner = stats.Runner;
            if (runner == null)
                return false;

            return runner.LocalPlayer == stats.Object.InputAuthority;
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

        private void AttachProfessions(Professions professions)
        {
            if (_trackedProfessions == professions)
                return;

            DetachProfessions();

            _trackedProfessions = professions;
            if (_trackedProfessions != null)
            {
                _trackedProfessions.ProfessionChanged += OnProfessionChanged;
            }
        }

        private void DetachProfessions()
        {
            if (_trackedProfessions == null)
                return;

            _trackedProfessions.ProfessionChanged -= OnProfessionChanged;
            _trackedProfessions = null;
        }

        private void AttachStats(Stats stats)
        {
            if (_trackedStats == stats)
                return;

            DetachStats();

            _trackedStats = stats;
            if (_trackedStats != null)
            {
                _trackedStats.StatChanged += OnStatChanged;
            }
        }

        private void DetachStats()
        {
            if (_trackedStats == null)
                return;

            _trackedStats.StatChanged -= OnStatChanged;
            _trackedStats = null;
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

        private PlayerCharacterProfessionSaveData GetCharacterProfessionData(string characterId)
        {
            if (string.IsNullOrEmpty(characterId) == true)
                return null;

            for (int i = 0; i < _characterProfessions.Count; i++)
            {
                var professions = _characterProfessions[i];
                if (professions == null)
                    continue;

                if (string.Equals(professions.CharacterId, characterId, StringComparison.Ordinal) == true)
                {
                    return professions;
                }
            }

            return null;
        }

        private PlayerCharacterProfessionSaveData EnsureCharacterProfessionData(string characterId, bool markDirty = true)
        {
            if (string.IsNullOrEmpty(characterId) == true)
                return null;

            var professions = GetCharacterProfessionData(characterId);
            if (professions != null)
                return professions;

            professions = new PlayerCharacterProfessionSaveData
            {
                CharacterId = characterId,
                Professions = null,
            };

            _characterProfessions.Add(professions);
            if (markDirty == true)
            {
                MarkDirty();
            }
            return professions;
        }

        private void StoreCharacterProfessionSnapshot(PlayerCharacterProfessionSaveData snapshot)
        {
            if (snapshot == null || snapshot.CharacterId.HasValue() == false)
                return;

            for (int i = 0; i < _characterProfessions.Count; i++)
            {
                var professions = _characterProfessions[i];
                if (professions != null && string.Equals(professions.CharacterId, snapshot.CharacterId, StringComparison.Ordinal) == true)
                {
                    _characterProfessions[i] = snapshot;
                    return;
                }
            }

            _characterProfessions.Add(snapshot);
        }

        private PlayerCharacterStatsSaveData GetCharacterStatsData(string characterId)
        {
            if (string.IsNullOrEmpty(characterId) == true)
                return null;

            for (int i = 0; i < _characterStats.Count; i++)
            {
                var stats = _characterStats[i];
                if (stats == null)
                    continue;

                if (string.Equals(stats.CharacterId, characterId, StringComparison.Ordinal) == true)
                {
                    return stats;
                }
            }

            return null;
        }

        private PlayerCharacterStatsSaveData EnsureCharacterStatsData(string characterId, bool markDirty = true)
        {
            if (string.IsNullOrEmpty(characterId) == true)
                return null;

            var stats = GetCharacterStatsData(characterId);
            if (stats != null)
                return stats;

            stats = new PlayerCharacterStatsSaveData
            {
                CharacterId = characterId,
                Stats = null,
            };

            _characterStats.Add(stats);
            if (markDirty == true)
            {
                MarkDirty();
            }

            return stats;
        }

        private void StoreCharacterStatsSnapshot(PlayerCharacterStatsSaveData snapshot)
        {
            if (snapshot == null || snapshot.CharacterId.HasValue() == false)
                return;

            for (int i = 0; i < _characterStats.Count; i++)
            {
                var stats = _characterStats[i];
                if (stats != null && string.Equals(stats.CharacterId, snapshot.CharacterId, StringComparison.Ordinal) == true)
                {
                    _characterStats[i] = snapshot;
                    return;
                }
            }

            _characterStats.Add(snapshot);
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

                    if (_trackedProfessions != null)
                    {
                        if (_trackedProfessions.HasStateAuthority == true)
                        {
                            _pendingRestoreProfessions = _trackedProfessions;
                        }
                        else if (ReferenceEquals(_pendingRestoreProfessions, _trackedProfessions) == false)
                        {
                            _pendingRestoreProfessions = _trackedProfessions;
                        }
                    }
                    else if (_pendingProfessionsRegistration != null)
                    {
                        _pendingRestoreProfessions = _pendingProfessionsRegistration;
                    }

                    if (_trackedStats != null)
                    {
                        if (_trackedStats.HasStateAuthority == true)
                        {
                            _pendingRestoreStats = _trackedStats;
                        }
                        else if (ReferenceEquals(_pendingRestoreStats, _trackedStats) == false)
                        {
                            _pendingRestoreStats = _trackedStats;
                        }
                    }
                    else if (_pendingStatsRegistration != null)
                    {
                        _pendingRestoreStats = _pendingStatsRegistration;
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

        private void ProcessDeferredComponents()
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

            if (_pendingProfessionsRegistration != null)
            {
                var professions = _pendingProfessionsRegistration;
                _pendingProfessionsRegistration = null;

                if (IsProfessionsEligible(professions) == true)
                {
                    AttachProfessions(professions);

                    if (_cachedData != null)
                    {
                        bool restored = TryRestoreProfessions(professions);
                        if (restored == false)
                        {
                            _pendingRestoreProfessions = professions;
                        }
                    }
                }
            }

            if (_pendingRestoreProfessions != null)
            {
                var professions = _pendingRestoreProfessions;

                if (IsProfessionsEligible(professions) == false)
                {
                    _pendingRestoreProfessions = null;
                }
                else if (_cachedData == null)
                {
                    if (_initialLoadComplete == true)
                    {
                        _pendingRestoreProfessions = null;
                    }
                }
                else if (professions.HasStateAuthority == true)
                {
                    if (TryRestoreProfessions(professions) == true)
                    {
                        _pendingRestoreProfessions = null;
                    }
                }
            }

            if (_pendingStatsRegistration != null)
            {
                var stats = _pendingStatsRegistration;
                _pendingStatsRegistration = null;

                if (IsStatsEligible(stats) == true)
                {
                    AttachStats(stats);

                    if (_cachedData != null)
                    {
                        bool restored = TryRestoreStats(stats);
                        if (restored == false)
                        {
                            _pendingRestoreStats = stats;
                        }
                    }
                }
            }

            if (_pendingRestoreStats != null)
            {
                var stats = _pendingRestoreStats;

                if (IsStatsEligible(stats) == false)
                {
                    _pendingRestoreStats = null;
                }
                else if (_cachedData == null)
                {
                    if (_initialLoadComplete == true)
                    {
                        _pendingRestoreStats = null;
                    }
                }
                else if (stats.HasStateAuthority == true)
                {
                    if (TryRestoreStats(stats) == true)
                    {
                        _pendingRestoreStats = null;
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

        private void OnProfessionChanged(Professions.ProfessionIndex profession, Professions.ProfessionSnapshot previousSnapshot, Professions.ProfessionSnapshot newSnapshot)
        {
            if (_suppressTracking == true)
                return;

            if (_trackedProfessions == null || _trackedProfessions.HasStateAuthority == false)
                return;

            if (ReferenceEquals(_pendingRestoreProfessions, _trackedProfessions) == true)
                return;

            _pendingSave = true;
        }

        private void OnStatChanged(Stats.StatIndex stat, int previousValue, int newValue)
        {
            if (_suppressTracking == true)
                return;

            if (_trackedStats == null || _trackedStats.HasStateAuthority == false)
                return;

            if (ReferenceEquals(_pendingRestoreStats, _trackedStats) == true)
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

            PlayerCharacterInventorySaveData inventorySnapshot = null;
            if (_trackedInventory != null && _trackedInventory.HasStateAuthority == true)
            {
                _suppressTracking = true;
                inventorySnapshot = _trackedInventory.CreateSaveData();
                _suppressTracking = false;

                if (inventorySnapshot != null && _activeCharacterId.HasValue() == true)
                {
                    inventorySnapshot.CharacterId = _activeCharacterId;
                }
            }

            PlayerCharacterProfessionSaveData professionSnapshot = null;
            if (_trackedProfessions != null && _trackedProfessions.HasStateAuthority == true && _activeCharacterId.HasValue() == true)
            {
                var professionData = _trackedProfessions.CreateSaveData();
                if (professionData != null && professionData.Length > 0)
                {
                    professionSnapshot = new PlayerCharacterProfessionSaveData
                    {
                        CharacterId = _activeCharacterId,
                        Professions = professionData,
                    };
                }
            }

            PlayerCharacterStatsSaveData statsSnapshot = null;
            if (_trackedStats != null && _trackedStats.HasStateAuthority == true && _activeCharacterId.HasValue() == true)
            {
                var statData = _trackedStats.CreateSaveData();
                if (statData != null && statData.Length > 0)
                {
                    statsSnapshot = new PlayerCharacterStatsSaveData
                    {
                        CharacterId = _activeCharacterId,
                        Stats = statData,
                    };
                }
            }

            if (inventorySnapshot != null && inventorySnapshot.CharacterId.HasValue() == true)
            {
                StoreCharacterInventorySnapshot(inventorySnapshot);
            }

            if (professionSnapshot != null)
            {
                StoreCharacterProfessionSnapshot(professionSnapshot);
            }

            if (statsSnapshot != null)
            {
                StoreCharacterStatsSnapshot(statsSnapshot);
            }

            if (_activeCharacterId.HasValue() == true)
            {
                EnsureCharacterInventoryData(_activeCharacterId, false);
                EnsureCharacterProfessionData(_activeCharacterId, false);
                EnsureCharacterStatsData(_activeCharacterId, false);
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

            if (_characterProfessions.Count > 0)
            {
                _cachedData.CharacterProfessions = _characterProfessions.ToArray();
            }
            else
            {
                _cachedData.CharacterProfessions = null;
            }

            if (_characterStats.Count > 0)
            {
                _cachedData.CharacterStats = _characterStats.ToArray();
            }
            else
            {
                _cachedData.CharacterStats = null;
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
            _characterProfessions.Clear();
            _characterStats.Clear();
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

                if (_cachedData.CharacterProfessions != null && _cachedData.CharacterProfessions.Length > 0)
                {
                    for (int i = 0; i < _cachedData.CharacterProfessions.Length; i++)
                    {
                        var professions = _cachedData.CharacterProfessions[i];
                        if (professions == null)
                            continue;

                        if (string.IsNullOrEmpty(professions.CharacterId) == true)
                            continue;

                        _characterProfessions.Add(professions);
                    }
                }

                if (_cachedData.CharacterStats != null && _cachedData.CharacterStats.Length > 0)
                {
                    for (int i = 0; i < _cachedData.CharacterStats.Length; i++)
                    {
                        var stats = _cachedData.CharacterStats[i];
                        if (stats == null)
                            continue;

                        if (string.IsNullOrEmpty(stats.CharacterId) == true)
                            continue;

                        _characterStats.Add(stats);
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
                EnsureCharacterProfessionData(character.CharacterId, false);
                EnsureCharacterStatsData(character.CharacterId, false);
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
