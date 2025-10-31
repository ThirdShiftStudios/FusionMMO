namespace TPSBR
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Fusion;
    using TSS.Data;
    using Unity.Template.CompetitiveActionMultiplayer;

    [Serializable]
    public sealed class WeaponSlot
    {
        public Transform Active;
        public Transform Inactive;
        [NonSerialized] public Quaternion BaseRotation;
    }

    [Serializable]
    public struct WeaponSizeSlot
    {
        public WeaponSize Size;
        public int SlotIndex;
    }

    public struct InventorySlot : INetworkStruct, IEquatable<InventorySlot>
    {
        public InventorySlot(int itemDefinitionId, byte quantity, NetworkString<_32> configurationHash)
        {
            ItemDefinitionId = itemDefinitionId;
            Quantity = quantity;
            ConfigurationHash = configurationHash;
        }

        public int ItemDefinitionId { get; private set; }
        public byte Quantity { get; private set; }
        public NetworkString<_32> ConfigurationHash { get; private set; }

        public bool IsEmpty => Quantity == 0;

        public void Clear()
        {
            ItemDefinitionId = 0;
            Quantity = 0;
            ConfigurationHash = default;
        }

        public void Add(byte amount)
        {
            int newQuantity = Quantity + amount;
            Quantity = (byte)Mathf.Clamp(newQuantity, 0, byte.MaxValue);
        }

        public void Remove(byte amount)
        {
            int newQuantity = Quantity - amount;
            Quantity = (byte)Mathf.Clamp(newQuantity, 0, byte.MaxValue);

            if (Quantity == 0)
            {
                ItemDefinitionId = 0;
                ConfigurationHash = default;
            }
        }

        public bool Equals(InventorySlot other)
        {
            return ItemDefinitionId == other.ItemDefinitionId && Quantity == other.Quantity &&
                   ConfigurationHash == other.ConfigurationHash;
        }

        public override bool Equals(object obj)
        {
            return obj is InventorySlot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ItemDefinitionId;
                hashCode = (hashCode * 397) ^ Quantity.GetHashCode();
                hashCode = (hashCode * 397) ^ ConfigurationHash.GetHashCode();
                return hashCode;
            }
        }

        public ItemDefinition GetDefinition()
        {
            return Quantity == 0 ? null : ItemDefinition.Get(ItemDefinitionId);
        }
    }

    public sealed class Inventory : NetworkBehaviour, IBeforeTick
    {
        // PUBLIC MEMBERS
        public Weapon CurrentWeapon { get; private set; }
        public Transform CurrentWeaponHandle { get; private set; }
        public Quaternion CurrentWeaponBaseRotation { get; private set; }
        public int Gold => _gold;

        public LayerMask HitMask => _hitMask;
        public int CurrentWeaponSlot => _currentWeaponSlot;
        public int PreviousWeaponSlot => _previousWeaponSlot;
        public WeaponSize CurrentWeaponSize => CurrentWeapon != null ? CurrentWeapon.Size : WeaponSize.Unarmed;
        public const int INVENTORY_SIZE = 10;
        public const int PICKAXE_SLOT_INDEX = byte.MaxValue;
        public const int WOOD_AXE_SLOT_INDEX = byte.MaxValue - 1;
        public const int FISHING_POLE_SLOT_INDEX = byte.MaxValue - 2;
        public const int HEAD_SLOT_INDEX = byte.MaxValue - 3;
        public const int UPPER_BODY_SLOT_INDEX = byte.MaxValue - 4;
        public const int LOWER_BODY_SLOT_INDEX = byte.MaxValue - 5;
        public const int HOTBAR_CAPACITY = 7;
        public const int HOTBAR_VISIBLE_SLOTS = HOTBAR_CAPACITY - 1;
        public const int HOTBAR_UNARMED_SLOT = 0;
        public const int HOTBAR_PRIMARY_WEAPON_SLOT = 1;
        public const int HOTBAR_SECONDARY_WEAPON_SLOT = 2;
        public const int HOTBAR_FIRST_CONSUMABLE_SLOT = 3;
        public const int HOTBAR_SECOND_CONSUMABLE_SLOT = 4;
        public const int HOTBAR_THIRD_CONSUMABLE_SLOT = 5;
        public const int HOTBAR_FISHING_POLE_SLOT = HOTBAR_CAPACITY - 1;
        // PRIVATE MEMBERS

        [SerializeField] private WeaponSlot[] _slots;
        [SerializeField] private WeaponSizeSlot[] _weaponSizeSlots;
        [SerializeField] private Weapon[] _initialWeapons;
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private InventoryItemPickupProvider _inventoryItemPickupPrefab;
        [SerializeField] private float _itemDropForwardOffset = 1.5f;
        [SerializeField] private float _itemDropUpOffset = 0.35f;
        [SerializeField] private float _itemDropImpulse = 3f;
        [SerializeField] private Transform _pickaxeEquippedParent;
        [SerializeField] private Transform _pickaxeUnequippedParent;
        [SerializeField] private Transform _woodAxeEquippedParent;
        [SerializeField] private Transform _woodAxeUnequippedParent;
        [SerializeField] private Transform _fishingPoleEquippedParent;
        [SerializeField] private Transform _fishingPoleUnequippedParent;
        [SerializeField] private Vector2Int _fightingSuccessHitRange = new Vector2Int(2, 4);

        [Header("Audio")] [SerializeField] private Transform _fireAudioEffectsRoot;

        [Networked, Capacity(HOTBAR_CAPACITY)] private NetworkArray<Weapon> _hotbar { get; }
        [Networked, Capacity(INVENTORY_SIZE)] private NetworkArray<InventorySlot> _items { get; }
        [Networked] private InventorySlot _pickaxeSlot { get; set; }
        [Networked] private InventorySlot _woodAxeSlot { get; set; }
        [Networked] private InventorySlot _fishingPoleSlot { get; set; }
        [Networked] private InventorySlot _headSlot { get; set; }
        [Networked] private InventorySlot _upperBodySlot { get; set; }
        [Networked] private InventorySlot _lowerBodySlot { get; set; }
        [Networked] private byte _currentWeaponSlot { get; set; }

        [Networked]
        private int _gold { get; set; }

        [Networked] private byte _previousWeaponSlot { get; set; }
        [Networked] private Pickaxe _pickaxe { get; set; }
        [Networked] private NetworkBool _isPickaxeEquipped { get; set; }
        [Networked] private WoodAxe _woodAxe { get; set; }
        [Networked] private NetworkBool _isWoodAxeEquipped { get; set; }
        [Networked] private FishingPoleWeapon _fishingPole { get; set; }
        [Networked] private NetworkBool _isFishingPoleEquipped { get; set; }
        [Networked] private byte _fightingHitsRequired { get; set; }
        [Networked] private byte _fightingHitsSucceeded { get; set; }

        private Health _health;
        private Character _character;
        private Interactions _interactions;
        private Stats _stats;
        private int[] _statBaseValues;
        private int[] _appliedHotbarBonuses;
        private int[] _hotbarStatBuffer;
        private AudioEffect[] _fireAudioEffects;
        private Weapon[] _localWeapons = new Weapon[HOTBAR_CAPACITY];
        private Weapon[] _lastHotbarWeapons;
        private InventorySlot[] _localItems;
        private InventorySlot _localPickaxeSlot;
        private InventorySlot _localWoodAxeSlot;
        private InventorySlot _localFishingPoleSlot;
        private InventorySlot _localHeadSlot;
        private InventorySlot _localUpperBodySlot;
        private InventorySlot _localLowerBodySlot;
        private Pickaxe _localPickaxe;
        private bool _localPickaxeEquipped;
        private byte _weaponSlotBeforePickaxe = byte.MaxValue;
        private WoodAxe _localWoodAxe;
        private bool _localWoodAxeEquipped;
        private byte _weaponSlotBeforeWoodAxe = byte.MaxValue;
        private FishingPoleWeapon _localFishingPole;
        private bool _localFishingPoleEquipped;
        private byte _weaponSlotBeforeFishingPole = byte.MaxValue;
        private Dictionary<int, WeaponDefinition> _weaponDefinitionsBySlot = new Dictionary<int, WeaponDefinition>();
        private readonly Dictionary<WeaponSize, int> _weaponSizeToSlotIndex = new Dictionary<WeaponSize, int>();
        private HashSet<int> _suppressedItemFeedSlots;
        private int _localGold;
        private ChangeDetector _changeDetector;
        private FishingLifecycleState _fishingLifecycleState = FishingLifecycleState.Inactive;
        private bool _isHookSetSuccessZoneActive;

        private static readonly Dictionary<int, Weapon> _weaponPrefabsByDefinitionId = new Dictionary<int, Weapon>();
        private static PickaxeDefinition _cachedFallbackPickaxe;
        private static WoodAxeDefinition _cachedFallbackWoodAxe;

        public event Action<int, InventorySlot> ItemSlotChanged;
        public event Action<int, Weapon> HotbarSlotChanged;
        public event Action<int> GoldChanged;
        public event Action<bool> FishingPoleEquippedChanged;
        public event Action<FishingLifecycleState> FishingLifecycleStateChanged;

        public bool IsFishingPoleEquipped => _localFishingPoleEquipped;
        public FishingLifecycleState FishingLifecycleState => _fishingLifecycleState;
        public int FightingMinigameHitsRequired => Mathf.Max(1, _fightingHitsRequired);
        public int FightingMinigameHitsSucceeded => _fightingHitsSucceeded;

        // PUBLIC METHODS

        public int InventorySize => _items.Length;
        public int HotbarSize => _hotbar.Length;

        public Weapon GetHotbarWeapon(int index)
        {
            if (index < 0 || index >= _hotbar.Length)
            {
                return null;
            }

            if (_localWeapons != null && index < _localWeapons.Length)
            {
                Weapon localWeapon = _localWeapons[index];
                if (localWeapon != null)
                {
                    return localWeapon;
                }
            }

            return _hotbar[index];
        }

        public void SetGold(int amount)
        {
            if (HasStateAuthority == false)
                return;

            amount = Mathf.Max(0, amount);

            if (_gold == amount)
                return;

            _gold = amount;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            SetGold(_gold + amount);
        }

        public void RequestAddGold(int amount)
        {
            if (amount <= 0)
                return;

            if (HasStateAuthority == true)
            {
                AddGold(amount);
            }
            else
            {
                RPC_RequestAddGold(amount);
            }
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0)
                return true;

            if (_gold < amount)
                return false;

            SetGold(_gold - amount);
            return true;
        }

        internal PlayerCharacterInventorySaveData CreateSaveData()
        {
            var data = new PlayerCharacterInventorySaveData
            {
                InventorySlots    = new PlayerInventoryItemData[_items.Length],
                HotbarSlots       = new PlayerHotbarSlotData[_hotbar.Length],
                CurrentWeaponSlot = _currentWeaponSlot,
                Gold              = _gold,
                PickaxeSlot       = CreateInventoryItemData(_pickaxeSlot),
                WoodAxeSlot       = CreateInventoryItemData(_woodAxeSlot),
                FishingPoleSlot   = CreateInventoryItemData(_fishingPoleSlot),
                HeadSlot          = CreateInventoryItemData(_headSlot),
                UpperBodySlot     = CreateInventoryItemData(_upperBodySlot),
                LowerBodySlot     = CreateInventoryItemData(_lowerBodySlot)
            };

            for (int i = 0; i < _items.Length; i++)
            {
                data.InventorySlots[i] = CreateInventoryItemData(_items[i]);
            }

            for (int i = 0; i < _hotbar.Length; i++)
            {
                data.HotbarSlots[i] = CreateHotbarSlotData(_hotbar[i]);
            }

            return data;
        }

        private static PlayerInventoryItemData CreateInventoryItemData(InventorySlot slot)
        {
            if (slot.IsEmpty == true)
            {
                return default;
            }

            return new PlayerInventoryItemData
            {
                ItemDefinitionId = slot.ItemDefinitionId,
                Quantity         = slot.Quantity,
                ConfigurationHash = NormalizeConfigurationHash(slot.ConfigurationHash)
            };
        }

        private static PlayerHotbarSlotData CreateHotbarSlotData(Weapon weapon)
        {
            if (weapon == null)
            {
                return default;
            }

            return new PlayerHotbarSlotData
            {
                WeaponDefinitionId = weapon.Definition != null ? weapon.Definition.ID : 0,
                ConfigurationHash  = NormalizeConfigurationHash(weapon.ConfigurationHash)
            };
        }

        private static InventorySlot CreateInventorySlot(in PlayerInventoryItemData data)
        {
            if (data.ItemDefinitionId == 0 || data.Quantity == 0)
            {
                return default;
            }

            return new InventorySlot(data.ItemDefinitionId, data.Quantity, ConvertHashToNetworkString(data.ConfigurationHash));
        }

        private static string NormalizeConfigurationHash(NetworkString<_32> hash)
        {
            string rawHash = hash.ToString();
            return string.IsNullOrEmpty(rawHash) == false ? rawHash : null;
        }

        private static NetworkString<_32> ConvertHashToNetworkString(string hash)
        {
            return string.IsNullOrEmpty(hash) == false ? hash : default;
        }

        private void ApplySpecialSlotData(PlayerInventoryItemData slotData, Func<InventorySlot> getSlot, Action<InventorySlot> setSlot, Action refreshAction)
        {
            var currentSlot = getSlot != null ? getSlot() : default;
            var newSlot = CreateInventorySlot(slotData);

            if (currentSlot.Equals(newSlot) == true)
            {
                return;
            }

            setSlot?.Invoke(newSlot);
            refreshAction?.Invoke();
        }

        internal void ApplySaveData(PlayerCharacterInventorySaveData data)
        {
            if (data == null)
                return;

            if (HasStateAuthority == false)
                return;

            DisarmCurrentWeapon();

            for (int i = 0; i < _hotbar.Length; i++)
            {
                RemoveWeapon(i);
            }

            _pickaxeSlot = default;
            RefreshPickaxeSlot();
            _woodAxeSlot = default;
            RefreshWoodAxeSlot();
            _fishingPoleSlot = default;
            RefreshFishingPoleSlot();
            _headSlot = default;
            RefreshHeadSlot();
            _upperBodySlot = default;
            RefreshUpperBodySlot();
            _lowerBodySlot = default;
            RefreshLowerBodySlot();

            for (int i = 0; i < _items.Length; i++)
            {
                SetInventorySlot(i, default);
            }

            if (data.InventorySlots != null)
            {
                int count = Mathf.Min(data.InventorySlots.Length, _items.Length);
                for (int i = 0; i < count; i++)
                {
                    var slot = CreateInventorySlot(data.InventorySlots[i]);
                    if (slot.IsEmpty == true)
                        continue;

                    SetInventorySlot(i, slot);
                }
            }

            ApplySpecialSlotData(data.PickaxeSlot, () => _pickaxeSlot, slot => _pickaxeSlot = slot, RefreshPickaxeSlot);
            ApplySpecialSlotData(data.WoodAxeSlot, () => _woodAxeSlot, slot => _woodAxeSlot = slot, RefreshWoodAxeSlot);
            ApplySpecialSlotData(data.FishingPoleSlot, () => _fishingPoleSlot, slot => _fishingPoleSlot = slot, RefreshFishingPoleSlot);
            ApplySpecialSlotData(data.HeadSlot, () => _headSlot, slot => _headSlot = slot, RefreshHeadSlot);
            ApplySpecialSlotData(data.UpperBodySlot, () => _upperBodySlot, slot => _upperBodySlot = slot, RefreshUpperBodySlot);
            ApplySpecialSlotData(data.LowerBodySlot, () => _lowerBodySlot, slot => _lowerBodySlot = slot, RefreshLowerBodySlot);

            if (data.HotbarSlots != null)
            {
                int count = Mathf.Min(data.HotbarSlots.Length, _hotbar.Length);
                for (int i = 0; i < count; i++)
                {
                    if (i == 0)
                        continue;

                    if(data.HotbarSlots[i].WeaponDefinitionId == 0)
                        continue;
                    
                    var slotData = data.HotbarSlots[i];

                    var itemDefinition = ItemDefinition.Get(slotData.WeaponDefinitionId) as WeaponDefinition;
                    if (itemDefinition == null)
                        continue;

                    var weaponPrefab = EnsureWeaponPrefabRegistered(itemDefinition);
                    if (weaponPrefab == null)
                        continue;

                    var weapon = Runner.Spawn(weaponPrefab, inputAuthority: Object.InputAuthority);
                    if (weapon == null)
                        continue;

                    if (string.IsNullOrEmpty(slotData.ConfigurationHash) == false)
                    {
                        NetworkString<_32> configurationHash = slotData.ConfigurationHash;
                        weapon.SetConfigurationHash(configurationHash);
                    }

                    AddWeapon(weapon, i);
                }
            }

            _previousWeaponSlot = 0;
            byte targetSlot = data.CurrentWeaponSlot;
            if (targetSlot >= _hotbar.Length)
            {
                targetSlot = 0;
            }

            SetCurrentWeapon(targetSlot);
            RefreshItems();
            EnsureToolAvailability();
            ArmCurrentWeapon();

            SetGold(data.Gold);
        }

        public InventorySlot GetItemSlot(int index)
        {
            if (index == PICKAXE_SLOT_INDEX)
                return _pickaxeSlot;

            if (index == WOOD_AXE_SLOT_INDEX)
                return _woodAxeSlot;

            if (index == FISHING_POLE_SLOT_INDEX)
                return _fishingPoleSlot;

            if (index == HEAD_SLOT_INDEX)
                return _headSlot;

            if (index == UPPER_BODY_SLOT_INDEX)
                return _upperBodySlot;

            if (index == LOWER_BODY_SLOT_INDEX)
                return _lowerBodySlot;

            if (index < 0 || index >= _items.Length)
                return default;

            return _items[index];
        }

        public InventorySlot GetEquipmentSlot(ESlotCategory category)
        {
            return category switch
            {
                ESlotCategory.Head => _headSlot,
                ESlotCategory.UpperBody => _upperBodySlot,
                ESlotCategory.LowerBody => _lowerBodySlot,
                ESlotCategory.Pickaxe => _pickaxeSlot,
                ESlotCategory.WoodAxe => _woodAxeSlot,
                ESlotCategory.FishingPole => _fishingPoleSlot,
                _ => default,
            };
        }

        public byte AddItem(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash = default)
        {
            if (definition == null || quantity == 0)
                return quantity;

            if (HasStateAuthority == false)
                return quantity;

            return AddItemInternal(definition, quantity, configurationHash);
        }

        public void RequestAddItem(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash = default)
        {
            if (definition == null || quantity == 0)
                return;

            if (HasStateAuthority == true)
            {
                AddItemInternal(definition, quantity, configurationHash);
            }
            else
            {
                RPC_RequestAddItem(definition.ID, quantity, configurationHash);
            }
        }

        public bool TryExtractInventoryItem(int inventoryIndex, byte quantity, out InventorySlot removedSlot)
        {
            removedSlot = default;

            if (HasStateAuthority == false)
                return false;

            if (quantity == 0)
                return false;

            if (IsGeneralInventoryIndex(inventoryIndex) == false)
                return false;

            var slot = _items[inventoryIndex];
            if (slot.IsEmpty == true)
                return false;

            if (slot.Quantity < quantity)
                return false;

            if (IsPickaxeSlotItem(slot) == true || IsWoodAxeSlotItem(slot) == true ||
                IsHeadSlotItem(slot) == true || IsUpperBodySlotItem(slot) == true ||
                IsLowerBodySlotItem(slot) == true)
                return false;

            removedSlot = new InventorySlot(slot.ItemDefinitionId, quantity, slot.ConfigurationHash);

            if (slot.Quantity == quantity)
            {
                SetInventorySlot(inventoryIndex, default);
            }
            else
            {
                slot.Remove(quantity);
                SetInventorySlot(inventoryIndex, slot);
            }

            RefreshItems();

            return true;
        }

        public void RequestMoveItem(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex)
                return;

            if (IsValidInventoryIndex(fromIndex) == false)
                return;

            if (IsValidInventoryIndex(toIndex) == false)
                return;

            if (HasStateAuthority == true)
            {
                MoveItem((byte)fromIndex, (byte)toIndex);
            }
            else
            {
                RPC_RequestMoveItem((byte)fromIndex, (byte)toIndex);
            }
        }

        public void RequestAssignHotbar(int inventoryIndex, int hotbarIndex)
        {
            if (IsGeneralInventoryIndex(inventoryIndex) == false)
                return;

            if (HasStateAuthority == true)
            {
                AssignHotbar(inventoryIndex, hotbarIndex);
            }
            else
            {
                RPC_RequestAssignHotbar((byte)inventoryIndex, (byte)hotbarIndex);
            }
        }

        public void RequestStoreHotbar(int hotbarIndex, int inventoryIndex)
        {
            if (IsGeneralInventoryIndex(inventoryIndex) == false)
                return;

            if (HasStateAuthority == true)
            {
                StoreHotbar(hotbarIndex, inventoryIndex);
            }
            else
            {
                RPC_RequestStoreHotbar((byte)hotbarIndex, (byte)inventoryIndex);
            }
        }

        public void RequestSwapHotbar(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex)
                return;

            if (HasStateAuthority == true)
            {
                SwapHotbar(fromIndex, toIndex);
            }
            else
            {
                RPC_RequestSwapHotbar((byte)fromIndex, (byte)toIndex);
            }
        }

        public void RequestDropInventoryItem(int inventoryIndex)
        {
            if (IsValidInventoryIndex(inventoryIndex) == false)
                return;

            if (HasStateAuthority == true)
            {
                DropInventoryItem(inventoryIndex);
            }
            else
            {
                RPC_RequestDropInventoryItem((byte)inventoryIndex);
            }
        }

        public void RequestDropHotbar(int hotbarIndex)
        {
            int weaponSlot = hotbarIndex + 1;
            if (weaponSlot <= 0 || weaponSlot >= _hotbar.Length)
                return;

            if (HasStateAuthority == true)
            {
                DropWeapon(weaponSlot);
            }
            else
            {
                RPC_RequestDropHotbar((byte)weaponSlot);
            }
        }

        public void DisarmCurrentWeapon()
        {
            if (_currentWeaponSlot == 0)
                return;

            if (_currentWeaponSlot > 0)
            {
                _previousWeaponSlot = _currentWeaponSlot;
            }

            _currentWeaponSlot = 0;

            ArmCurrentWeapon();
        }

        public void ToggleFishingPole()
        {
            if (HasStateAuthority == true)
            {
                ToggleFishingPoleInternal();
            }
            else
            {
                RPC_RequestToggleFishingPole();
            }
        }

        public void SubmitHookSetMinigameResult(bool wasSuccessful)
        {
            if (_localFishingPole == null)
                return;

            if (HasStateAuthority == true)
            {
                HandleHookSetMinigameResultInternal(wasSuccessful);
            }
            else
            {
                RPC_SubmitHookSetMinigameResult(wasSuccessful);
            }
        }

        public void SubmitFightingMinigameResult(bool wasSuccessful)
        {
            if (_localFishingPole == null)
                return;

            if (HasStateAuthority == true)
            {
                HandleFightingMinigameResultInternal(wasSuccessful);
            }
            else
            {
                RPC_SubmitFightingMinigameResult(wasSuccessful);
            }
        }

        public void SubmitFightingMinigameProgress(int successHits, int requiredHits)
        {
            if (_localFishingPole == null)
                return;

            successHits = Mathf.Max(0, successHits);
            requiredHits = Mathf.Max(1, requiredHits);

            if (HasStateAuthority == true)
            {
                HandleFightingMinigameProgressInternal(successHits, requiredHits);
            }
            else
            {
                HandleFightingMinigameProgressInternal(successHits, requiredHits);
                RPC_SubmitFightingMinigameProgress((byte)Mathf.Clamp(successHits, 0, byte.MaxValue), (byte)Mathf.Clamp(requiredHits, 1, byte.MaxValue));
            }
        }

        public void UpdateHookSetSuccessZoneState(bool isInSuccessZone)
        {
            if (_isHookSetSuccessZoneActive == isInSuccessZone)
            {
                return;
            }

            _isHookSetSuccessZoneActive = isInSuccessZone;

            var fishingPole = _localFishingPole ?? _fishingPole;

            if (fishingPole == null)
            {
                return;
            }

            fishingPole.SetHookSetSuccessZoneState(isInSuccessZone);

            if (HasStateAuthority == true)
            {
                return;
            }

            RPC_SetHookSetSuccessZoneState(isInSuccessZone);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetHookSetSuccessZoneState(bool isInSuccessZone)
        {
            var fishingPole = _fishingPole ?? _localFishingPole;

            if (fishingPole == null)
            {
                return;
            }

            if (_isHookSetSuccessZoneActive == isInSuccessZone)
            {
                return;
            }

            _isHookSetSuccessZoneActive = isInSuccessZone;
            fishingPole.SetHookSetSuccessZoneState(isInSuccessZone);
        }

        public void SetCurrentWeapon(int slot)
        {
            slot = Mathf.Clamp(slot, 0, _hotbar.Length - 1);

            if (_currentWeaponSlot == slot)
                return;

            bool wasFishingPole = _currentWeaponSlot == HOTBAR_FISHING_POLE_SLOT;

            if (_currentWeaponSlot > 0)
            {
                _previousWeaponSlot = _currentWeaponSlot;
            }

            _currentWeaponSlot = (byte)slot;

            if (HasStateAuthority == true)
            {
                if (_currentWeaponSlot == HOTBAR_FISHING_POLE_SLOT)
                {
                    if (_weaponSlotBeforeFishingPole == byte.MaxValue)
                    {
                        _weaponSlotBeforeFishingPole = _previousWeaponSlot;
                    }

                    _isFishingPoleEquipped = true;
                }
                else
                {
                    if (wasFishingPole == true)
                    {
                        _weaponSlotBeforeFishingPole = byte.MaxValue;
                    }

                    _isFishingPoleEquipped = false;
                }
            }
        }

        public void ArmCurrentWeapon()
        {
            RefreshWeapons();

            if (_currentWeaponSlot < _slots.Length)
            {
                var currentSlot = _slots[_currentWeaponSlot];
                if (currentSlot != null)
                {
                    CurrentWeaponHandle = currentSlot.Active;
                    CurrentWeaponBaseRotation = currentSlot.BaseRotation;
                }
            }

            if (CurrentWeapon != null && CurrentWeapon.IsArmed == false)
            {
                CurrentWeapon.ArmWeapon();
            }
        }

        public bool BeginPickaxeUse()
        {
            if (_pickaxeSlot.IsEmpty == true)
                return false;

            if (_weaponSlotBeforePickaxe == byte.MaxValue)
            {
                _weaponSlotBeforePickaxe = _currentWeaponSlot;
            }

            if (HasStateAuthority == true)
            {
                EnsureToolAvailability();
                EnsurePickaxeInstance();

                if (_pickaxe == null)
                {
                    RefreshPickaxeVisuals();
                    return false;
                }

                if (_currentWeaponSlot != 0)
                {
                    SetCurrentWeapon(0);
                    ArmCurrentWeapon();
                }

                _isPickaxeEquipped = true;
            }

            RefreshPickaxeVisuals();
            return _pickaxe != null;
        }

        public void EndPickaxeUse()
        {
            if (HasStateAuthority == true)
            {
                if (_isPickaxeEquipped == true)
                {
                    _isPickaxeEquipped = false;
                }

                if (_weaponSlotBeforePickaxe != byte.MaxValue)
                {
                    byte targetSlot = _weaponSlotBeforePickaxe;
                    _weaponSlotBeforePickaxe = byte.MaxValue;

                    SetCurrentWeapon(targetSlot);
                    ArmCurrentWeapon();
                }
            }

            _weaponSlotBeforePickaxe = byte.MaxValue;

            RefreshPickaxeVisuals();
        }

        public bool BeginWoodAxeUse()
        {
            if (_woodAxeSlot.IsEmpty == true)
                return false;

            if (_weaponSlotBeforeWoodAxe == byte.MaxValue)
            {
                _weaponSlotBeforeWoodAxe = _currentWeaponSlot;
            }

            if (HasStateAuthority == true)
            {
                EnsureToolAvailability();
                EnsureWoodAxeInstance();

                if (_woodAxe == null)
                {
                    RefreshWoodAxeVisuals();
                    return false;
                }

                if (_currentWeaponSlot != 0)
                {
                    SetCurrentWeapon(0);
                    ArmCurrentWeapon();
                }

                _isWoodAxeEquipped = true;
            }

            RefreshWoodAxeVisuals();
            return _woodAxe != null;
        }

        public void EndWoodAxeUse()
        {
            if (HasStateAuthority == true)
            {
                if (_isWoodAxeEquipped == true)
                {
                    _isWoodAxeEquipped = false;
                }

                if (_weaponSlotBeforeWoodAxe != byte.MaxValue)
                {
                    byte targetSlot = _weaponSlotBeforeWoodAxe;
                    _weaponSlotBeforeWoodAxe = byte.MaxValue;

                    SetCurrentWeapon(targetSlot);
                    ArmCurrentWeapon();
                }
            }

            _weaponSlotBeforeWoodAxe = byte.MaxValue;

            RefreshWoodAxeVisuals();
        }

        private void ToggleFishingPoleInternal()
        {
            bool fishingPoleInHotbar = _hotbar.Length > HOTBAR_FISHING_POLE_SLOT && _hotbar[HOTBAR_FISHING_POLE_SLOT] != null && _hotbar[HOTBAR_FISHING_POLE_SLOT] == _fishingPole;

            if (_isFishingPoleEquipped == true || (fishingPoleInHotbar == true && _currentWeaponSlot == HOTBAR_FISHING_POLE_SLOT))
            {
                UnequipFishingPoleInternal();
            }
            else
            {
                EquipFishingPoleInternal();
            }
        }

        private void EquipFishingPoleInternal()
        {
            EnsureToolAvailability();

            if (_fishingPoleSlot.IsEmpty == true || IsFishingPoleSlotItem(_fishingPoleSlot) == false)
                return;

            EnsureFishingPoleInstance();

            var fishingPole = _fishingPole;
            if (fishingPole == null)
                return;

            if (_weaponSlotBeforeFishingPole == byte.MaxValue)
            {
                _weaponSlotBeforeFishingPole = _currentWeaponSlot;
            }

            if (_hotbar.Length > HOTBAR_FISHING_POLE_SLOT && _hotbar[HOTBAR_FISHING_POLE_SLOT] != fishingPole)
            {
                AddWeapon(fishingPole, HOTBAR_FISHING_POLE_SLOT);
            }

            _isFishingPoleEquipped = true;

            SetCurrentWeapon(HOTBAR_FISHING_POLE_SLOT);
            ArmCurrentWeapon();

            RefreshFishingPoleVisuals();
        }

        private void UnequipFishingPoleInternal()
        {
            if (_isFishingPoleEquipped == true)
            {
                _isFishingPoleEquipped = false;
            }

            byte previousSlot = _weaponSlotBeforeFishingPole;
            _weaponSlotBeforeFishingPole = byte.MaxValue;

            if (previousSlot != byte.MaxValue)
            {
                SetCurrentWeapon(previousSlot);
            }
            else
            {
                SetCurrentWeapon(0);
            }

            ArmCurrentWeapon();
            RefreshFishingPoleVisuals();
        }

        public int GetPickaxeSpeed()
        {
            return GetToolSpeedValue(_pickaxe, _localPickaxe, _pickaxeSlot);
        }

        public int GetWoodAxeSpeed()
        {
            return GetToolSpeedValue(_woodAxe, _localWoodAxe, _woodAxeSlot);
        }

        public void DropCurrentWeapon()
        {
            DropWeapon(_currentWeaponSlot);
        }

        public void Pickup(InventoryItemPickupProvider provider)
        {
            if (HasStateAuthority == false || provider == null)
                return;

            var definition = provider.Definition;
            if (definition == null)
                return;

            byte quantity = provider.Quantity;
            if (quantity == 0)
                return;

            byte remainder = AddItemInternal(definition, quantity, provider.ConfigurationHash);

            if (remainder == quantity)
                return;

            provider.SetQuantity(remainder);

            if (remainder == 0)
            {
                Runner.Despawn(provider.Object);
            }
        }

        public void Pickup(WeaponPickup weaponPickup)
        {
            if (HasStateAuthority == false)
                return;

            if (weaponPickup.Consumed == true || weaponPickup.IsDisabled == true)
                return;
            
                weaponPickup.TryConsume(gameObject, out string weaponPickupResult2);

                EnsureWeaponPrefabRegistered(
                    weaponPickup.WeaponPrefab != null ? weaponPickup.WeaponPrefab.Definition : null,
                    weaponPickup.WeaponPrefab);

                if (TryAddWeaponDefinitionToInventory(weaponPickup.WeaponPrefab != null
                        ? weaponPickup.WeaponPrefab.Definition
                        : null) == true)
                    return;

                var weapon = Runner.Spawn(weaponPickup.WeaponPrefab, inputAuthority: Object.InputAuthority);
                PickupWeapon(weapon);
         
        }

        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            bool restoredFromCloud = Global.PlayerCloudSaveService != null &&
                                      Global.PlayerCloudSaveService.RegisterInventoryAndRestore(this);

            if (HasStateAuthority == false)
            {
                RefreshWeapons();
                RefreshItems();
                RefreshPickaxeVisuals();
                RefreshWoodAxeVisuals();
                HandleGoldChanged(_gold);
                return;
            }

            int initialHits = Mathf.Clamp(Mathf.Max(1, _fightingSuccessHitRange.x), 1, byte.MaxValue);
            _fightingHitsRequired = (byte)initialHits;
            _fightingHitsSucceeded = 0;

            if (restoredFromCloud == true)
            {
                RefreshWeapons();
                RefreshItems();
                RefreshPickaxeVisuals();
                RefreshWoodAxeVisuals();
                HandleGoldChanged(_gold);
                return;
            }

            _currentWeaponSlot = 0;
            _previousWeaponSlot = 0;

            byte bestWeaponSlot = 0;
            int bestPriority = -1;

            // Spawn initial weapons
            for (byte i = 0; i < _initialWeapons.Length; i++)
            {
                var weaponPrefab = _initialWeapons[i];
                if (weaponPrefab == null)
                    continue;

                EnsureWeaponPrefabRegistered(weaponPrefab.Definition, weaponPrefab);

                var weapon = Runner.Spawn(weaponPrefab, inputAuthority: Object.InputAuthority);
                AddWeapon(weapon);

                int weaponSlot = ClampToValidSlot(GetSlotIndex(weapon.Size));
                int priority = GetWeaponPriority(weapon.Size);
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestWeaponSlot = (byte)weaponSlot;
                }
            }


            SetCurrentWeapon(bestWeaponSlot);
            ArmCurrentWeapon();
            RefreshWeapons();
            RefreshItems();
            EnsureToolAvailability();
            RefreshPickaxeVisuals();
            RefreshWoodAxeVisuals();
            RefreshFishingPoleVisuals();
            HandleGoldChanged(_gold);
        }

        public void OnDespawned()
        {
            Global.PlayerCloudSaveService?.UnregisterInventory(this);

            ResetHotbarStatBonuses();

            DespawnPickaxe();
            DespawnWoodAxe();

            GoldChanged = null;
            _localGold = 0;
            _changeDetector = null;

            // Cleanup weapons
            for (int i = 0; i < _hotbar.Length; i++)
            {
                Weapon weapon = _hotbar[i];
                if (weapon != null)
                {
                    weapon.Deinitialize(Object);
                    Runner.Despawn(weapon.Object);
                    _hotbar.Set(i, null);
                    _localWeapons[i] = null;
                }
            }

            for (int i = 0; i < _localWeapons.Length; i++)
            {
                Weapon weapon = _localWeapons[i];
                if (weapon != null)
                {
                    weapon.Deinitialize(Object);
                    _localWeapons[i] = null;
                }
            }

            _currentWeaponSlot = 0;
            _previousWeaponSlot = 0;

            CurrentWeapon = default;
            CurrentWeaponHandle = default;
            CurrentWeaponBaseRotation = default;

            if (_localItems != null)
            {
                Array.Clear(_localItems, 0, _localItems.Length);
            }

            if (_lastHotbarWeapons != null)
            {
                Array.Clear(_lastHotbarWeapons, 0, _lastHotbarWeapons.Length);
            }

            _weaponDefinitionsBySlot.Clear();

            _pickaxeSlot = default;
            _localPickaxeSlot = default;
            _woodAxeSlot = default;
            _localWoodAxeSlot = default;
            _headSlot = default;
            _localHeadSlot = default;
            _upperBodySlot = default;
            _localUpperBodySlot = default;
            _lowerBodySlot = default;
            _localLowerBodySlot = default;
            _localWoodAxe = null;

            ItemSlotChanged = null;
            HotbarSlotChanged = null;
        }

        public void OnFixedUpdate()
        {
            if (HasStateAuthority == false)
                return;

            if (_health.IsAlive == false)
            {
                DropAllWeapons();
                return;
            }

            // Autoswitch to valid weapon if current is invalid
            if (CurrentWeapon != null && CurrentWeapon.ValidOnlyWithAmmo == true && CurrentWeapon.HasAmmo() == false)
            {
                byte bestWeaponSlot = _previousWeaponSlot;
                if (bestWeaponSlot == 0 || bestWeaponSlot == _currentWeaponSlot)
                {
                    bestWeaponSlot = FindBestWeaponSlot(_currentWeaponSlot);
                }

                DisarmCurrentWeapon();
                SetCurrentWeapon(bestWeaponSlot);
            }
        }

        public override void Render()
        {
            RefreshWeapons();
            RefreshItems();
            RefreshPickaxeVisuals();
            RefreshWoodAxeVisuals();
            RefreshFishingPoleVisuals();

            if (_changeDetector != null)
            {
                foreach (var changedProperty in _changeDetector.DetectChanges(this))
                {
                    if (changedProperty == nameof(_gold))
                    {
                        HandleGoldChanged(_gold);
                    }
                }
            }
        }

        public bool CanFireWeapon(bool keyDown)
        {
            return CurrentWeapon != null && CurrentWeapon.CanFire(keyDown) == true;
        }

        public bool CanReloadWeapon(bool autoReload)
        {
            return CurrentWeapon != null && CurrentWeapon.CanReload(autoReload) == true;
        }

        public bool CanAim()
        {
            return CurrentWeapon != null && CurrentWeapon.CanAim() == true;
        }

        public bool HasWeapon(int slot, bool checkAmmo = false)
        {
            if (slot < 0 || slot >= _hotbar.Length)
                return false;

            var weapon = _hotbar[slot];
            return weapon != null && (checkAmmo == false || (weapon.Object != null && weapon.HasAmmo() == true));
        }

        public Weapon GetWeapon(int slot)
        {
            if (slot < 0 || slot >= _hotbar.Length)
            {
                return null;
            }

            return _hotbar[slot];
        }

        public Weapon GetWeapon(WeaponSize size)
        {
            for (int i = 0; i < _hotbar.Length; i++)
            {
                var weapon = _hotbar[i];
                if (weapon != null && weapon.Size == size)
                {
                    return weapon;
                }
            }

            return null;
        }

        public int GetSlotForSize(WeaponSize size)
        {
            return ClampToValidSlot(GetSlotIndex(size));
        }

        public int GetNextWeaponSlot(int fromSlot, int minSlot = 0, bool checkAmmo = true)
        {
            int weaponCount = _hotbar.Length;

            for (int i = 0; i < weaponCount; i++)
            {
                int slot = (i + fromSlot + 1) % weaponCount;

                if (slot < minSlot)
                    continue;

                if (IsWeaponHotbarSlot(slot) == false)
                    continue;

                var weapon = _hotbar[slot];

                if (weapon == null)
                    continue;

                if (checkAmmo == true && weapon.HasAmmo() == false)
                    continue;

                return slot;
            }

            return 0;
        }

        public bool Fire()
        {
            if (CurrentWeapon == null)
                return false;

            Vector3 targetPoint = _interactions.GetTargetPoint(false, true);
            TransformData fireTransform = _character.GetFireTransform(true);

            CurrentWeapon.Fire(fireTransform.Position, targetPoint, _hitMask);

            return true;
        }

        public bool Reload()
        {
            if (CurrentWeapon == null)
                return false;

            CurrentWeapon.Reload();
            return true;
        }

        public bool AddAmmo(WeaponSize weaponSize, int amount, out string result)
        {
            Weapon weapon = null;

            for (int i = 0; i < _hotbar.Length; i++)
            {
                if (IsWeaponHotbarSlot(i) == false)
                    continue;

                var candidate = _hotbar[i];
                if (candidate != null && candidate.Size == weaponSize)
                {
                    weapon = candidate;
                    break;
                }
            }

            if (weapon == null)
            {
                result = "No weapon with this type of ammo";
                return false;
            }

            bool ammoAdded = weapon.AddAmmo(amount);
            result = ammoAdded == true ? string.Empty : "Cannot add more ammo";

            return ammoAdded;
        }

        // IBeforeTick INTERFACE

        void IBeforeTick.BeforeTick()
        {
            RefreshWeapons();
            RefreshItems();

            if (HasStateAuthority == true)
            {
                EnsureToolAvailability();
            }
        }

        // MONOBEHAVIOUR

        private void Awake()
        {
            _health = GetComponent<Health>();
            _character = GetComponent<Character>();
            _interactions = GetComponent<Interactions>();
            _stats = GetComponent<Stats>();
            _fireAudioEffects = _fireAudioEffectsRoot.GetComponentsInChildren<AudioEffect>();
            _localItems = new InventorySlot[INVENTORY_SIZE];
            _lastHotbarWeapons = new Weapon[_localWeapons.Length];
            _weaponDefinitionsBySlot.Clear();
            _weaponSizeToSlotIndex.Clear();

            if (_weaponSizeSlots != null)
            {
                foreach (var sizeSlot in _weaponSizeSlots)
                {
                    if (sizeSlot.SlotIndex < 0)
                    {
                        continue;
                    }

                    _weaponSizeToSlotIndex[sizeSlot.Size] = sizeSlot.SlotIndex;
                }
            }

            foreach (WeaponSlot slot in _slots)
            {
                if (slot.Active != null)
                {
                    slot.BaseRotation = slot.Active.localRotation;
                }
            }
        }

        // PRIVATE METHODS

        private int GetSlotIndex(WeaponSize size)
        {
            if (_slots == null || _slots.Length == 0)
            {
                return 0;
            }

            if (_weaponSizeToSlotIndex.TryGetValue(size, out int slotIndex) == false)
            {
                slotIndex = GetDefaultSlotIndex(size);
            }

            if (slotIndex < 0)
            {
                slotIndex = 0;
            }

            if (slotIndex >= _slots.Length)
            {
                slotIndex = Mathf.Clamp(_slots.Length - 1, 0, int.MaxValue);
            }

            return slotIndex;
        }

        private int ClampToValidSlot(int slotIndex)
        {
            int slotsLength = _slots != null ? _slots.Length : 0;
            int maxSlot = Mathf.Min(slotsLength, _hotbar.Length) - 1;

            if (maxSlot < 0)
            {
                return 0;
            }

            return Mathf.Clamp(slotIndex, 0, maxSlot);
        }

        private WeaponSlot ResolveWeaponSlotForIndex(int slotIndex)
        {
            if (_slots == null || _slots.Length == 0)
                return null;

            if (slotIndex >= 0 && slotIndex < _slots.Length)
                return _slots[slotIndex];

            int fallbackIndex = Mathf.Clamp(_slots.Length - 1, 0, int.MaxValue);
            if (fallbackIndex < 0 || fallbackIndex >= _slots.Length)
                return null;

            return _slots[fallbackIndex];
        }

        private static bool IsWeaponHotbarSlot(int slot)
        {
            return TryGetHotbarSlotCategory(slot, out var category) && category == ESlotCategory.Weapon;
        }

        private static bool IsConsumableHotbarSlot(int slot)
        {
            return TryGetHotbarSlotCategory(slot, out var category) && category == ESlotCategory.Consumable;
        }

        private static bool IsFishingHotbarSlot(int slot)
        {
            return TryGetHotbarSlotCategory(slot, out var category) && category == ESlotCategory.Fishing;
        }

        private static bool IsValidHotbarAssignmentSlot(int slot)
        {
            return TryGetHotbarSlotCategory(slot, out _);
        }

        private static bool CanAssignDefinitionToHotbarSlot(ItemDefinition definition, int slot)
        {
            if (definition == null)
                return false;

            if (TryGetHotbarSlotCategory(slot, out var requiredCategory) == false)
                return false;

            if (requiredCategory == ESlotCategory.Fishing)
            {
                return definition.SlotCategory == ESlotCategory.Fishing ||
                       definition.SlotCategory == ESlotCategory.FishingPole;
            }

            return definition.SlotCategory == requiredCategory;
        }

        private static bool TryGetHotbarSlotCategory(int slot, out ESlotCategory category)
        {
            switch (slot)
            {
                case HOTBAR_PRIMARY_WEAPON_SLOT:
                case HOTBAR_SECONDARY_WEAPON_SLOT:
                    category = ESlotCategory.Weapon;
                    return true;

                case int consumableSlot when consumableSlot >= HOTBAR_FIRST_CONSUMABLE_SLOT && consumableSlot <= HOTBAR_THIRD_CONSUMABLE_SLOT:
                    category = ESlotCategory.Consumable;
                    return true;

                case HOTBAR_FISHING_POLE_SLOT:
                    category = ESlotCategory.Fishing;
                    return true;

                default:
                    category = default;
                    return false;
            }
        }

        private static bool CanAssignWeaponToHotbarSlot(Weapon weapon, int slot)
        {
            if (weapon == null)
                return true;

            var definition = weapon.Definition as ItemDefinition;
            return CanAssignDefinitionToHotbarSlot(definition, slot);
        }

        private int FindWeaponSlotBySize(WeaponSize size)
        {
            for (int i = 0; i < _hotbar.Length; i++)
            {
                if (IsWeaponHotbarSlot(i) == false)
                    continue;

                var weapon = _hotbar[i];
                if (weapon != null && weapon.Size == size)
                {
                    return i;
                }
            }

            return -1;
        }

        private Weapon FindWeaponById(int weaponId, out int slotIndex)
        {
            slotIndex = -1;

            for (int i = 0; i < _hotbar.Length; i++)
            {
                if (IsWeaponHotbarSlot(i) == false)
                    continue;

                var weapon = _hotbar[i];
                if (weapon != null && weapon.WeaponID == weaponId)
                {
                    slotIndex = i;
                    return weapon;
                }
            }

            return null;
        }

        private static int GetDefaultSlotIndex(WeaponSize size)
        {
            return size switch
            {
                WeaponSize.Unarmed => HOTBAR_UNARMED_SLOT,
                WeaponSize.Staff => HOTBAR_PRIMARY_WEAPON_SLOT,
                WeaponSize.Throwable => HOTBAR_THIRD_CONSUMABLE_SLOT,
                _ => HOTBAR_UNARMED_SLOT,
            };
        }


        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestAddGold(int amount)
        {
            if (amount <= 0)
                return;

            AddGold(amount);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestAddItem(int itemDefinitionId, byte quantity, NetworkString<_32> configurationHash)
        {
            if (quantity == 0)
                return;

            ItemDefinition definition = ItemDefinition.Get(itemDefinitionId);
            if (definition == null)
                return;

            AddItemInternal(definition, quantity, configurationHash);
        }

        private void RPC_RequestMoveItem(byte fromIndex, byte toIndex)
        {
            MoveItem(fromIndex, toIndex);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestAssignHotbar(byte inventoryIndex, byte hotbarIndex)
        {
            AssignHotbar(inventoryIndex, hotbarIndex);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestStoreHotbar(byte hotbarIndex, byte inventoryIndex)
        {
            StoreHotbar(hotbarIndex, inventoryIndex);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestSwapHotbar(byte fromIndex, byte toIndex)
        {
            SwapHotbar(fromIndex, toIndex);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestDropInventoryItem(byte inventoryIndex)
        {
            DropInventoryItem(inventoryIndex);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestDropHotbar(byte weaponSlot)
        {
            DropWeapon(weaponSlot);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestToggleFishingPole()
        {
            ToggleFishingPoleInternal();
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SubmitHookSetMinigameResult(bool wasSuccessful)
        {
            HandleHookSetMinigameResultInternal(wasSuccessful);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SubmitFightingMinigameResult(bool wasSuccessful)
        {
            HandleFightingMinigameResultInternal(wasSuccessful);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SubmitFightingMinigameProgress(byte successHits, byte requiredHits)
        {
            HandleFightingMinigameProgressInternal(successHits, Mathf.Max(1, requiredHits));
        }

        private byte AddItemInternal(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash)
        {
            if (definition == null)
            {
                return quantity;
            }

            ESlotCategory slotCategory = definition.SlotCategory;
            bool isPickaxe = slotCategory == ESlotCategory.Pickaxe;
            bool isWoodAxe = slotCategory == ESlotCategory.WoodAxe;
            bool isFishingPole = slotCategory == ESlotCategory.FishingPole;
            bool isHead = slotCategory == ESlotCategory.Head;
            bool isUpperBody = slotCategory == ESlotCategory.UpperBody;
            bool isLowerBody = slotCategory == ESlotCategory.LowerBody;

            ushort maxStack = ItemDefinition.GetMaxStack(definition.ID);
            if (maxStack == 0)
            {
                maxStack = 1;
            }

            int clampedMaxStack = Mathf.Clamp(maxStack, 1, byte.MaxValue);
            byte maxStackByte = (byte)clampedMaxStack;

            byte remaining = quantity;

            if (isPickaxe == true && remaining > 0)
            {
                remaining = AddToPickaxeSlot(definition, remaining, configurationHash);
            }

            if (isWoodAxe == true && remaining > 0)
            {
                remaining = AddToWoodAxeSlot(definition, remaining, configurationHash);
            }

            if (isFishingPole == true && remaining > 0)
            {
                remaining = AddToFishingPoleSlot(definition, remaining, configurationHash);
            }

            if (isHead == true && remaining > 0)
            {
                remaining = AddToHeadSlot(definition, remaining, configurationHash);
            }

            if (isUpperBody == true && remaining > 0)
            {
                remaining = AddToUpperBodySlot(definition, remaining, configurationHash);
            }

            if (isLowerBody == true && remaining > 0)
            {
                remaining = AddToLowerBodySlot(definition, remaining, configurationHash);
            }

            for (int i = 0; i < _items.Length && remaining > 0; i++)
            {
                var slot = _items[i];
                if (slot.IsEmpty == true)
                    continue;

                if (slot.ItemDefinitionId != definition.ID)
                    continue;

                if (slot.ConfigurationHash != configurationHash)
                    continue;

                if (slot.Quantity >= maxStackByte)
                    continue;

                byte space = (byte)Mathf.Min(maxStackByte - slot.Quantity, remaining);
                if (space == 0)
                    continue;

                slot.Add(space);
                SetInventorySlot(i, slot);
                remaining -= space;
            }

            for (int i = 0; i < _items.Length && remaining > 0; i++)
            {
                var slot = _items[i];
                if (slot.IsEmpty == false)
                    continue;

                byte addAmount = (byte)Mathf.Min(maxStackByte, remaining);
                slot = new InventorySlot(definition.ID, addAmount, configurationHash);
                SetInventorySlot(i, slot);
                remaining -= addAmount;
            }

            if (remaining != quantity)
            {
                RefreshItems();
            }

            return remaining;
        }

        private void MoveItem(byte fromIndex, byte toIndex)
        {
            if (fromIndex == toIndex)
                return;

            bool fromPickaxe = fromIndex == PICKAXE_SLOT_INDEX;
            bool toPickaxe = toIndex == PICKAXE_SLOT_INDEX;
            bool fromWoodAxe = fromIndex == WOOD_AXE_SLOT_INDEX;
            bool toWoodAxe = toIndex == WOOD_AXE_SLOT_INDEX;
            bool fromFishingPole = fromIndex == FISHING_POLE_SLOT_INDEX;
            bool toFishingPole = toIndex == FISHING_POLE_SLOT_INDEX;
            bool fromHead = fromIndex == HEAD_SLOT_INDEX;
            bool toHead = toIndex == HEAD_SLOT_INDEX;
            bool fromUpperBody = fromIndex == UPPER_BODY_SLOT_INDEX;
            bool toUpperBody = toIndex == UPPER_BODY_SLOT_INDEX;
            bool fromLowerBody = fromIndex == LOWER_BODY_SLOT_INDEX;
            bool toLowerBody = toIndex == LOWER_BODY_SLOT_INDEX;
            bool fromSpecial = fromPickaxe || fromWoodAxe || fromFishingPole || fromHead || fromUpperBody || fromLowerBody;
            bool toSpecial = toPickaxe || toWoodAxe || toFishingPole || toHead || toUpperBody || toLowerBody;
            bool generalToGeneralTransfer = fromSpecial == false && toSpecial == false;

            if (fromSpecial == false && fromIndex >= _items.Length)
                return;

            if (toSpecial == false && toIndex >= _items.Length)
                return;

            if (fromPickaxe == true)
            {
                var pickaxeSourceSlot = _pickaxeSlot;
                if (pickaxeSourceSlot.IsEmpty == true)
                    return;

                if (toPickaxe == true)
                    return;

                var targetSlot = _items[toIndex];
                if (targetSlot.IsEmpty == false && IsPickaxeSlotItem(targetSlot) == false)
                    return;

                SetInventorySlot(toIndex, pickaxeSourceSlot);

                if (targetSlot.IsEmpty == false && IsPickaxeSlotItem(targetSlot) == true)
                {
                    _pickaxeSlot = targetSlot;
                }
                else
                {
                    _pickaxeSlot = default;
                }

                RefreshPickaxeSlot();
                RefreshItems();
                EnsureToolAvailability();
                return;
            }

            if (toPickaxe == true)
            {
                var sourceSlot = _items[fromIndex];
                if (sourceSlot.IsEmpty == true)
                    return;

                if (IsPickaxeSlotItem(sourceSlot) == false)
                    return;

                var previousPickaxe = _pickaxeSlot;
                _pickaxeSlot = sourceSlot;
                RefreshPickaxeSlot();

                if (previousPickaxe.IsEmpty == true)
                {
                    SetInventorySlot(fromIndex, default);
                }
                else
                {
                    SetInventorySlot(fromIndex, previousPickaxe);
                }

                RefreshItems();
                EnsureToolAvailability();
                return;
            }

            if (fromWoodAxe == true)
            {
                var woodAxeSourceSlot = _woodAxeSlot;
                if (woodAxeSourceSlot.IsEmpty == true)
                    return;

                if (toWoodAxe == true)
                    return;

                var targetSlot = _items[toIndex];
                if (targetSlot.IsEmpty == false && IsWoodAxeSlotItem(targetSlot) == false)
                    return;

                SetInventorySlot(toIndex, woodAxeSourceSlot);

                if (targetSlot.IsEmpty == false && IsWoodAxeSlotItem(targetSlot) == true)
                {
                    _woodAxeSlot = targetSlot;
                }
                else
                {
                    _woodAxeSlot = default;
                }

                RefreshWoodAxeSlot();
                RefreshItems();
                EnsureToolAvailability();
                return;
            }

            if (fromFishingPole == true)
            {
                var fishingSourceSlot = _fishingPoleSlot;
                if (fishingSourceSlot.IsEmpty == true)
                    return;

                if (toFishingPole == true)
                    return;

                var targetSlot = _items[toIndex];
                if (targetSlot.IsEmpty == false && IsFishingPoleSlotItem(targetSlot) == false)
                    return;

                SetInventorySlot(toIndex, fishingSourceSlot);

                if (targetSlot.IsEmpty == false && IsFishingPoleSlotItem(targetSlot) == true)
                {
                    _fishingPoleSlot = targetSlot;
                }
                else
                {
                    _fishingPoleSlot = default;
                }

                RefreshFishingPoleSlot();
                RefreshItems();
                EnsureToolAvailability();
                return;
            }

            if (toWoodAxe == true)
            {
                var sourceSlot = _items[fromIndex];
                if (sourceSlot.IsEmpty == true)
                    return;

                if (IsWoodAxeSlotItem(sourceSlot) == false)
                    return;

                var previousWoodAxe = _woodAxeSlot;
                _woodAxeSlot = sourceSlot;
                RefreshWoodAxeSlot();

                if (previousWoodAxe.IsEmpty == true)
                {
                    SetInventorySlot(fromIndex, default);
                }
                else
                {
                    SetInventorySlot(fromIndex, previousWoodAxe);
                }

                RefreshItems();
                EnsureToolAvailability();
                return;
            }

            if (toFishingPole == true)
            {
                var sourceSlot = _items[fromIndex];
                if (sourceSlot.IsEmpty == true)
                    return;

                if (IsFishingPoleSlotItem(sourceSlot) == false)
                    return;

                var previousFishingPole = _fishingPoleSlot;
                _fishingPoleSlot = sourceSlot;
                RefreshFishingPoleSlot();

                if (previousFishingPole.IsEmpty == true)
                {
                    SetInventorySlot(fromIndex, default);
                }
                else
                {
                    SetInventorySlot(fromIndex, previousFishingPole);
                }

                RefreshItems();
                EnsureToolAvailability();
                return;
            }

            if (fromHead == true)
            {
                var headSourceSlot = _headSlot;
                if (headSourceSlot.IsEmpty == true)
                    return;
                if (toHead == true)
                    return;
                var targetSlot = _items[toIndex];
                if (targetSlot.IsEmpty == false && IsHeadSlotItem(targetSlot) == false)
                    return;
                SetInventorySlot(toIndex, headSourceSlot);

                if (targetSlot.IsEmpty == false && IsHeadSlotItem(targetSlot) == true)
                {
                    _headSlot = targetSlot;
                }
                else
                {
                    _headSlot = default;
                }

                RefreshHeadSlot();
                RefreshItems();
                return;
            }

            if (toHead == true)
            {
                var sourceSlot = _items[fromIndex];
                if (sourceSlot.IsEmpty == true)
                    return;
                if (IsHeadSlotItem(sourceSlot) == false)
                    return;
                var previousHead = _headSlot;
                _headSlot = sourceSlot;
                RefreshHeadSlot();

                if (previousHead.IsEmpty == true)
                {
                    SetInventorySlot(fromIndex, default);
                }
                else
                {
                    SetInventorySlot(fromIndex, previousHead);
                }

                RefreshItems();
                return;
            }

            if (fromUpperBody == true)
            {
                var upperBodySourceSlot = _upperBodySlot;
                if (upperBodySourceSlot.IsEmpty == true)
                    return;
                if (toUpperBody == true)
                    return;
                var targetSlot = _items[toIndex];
                if (targetSlot.IsEmpty == false && IsUpperBodySlotItem(targetSlot) == false)
                    return;
                SetInventorySlot(toIndex, upperBodySourceSlot);

                if (targetSlot.IsEmpty == false && IsUpperBodySlotItem(targetSlot) == true)
                {
                    _upperBodySlot = targetSlot;
                }
                else
                {
                    _upperBodySlot = default;
                }

                RefreshUpperBodySlot();
                RefreshItems();
                return;
            }

            if (toUpperBody == true)
            {
                var sourceSlot = _items[fromIndex];
                if (sourceSlot.IsEmpty == true)
                    return;
                if (IsUpperBodySlotItem(sourceSlot) == false)
                    return;
                var previousUpperBody = _upperBodySlot;
                _upperBodySlot = sourceSlot;
                RefreshUpperBodySlot();

                if (previousUpperBody.IsEmpty == true)
                {
                    SetInventorySlot(fromIndex, default);
                }
                else
                {
                    SetInventorySlot(fromIndex, previousUpperBody);
                }

                RefreshItems();
                return;
            }

            if (fromLowerBody == true)
            {
                var lowerBodySourceSlot = _lowerBodySlot;
                if (lowerBodySourceSlot.IsEmpty == true)
                    return;
                if (toLowerBody == true)
                    return;
                var targetSlot = _items[toIndex];
                if (targetSlot.IsEmpty == false && IsLowerBodySlotItem(targetSlot) == false)
                    return;
                SetInventorySlot(toIndex, lowerBodySourceSlot);

                if (targetSlot.IsEmpty == false && IsLowerBodySlotItem(targetSlot) == true)
                {
                    _lowerBodySlot = targetSlot;
                }
                else
                {
                    _lowerBodySlot = default;
                }

                RefreshLowerBodySlot();
                RefreshItems();
                return;
            }

            if (toLowerBody == true)
            {
                var sourceSlot = _items[fromIndex];
                if (sourceSlot.IsEmpty == true)
                    return;
                if (IsLowerBodySlotItem(sourceSlot) == false)
                    return;
                var previousLowerBody = _lowerBodySlot;
                _lowerBodySlot = sourceSlot;
                RefreshLowerBodySlot();

                if (previousLowerBody.IsEmpty == true)
                {
                    SetInventorySlot(fromIndex, default);
                }
                else
                {
                    SetInventorySlot(fromIndex, previousLowerBody);
                }

                RefreshItems();
                return;
            }

            var generalSourceSlot = _items[fromIndex];
            if (generalSourceSlot.IsEmpty == true)
                return;

            var toSlot = _items[toIndex];

            if (toSlot.IsEmpty == true)
            {
                if (generalToGeneralTransfer == true)
                {
                    SuppressFeedForSlot(fromIndex);
                    SuppressFeedForSlot(toIndex);
                }

                SetInventorySlot(toIndex, generalSourceSlot);
                SetInventorySlot(fromIndex, default);
                RefreshItems();
                return;
            }

            if (generalSourceSlot.ItemDefinitionId == toSlot.ItemDefinitionId &&
                generalSourceSlot.ConfigurationHash == toSlot.ConfigurationHash)
            {
                ushort maxStack = ItemDefinition.GetMaxStack(generalSourceSlot.ItemDefinitionId);
                if (maxStack == 0)
                {
                    maxStack = 1;
                }

                int clampedMaxStack = Mathf.Clamp(maxStack, 1, byte.MaxValue);
                if (toSlot.Quantity < clampedMaxStack)
                {
                    byte space = (byte)Mathf.Min(clampedMaxStack - toSlot.Quantity, generalSourceSlot.Quantity);
                    if (space > 0)
                    {
                        if (generalToGeneralTransfer == true)
                        {
                            SuppressFeedForSlot(fromIndex);
                            SuppressFeedForSlot(toIndex);
                        }

                        toSlot.Add(space);
                        generalSourceSlot.Remove(space);

                        SetInventorySlot(toIndex, toSlot);

                        if (generalSourceSlot.IsEmpty == true)
                        {
                            SetInventorySlot(fromIndex, default);
                        }
                        else
                        {
                            SetInventorySlot(fromIndex, generalSourceSlot);
                        }

                        RefreshItems();
                        return;
                    }
                }
            }

            if (generalToGeneralTransfer == true)
            {
                if (generalSourceSlot.Equals(toSlot) == true)
                    return;

                SuppressFeedForSlot(fromIndex);
                SuppressFeedForSlot(toIndex);
            }

            SetInventorySlot(toIndex, generalSourceSlot);
            SetInventorySlot(fromIndex, toSlot);
            RefreshItems();
        }

        private void AssignHotbar(int inventoryIndex, int hotbarIndex)
        {
            if (IsGeneralInventoryIndex(inventoryIndex) == false)
                return;

            int slot = hotbarIndex + 1;
            if (slot < 0 || slot >= _hotbar.Length)
                return;

            if (IsValidHotbarAssignmentSlot(slot) == false)
                return;

            var inventorySlot = _items[inventoryIndex];
            if (inventorySlot.IsEmpty == true)
                return;

            var itemDefinition = inventorySlot.GetDefinition();
            if (CanAssignDefinitionToHotbarSlot(itemDefinition, slot) == false)
                return;

            var definition = itemDefinition as WeaponDefinition;
            if (definition == null)
                return;

            var configurationHash = inventorySlot.ConfigurationHash;

            var weaponPrefab = EnsureWeaponPrefabRegistered(definition);
            if (weaponPrefab == null)
                return;

            SuppressFeedForSlot(inventoryIndex);
            if (RemoveInventoryItemInternal(inventoryIndex, 1) == false)
            {
                ClearFeedSuppression(inventoryIndex);
                return;
            }

            var existingWeapon = _hotbar[slot];
            if (existingWeapon != null)
            {
                if (TryStoreWeapon(existingWeapon, slot) == false)
                {
                    RestoreInventoryItem(inventoryIndex, definition, configurationHash);
                    return;
                }
            }

            var spawnedWeapon = Runner.Spawn(weaponPrefab, inputAuthority: Object.InputAuthority);
            if (spawnedWeapon == null)
            {
                RestoreInventoryItem(inventoryIndex, definition, configurationHash);
                return;
            }

            spawnedWeapon.SetConfigurationHash(configurationHash);
            AddWeapon(spawnedWeapon, slot);

            if (_currentWeaponSlot == slot)
            {
                ArmCurrentWeapon();
            }
        }

        private void StoreHotbar(int hotbarIndex, int inventoryIndex)
        {
            int slot = hotbarIndex + 1;
            if (slot < 0 || slot >= _hotbar.Length)
                return;

            if (IsValidHotbarAssignmentSlot(slot) == false)
                return;

            if (IsGeneralInventoryIndex(inventoryIndex) == false)
                return;

            var targetSlot = _items[inventoryIndex];
            if (targetSlot.IsEmpty == false)
                return;

            var weapon = _hotbar[slot];
            if (weapon == null)
                return;

            var definition = weapon.Definition as WeaponDefinition;
            if (definition == null)
                return;

            EnsureWeaponPrefabRegistered(definition, weapon);

            SuppressFeedForSlot(inventoryIndex);
            if (slot == _currentWeaponSlot)
            {
                byte bestWeaponSlot = _previousWeaponSlot;
                if (bestWeaponSlot == 0 || bestWeaponSlot == _currentWeaponSlot)
                {
                    bestWeaponSlot = FindBestWeaponSlot(_currentWeaponSlot);
                }

                SetCurrentWeapon(bestWeaponSlot);
                ArmCurrentWeapon();
            }
            else if (_previousWeaponSlot == slot)
            {
                _previousWeaponSlot = 0;
            }

            var inventorySlot = new InventorySlot(definition.ID, 1, weapon.ConfigurationHash);
            SetInventorySlot(inventoryIndex, inventorySlot);
            RefreshItems();

            RemoveWeapon(slot);

            if (weapon.Object != null)
            {
                Runner.Despawn(weapon.Object);
            }
        }

        private void SwapHotbar(int fromIndex, int toIndex)
        {
            int fromSlot = fromIndex + 1;
            int toSlot = toIndex + 1;

            if (fromSlot < 0 || fromSlot >= _hotbar.Length)
                return;

            if (toSlot < 0 || toSlot >= _hotbar.Length)
                return;

            if (IsValidHotbarAssignmentSlot(fromSlot) == false || IsValidHotbarAssignmentSlot(toSlot) == false)
                return;

            if (fromSlot == toSlot)
                return;

            var fromWeapon = _hotbar[fromSlot];
            var toWeapon = _hotbar[toSlot];

            if (CanAssignWeaponToHotbarSlot(fromWeapon, toSlot) == false)
                return;

            if (CanAssignWeaponToHotbarSlot(toWeapon, fromSlot) == false)
                return;

            _hotbar.Set(fromSlot, toWeapon);
            _hotbar.Set(toSlot, fromWeapon);

            if (_currentWeaponSlot == fromSlot)
            {
                _currentWeaponSlot = (byte)toSlot;
            }
            else if (_currentWeaponSlot == toSlot)
            {
                _currentWeaponSlot = (byte)fromSlot;
            }

            if (_previousWeaponSlot == fromSlot)
            {
                _previousWeaponSlot = (byte)toSlot;
            }
            else if (_previousWeaponSlot == toSlot)
            {
                _previousWeaponSlot = (byte)fromSlot;
            }

            NotifyHotbarSlotChanged(fromSlot);
            NotifyHotbarSlotChanged(toSlot);

            RefreshWeapons();
        }

        private void DropInventoryItem(int index)
        {
            if (index == PICKAXE_SLOT_INDEX)
            {
                var pickaxeSlotData = _pickaxeSlot;
                if (pickaxeSlotData.IsEmpty == true)
                    return;

                var pickaxeDefinition = pickaxeSlotData.GetDefinition();
                if (pickaxeDefinition == null)
                    return;

                byte pickaxeQuantity = pickaxeSlotData.Quantity;
                if (pickaxeQuantity == 0)
                    return;

                _pickaxeSlot = default;
                RefreshPickaxeSlot();

                SpawnInventoryItemPickup(pickaxeDefinition, pickaxeQuantity, pickaxeSlotData.ConfigurationHash);
                EnsureToolAvailability();
                return;
            }

            if (index == WOOD_AXE_SLOT_INDEX)
            {
                var woodAxeSlotData = _woodAxeSlot;
                if (woodAxeSlotData.IsEmpty == true)
                    return;

                var woodAxeDefinition = woodAxeSlotData.GetDefinition();
                if (woodAxeDefinition == null)
                    return;

                byte woodAxeQuantity = woodAxeSlotData.Quantity;
                if (woodAxeQuantity == 0)
                    return;

                _woodAxeSlot = default;
                RefreshWoodAxeSlot();

                SpawnInventoryItemPickup(woodAxeDefinition, woodAxeQuantity, woodAxeSlotData.ConfigurationHash);
                EnsureToolAvailability();
                return;
            }

            if (index == FISHING_POLE_SLOT_INDEX)
            {
                var fishingSlotData = _fishingPoleSlot;
                if (fishingSlotData.IsEmpty == true)
                    return;

                var fishingDefinition = fishingSlotData.GetDefinition();
                if (fishingDefinition == null)
                    return;

                byte fishingQuantity = fishingSlotData.Quantity;
                if (fishingQuantity == 0)
                    return;

                UnequipFishingPoleInternal();

                _fishingPoleSlot = default;
                RefreshFishingPoleSlot();

                SpawnInventoryItemPickup(fishingDefinition, fishingQuantity, fishingSlotData.ConfigurationHash);
                EnsureToolAvailability();
                return;
            }

            if (index == HEAD_SLOT_INDEX)
            {
                var headSlotData = _headSlot;
                if (headSlotData.IsEmpty == true)
                    return;
                var headDefinition = headSlotData.GetDefinition();
                if (headDefinition == null)
                    return;
                byte headQuantity = headSlotData.Quantity;
                if (headQuantity == 0)
                    return;
                _headSlot = default;
                RefreshHeadSlot();

                SpawnInventoryItemPickup(headDefinition, headQuantity, headSlotData.ConfigurationHash);
                return;
            }

            if (index == UPPER_BODY_SLOT_INDEX)
            {
                var upperBodySlotData = _upperBodySlot;
                if (upperBodySlotData.IsEmpty == true)
                    return;
                var upperBodyDefinition = upperBodySlotData.GetDefinition();
                if (upperBodyDefinition == null)
                    return;
                byte upperBodyQuantity = upperBodySlotData.Quantity;
                if (upperBodyQuantity == 0)
                    return;
                _upperBodySlot = default;
                RefreshUpperBodySlot();

                SpawnInventoryItemPickup(upperBodyDefinition, upperBodyQuantity, upperBodySlotData.ConfigurationHash);
                return;
            }

            if (index == LOWER_BODY_SLOT_INDEX)
            {
                var lowerBodySlotData = _lowerBodySlot;
                if (lowerBodySlotData.IsEmpty == true)
                    return;
                var lowerBodyDefinition = lowerBodySlotData.GetDefinition();
                if (lowerBodyDefinition == null)
                    return;
                byte lowerBodyQuantity = lowerBodySlotData.Quantity;
                if (lowerBodyQuantity == 0)
                    return;
                _lowerBodySlot = default;
                RefreshLowerBodySlot();

                SpawnInventoryItemPickup(lowerBodyDefinition, lowerBodyQuantity, lowerBodySlotData.ConfigurationHash);
                return;
            }

            if (IsGeneralInventoryIndex(index) == false)
                return;

            var inventorySlot = _items[index];
            if (inventorySlot.IsEmpty == true)
                return;

            var inventoryDefinition = inventorySlot.GetDefinition();
            if (inventoryDefinition == null)
                return;

            byte inventoryQuantity = inventorySlot.Quantity;
            if (inventoryQuantity == 0)
                return;

            if (RemoveInventoryItemInternal(index, inventoryQuantity) == false)
                return;

            SpawnInventoryItemPickup(inventoryDefinition, inventoryQuantity, inventorySlot.ConfigurationHash);
        }

        private bool RemoveInventoryItemInternal(int index, byte quantity)
        {
            if (index == PICKAXE_SLOT_INDEX)
            {
                if (quantity == 0)
                    return false;

                var pickaxeSlotData = _pickaxeSlot;
                if (pickaxeSlotData.IsEmpty == true)
                    return false;

                if (pickaxeSlotData.Quantity <= quantity)
                {
                    _pickaxeSlot = default;
                }
                else
                {
                    pickaxeSlotData.Remove(quantity);
                    _pickaxeSlot = pickaxeSlotData;
                }

                RefreshPickaxeSlot();
                EnsureToolAvailability();
                return true;
            }

            if (index == WOOD_AXE_SLOT_INDEX)
            {
                if (quantity == 0)
                    return false;

                var woodAxeSlotData = _woodAxeSlot;
                if (woodAxeSlotData.IsEmpty == true)
                    return false;

                if (woodAxeSlotData.Quantity <= quantity)
                {
                    _woodAxeSlot = default;
                }
                else
                {
                    woodAxeSlotData.Remove(quantity);
                    _woodAxeSlot = woodAxeSlotData;
                }

                RefreshWoodAxeSlot();
                EnsureToolAvailability();
                return true;
            }

            if (index == HEAD_SLOT_INDEX)
            {
                if (quantity == 0)
                    return false;

                var headSlotData = _headSlot;
                if (headSlotData.IsEmpty == true)
                    return false;

                if (headSlotData.Quantity <= quantity)
                {
                    _headSlot = default;
                }
                else
                {
                    headSlotData.Remove(quantity);
                    _headSlot = headSlotData;
                }

                RefreshHeadSlot();
                return true;
            }

            if (index == UPPER_BODY_SLOT_INDEX)
            {
                if (quantity == 0)
                    return false;

                var upperBodySlotData = _upperBodySlot;
                if (upperBodySlotData.IsEmpty == true)
                    return false;

                if (upperBodySlotData.Quantity <= quantity)
                {
                    _upperBodySlot = default;
                }
                else
                {
                    upperBodySlotData.Remove(quantity);
                    _upperBodySlot = upperBodySlotData;
                }

                RefreshUpperBodySlot();
                return true;
            }

            if (index == LOWER_BODY_SLOT_INDEX)
            {
                if (quantity == 0)
                    return false;

                var lowerBodySlotData = _lowerBodySlot;
                if (lowerBodySlotData.IsEmpty == true)
                    return false;

                if (lowerBodySlotData.Quantity <= quantity)
                {
                    _lowerBodySlot = default;
                }
                else
                {
                    lowerBodySlotData.Remove(quantity);
                    _lowerBodySlot = lowerBodySlotData;
                }

                RefreshLowerBodySlot();
                return true;
            }

            if (IsGeneralInventoryIndex(index) == false)
                return false;

            var inventorySlot = _items[index];
            if (inventorySlot.IsEmpty == true)
                return false;

            bool removedSpecialItem = IsPickaxeSlotItem(inventorySlot) || IsWoodAxeSlotItem(inventorySlot) ||
                                      IsHeadSlotItem(inventorySlot) || IsUpperBodySlotItem(inventorySlot) ||
                                      IsLowerBodySlotItem(inventorySlot);

            if (inventorySlot.Quantity <= quantity)
            {
                SetInventorySlot(index, default);
            }
            else
            {
                inventorySlot.Remove(quantity);
                SetInventorySlot(index, inventorySlot);
            }

            RefreshItems();

            if (removedSpecialItem == true)
            {
                EnsureToolAvailability();
            }

            return true;
        }

        private void RestoreInventoryItem(int index, WeaponDefinition definition, NetworkString<_32> configurationHash)
        {
            if (IsGeneralInventoryIndex(index) == false || definition == null)
                return;

            var slot = new InventorySlot(definition.ID, 1, configurationHash);
            SuppressFeedForSlot(index);
            SetInventorySlot(index, slot);
            RefreshItems();
        }

        private bool TryStoreWeapon(Weapon weapon, int sourceSlot)
        {
            if (weapon == null)
                return true;

            var definition = weapon.Definition;
            if (definition == null)
                return false;

            EnsureWeaponPrefabRegistered(definition, weapon);

            int emptySlot = FindEmptyInventorySlot();
            if (emptySlot < 0)
                return false;

            var slot = new InventorySlot(definition.ID, 1, weapon.ConfigurationHash);
            SuppressFeedForSlot(emptySlot);
            SetInventorySlot(emptySlot, slot);
            RefreshItems();

            RemoveWeapon(sourceSlot);
            if (weapon.Object != null)
            {
                Runner.Despawn(weapon.Object);
            }

            return true;
        }

        private void SetInventorySlot(int index, InventorySlot slot)
        {
            if (index < 0 || index >= _items.Length)
            {
                return;
            }

            var currentSlot = _items[index];
            if (currentSlot.Equals(slot) == true)
            {
                return;
            }

            _items.Set(index, slot);
            UpdateWeaponDefinitionMapping(index, slot);
        }

        private void RefreshItems()
        {
            int length = _items.Length;
            if (_localItems == null || _localItems.Length != length)
            {
                _localItems = new InventorySlot[length];
            }

            for (int i = 0; i < length; i++)
            {
                var slot = _items[i];
                if (_localItems[i].Equals(slot) == false)
                {
                    _localItems[i] = slot;
                    ItemSlotChanged?.Invoke(i, slot);
                }
            }

            RefreshPickaxeSlot();
            RefreshWoodAxeSlot();
            RefreshFishingPoleSlot();
            RefreshHeadSlot();
            RefreshUpperBodySlot();
            RefreshLowerBodySlot();
        }

        internal bool ConsumeFeedSuppression(int index)
        {
            if (_suppressedItemFeedSlots == null)
                return false;

            return _suppressedItemFeedSlots.Remove(index);
        }

        private void SuppressFeedForSlot(int index)
        {
            if (_suppressedItemFeedSlots == null)
            {
                _suppressedItemFeedSlots = new HashSet<int>();
            }

            _suppressedItemFeedSlots.Add(index);
        }

        private void ClearFeedSuppression(int index)
        {
            if (_suppressedItemFeedSlots == null)
                return;

            _suppressedItemFeedSlots.Remove(index);
        }

        private void RefreshWeapons()
        {
            // keep previous reference BEFORE reading the networked value
            var previousWeapon = CurrentWeapon;
            var nextWeapon = _hotbar[_currentWeaponSlot];

            Vector2 lastRecoil = Vector2.zero;

            // Initialize and keep last recoil from armed weapons
            for (int i = 0; i < _hotbar.Length; i++)
            {
                var weapon = _hotbar[i];
                if (weapon == null)
                {
                    if (i < _localWeapons.Length)
                    {
                        _localWeapons[i] = null;
                    }

                    continue;
                }

                bool cacheChanged = i < _localWeapons.Length && _localWeapons[i] != weapon;
                WeaponSlot targetSlot = i < _slots.Length ? _slots[i] : null;
                if (targetSlot != null && (weapon.IsInitialized == false || cacheChanged == true))
                {
                    weapon.Initialize(Object, targetSlot.Active, targetSlot.Inactive);
                    weapon.AssignFireAudioEffects(_fireAudioEffectsRoot, _fireAudioEffects);
                }

                if (i < _localWeapons.Length)
                {
                    _localWeapons[i] = weapon;
                }

                // Disarm non-current armed weapons
                if (weapon.IsArmed == true && i != _currentWeaponSlot)
                {
                    weapon.DisarmWeapon();
                }
            }

            // Only run swap logic when the slot changed (or weapon ref changed)
            if (previousWeapon != nextWeapon)
            {
                // Disarm previously current weapon
                if (previousWeapon != null && previousWeapon.IsArmed)
                {
                    previousWeapon.DisarmWeapon();
                }

                CurrentWeapon = nextWeapon;
                CurrentWeaponHandle = _slots[_currentWeaponSlot].Active;
                CurrentWeaponBaseRotation = _slots[_currentWeaponSlot].BaseRotation;

                if (CurrentWeapon != null)
                {
                    CurrentWeapon.ArmWeapon();
                }
                else
                {
                    // make sure local cache clears when weapon is gone
                    _localWeapons[_currentWeaponSlot] = default;
                }
            }
            for (int i = 0; i<_hotbar.Length; i++)
            {
                NotifyHotbarSlotChanged(i);
            }
        }

    private void EnsureHotbarCacheInitialized()
    {
        if (_lastHotbarWeapons == null || _lastHotbarWeapons.Length != _hotbar.Length)
        {
            _lastHotbarWeapons = new Weapon[_hotbar.Length];
        }
    }

        private void NotifyHotbarSlotChanged(int slot)
        {
            if (slot < 0 || slot >= _hotbar.Length)
                return;

            EnsureHotbarCacheInitialized();

            var slotWeapon = _hotbar[slot];
            if (_lastHotbarWeapons[slot] == slotWeapon)
                return;

            _lastHotbarWeapons[slot] = slotWeapon;
            HotbarSlotChanged?.Invoke(slot, slotWeapon);

            RecalculateHotbarStats();
        }

        internal void RecalculateHotbarStats()
        {
            if (HasStateAuthority == false)
                return;

            if (_stats == null)
            {
                _stats = GetComponent<Stats>();

                if (_stats == null)
                    return;
            }

            EnsureHotbarStatCaches();

            for (int i = 0; i < Stats.Count; ++i)
            {
                int currentValue = _stats.GetStat(i);
                int previousBonus = _appliedHotbarBonuses[i];
                int baseValue = Mathf.Max(0, currentValue - previousBonus);

                _statBaseValues[i] = baseValue;
                _hotbarStatBuffer[i] = 0;
            }

            for (int slot = 1; slot < _hotbar.Length; ++slot)
            {
                Weapon weapon = _hotbar[slot];
                StaffWeapon staffWeapon = weapon as StaffWeapon;
                if (staffWeapon == null)
                    continue;

                IReadOnlyList<int> bonuses = staffWeapon.StatBonuses;
                if (bonuses == null)
                    continue;

                int limit = Mathf.Min(bonuses.Count, _hotbarStatBuffer.Length);
                for (int statIndex = 0; statIndex < limit; ++statIndex)
                {
                    _hotbarStatBuffer[statIndex] += bonuses[statIndex];
                }
            }

            for (int i = 0; i < _hotbarStatBuffer.Length; ++i)
            {
                int targetValue = Mathf.Max(0, _statBaseValues[i] + _hotbarStatBuffer[i]);
                _appliedHotbarBonuses[i] = _hotbarStatBuffer[i];
                _stats.SetStat(i, targetValue);
            }
        }

        private void EnsureHotbarStatCaches()
        {
            if (_statBaseValues == null || _statBaseValues.Length != Stats.Count)
            {
                _statBaseValues = new int[Stats.Count];
            }

            if (_appliedHotbarBonuses == null || _appliedHotbarBonuses.Length != Stats.Count)
            {
                _appliedHotbarBonuses = new int[Stats.Count];
            }

            if (_hotbarStatBuffer == null || _hotbarStatBuffer.Length != Stats.Count)
            {
                _hotbarStatBuffer = new int[Stats.Count];
            }
        }

        private void ResetHotbarStatBonuses()
        {
            if (HasStateAuthority == false)
                return;

            if (_stats == null)
            {
                _stats = GetComponent<Stats>();
            }

            if (_stats == null || _appliedHotbarBonuses == null)
                return;

            EnsureHotbarStatCaches();

            for (int i = 0; i < _appliedHotbarBonuses.Length; ++i)
            {
                int appliedBonus = _appliedHotbarBonuses[i];
                if (appliedBonus == 0)
                {
                    _statBaseValues[i] = Mathf.Max(0, _stats.GetStat(i));
                    _hotbarStatBuffer[i] = 0;
                    continue;
                }

                int currentValue = _stats.GetStat(i);
                int targetValue = Mathf.Max(0, currentValue - appliedBonus);
                _stats.SetStat(i, targetValue);

                _statBaseValues[i] = targetValue;
                _appliedHotbarBonuses[i] = 0;
                _hotbarStatBuffer[i] = 0;
            }
        }

        private void HandleGoldChanged(int value)
        {
            _localGold = value;
            GoldChanged?.Invoke(value);
        }

        [ContextMenu("Debug/Add 10 Gold")]
        private void Debug_AddTenGold()
        {
            int currentGold = _gold;
            AddGold(10);
            Debug.Log($"Added {_gold - currentGold} gold. Total: {_gold}");
        }

        private void DropAllWeapons()
        {
            for (int i = 1; i < _hotbar.Length; i++)
            {
                DropWeapon(i);
            }
        }

        private void DropWeapon(int weaponSlot)
        {
            if (weaponSlot <= 0 || weaponSlot >= _hotbar.Length)
                return;

            var weapon = _hotbar[weaponSlot];
            if (weapon == null)
                return;

            var definition = weapon.Definition;

            weapon.Deinitialize(Object);

        if (weaponSlot == _currentWeaponSlot)
        {
            byte bestWeaponSlot = _previousWeaponSlot;
            if (bestWeaponSlot == 0 || bestWeaponSlot == _currentWeaponSlot)
            {
                bestWeaponSlot = FindBestWeaponSlot(_currentWeaponSlot);
            }

            SetCurrentWeapon(bestWeaponSlot);
            ArmCurrentWeapon();
        }

        RemoveWeapon(weaponSlot);

        if (definition != null)
        {
            SpawnInventoryItemPickup(definition, 1, weapon.ConfigurationHash);
        }
        
        if (weapon != null && weapon.Object != null)
        {
            Runner.Despawn(weapon.Object);
        }
    }

    private Weapon EnsureWeaponPrefabRegistered(WeaponDefinition definition, Weapon weaponInstance = null)
    {
        if (definition == null)
            return null;

        if (_weaponPrefabsByDefinitionId.TryGetValue(definition.ID, out var cachedPrefab) == true &&
            cachedPrefab != null)
            return cachedPrefab;

        var definitionPrefab = definition.WeaponPrefab;
        if (definitionPrefab != null)
        {
            _weaponPrefabsByDefinitionId[definition.ID] = definitionPrefab;
            return definitionPrefab;
        }

        if (weaponInstance != null)
        {
            var resolvedPrefab = ResolveWeaponPrefabFromInstance(weaponInstance);
            if (resolvedPrefab != null)
            {
                _weaponPrefabsByDefinitionId[definition.ID] = resolvedPrefab;
                return resolvedPrefab;
            }
        }

        return null;
    }

    private Weapon ResolveWeaponPrefabFromInstance(Weapon weaponInstance)
    {
        if (weaponInstance == null)
            return null;

        var networkObject = weaponInstance.Object;
        if (networkObject == null)
            return null;

        var networkTypeId = networkObject.NetworkTypeId;
        if (networkTypeId.IsValid == false)
            return null;

        var prefabId = networkTypeId.AsPrefabId;
        if (prefabId.IsValid == false)
            return null;

        var prefabTable = Runner != null ? Runner.Config?.PrefabTable : null;
        if (prefabTable == null)
            return null;

        var prefabObject = prefabTable.Load(prefabId, true);
        if (prefabObject == null)
            return null;

        return prefabObject.GetComponent<Weapon>();
    }

    private int PickupWeapon(Weapon weapon, int? slotOverride = null)
    {
        if (weapon == null)
            return -1;

        int targetSlot;
        if (slotOverride.HasValue)
        {
            targetSlot = slotOverride.Value;
        }
        else
        {
            int existingSlot = FindWeaponSlotBySize(weapon.Size);
            targetSlot = existingSlot >= 0 ? existingSlot : GetSlotIndex(weapon.Size);
        }

        targetSlot = ClampToValidSlot(targetSlot);

        DropWeapon(targetSlot);
        AddWeapon(weapon, targetSlot);

        if (targetSlot >= _currentWeaponSlot && targetSlot < 5)
        {
            SetCurrentWeapon(targetSlot);
            ArmCurrentWeapon();
        }

        return targetSlot;
    }

    private bool TryAddWeaponToInventory(Weapon weapon)
    {
        if (weapon == null)
            return false;

        EnsureWeaponPrefabRegistered(weapon.Definition, weapon);

        return TryAddWeaponDefinitionToInventory(weapon.Definition);
    }

    private bool TryAddWeaponDefinitionToInventory(WeaponDefinition definition)
    {
        if (definition == null)
            return false;

        EnsureWeaponPrefabRegistered(definition);

        int emptySlot = FindEmptyInventorySlot();
        if (emptySlot < 0)
            return false;

        var slot = new InventorySlot(definition.ID, 1, default);
        SetInventorySlot(emptySlot, slot);
        RefreshItems();

        return true;
    }

    private int FindEmptyInventorySlot()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i].IsEmpty == true)
                return i;
        }

        return -1;
    }

        private void UpdateWeaponDefinitionMapping(int index, InventorySlot slot)
        {
            if (slot.IsEmpty == true)
            {
                _weaponDefinitionsBySlot.Remove(index);
                return;
            }

            var definition = slot.GetDefinition() as WeaponDefinition;
            if (definition != null)
            {
                _weaponDefinitionsBySlot[index] = definition;
            }
            else
            {
                _weaponDefinitionsBySlot.Remove(index);
            }
        }

        private byte AddToPickaxeSlot(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash)
        {
            if (quantity == 0)
                return 0;

            var slot = _pickaxeSlot;

            if (slot.IsEmpty == false && IsPickaxeSlotItem(slot) == false)
            {
                _pickaxeSlot = default;
                RefreshPickaxeSlot();
                slot = default;
            }

            int clampedMaxStack = Mathf.Clamp((int)ItemDefinition.GetMaxStack(definition.ID), 1, byte.MaxValue);

            if (slot.IsEmpty == true)
            {
                byte addAmount = (byte)Mathf.Min(quantity, clampedMaxStack);
                if (addAmount > 0)
                {
                    slot = new InventorySlot(definition.ID, addAmount, configurationHash);
                    _pickaxeSlot = slot;
                    RefreshPickaxeSlot();
                    quantity -= addAmount;
                }

                return quantity;
            }

            if (slot.ItemDefinitionId != definition.ID)
                return quantity;

            if (slot.ConfigurationHash != configurationHash)
                return quantity;

            if (slot.Quantity >= clampedMaxStack)
                return quantity;

            byte space = (byte)Mathf.Min(clampedMaxStack - slot.Quantity, quantity);
            if (space == 0)
                return quantity;

            slot.Add(space);
            _pickaxeSlot = slot;
            RefreshPickaxeSlot();

            return (byte)(quantity - space);
        }

        private void RefreshPickaxeSlot()
        {
            var slot = _pickaxeSlot;
            if (_localPickaxeSlot.Equals(slot) == false)
            {
                _localPickaxeSlot = slot;
                ItemSlotChanged?.Invoke(PICKAXE_SLOT_INDEX, slot);
            }
        }

        private byte AddToWoodAxeSlot(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash)
        {
            if (quantity == 0)
                return 0;

            var slot = _woodAxeSlot;

            if (slot.IsEmpty == false && IsWoodAxeSlotItem(slot) == false)
            {
                _woodAxeSlot = default;
                RefreshWoodAxeSlot();
                slot = default;
            }

            int clampedMaxStack = Mathf.Clamp((int)ItemDefinition.GetMaxStack(definition.ID), 1, byte.MaxValue);

            if (slot.IsEmpty == true)
            {
                byte addAmount = (byte)Mathf.Min(quantity, clampedMaxStack);
                if (addAmount > 0)
                {
                    slot = new InventorySlot(definition.ID, addAmount, configurationHash);
                    _woodAxeSlot = slot;
                    RefreshWoodAxeSlot();
                    quantity -= addAmount;
                }

                return quantity;
            }

            if (slot.ItemDefinitionId != definition.ID)
                return quantity;

            if (slot.ConfigurationHash != configurationHash)
                return quantity;

            if (slot.Quantity >= clampedMaxStack)
                return quantity;

            byte space = (byte)Mathf.Min(clampedMaxStack - slot.Quantity, quantity);
            if (space == 0)
                return quantity;

            slot.Add(space);
            _woodAxeSlot = slot;
            RefreshWoodAxeSlot();

            return (byte)(quantity - space);
        }

        private void RefreshWoodAxeSlot()
        {
            var slot = _woodAxeSlot;
            if (_localWoodAxeSlot.Equals(slot) == false)
            {
                _localWoodAxeSlot = slot;
                ItemSlotChanged?.Invoke(WOOD_AXE_SLOT_INDEX, slot);
            }
        }

        private byte AddToFishingPoleSlot(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash)
        {
            if (quantity == 0)
                return 0;

            var slot = _fishingPoleSlot;

            if (slot.IsEmpty == false && IsFishingPoleSlotItem(slot) == false)
            {
                _fishingPoleSlot = default;
                RefreshFishingPoleSlot();
                slot = default;
            }

            int clampedMaxStack = Mathf.Clamp((int)ItemDefinition.GetMaxStack(definition.ID), 1, byte.MaxValue);

            if (slot.IsEmpty == true)
            {
                byte addAmount = (byte)Mathf.Min(quantity, clampedMaxStack);
                if (addAmount > 0)
                {
                    slot = new InventorySlot(definition.ID, addAmount, configurationHash);
                    _fishingPoleSlot = slot;
                    RefreshFishingPoleSlot();
                    quantity -= addAmount;
                }

                return quantity;
            }

            if (slot.ItemDefinitionId != definition.ID)
                return quantity;

            if (slot.ConfigurationHash != configurationHash)
                return quantity;

            if (slot.Quantity >= clampedMaxStack)
                return quantity;

            byte space = (byte)Mathf.Min(clampedMaxStack - slot.Quantity, quantity);
            if (space == 0)
                return quantity;

            slot.Add(space);
            _fishingPoleSlot = slot;
            RefreshFishingPoleSlot();

            return (byte)(quantity - space);
        }

        private void RefreshFishingPoleSlot()
        {
            var slot = _fishingPoleSlot;
            if (_localFishingPoleSlot.Equals(slot) == false)
            {
                _localFishingPoleSlot = slot;
                ItemSlotChanged?.Invoke(FISHING_POLE_SLOT_INDEX, slot);
            }
        }

        private byte AddToHeadSlot(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash)
        {
            if (quantity == 0)
                return 0;

            var slot = _headSlot;

            if (slot.IsEmpty == false && IsHeadSlotItem(slot) == false)
            {
                _headSlot = default;
                RefreshHeadSlot();
                slot = default;
            }

            int clampedMaxStack = Mathf.Clamp((int)ItemDefinition.GetMaxStack(definition.ID), 1, byte.MaxValue);

            if (slot.IsEmpty == true)
            {
                byte addAmount = (byte)Mathf.Min(quantity, clampedMaxStack);
                if (addAmount > 0)
                {
                    slot = new InventorySlot(definition.ID, addAmount, configurationHash);
                    _headSlot = slot;
                    RefreshHeadSlot();
                    quantity -= addAmount;
                }

                return quantity;
            }

            if (slot.ItemDefinitionId != definition.ID)
                return quantity;

            if (slot.ConfigurationHash != configurationHash)
                return quantity;

            if (slot.Quantity >= clampedMaxStack)
                return quantity;

            byte space = (byte)Mathf.Min(clampedMaxStack - slot.Quantity, quantity);
            if (space == 0)
                return quantity;

            slot.Add(space);
            _headSlot = slot;
            RefreshHeadSlot();

            return (byte)(quantity - space);
        }

        private void RefreshHeadSlot()
        {
            var slot = _headSlot;
            if (_localHeadSlot.Equals(slot) == false)
            {
                _localHeadSlot = slot;
                ItemSlotChanged?.Invoke(HEAD_SLOT_INDEX, slot);
            }
        }

        private byte AddToUpperBodySlot(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash)
        {
            if (quantity == 0)
                return 0;

            var slot = _upperBodySlot;

            if (slot.IsEmpty == false && IsUpperBodySlotItem(slot) == false)
            {
                _upperBodySlot = default;
                RefreshUpperBodySlot();
                slot = default;
            }

            int clampedMaxStack = Mathf.Clamp((int)ItemDefinition.GetMaxStack(definition.ID), 1, byte.MaxValue);

            if (slot.IsEmpty == true)
            {
                byte addAmount = (byte)Mathf.Min(quantity, clampedMaxStack);
                if (addAmount > 0)
                {
                    slot = new InventorySlot(definition.ID, addAmount, configurationHash);
                    _upperBodySlot = slot;
                    RefreshUpperBodySlot();
                    quantity -= addAmount;
                }

                return quantity;
            }

            if (slot.ItemDefinitionId != definition.ID)
                return quantity;

            if (slot.ConfigurationHash != configurationHash)
                return quantity;

            if (slot.Quantity >= clampedMaxStack)
                return quantity;

            byte space = (byte)Mathf.Min(clampedMaxStack - slot.Quantity, quantity);
            if (space == 0)
                return quantity;

            slot.Add(space);
            _upperBodySlot = slot;
            RefreshUpperBodySlot();

            return (byte)(quantity - space);
        }

        private void RefreshUpperBodySlot()
        {
            var slot = _upperBodySlot;
            if (_localUpperBodySlot.Equals(slot) == false)
            {
                _localUpperBodySlot = slot;
                ItemSlotChanged?.Invoke(UPPER_BODY_SLOT_INDEX, slot);
            }
        }

        private byte AddToLowerBodySlot(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash)
        {
            if (quantity == 0)
                return 0;

            var slot = _lowerBodySlot;

            if (slot.IsEmpty == false && IsLowerBodySlotItem(slot) == false)
            {
                _lowerBodySlot = default;
                RefreshLowerBodySlot();
                slot = default;
            }

            int clampedMaxStack = Mathf.Clamp((int)ItemDefinition.GetMaxStack(definition.ID), 1, byte.MaxValue);

            if (slot.IsEmpty == true)
            {
                byte addAmount = (byte)Mathf.Min(quantity, clampedMaxStack);
                if (addAmount > 0)
                {
                    slot = new InventorySlot(definition.ID, addAmount, configurationHash);
                    _lowerBodySlot = slot;
                    RefreshLowerBodySlot();
                    quantity -= addAmount;
                }

                return quantity;
            }

            if (slot.ItemDefinitionId != definition.ID)
                return quantity;

            if (slot.ConfigurationHash != configurationHash)
                return quantity;

            if (slot.Quantity >= clampedMaxStack)
                return quantity;

            byte space = (byte)Mathf.Min(clampedMaxStack - slot.Quantity, quantity);
            if (space == 0)
                return quantity;

            slot.Add(space);
            _lowerBodySlot = slot;
            RefreshLowerBodySlot();

            return (byte)(quantity - space);
        }

        private void RefreshLowerBodySlot()
        {
            var slot = _lowerBodySlot;
            if (_localLowerBodySlot.Equals(slot) == false)
            {
                _localLowerBodySlot = slot;
                ItemSlotChanged?.Invoke(LOWER_BODY_SLOT_INDEX, slot);
            }
        }

        private void RefreshPickaxeVisuals()
        {
            var networkPickaxe = _pickaxe;

            if (_localPickaxe != networkPickaxe)
            {
                if (_localPickaxe != null)
                {
                    _localPickaxe.DeinitializeTool(Object);
                }

                _localPickaxe = networkPickaxe;
                _localPickaxeEquipped = false;

                if (_localPickaxe != null)
                {
                    _localPickaxe.InitializeTool(Object, _pickaxeEquippedParent, _pickaxeUnequippedParent);
                }
            }

            if (_localPickaxe != null)
            {
                _localPickaxe.RefreshParents(_pickaxeEquippedParent, _pickaxeUnequippedParent);

                bool desiredState = _isPickaxeEquipped;
                _localPickaxe.SetEquipped(desiredState);
                _localPickaxeEquipped = desiredState;
            }
            else
            {
                _localPickaxeEquipped = false;
            }
        }

        private void RefreshWoodAxeVisuals()
        {
            var networkWoodAxe = _woodAxe;

            if (_localWoodAxe != networkWoodAxe)
            {
                if (_localWoodAxe != null)
                {
                    _localWoodAxe.DeinitializeTool(Object);
                }

                _localWoodAxe = networkWoodAxe;
                _localWoodAxeEquipped = false;

                if (_localWoodAxe != null)
                {
                    _localWoodAxe.InitializeTool(Object, _woodAxeEquippedParent, _woodAxeUnequippedParent);
                }
            }

            if (_localWoodAxe != null)
            {
                _localWoodAxe.RefreshParents(_woodAxeEquippedParent, _woodAxeUnequippedParent);

                bool desiredState = _isWoodAxeEquipped;
                _localWoodAxe.SetEquipped(desiredState);
                _localWoodAxeEquipped = desiredState;
            }
            else
            {
                _localWoodAxeEquipped = false;
            }
        }

        private int GetToolSpeedValue(Tool networkTool, Tool localTool, InventorySlot slot)
        {
            Tool toolInstance = networkTool != null ? networkTool : localTool;

            if (toolInstance != null)
            {
                return Mathf.Max(0, toolInstance.Speed);
            }

            string configurationHash = slot.ConfigurationHash.ToString();
            if (string.IsNullOrWhiteSpace(configurationHash) == false &&
                Tool.TryDecodeConfiguration(configurationHash, out ToolConfiguration configuration) == true)
            {
                return Mathf.Max(0, configuration.Speed);
            }

            return 0;
        }

        private void EnsurePickaxeInstance()
        {
            if (HasStateAuthority == false)
            {
                RefreshPickaxeVisuals();
                return;
            }

            if (_pickaxeSlot.IsEmpty == true || IsPickaxeSlotItem(_pickaxeSlot) == false)
            {
                DespawnPickaxe();
                return;
            }

            var definition = _pickaxeSlot.GetDefinition() as PickaxeDefinition;
            if (definition == null || definition.PickaxePrefab == null || Runner == null)
            {
                DespawnPickaxe();
                return;
            }

            var pickaxe = _pickaxe;
            if (pickaxe != null && pickaxe.Definition != definition)
            {
                DespawnPickaxe();
                pickaxe = null;
            }

            if (pickaxe == null)
            {
                pickaxe = Runner.Spawn(definition.PickaxePrefab, inputAuthority: Object.InputAuthority);
                _pickaxe = pickaxe;
            }

            if (pickaxe != null)
            {
                pickaxe.SetConfigurationHash(_pickaxeSlot.ConfigurationHash);
            }

            RefreshPickaxeVisuals();
        }

        private void DespawnPickaxe()
        {
            if ((_weaponSlotBeforePickaxe != byte.MaxValue || _isPickaxeEquipped == true) && (_pickaxe != null || _localPickaxe != null))
            {
                EndPickaxeUse();
            }

            if (HasStateAuthority == true)
            {
                var pickaxe = _pickaxe;
                if (pickaxe != null)
                {
                    Runner.Despawn(pickaxe.Object);
                }

                _pickaxe = null;
                _isPickaxeEquipped = false;
            }

            if (_localPickaxe != null)
            {
                _localPickaxe.DeinitializeTool(Object);
                _localPickaxe = null;
            }

            _localPickaxeEquipped = false;
            _weaponSlotBeforePickaxe = byte.MaxValue;
        }

        private void EnsureWoodAxeInstance()
        {
            if (HasStateAuthority == false)
            {
                RefreshWoodAxeVisuals();
                return;
            }

            if (_woodAxeSlot.IsEmpty == true || IsWoodAxeSlotItem(_woodAxeSlot) == false)
            {
                DespawnWoodAxe();
                return;
            }

            var definition = _woodAxeSlot.GetDefinition() as WoodAxeDefinition;
            if (definition == null || definition.WoodAxePrefab == null || Runner == null)
            {
                DespawnWoodAxe();
                return;
            }

            var woodAxe = _woodAxe;
            if (woodAxe != null && woodAxe.Definition != definition)
            {
                DespawnWoodAxe();
                woodAxe = null;
            }

            if (woodAxe == null)
            {
                woodAxe = Runner.Spawn(definition.WoodAxePrefab, inputAuthority: Object.InputAuthority);
                _woodAxe = woodAxe;
            }

            if (woodAxe != null)
            {
                woodAxe.SetConfigurationHash(_woodAxeSlot.ConfigurationHash);
            }

            RefreshWoodAxeVisuals();
        }

        private void DespawnWoodAxe()
        {
            if ((_weaponSlotBeforeWoodAxe != byte.MaxValue || _isWoodAxeEquipped == true) && (_woodAxe != null || _localWoodAxe != null))
            {
                EndWoodAxeUse();
            }

            if (HasStateAuthority == true)
            {
                var woodAxe = _woodAxe;
                if (woodAxe != null)
                {
                    Runner.Despawn(woodAxe.Object);
                }

                _woodAxe = null;
                _isWoodAxeEquipped = false;
            }

            if (_localWoodAxe != null)
            {
                _localWoodAxe.DeinitializeTool(Object);
                _localWoodAxe = null;
            }

            _localWoodAxeEquipped = false;
            _weaponSlotBeforeWoodAxe = byte.MaxValue;
        }

        private void EnsureToolAvailability()
        {
            EnsurePickaxeAvailabilityInternal();
            EnsureWoodAxeAvailability();
            EnsureFishingPoleAvailability();
        }

        private void EnsurePickaxeAvailabilityInternal()
        {
            if (HasStateAuthority == false)
                return;

            if (_pickaxeSlot.IsEmpty == false && IsPickaxeSlotItem(_pickaxeSlot) == false)
            {
                _pickaxeSlot = default;
                RefreshPickaxeSlot();
                DespawnPickaxe();
            }

            if (IsPickaxeSlotItem(_pickaxeSlot) == false)
            {
                int pickaxeIndex = FindPickaxeInInventory();
                if (pickaxeIndex >= 0)
                {
                    var slot = _items[pickaxeIndex];
                    _pickaxeSlot = slot;
                    SetInventorySlot(pickaxeIndex, default);
                    RefreshPickaxeSlot();
                    RefreshItems();
                    EnsurePickaxeInstance();
                    return;
                }
            }

            if (HasAnyPickaxe() == true)
            {
                EnsurePickaxeInstance();
                return;
            }

            var defaultPickaxe = ResolveDefaultPickaxe();
            if (defaultPickaxe == null)
            {
                EnsurePickaxeInstance();
                return;
            }

            // Ensure the pickaxe definition is registered before we assign it so UI lookups succeed.
            ItemDefinition.Get(defaultPickaxe.ID);

            var defaultConfiguration = ResolveDefaultPickaxeConfiguration();

            _pickaxeSlot = new InventorySlot(defaultPickaxe.ID, 1, defaultConfiguration);
            RefreshPickaxeSlot();
            EnsurePickaxeInstance();
        }

        private void EnsureWoodAxeAvailability()
        {
            if (HasStateAuthority == false)
                return;

            if (_woodAxeSlot.IsEmpty == false && IsWoodAxeSlotItem(_woodAxeSlot) == false)
            {
                _woodAxeSlot = default;
                RefreshWoodAxeSlot();
                DespawnWoodAxe();
            }

            if (IsWoodAxeSlotItem(_woodAxeSlot) == false)
            {
                int woodAxeIndex = FindWoodAxeInInventory();
                if (woodAxeIndex >= 0)
                {
                    var slot = _items[woodAxeIndex];
                    _woodAxeSlot = slot;
                    SetInventorySlot(woodAxeIndex, default);
                    RefreshWoodAxeSlot();
                    RefreshItems();
                    EnsureWoodAxeInstance();
                    return;
                }
            }

            if (HasAnyWoodAxe() == true)
            {
                EnsureWoodAxeInstance();
                return;
            }

            var defaultWoodAxe = ResolveDefaultWoodAxe();
            if (defaultWoodAxe == null)
            {
                DespawnWoodAxe();
                return;
            }

            ItemDefinition.Get(defaultWoodAxe.ID);

            var defaultConfiguration = ResolveDefaultWoodAxeConfiguration();

            _woodAxeSlot = new InventorySlot(defaultWoodAxe.ID, 1, defaultConfiguration);
            RefreshWoodAxeSlot();
            EnsureWoodAxeInstance();
        }

        private void EnsureFishingPoleAvailability()
        {
            if (HasStateAuthority == false)
            {
                RefreshFishingPoleVisuals();
                return;
            }

            if (_fishingPoleSlot.IsEmpty == false && IsFishingPoleSlotItem(_fishingPoleSlot) == false)
            {
                _fishingPoleSlot = default;
                RefreshFishingPoleSlot();
                DespawnFishingPole();
            }

            if (IsFishingPoleSlotItem(_fishingPoleSlot) == false)
            {
                DespawnFishingPole();
                return;
            }

            EnsureFishingPoleInstance();
        }

        private void EnsureFishingPoleInstance()
        {
            if (HasStateAuthority == false)
            {
                RefreshFishingPoleVisuals();
                return;
            }

            if (_fishingPoleSlot.IsEmpty == true || IsFishingPoleSlotItem(_fishingPoleSlot) == false)
            {
                DespawnFishingPole();
                return;
            }

            var definition = _fishingPoleSlot.GetDefinition() as FishingPoleDefinition;
            if (definition == null || Runner == null)
            {
                DespawnFishingPole();
                return;
            }

            var prefab = definition.FishingPolePrefab ?? definition.WeaponPrefab as FishingPoleWeapon;
            if (prefab == null)
            {
                DespawnFishingPole();
                return;
            }

            var fishingPole = _fishingPole;
            if (fishingPole != null && fishingPole.Definition != definition)
            {
                DespawnFishingPole();
                fishingPole = null;
            }

            if (fishingPole == null)
            {
                fishingPole = Runner.Spawn(prefab, inputAuthority: Object.InputAuthority);
                _fishingPole = fishingPole;
            }

            if (fishingPole != null)
            {
                fishingPole.SetConfigurationHash(_fishingPoleSlot.ConfigurationHash);
                EnsureWeaponPrefabRegistered(definition, fishingPole);

                if (_hotbar.Length > HOTBAR_FISHING_POLE_SLOT && _hotbar[HOTBAR_FISHING_POLE_SLOT] != fishingPole)
                {
                    AddWeapon(fishingPole, HOTBAR_FISHING_POLE_SLOT);
                }
            }

            RefreshFishingPoleVisuals();
        }

        private void DespawnFishingPole()
        {
            if ((_weaponSlotBeforeFishingPole != byte.MaxValue || _isFishingPoleEquipped == true) &&
                (_fishingPole != null || _localFishingPole != null))
            {
                UnequipFishingPoleInternal();
            }

            if (HasStateAuthority == true)
            {
                var fishingPole = _fishingPole;
                if (fishingPole != null)
                {
                    Runner.Despawn(fishingPole.Object);
                }

                _fishingPole = null;
                _isFishingPoleEquipped = false;

                if (_hotbar.Length > HOTBAR_FISHING_POLE_SLOT && _hotbar[HOTBAR_FISHING_POLE_SLOT] != null)
                {
                    RemoveWeapon(HOTBAR_FISHING_POLE_SLOT);
                }
            }

            if (_localFishingPole != null)
            {
                UnsubscribeFromFishingLifecycle(_localFishingPole);
                _localFishingPole.Deinitialize(Object);
                _localFishingPole = null;
            }

            UpdateLocalFishingPoleEquipped(false);
            _weaponSlotBeforeFishingPole = byte.MaxValue;
        }

        private void RefreshFishingPoleVisuals()
        {
            var networkFishingPole = _fishingPole;

            if (_localFishingPole != networkFishingPole)
            {
                if (_localFishingPole != null)
                {
                    UnsubscribeFromFishingLifecycle(_localFishingPole);
                    _localFishingPole.Deinitialize(Object);
                }

                _localFishingPole = networkFishingPole;

                if (_localFishingPole != null)
                {
                    Transform equippedParent = _fishingPoleEquippedParent;
                    Transform unequippedParent = _fishingPoleUnequippedParent;

                    int slotsLength = _slots?.Length ?? 0;
                    if (slotsLength > 0)
                    {
                        int targetSlotIndex = HOTBAR_FISHING_POLE_SLOT < slotsLength ? HOTBAR_FISHING_POLE_SLOT : 0;
                        WeaponSlot baseSlot = _slots[targetSlotIndex];
                        equippedParent ??= baseSlot.Active;
                        unequippedParent ??= baseSlot.Inactive;
                    }

                    _localFishingPole.Initialize(Object, equippedParent, unequippedParent);
                    _localFishingPole.AssignFireAudioEffects(_fireAudioEffectsRoot, _fireAudioEffects);
                    SubscribeToFishingLifecycle(_localFishingPole);
                }
            }

            UpdateLocalFishingPoleEquipped(_isFishingPoleEquipped);
        }

        private void SubscribeToFishingLifecycle(FishingPoleWeapon weapon)
        {
            if (weapon == null)
                return;

            weapon.LifecycleStateChanged -= OnFishingLifecycleStateChanged;
            weapon.LifecycleStateChanged += OnFishingLifecycleStateChanged;
        }

        private void UnsubscribeFromFishingLifecycle(FishingPoleWeapon weapon)
        {
            if (weapon == null)
                return;

            weapon.LifecycleStateChanged -= OnFishingLifecycleStateChanged;
        }

        private void PrepareFightingMinigame()
        {
            if (HasStateAuthority == false)
                return;

            int minHits = Mathf.Max(1, _fightingSuccessHitRange.x);
            int maxHits = Mathf.Max(minHits, _fightingSuccessHitRange.y);
            maxHits = Mathf.Clamp(maxHits, 1, byte.MaxValue);
            minHits = Mathf.Clamp(minHits, 1, maxHits);
            _fightingHitsRequired = (byte)UnityEngine.Random.Range(minHits, maxHits + 1);
            _fightingHitsSucceeded = 0;
        }

        private void ResetFightingMinigameProgress()
        {
            if (HasStateAuthority == false)
                return;

            _fightingHitsSucceeded = 0;
        }

        private void HandleHookSetMinigameResultInternal(bool wasSuccessful)
        {
            UpdateHookSetSuccessZoneState(false);

            var fishingPole = _fishingPole ?? _localFishingPole;

            if (fishingPole == null)
                return;

            if (wasSuccessful == true)
            {
                PrepareFightingMinigame();
                fishingPole.EnterFightingPhase();
            }
            else
            {
                ResetFightingMinigameProgress();
                fishingPole.HandleHookSetFailed();
            }
        }

        private void HandleFightingMinigameProgressInternal(int successHits, int requiredHits)
        {
            var fishingPole = _fishingPole ?? _localFishingPole;

            if (fishingPole == null)
                return;

            successHits = Mathf.Max(0, successHits);

            int clampedRequired = Mathf.Max(1, _fightingHitsRequired);
            if (requiredHits > 0)
            {
                clampedRequired = Mathf.Max(clampedRequired, requiredHits);
            }

            successHits = Mathf.Min(successHits, clampedRequired);

            fishingPole.HandleFightingMinigameProgress(successHits, clampedRequired);
        }

        private void HandleFightingMinigameResultInternal(bool wasSuccessful)
        {
            var fishingPole = _fishingPole ?? _localFishingPole;

            if (fishingPole == null)
                return;

            if (wasSuccessful == true)
            {
                int requiredHits = Mathf.Max(1, _fightingHitsRequired);
                if (_fightingHitsSucceeded < byte.MaxValue)
                {
                    _fightingHitsSucceeded = (byte)Mathf.Min(_fightingHitsSucceeded + 1, requiredHits);
                }

                ResetFightingMinigameProgress();
                fishingPole.EnterCatchPhase();
            }
            else
            {
                ResetFightingMinigameProgress();
                fishingPole.HandleFightingFailed();
            }
        }

        private void UpdateLocalFishingPoleEquipped(bool isEquipped)
        {
            if (_localFishingPoleEquipped == isEquipped)
            {
                if (isEquipped == false)
                {
                    SetFishingLifecycleState(FishingLifecycleState.Inactive);
                    UpdateHookSetSuccessZoneState(false);
                }

                return;
            }

            _localFishingPoleEquipped = isEquipped;
            FishingPoleEquippedChanged?.Invoke(isEquipped);

            if (isEquipped == true)
            {
                SetFishingLifecycleState(FishingLifecycleState.Ready);
            }
            else
            {
                SetFishingLifecycleState(FishingLifecycleState.Inactive);
                UpdateHookSetSuccessZoneState(false);
            }
        }

        private void OnFishingLifecycleStateChanged(FishingLifecycleState state)
        {
            SetFishingLifecycleState(state);
        }

        private void SetFishingLifecycleState(FishingLifecycleState state)
        {
            if (_fishingLifecycleState == state)
                return;

            _fishingLifecycleState = state;
            FishingLifecycleStateChanged?.Invoke(state);
        }

        private static PickaxeDefinition ResolveDefaultPickaxe()
        {
            var defaultDefinitions = DefaultItemDefinitions.Instance;
            if (defaultDefinitions != null && defaultDefinitions.DefaultPickaxe != null)
            {
                return defaultDefinitions.DefaultPickaxe;
            }

            // Fallback: locate any pickaxe definition so new profiles always receive a tool.
            if (_cachedFallbackPickaxe == null)
            {
                var pickaxes = Resources.LoadAll<PickaxeDefinition>(string.Empty);
                for (int i = 0; i < pickaxes.Length; i++)
                {
                    if (pickaxes[i] != null)
                    {
                        _cachedFallbackPickaxe = pickaxes[i];
                        break;
                    }
                }
            }

            return _cachedFallbackPickaxe;
        }

        private static WoodAxeDefinition ResolveDefaultWoodAxe()
        {
            var defaultDefinitions = DefaultItemDefinitions.Instance;
            if (defaultDefinitions != null && defaultDefinitions.DefaultWoodAxe != null)
            {
                return defaultDefinitions.DefaultWoodAxe;
            }

            if (_cachedFallbackWoodAxe == null)
            {
                var woodAxes = Resources.LoadAll<WoodAxeDefinition>(string.Empty);
                for (int i = 0; i < woodAxes.Length; i++)
                {
                    if (woodAxes[i] != null)
                    {
                        _cachedFallbackWoodAxe = woodAxes[i];
                        break;
                    }
                }
            }

            return _cachedFallbackWoodAxe;
        }

        private static NetworkString<_32> ResolveDefaultPickaxeConfiguration()
        {
            var defaultDefinitions = DefaultItemDefinitions.Instance;
            if (defaultDefinitions == null)
                return default;

            return ToNetworkConfiguration(defaultDefinitions.DefaultPickaxeConfiguration);
        }

        private static NetworkString<_32> ResolveDefaultWoodAxeConfiguration()
        {
            var defaultDefinitions = DefaultItemDefinitions.Instance;
            if (defaultDefinitions == null)
                return default;

            return ToNetworkConfiguration(defaultDefinitions.DefaultWoodAxeConfiguration);
        }

        private static NetworkString<_32> ToNetworkConfiguration(ToolConfiguration configuration)
        {
            string encodedConfiguration = Tool.EncodeConfiguration(configuration);
            if (string.IsNullOrEmpty(encodedConfiguration) == true)
            {
                return default;
            }

            NetworkString<_32> networkHash = encodedConfiguration;
            return networkHash;
        }

        private bool HasAnyPickaxe()
        {
            if (IsPickaxeSlotItem(_pickaxeSlot) == true)
                return true;

            for (int i = 0; i < _items.Length; i++)
            {
                if (IsPickaxeSlotItem(_items[i]) == true)
                    return true;
            }

            return false;
        }

        private bool HasAnyWoodAxe()
        {
            if (IsWoodAxeSlotItem(_woodAxeSlot) == true)
                return true;

            for (int i = 0; i < _items.Length; i++)
            {
                if (IsWoodAxeSlotItem(_items[i]) == true)
                    return true;
            }

            return false;
        }

        private int FindPickaxeInInventory()
        {
            for (int i = 0; i < _items.Length; i++)
            {
                if (IsPickaxeSlotItem(_items[i]) == true)
                    return i;
            }

            return -1;
        }

        private int FindWoodAxeInInventory()
        {
            for (int i = 0; i < _items.Length; i++)
            {
                if (IsWoodAxeSlotItem(_items[i]) == true)
                    return i;
            }

            return -1;
        }

        private bool IsGeneralInventoryIndex(int index)
        {
            return index >= 0 && index < _items.Length;
        }

        private bool IsValidInventoryIndex(int index)
        {
            return IsGeneralInventoryIndex(index) || index == PICKAXE_SLOT_INDEX || index == WOOD_AXE_SLOT_INDEX ||
                   index == FISHING_POLE_SLOT_INDEX || index == HEAD_SLOT_INDEX || index == UPPER_BODY_SLOT_INDEX ||
                   index == LOWER_BODY_SLOT_INDEX;
        }

        private static bool IsPickaxeSlotItem(InventorySlot slot)
        {
            return HasSlotCategory(slot, ESlotCategory.Pickaxe);
        }

        private static bool IsWoodAxeSlotItem(InventorySlot slot)
        {
            return HasSlotCategory(slot, ESlotCategory.WoodAxe);
        }

        private static bool IsFishingPoleSlotItem(InventorySlot slot)
        {
            return HasSlotCategory(slot, ESlotCategory.FishingPole);
        }

        private static bool IsHeadSlotItem(InventorySlot slot)
        {
            return HasSlotCategory(slot, ESlotCategory.Head);
        }

        private static bool IsUpperBodySlotItem(InventorySlot slot)
        {
            return HasSlotCategory(slot, ESlotCategory.UpperBody);
        }

        private static bool IsLowerBodySlotItem(InventorySlot slot)
        {
            return HasSlotCategory(slot, ESlotCategory.LowerBody);
        }

        private static bool HasSlotCategory(InventorySlot slot, ESlotCategory category)
        {
            if (slot.IsEmpty == true)
                return false;

            ItemDefinition definition = slot.GetDefinition();
            if (definition == null)
                return false;

            return definition.SlotCategory == category;
        }

        private void SpawnInventoryItemPickup(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash = default)
        {
            if (HasStateAuthority == false)
                return;

            if (_inventoryItemPickupPrefab == null)
                return;

            if (definition == null || quantity == 0)
                return;

            Vector3 forward = transform.forward;
            Vector3 origin = transform.position;

            if (_character != null)
            {
                var characterTransform = _character.transform;
                origin = characterTransform.position;
                forward = characterTransform.forward;
            }

            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = transform.forward;
                forward.y = 0f;
            }

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();

            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), 0f, UnityEngine.Random.Range(-0.25f, 0.25f));
            Vector3 spawnPosition = origin + forward * _itemDropForwardOffset + Vector3.up * _itemDropUpOffset + randomOffset;
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);

            var provider = Runner.Spawn(_inventoryItemPickupPrefab, spawnPosition, rotation);
            if (provider == null)
                return;

            provider.Initialize(definition, quantity, configurationHash);

            var rigidbody = provider.GetComponent<Rigidbody>();
            if (rigidbody != null && _itemDropImpulse > 0f)
            {
                Vector3 impulseDirection = (forward + Vector3.up * 0.5f).normalized;
                if (impulseDirection.sqrMagnitude < 0.0001f)
                {
                    impulseDirection = Vector3.up;
                }

                rigidbody.AddForce(impulseDirection * _itemDropImpulse, ForceMode.VelocityChange);
            }
        }

    private void AddWeapon(Weapon weapon, int? slotOverride = null)
    {
        if (weapon == null)
            return;

        if (_slots == null || _slots.Length == 0)
            return;

        int targetSlot;
        if (slotOverride.HasValue)
        {
            targetSlot = slotOverride.Value;
        }
        else
        {
            int existingSlot = FindWeaponSlotBySize(weapon.Size);
            targetSlot = existingSlot >= 0 ? existingSlot : GetSlotIndex(weapon.Size);
        }
        targetSlot = ClampToValidSlot(targetSlot);

        if (CanAssignWeaponToHotbarSlot(weapon, targetSlot) == false)
        {
            int fallbackSlot = -1;
            for (int i = 0; i < _hotbar.Length; ++i)
            {
                if (CanAssignWeaponToHotbarSlot(weapon, i) == true)
                {
                    fallbackSlot = i;
                    break;
                }
            }

            if (fallbackSlot < 0)
                return;

            targetSlot = fallbackSlot;
        }

        RemoveWeapon(targetSlot);

        weapon.Object.AssignInputAuthority(Object.InputAuthority);
        WeaponSlot slot = ResolveWeaponSlotForIndex(targetSlot);
        Transform activeParent = slot != null ? slot.Active : null;
        Transform inactiveParent = slot != null ? slot.Inactive : null;

        weapon.Initialize(Object, activeParent, inactiveParent);
        weapon.AssignFireAudioEffects(_fireAudioEffectsRoot, _fireAudioEffects);

        var aoiProxy = weapon.GetComponent<NetworkAreaOfInterestProxy>();
        aoiProxy.SetPositionSource(transform);

        Runner.SetPlayerAlwaysInterested(Object.InputAuthority, weapon.Object, true);

        _hotbar.Set(targetSlot, weapon);
        _localWeapons[targetSlot] = weapon;

        NotifyHotbarSlotChanged(targetSlot);
    }

    private void RemoveWeapon(int slot)
    {
        var weapon = _hotbar[slot];
        if (weapon == null)
            return;

        weapon.Deinitialize(Object);
        weapon.Object.RemoveInputAuthority();

        var aoiProxy = weapon.GetComponent<NetworkAreaOfInterestProxy>();
        aoiProxy.ResetPositionSource();

        Runner.SetPlayerAlwaysInterested(Object.InputAuthority, weapon.Object, false);

        _hotbar.Set(slot, null);
        _localWeapons[slot] = null;

        NotifyHotbarSlotChanged(slot);
    }

    private byte FindBestWeaponSlot(int ignoreSlot)
    {
        byte bestWeaponSlot = 0;
        int bestPriority = -1;

        for (int i = 0; i < _hotbar.Length; i++)
        {
            if (IsWeaponHotbarSlot(i) == false)
                continue;

            Weapon weapon = _hotbar[i];
            if (weapon != null)
            {
                if (i == ignoreSlot)
                    continue;

                int priority = GetWeaponPriority(weapon.Size);
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestWeaponSlot = (byte)i;
                }
            }
        }

        return bestWeaponSlot;
    }

    private static int GetWeaponPriority(WeaponSize size)
    {
        switch (size)
        {
            case WeaponSize.Staff:
                return 2;
            case WeaponSize.Throwable:
                return 1;
            case WeaponSize.Unarmed:
            default:
                return 0;
        }
    }

    public void SwitchWeapon(int hotbarIndex)
    {
        int targetSlot = hotbarIndex + 1;

        if (targetSlot <= 0 || targetSlot >= _hotbar.Length)
            return;

        if (_currentWeaponSlot == targetSlot)
            return;

        SetCurrentWeapon(targetSlot);
        ArmCurrentWeapon();
    }
}

}