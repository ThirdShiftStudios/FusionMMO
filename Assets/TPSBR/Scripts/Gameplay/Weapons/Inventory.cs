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

        public LayerMask HitMask => _hitMask;
        public int CurrentWeaponSlot => _currentWeaponSlot;
        public int PreviousWeaponSlot => _previousWeaponSlot;
        public WeaponSize CurrentWeaponSize => CurrentWeapon != null ? CurrentWeapon.Size : WeaponSize.Unarmed;
        public const int INVENTORY_SIZE = 10;
        public const int PICKAXE_SLOT_INDEX = byte.MaxValue;
        public const int WOOD_AXE_SLOT_INDEX = byte.MaxValue - 1;
        public const int HOTBAR_CAPACITY = 3;
        public const int HOTBAR_VISIBLE_SLOTS = HOTBAR_CAPACITY - 1;
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

        [Header("Audio")] [SerializeField] private Transform _fireAudioEffectsRoot;

        [Networked, Capacity(HOTBAR_CAPACITY)] private NetworkArray<Weapon> _hotbar { get; }
        [Networked, Capacity(INVENTORY_SIZE)] private NetworkArray<InventorySlot> _items { get; }
        [Networked] private InventorySlot _pickaxeSlot { get; set; }
        [Networked] private InventorySlot _woodAxeSlot { get; set; }
        [Networked] private byte _currentWeaponSlot { get; set; }

        [Networked] private byte _previousWeaponSlot { get; set; }
        [Networked] private Pickaxe _pickaxe { get; set; }
        [Networked] private NetworkBool _isPickaxeEquipped { get; set; }
        [Networked] private WoodAxe _woodAxe { get; set; }
        [Networked] private NetworkBool _isWoodAxeEquipped { get; set; }

        private Health _health;
        private Character _character;
        private Interactions _interactions;
        private AudioEffect[] _fireAudioEffects;
        private Weapon[] _localWeapons = new Weapon[HOTBAR_CAPACITY];
        private Weapon[] _lastHotbarWeapons;
        private InventorySlot[] _localItems;
        private InventorySlot _localPickaxeSlot;
        private InventorySlot _localWoodAxeSlot;
        private Pickaxe _localPickaxe;
        private bool _localPickaxeEquipped;
        private byte _weaponSlotBeforePickaxe = byte.MaxValue;
        private WoodAxe _localWoodAxe;
        private bool _localWoodAxeEquipped;
        private byte _weaponSlotBeforeWoodAxe = byte.MaxValue;
        private Dictionary<int, WeaponDefinition> _weaponDefinitionsBySlot = new Dictionary<int, WeaponDefinition>();
        private readonly Dictionary<WeaponSize, int> _weaponSizeToSlotIndex = new Dictionary<WeaponSize, int>();

        private static readonly Dictionary<int, Weapon> _weaponPrefabsByDefinitionId = new Dictionary<int, Weapon>();
        private static PickaxeDefinition _cachedFallbackPickaxe;
        private static WoodAxeDefinition _cachedFallbackWoodAxe;

        public event Action<int, InventorySlot> ItemSlotChanged;
        public event Action<int, Weapon> HotbarSlotChanged;

        // PUBLIC METHODS

        public int InventorySize => _items.Length;

        internal PlayerInventorySaveData CreateSaveData()
        {
            var data = new PlayerInventorySaveData
            {
                InventorySlots = new PlayerInventoryItemData[_items.Length],
                HotbarSlots    = new PlayerHotbarSlotData[_hotbar.Length],
                CurrentWeaponSlot = _currentWeaponSlot
            };

            for (int i = 0; i < _items.Length; i++)
            {
                var slot = _items[i];
                string configurationHash = slot.ConfigurationHash.ToString();

                data.InventorySlots[i] = new PlayerInventoryItemData
                {
                    ItemDefinitionId = slot.ItemDefinitionId,
                    Quantity = slot.Quantity,
                    ConfigurationHash = string.IsNullOrEmpty(configurationHash) == false ? configurationHash : null
                };
            }

            for (int i = 0; i < _hotbar.Length; i++)
            {
                var weapon = _hotbar[i];
                if (weapon == null)
                {
                    data.HotbarSlots[i] = default;
                    continue;
                }

                string configurationHash = weapon.ConfigurationHash.ToString();

                data.HotbarSlots[i] = new PlayerHotbarSlotData
                {
                    WeaponDefinitionId = weapon.Definition != null ? weapon.Definition.ID : 0,
                    ConfigurationHash = string.IsNullOrEmpty(configurationHash) == false ? configurationHash : null
                };
            }

            string pickaxeConfigurationHash = _pickaxeSlot.ConfigurationHash.ToString();
            data.PickaxeSlot = new PlayerInventoryItemData
            {
                ItemDefinitionId = _pickaxeSlot.ItemDefinitionId,
                Quantity = _pickaxeSlot.Quantity,
                ConfigurationHash = string.IsNullOrEmpty(pickaxeConfigurationHash) == false ? pickaxeConfigurationHash : null
            };

            string woodAxeConfigurationHash = _woodAxeSlot.ConfigurationHash.ToString();
            data.WoodAxeSlot = new PlayerInventoryItemData
            {
                ItemDefinitionId = _woodAxeSlot.ItemDefinitionId,
                Quantity = _woodAxeSlot.Quantity,
                ConfigurationHash = string.IsNullOrEmpty(woodAxeConfigurationHash) == false ? woodAxeConfigurationHash : null
            };

            return data;
        }

        internal void ApplySaveData(PlayerInventorySaveData data)
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

            for (int i = 0; i < _items.Length; i++)
            {
                _items.Set(i, default);
                UpdateWeaponDefinitionMapping(i, default);
            }

            if (data.InventorySlots != null)
            {
                int count = Mathf.Min(data.InventorySlots.Length, _items.Length);
                for (int i = 0; i < count; i++)
                {
                    var slotData = data.InventorySlots[i];
                    if (slotData.ItemDefinitionId == 0 || slotData.Quantity == 0)
                        continue;

                    NetworkString<_32> configurationHash = default;
                    if (string.IsNullOrEmpty(slotData.ConfigurationHash) == false)
                    {
                        configurationHash = slotData.ConfigurationHash;
                    }

                    var slot = new InventorySlot(slotData.ItemDefinitionId, slotData.Quantity, configurationHash);
                    _items.Set(i, slot);
                    UpdateWeaponDefinitionMapping(i, slot);
                }
            }

            if (data.PickaxeSlot.ItemDefinitionId != 0 && data.PickaxeSlot.Quantity != 0)
            {
                NetworkString<_32> pickaxeHash = default;
                if (string.IsNullOrEmpty(data.PickaxeSlot.ConfigurationHash) == false)
                {
                    pickaxeHash = data.PickaxeSlot.ConfigurationHash;
                }

                _pickaxeSlot = new InventorySlot(data.PickaxeSlot.ItemDefinitionId, data.PickaxeSlot.Quantity, pickaxeHash);
                RefreshPickaxeSlot();
            }

            if (data.WoodAxeSlot.ItemDefinitionId != 0 && data.WoodAxeSlot.Quantity != 0)
            {
                NetworkString<_32> woodAxeHash = default;
                if (string.IsNullOrEmpty(data.WoodAxeSlot.ConfigurationHash) == false)
                {
                    woodAxeHash = data.WoodAxeSlot.ConfigurationHash;
                }

                _woodAxeSlot = new InventorySlot(data.WoodAxeSlot.ItemDefinitionId, data.WoodAxeSlot.Quantity, woodAxeHash);
                RefreshWoodAxeSlot();
            }

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
        }

        public InventorySlot GetItemSlot(int index)
        {
            if (index == PICKAXE_SLOT_INDEX)
                return _pickaxeSlot;

            if (index == WOOD_AXE_SLOT_INDEX)
                return _woodAxeSlot;

            if (index < 0 || index >= _items.Length)
                return default;

            return _items[index];
        }

        public byte AddItem(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash = default)
        {
            if (definition == null || quantity == 0)
                return quantity;

            if (HasStateAuthority == false)
                return quantity;

            return AddItemInternal(definition, quantity, configurationHash);
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

        public void SetCurrentWeapon(int slot)
        {
            slot = Mathf.Clamp(slot, 0, _hotbar.Length - 1);

            if (_currentWeaponSlot == slot)
                return;

            if (_currentWeaponSlot > 0)
            {
                _previousWeaponSlot = _currentWeaponSlot;
            }

            _currentWeaponSlot = (byte)slot;
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
            bool restoredFromCloud = Global.PlayerCloudSaveService != null &&
                                      Global.PlayerCloudSaveService.RegisterInventoryAndRestore(this);

            if (HasStateAuthority == false)
            {
                RefreshWeapons();
                RefreshItems();
                RefreshPickaxeVisuals();
                RefreshWoodAxeVisuals();
                return;
            }

            if (restoredFromCloud == true)
            {
                RefreshWeapons();
                RefreshItems();
                RefreshPickaxeVisuals();
                RefreshWoodAxeVisuals();
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
        }

        public void OnDespawned()
        {
            Global.PlayerCloudSaveService?.UnregisterInventory(this);

            DespawnPickaxe();
            DespawnWoodAxe();

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

        private int FindWeaponSlotBySize(WeaponSize size)
        {
            for (int i = 0; i < _hotbar.Length; i++)
            {
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
                WeaponSize.Unarmed => 0,
                WeaponSize.Staff => 1,
                WeaponSize.Throwable => 5,
                _ => 0,
            };
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
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

        private byte AddItemInternal(ItemDefinition definition, byte quantity, NetworkString<_32> configurationHash)
        {
            bool isPickaxe = definition is PickaxeDefinition;
            bool isWoodAxe = definition is WoodAxeDefinition;

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
                _items.Set(i, slot);
                UpdateWeaponDefinitionMapping(i, slot);
                remaining -= space;
            }

            for (int i = 0; i < _items.Length && remaining > 0; i++)
            {
                var slot = _items[i];
                if (slot.IsEmpty == false)
                    continue;

                byte addAmount = (byte)Mathf.Min(maxStackByte, remaining);
                slot = new InventorySlot(definition.ID, addAmount, configurationHash);
                _items.Set(i, slot);
                UpdateWeaponDefinitionMapping(i, slot);
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
            bool fromSpecial = fromPickaxe || fromWoodAxe;
            bool toSpecial = toPickaxe || toWoodAxe;

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

                _items.Set(toIndex, pickaxeSourceSlot);
                UpdateWeaponDefinitionMapping(toIndex, pickaxeSourceSlot);

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
                    _items.Set(fromIndex, default);
                    UpdateWeaponDefinitionMapping(fromIndex, default);
                }
                else
                {
                    _items.Set(fromIndex, previousPickaxe);
                    UpdateWeaponDefinitionMapping(fromIndex, previousPickaxe);
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

                _items.Set(toIndex, woodAxeSourceSlot);
                UpdateWeaponDefinitionMapping(toIndex, woodAxeSourceSlot);

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
                    _items.Set(fromIndex, default);
                    UpdateWeaponDefinitionMapping(fromIndex, default);
                }
                else
                {
                    _items.Set(fromIndex, previousWoodAxe);
                    UpdateWeaponDefinitionMapping(fromIndex, previousWoodAxe);
                }

                RefreshItems();
                EnsureToolAvailability();
                return;
            }

            var generalSourceSlot = _items[fromIndex];
            if (generalSourceSlot.IsEmpty == true)
                return;

            var toSlot = _items[toIndex];

            if (toSlot.IsEmpty == true)
            {
                _items.Set(toIndex, generalSourceSlot);
                _items.Set(fromIndex, default);
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
                        toSlot.Add(space);
                        generalSourceSlot.Remove(space);

                        _items.Set(toIndex, toSlot);
                        UpdateWeaponDefinitionMapping(toIndex, toSlot);

                        if (generalSourceSlot.IsEmpty == true)
                        {
                            _items.Set(fromIndex, default);
                            UpdateWeaponDefinitionMapping(fromIndex, default);
                        }
                        else
                        {
                            _items.Set(fromIndex, generalSourceSlot);
                            UpdateWeaponDefinitionMapping(fromIndex, generalSourceSlot);
                        }

                        RefreshItems();
                        return;
                    }
                }
            }

            _items.Set(toIndex, generalSourceSlot);
            UpdateWeaponDefinitionMapping(toIndex, generalSourceSlot);

            _items.Set(fromIndex, toSlot);
            UpdateWeaponDefinitionMapping(fromIndex, toSlot);
            RefreshItems();
        }

        private void AssignHotbar(int inventoryIndex, int hotbarIndex)
        {
            if (IsGeneralInventoryIndex(inventoryIndex) == false)
                return;

            int slot = hotbarIndex + 1;
            if (slot <= 0 || slot >= _hotbar.Length)
                return;

            var inventorySlot = _items[inventoryIndex];
            if (inventorySlot.IsEmpty == true)
                return;

            var definition = inventorySlot.GetDefinition() as WeaponDefinition;
            if (definition == null)
                return;

            var configurationHash = inventorySlot.ConfigurationHash;

            var weaponPrefab = EnsureWeaponPrefabRegistered(definition);
            if (weaponPrefab == null)
                return;

            if (RemoveInventoryItemInternal(inventoryIndex, 1) == false)
                return;

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
            if (slot <= 0 || slot >= _hotbar.Length)
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
            _items.Set(inventoryIndex, inventorySlot);
            UpdateWeaponDefinitionMapping(inventoryIndex, inventorySlot);
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

            if (fromSlot <= 0 || fromSlot >= _hotbar.Length)
                return;

            if (toSlot <= 0 || toSlot >= _hotbar.Length)
                return;

            if (fromSlot == toSlot)
                return;

            var fromWeapon = _hotbar[fromSlot];
            var toWeapon = _hotbar[toSlot];

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

            if (IsGeneralInventoryIndex(index) == false)
                return false;

            var inventorySlot = _items[index];
            if (inventorySlot.IsEmpty == true)
                return false;

            bool removedSpecialItem = IsPickaxeSlotItem(inventorySlot) || IsWoodAxeSlotItem(inventorySlot);

            if (inventorySlot.Quantity <= quantity)
            {
                _items.Set(index, default);
                UpdateWeaponDefinitionMapping(index, default);
            }
            else
            {
                inventorySlot.Remove(quantity);
                _items.Set(index, inventorySlot);
                UpdateWeaponDefinitionMapping(index, inventorySlot);
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
            _items.Set(index, slot);
            UpdateWeaponDefinitionMapping(index, slot);
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
            _items.Set(emptySlot, slot);
            UpdateWeaponDefinitionMapping(emptySlot, slot);
            RefreshItems();

            RemoveWeapon(sourceSlot);
            if (weapon.Object != null)
            {
                Runner.Despawn(weapon.Object);
            }

            return true;
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
        _items.Set(emptySlot, slot);
        UpdateWeaponDefinitionMapping(emptySlot, slot);
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
                    _items.Set(pickaxeIndex, default);
                    UpdateWeaponDefinitionMapping(pickaxeIndex, default);
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

            _pickaxeSlot = new InventorySlot(defaultPickaxe.ID, 1, default);
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
                    _items.Set(woodAxeIndex, default);
                    UpdateWeaponDefinitionMapping(woodAxeIndex, default);
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

            _woodAxeSlot = new InventorySlot(defaultWoodAxe.ID, 1, default);
            RefreshWoodAxeSlot();
            EnsureWoodAxeInstance();
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
            return IsGeneralInventoryIndex(index) || index == PICKAXE_SLOT_INDEX || index == WOOD_AXE_SLOT_INDEX;
        }

        private static bool IsPickaxeSlotItem(InventorySlot slot)
        {
            if (slot.IsEmpty == true)
                return false;

            var definition = slot.GetDefinition();
            return definition is PickaxeDefinition;
        }

        private static bool IsWoodAxeSlotItem(InventorySlot slot)
        {
            if (slot.IsEmpty == true)
                return false;

            var definition = slot.GetDefinition();
            return definition is WoodAxeDefinition;
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

        RemoveWeapon(targetSlot);

        weapon.Object.AssignInputAuthority(Object.InputAuthority);
        WeaponSlot slot = _slots[targetSlot];
        weapon.Initialize(Object, slot.Active, slot.Inactive);
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