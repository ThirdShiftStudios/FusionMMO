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
        public WeaponSize Size;
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
        public InventorySlot(int itemDefinitionId, byte quantity, NetworkString<_64> configurationHash)
        {
            ItemDefinitionId = itemDefinitionId;
            Quantity = quantity;
            ConfigurationHash = configurationHash;
        }

        public int ItemDefinitionId { get; private set; }
        public byte Quantity { get; private set; }
        public NetworkString<_64> ConfigurationHash { get; private set; }

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
        public const int BASE_GENERAL_INVENTORY_SLOTS = 8;
        public const int MAX_BAG_SLOT_BONUS = 40;
        public const int INVENTORY_SIZE = BASE_GENERAL_INVENTORY_SLOTS + MAX_BAG_SLOT_BONUS;
        public const int BAG_SLOT_COUNT = 5;
        public const int PICKAXE_SLOT_INDEX = byte.MaxValue;
        public const int WOOD_AXE_SLOT_INDEX = byte.MaxValue - 1;
        public const int FISHING_POLE_SLOT_INDEX = byte.MaxValue - 2;
        public const int HEAD_SLOT_INDEX = byte.MaxValue - 3;
        public const int UPPER_BODY_SLOT_INDEX = byte.MaxValue - 4;
        public const int LOWER_BODY_SLOT_INDEX = byte.MaxValue - 5;
        public const int PIPE_SLOT_INDEX = byte.MaxValue - 6;
        public const int BAG_SLOT_1_INDEX = byte.MaxValue - 7;
        public const int BAG_SLOT_2_INDEX = byte.MaxValue - 8;
        public const int BAG_SLOT_3_INDEX = byte.MaxValue - 9;
        public const int BAG_SLOT_4_INDEX = byte.MaxValue - 10;
        public const int BAG_SLOT_5_INDEX = byte.MaxValue - 11;
        public const int MOUNT_SLOT_INDEX = byte.MaxValue - 12;
        public const int HOTBAR_CAPACITY = 7;
        public const int HOTBAR_VISIBLE_SLOTS = HOTBAR_CAPACITY - 1;
        public const int HOTBAR_UNARMED_SLOT = 0;
        public const int HOTBAR_PRIMARY_WEAPON_SLOT = 1;
        public const int HOTBAR_SECONDARY_WEAPON_SLOT = 2;
        public const int HOTBAR_FIRST_CONSUMABLE_SLOT = 3;
        public const int HOTBAR_SECOND_CONSUMABLE_SLOT = 4;
        public const int HOTBAR_THIRD_CONSUMABLE_SLOT = 5;
        public const int HOTBAR_FISHING_POLE_SLOT = HOTBAR_CAPACITY - 1;
        private const int HOTBAR_INVENTORY_INDEX_BASE = -1000;
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
        [Networked, Capacity(HOTBAR_CAPACITY)] private NetworkArray<InventorySlot> _hotbarItems { get; }
        [Networked, Capacity(INVENTORY_SIZE)] private NetworkArray<InventorySlot> _items { get; }
        [Networked, Capacity(BAG_SLOT_COUNT)] private NetworkArray<InventorySlot> _bagSlots { get; }
        [Networked] private InventorySlot _pickaxeSlot { get; set; }
        [Networked] private InventorySlot _woodAxeSlot { get; set; }
        [Networked] private InventorySlot _fishingPoleSlot { get; set; }
        [Networked] private InventorySlot _headSlot { get; set; }
        [Networked] private InventorySlot _upperBodySlot { get; set; }
        [Networked] private InventorySlot _lowerBodySlot { get; set; }
        [Networked] private InventorySlot _pipeSlot { get; set; }
        [Networked] private InventorySlot _mountSlot { get; set; }
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
        private InventorySlot[] _localBagSlots;
        private InventorySlot _localPickaxeSlot;
        private InventorySlot _localWoodAxeSlot;
        private InventorySlot _localFishingPoleSlot;
        private InventorySlot _localHeadSlot;
        private InventorySlot _localUpperBodySlot;
        private InventorySlot _localLowerBodySlot;
        private InventorySlot _localPipeSlot;
        private InventorySlot _localMountSlot;
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
        private PlayerCharacterInventorySaveData _pendingRestoreData;

        private enum SpecialRestoreSlot
        {
            Pickaxe,
            WoodAxe,
            FishingPole,
            Head,
            UpperBody,
            LowerBody,
            Pipe,
            Mount,
        }
        private FishingLifecycleState _fishingLifecycleState = FishingLifecycleState.Inactive;
        private bool _isHookSetSuccessZoneActive;
        private int _generalInventorySize = BASE_GENERAL_INVENTORY_SLOTS;

        private static readonly Dictionary<int, Weapon> _weaponPrefabsByDefinitionId = new Dictionary<int, Weapon>();
        private static readonly int[] _bagSlotIndices =
        {
            BAG_SLOT_1_INDEX,
            BAG_SLOT_2_INDEX,
            BAG_SLOT_3_INDEX,
            BAG_SLOT_4_INDEX,
            BAG_SLOT_5_INDEX,
        };

        public event Action<int, InventorySlot> ItemSlotChanged;
        public event Action<int, Weapon> HotbarSlotChanged;
        public event Action<int> GoldChanged;
        public event Action<bool> FishingPoleEquippedChanged;
        public event Action<FishingLifecycleState> FishingLifecycleStateChanged;
        public event Action<int> GeneralInventorySizeChanged;

        public bool IsFishingPoleEquipped => _localFishingPoleEquipped;
        public FishingLifecycleState FishingLifecycleState => _fishingLifecycleState;
        public int FightingMinigameHitsRequired => Mathf.Max(1, _fightingHitsRequired);
        public int FightingMinigameHitsSucceeded => _fightingHitsSucceeded;

        // PUBLIC METHODS

        public int InventorySize => _generalInventorySize;
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

        public InventorySlot GetHotbarItemSlot(int index)
        {
            if (index < 0 || index >= _hotbarItems.Length)
            {
                return default;
            }

            return _hotbarItems[index];
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
                InventorySlots     = new PlayerInventoryItemData[_items.Length],
                HotbarSlots        = new PlayerHotbarSlotData[_hotbar.Length],
                CurrentWeaponSlot  = _currentWeaponSlot,
                Gold               = _gold
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

                var hotbarSlot = _hotbarItems[i];
                string configurationHash = hotbarSlot.ConfigurationHash.ToString();
                if (string.IsNullOrEmpty(configurationHash) == true)
                {
                    configurationHash = weapon.ConfigurationHash.ToString();
                }

                byte quantity = hotbarSlot.Quantity;
                if (quantity == 0)
                {
                    quantity = 1;
                }

                data.HotbarSlots[i] = new PlayerHotbarSlotData
                {
                    WeaponDefinitionId = weapon.Definition != null ? weapon.Definition.ID : 0,
                    ConfigurationHash = string.IsNullOrEmpty(configurationHash) == false ? configurationHash : null,
                    Quantity = quantity
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

            string fishingPoleConfigurationHash = _fishingPoleSlot.ConfigurationHash.ToString();
            data.FishingPoleSlot = new PlayerInventoryItemData
            {
                ItemDefinitionId = _fishingPoleSlot.ItemDefinitionId,
                Quantity = _fishingPoleSlot.Quantity,
                ConfigurationHash = string.IsNullOrEmpty(fishingPoleConfigurationHash) == false ? fishingPoleConfigurationHash : null
            };

            string headConfigurationHash = _headSlot.ConfigurationHash.ToString();
            data.HeadSlot = new PlayerInventoryItemData
            {
                ItemDefinitionId = _headSlot.ItemDefinitionId,
                Quantity = _headSlot.Quantity,
                ConfigurationHash = string.IsNullOrEmpty(headConfigurationHash) == false ? headConfigurationHash : null
            };

            string upperBodyConfigurationHash = _upperBodySlot.ConfigurationHash.ToString();
            data.UpperBodySlot = new PlayerInventoryItemData
            {
                ItemDefinitionId = _upperBodySlot.ItemDefinitionId,
                Quantity = _upperBodySlot.Quantity,
                ConfigurationHash = string.IsNullOrEmpty(upperBodyConfigurationHash) == false ? upperBodyConfigurationHash : null
            };

            string lowerBodyConfigurationHash = _lowerBodySlot.ConfigurationHash.ToString();
            data.LowerBodySlot = new PlayerInventoryItemData
            {
                ItemDefinitionId = _lowerBodySlot.ItemDefinitionId,
                Quantity = _lowerBodySlot.Quantity,
                ConfigurationHash = string.IsNullOrEmpty(lowerBodyConfigurationHash) == false ? lowerBodyConfigurationHash : null
            };

            string pipeConfigurationHash = _pipeSlot.ConfigurationHash.ToString();
            data.PipeSlot = new PlayerInventoryItemData
            {
                ItemDefinitionId = _pipeSlot.ItemDefinitionId,
                Quantity = _pipeSlot.Quantity,
                ConfigurationHash = string.IsNullOrEmpty(pipeConfigurationHash) == false ? pipeConfigurationHash : null
            };

            string mountConfigurationHash = _mountSlot.ConfigurationHash.ToString();
            data.MountSlot = new PlayerInventoryItemData
            {
                ItemDefinitionId = _mountSlot.ItemDefinitionId,
                Quantity = _mountSlot.Quantity,
                ConfigurationHash = string.IsNullOrEmpty(mountConfigurationHash) == false ? mountConfigurationHash : null,
            };

            data.BagSlots = new PlayerInventoryItemData[BAG_SLOT_COUNT];
            for (int i = 0; i < BAG_SLOT_COUNT; i++)
            {
                var bagSlot = _bagSlots[i];
                string bagConfigurationHash = bagSlot.ConfigurationHash.ToString();
                data.BagSlots[i] = new PlayerInventoryItemData
                {
                    ItemDefinitionId = bagSlot.ItemDefinitionId,
                    Quantity = bagSlot.Quantity,
                    ConfigurationHash = string.IsNullOrEmpty(bagConfigurationHash) == false ? bagConfigurationHash : null,
                };
            }

            return data;
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
            _pipeSlot = default;
            RefreshPipeSlot();

            for (int i = 0; i < _items.Length; i++)
            {
                _items.Set(i, default);
                UpdateWeaponDefinitionMapping(i, default);
            }

            for (int i = 0; i < BAG_SLOT_COUNT; i++)
            {
                _bagSlots.Set(i, default);
            }

            if (data.InventorySlots != null)
            {
                int count = Mathf.Min(data.InventorySlots.Length, _items.Length);
                for (int i = 0; i < count; i++)
                {
                    var slotData = data.InventorySlots[i];
                    if (slotData.ItemDefinitionId == 0 || slotData.Quantity == 0)
                        continue;

                    NetworkString<_64> configurationHash = default;
                    if (string.IsNullOrEmpty(slotData.ConfigurationHash) == false)
                    {
                        configurationHash = slotData.ConfigurationHash;
                    }

                    var slot = new InventorySlot(slotData.ItemDefinitionId, slotData.Quantity, configurationHash);
                    _items.Set(i, slot);
                    UpdateWeaponDefinitionMapping(i, slot);
                }
            }

            if (data.BagSlots != null)
            {
                int count = Mathf.Min(data.BagSlots.Length, BAG_SLOT_COUNT);
                for (int i = 0; i < count; i++)
                {
                    var slotData = data.BagSlots[i];
                    if (slotData.ItemDefinitionId == 0 || slotData.Quantity == 0)
                        continue;

                    NetworkString<_64> configurationHash = default;
                    if (string.IsNullOrEmpty(slotData.ConfigurationHash) == false)
                    {
                        configurationHash = slotData.ConfigurationHash;
                    }

                    var slot = new InventorySlot(slotData.ItemDefinitionId, slotData.Quantity, configurationHash);
                    _bagSlots.Set(i, slot);
                }
            }

            if (data.PickaxeSlot.ItemDefinitionId != 0 && data.PickaxeSlot.Quantity != 0)
            {
                NetworkString<_64> pickaxeHash = default;
                if (string.IsNullOrEmpty(data.PickaxeSlot.ConfigurationHash) == false)
                {
                    pickaxeHash = data.PickaxeSlot.ConfigurationHash;
                }

                _pickaxeSlot = new InventorySlot(data.PickaxeSlot.ItemDefinitionId, data.PickaxeSlot.Quantity, pickaxeHash);
                RefreshPickaxeSlot();
            }

            if (data.WoodAxeSlot.ItemDefinitionId != 0 && data.WoodAxeSlot.Quantity != 0)
            {
                NetworkString<_64> woodAxeHash = default;
                if (string.IsNullOrEmpty(data.WoodAxeSlot.ConfigurationHash) == false)
                {
                    woodAxeHash = data.WoodAxeSlot.ConfigurationHash;
                }

                _woodAxeSlot = new InventorySlot(data.WoodAxeSlot.ItemDefinitionId, data.WoodAxeSlot.Quantity, woodAxeHash);
                RefreshWoodAxeSlot();
            }

            if (data.FishingPoleSlot.ItemDefinitionId != 0 && data.FishingPoleSlot.Quantity != 0)
            {
                NetworkString<_64> fishingPoleHash = default;
                if (string.IsNullOrEmpty(data.FishingPoleSlot.ConfigurationHash) == false)
                {
                    fishingPoleHash = data.FishingPoleSlot.ConfigurationHash;
                }

                _fishingPoleSlot = new InventorySlot(data.FishingPoleSlot.ItemDefinitionId, data.FishingPoleSlot.Quantity, fishingPoleHash);
                RefreshFishingPoleSlot();
            }

            if (data.HeadSlot.ItemDefinitionId != 0 && data.HeadSlot.Quantity != 0)
            {
                NetworkString<_64> headHash = default;
                if (string.IsNullOrEmpty(data.HeadSlot.ConfigurationHash) == false)
                {
                    headHash = data.HeadSlot.ConfigurationHash;
                }

                _headSlot = new InventorySlot(data.HeadSlot.ItemDefinitionId, data.HeadSlot.Quantity, headHash);
                RefreshHeadSlot();
            }

            if (data.UpperBodySlot.ItemDefinitionId != 0 && data.UpperBodySlot.Quantity != 0)
            {
                NetworkString<_64> upperBodyHash = default;
                if (string.IsNullOrEmpty(data.UpperBodySlot.ConfigurationHash) == false)
                {
                    upperBodyHash = data.UpperBodySlot.ConfigurationHash;
                }

                _upperBodySlot = new InventorySlot(data.UpperBodySlot.ItemDefinitionId, data.UpperBodySlot.Quantity, upperBodyHash);
                RefreshUpperBodySlot();
            }

            if (data.LowerBodySlot.ItemDefinitionId != 0 && data.LowerBodySlot.Quantity != 0)
            {
                NetworkString<_64> lowerBodyHash = default;
                if (string.IsNullOrEmpty(data.LowerBodySlot.ConfigurationHash) == false)
                {
                    lowerBodyHash = data.LowerBodySlot.ConfigurationHash;
                }

                _lowerBodySlot = new InventorySlot(data.LowerBodySlot.ItemDefinitionId, data.LowerBodySlot.Quantity, lowerBodyHash);
                RefreshLowerBodySlot();
            }

            if (data.PipeSlot.ItemDefinitionId != 0 && data.PipeSlot.Quantity != 0)
            {
                NetworkString<_64> pipeHash = default;
                if (string.IsNullOrEmpty(data.PipeSlot.ConfigurationHash) == false)
                {
                    pipeHash = data.PipeSlot.ConfigurationHash;
                }

                _pipeSlot = new InventorySlot(data.PipeSlot.ItemDefinitionId, data.PipeSlot.Quantity, pipeHash);
                RefreshPipeSlot();
            }

            if (data.MountSlot.ItemDefinitionId != 0 && data.MountSlot.Quantity != 0)
            {
                NetworkString<_64> mountHash = default;
                if (string.IsNullOrEmpty(data.MountSlot.ConfigurationHash) == false)
                {
                    mountHash = data.MountSlot.ConfigurationHash;
                }

                _mountSlot = new InventorySlot(data.MountSlot.ItemDefinitionId, data.MountSlot.Quantity, mountHash);
                RefreshMountSlot();
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

                    byte quantity = slotData.Quantity;
                    if (quantity == 0)
                        quantity = 1;

                    bool hasConfiguration = string.IsNullOrEmpty(slotData.ConfigurationHash) == false;
                    NetworkString<_64> configurationHash = default;

                    var itemDefinition = ItemDefinition.Get(slotData.WeaponDefinitionId) as WeaponDefinition;
                    if (itemDefinition == null)
                        continue;

                    var weaponPrefab = EnsureWeaponPrefabRegistered(itemDefinition);
                    if (weaponPrefab == null)
                        continue;

                    var weapon = Runner.Spawn(weaponPrefab, inputAuthority: Object.InputAuthority);
                    if (weapon == null)
                        continue;

                    if (hasConfiguration == true)
                    {
                        configurationHash = slotData.ConfigurationHash;
                        weapon.SetConfigurationHash(configurationHash);
                    }
                    else
                    {
                        configurationHash = weapon.ConfigurationHash;
                    }

                    var hotbarSlot = new InventorySlot(slotData.WeaponDefinitionId, quantity, configurationHash);
                    _hotbarItems.Set(i, hotbarSlot);

                    AddWeapon(weapon, i);
                    NotifyHotbarSlotStackChanged(i);
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

        internal bool RequestRestoreFromSave(PlayerCharacterInventorySaveData data)
        {
            if (data == null)
                return false;

            if (HasStateAuthority == true)
            {
                ApplySaveData(data);
                return true;
            }

            if (HasInputAuthority == false)
                return false;

            NetworkString<_32> characterId = default;
            if (string.IsNullOrEmpty(data.CharacterId) == false)
            {
                characterId = data.CharacterId;
            }

            RPC_RequestBeginInventoryRestore(characterId);

            var inventorySlots = data.InventorySlots;
            if (inventorySlots != null)
            {
                int count = Mathf.Min(inventorySlots.Length, _items.Length);
                for (int i = 0; i < count; i++)
                {
                    var slotData = inventorySlots[i];
                    if (slotData.ItemDefinitionId == 0 || slotData.Quantity == 0)
                        continue;

                    NetworkString<_64> configurationHash = default;
                    if (string.IsNullOrEmpty(slotData.ConfigurationHash) == false)
                    {
                        configurationHash = slotData.ConfigurationHash;
                    }

                    RPC_RequestSetInventorySlot((byte)i, slotData.ItemDefinitionId, slotData.Quantity, configurationHash);
                }
            }

            var bagSlots = data.BagSlots;
            if (bagSlots != null)
            {
                int count = Mathf.Min(bagSlots.Length, BAG_SLOT_COUNT);
                for (int i = 0; i < count; i++)
                {
                    var slotData = bagSlots[i];
                    if (slotData.ItemDefinitionId == 0 || slotData.Quantity == 0)
                        continue;

                    NetworkString<_64> configurationHash = default;
                    if (string.IsNullOrEmpty(slotData.ConfigurationHash) == false)
                    {
                        configurationHash = slotData.ConfigurationHash;
                    }

                    RPC_RequestSetBagSlot((byte)i, slotData.ItemDefinitionId, slotData.Quantity, configurationHash);
                }
            }

            SendSpecialSlot(SpecialRestoreSlot.Pickaxe, data.PickaxeSlot);
            SendSpecialSlot(SpecialRestoreSlot.WoodAxe, data.WoodAxeSlot);
            SendSpecialSlot(SpecialRestoreSlot.FishingPole, data.FishingPoleSlot);
            SendSpecialSlot(SpecialRestoreSlot.Head, data.HeadSlot);
            SendSpecialSlot(SpecialRestoreSlot.UpperBody, data.UpperBodySlot);
            SendSpecialSlot(SpecialRestoreSlot.LowerBody, data.LowerBodySlot);
            SendSpecialSlot(SpecialRestoreSlot.Pipe, data.PipeSlot);
            SendSpecialSlot(SpecialRestoreSlot.Mount, data.MountSlot);

            var hotbarSlots = data.HotbarSlots;
            if (hotbarSlots != null)
            {
                int count = Mathf.Min(hotbarSlots.Length, _hotbar.Length);
                for (int i = 0; i < count; i++)
                {
                    var slotData = hotbarSlots[i];
                    if (slotData.WeaponDefinitionId == 0)
                        continue;

                    NetworkString<_64> configurationHash = default;
                    if (string.IsNullOrEmpty(slotData.ConfigurationHash) == false)
                    {
                        configurationHash = slotData.ConfigurationHash;
                    }

                    RPC_RequestSetHotbarSlot((byte)i, slotData.WeaponDefinitionId, slotData.Quantity, configurationHash);
                }
            }

            byte desiredWeaponSlot = data.CurrentWeaponSlot;
            if (desiredWeaponSlot >= _hotbar.Length)
            {
                desiredWeaponSlot = 0;
            }

            RPC_RequestFinalizeInventoryRestore(desiredWeaponSlot, data.Gold);

            return true;

            void SendSpecialSlot(SpecialRestoreSlot slot, PlayerInventoryItemData slotData)
            {
                if (slotData.ItemDefinitionId == 0 || slotData.Quantity == 0)
                    return;

                NetworkString<_64> configurationHash = default;
                if (string.IsNullOrEmpty(slotData.ConfigurationHash) == false)
                {
                    configurationHash = slotData.ConfigurationHash;
                }

                RPC_RequestSetSpecialSlot(slot, slotData.ItemDefinitionId, slotData.Quantity, configurationHash);
            }
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

            if (index == PIPE_SLOT_INDEX)
                return _pipeSlot;

            if (index == MOUNT_SLOT_INDEX)
                return _mountSlot;

            if (TryGetHotbarSlotFromInventoryIndex(index, out int hotbarSlot) == true)
                return _hotbarItems[hotbarSlot];

            if (TryGetBagArrayIndex(index, out int bagIndex) == true)
                return _bagSlots[bagIndex];

            if (index < 0 || index >= _generalInventorySize)
                return default;

            return _items[index];
        }

        public bool TryFindInventorySlot(ItemDefinition definition, out int index, out InventorySlot slot)
        {
            index = -1;
            slot = default;

            if (definition == null)
            {
                return false;
            }

            if (_pipeSlot.IsEmpty == false && _pipeSlot.ItemDefinitionId == definition.ID && _pipeSlot.Quantity > 0)
            {
                index = PIPE_SLOT_INDEX;
                slot = _pipeSlot;
                return true;
            }

            if (_mountSlot.IsEmpty == false && _mountSlot.ItemDefinitionId == definition.ID && _mountSlot.Quantity > 0)
            {
                index = MOUNT_SLOT_INDEX;
                slot = _mountSlot;
                return true;
            }

            int generalCapacity = Mathf.Clamp(_generalInventorySize, 0, _items.Length);
            for (int i = 0; i < generalCapacity; i++)
            {
                var candidate = _items[i];
                if (candidate.IsEmpty == true)
                    continue;

                if (candidate.ItemDefinitionId != definition.ID)
                    continue;

                if (candidate.Quantity == 0)
                    continue;

                index = i;
                slot = candidate;
                return true;
            }

            int bagCount = _bagSlots.Length;
            for (int i = 0; i < bagCount; i++)
            {
                var candidate = _bagSlots[i];
                if (candidate.IsEmpty == true)
                    continue;

                if (candidate.ItemDefinitionId != definition.ID)
                    continue;

                if (candidate.Quantity == 0)
                    continue;

                index = GetBagSlotIndex(i);
                slot = candidate;
                return true;
            }

            for (int i = 0; i < _hotbarItems.Length; i++)
            {
                var candidate = _hotbarItems[i];
                if (candidate.IsEmpty == true)
                    continue;

                if (candidate.ItemDefinitionId != definition.ID)
                    continue;

                if (candidate.Quantity == 0)
                    continue;

                index = GetHotbarInventoryIndex(i);
                slot = candidate;
                return true;
            }

            return false;
        }

        public bool TrySetInventorySlotConfiguration(int index, NetworkString<_64> configurationHash)
        {
            if (HasStateAuthority == false)
                return false;

            if (index < 0 || index >= _generalInventorySize)
                return false;

            var slot = _items[index];
            if (slot.IsEmpty == true)
                return false;

            slot = new InventorySlot(slot.ItemDefinitionId, slot.Quantity, configurationHash);
            _items.Set(index, slot);
            UpdateWeaponDefinitionMapping(index, slot);
            RefreshItems();
            return true;
        }

        public bool TryAddToInventorySlot(int index, byte quantity)
        {
            if (HasStateAuthority == false)
                return false;

            if (quantity == 0)
                return true;

            if (IsGeneralInventoryIndex(index) == false)
                return false;

            var slot = _items[index];
            if (slot.IsEmpty == true)
                return false;

            ushort rawMaxStack = ItemDefinition.GetMaxStack(slot.ItemDefinitionId);
            int maxStack = Mathf.Clamp((int)rawMaxStack, 1, byte.MaxValue);
            if (rawMaxStack == 0)
            {
                maxStack = byte.MaxValue;
            }
            if (slot.Quantity >= maxStack)
                return false;

            byte addAmount = (byte)Mathf.Min(quantity, maxStack - slot.Quantity);
            if (addAmount == 0)
                return false;

            slot.Add(addAmount);
            _items.Set(index, slot);
            UpdateWeaponDefinitionMapping(index, slot);
            RefreshItems();

            return addAmount == quantity;
        }

        public bool TryConsumeInventoryItem(int index, byte quantity)
        {
            if (HasStateAuthority == false)
                return false;

            if (quantity == 0)
                return true;

            if (IsValidInventoryIndex(index) == false)
                return false;

            return RemoveInventoryItemInternal(index, quantity);
        }

        public bool TrySetHotbarConfiguration(int index, NetworkString<_64> configurationHash)
        {
            if (HasStateAuthority == false)
                return false;

            if (index < 0 || index >= _hotbar.Length)
                return false;

            Weapon weapon = _hotbar[index];
            if (weapon == null)
                return false;

            weapon.SetConfigurationHash(configurationHash);

            var slot = _hotbarItems[index];

            if (slot.IsEmpty == false)
            {
                var updatedSlot = new InventorySlot(slot.ItemDefinitionId, slot.Quantity, configurationHash);

                if (updatedSlot.Equals(slot) == false)
                {
                    _hotbarItems.Set(index, updatedSlot);
                    NotifyHotbarSlotStackChanged(index);
                }
            }
            else
            {
                int definitionId = weapon.Definition != null ? weapon.Definition.ID : 0;
                byte quantity = 1;

                var updatedSlot = new InventorySlot(definitionId, quantity, configurationHash);
                _hotbarItems.Set(index, updatedSlot);
                NotifyHotbarSlotStackChanged(index);
            }

            return true;
        }

        public InventorySlot GetEquipmentSlot(ESlotCategory category)
        {
            return category switch
            {
                ESlotCategory.Head => _headSlot,
                ESlotCategory.UpperBody => _upperBodySlot,
                ESlotCategory.LowerBody => _lowerBodySlot,
                ESlotCategory.Pipe => _pipeSlot,
                ESlotCategory.Mount => _mountSlot,
                ESlotCategory.Pickaxe => _pickaxeSlot,
                ESlotCategory.WoodAxe => _woodAxeSlot,
                ESlotCategory.FishingPole => _fishingPoleSlot,
                _ => default,
            };
        }

        public byte AddItem(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash = default)
        {
            if (definition == null || quantity == 0)
                return quantity;

            if (HasStateAuthority == false)
                return quantity;

            return AddItemInternal(definition, quantity, configurationHash);
        }

        public void RequestAddItem(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash = default)
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
                IsLowerBodySlotItem(slot) == true || IsPipeSlotItem(slot) == true)
                return false;

            removedSlot = new InventorySlot(slot.ItemDefinitionId, quantity, slot.ConfigurationHash);

            if (slot.Quantity == quantity)
            {
                _items.Set(inventoryIndex, default);
                UpdateWeaponDefinitionMapping(inventoryIndex, default);
            }
            else
            {
                slot.Remove(quantity);
                _items.Set(inventoryIndex, slot);
                UpdateWeaponDefinitionMapping(inventoryIndex, slot);
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

        public void RequestEquipMount(MountDefinition mountDefinition)
        {
            if (mountDefinition == null)
                return;

            if (mountDefinition.SlotCategory != ESlotCategory.Mount)
                return;

            if (HasStateAuthority == true)
            {
                EquipMountInternal(mountDefinition.ID);
            }
            else
            {
                RPC_RequestEquipMount(mountDefinition.ID);
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

            var currentSlot = ResolveWeaponSlotForIndex(_currentWeaponSlot);
            if (currentSlot != null)
            {
                CurrentWeaponHandle = currentSlot.Active;
                CurrentWeaponBaseRotation = currentSlot.BaseRotation;
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
            _pendingRestoreData = null;

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

            _localBagSlots = null;

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
            _pipeSlot = default;
            _localPipeSlot = default;
            _localWoodAxe = null;

            ItemSlotChanged = null;
            HotbarSlotChanged = null;
            GeneralInventorySizeChanged = null;
            _generalInventorySize = BASE_GENERAL_INVENTORY_SLOTS;
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

            if (_slots != null)
            {
                for (int i = 0; i < _slots.Length; ++i)
                {
                    WeaponSlot slot = _slots[i];
                    if (slot == null)
                    {
                        continue;
                    }

                    if (slot.Active != null)
                    {
                        slot.BaseRotation = slot.Active.localRotation;
                    }

                    if (slot.Size != WeaponSize.Unknown)
                    {
                        _weaponSizeToSlotIndex[slot.Size] = i;
                    }
                }
            }

            if (_weaponSizeSlots != null && _slots != null)
            {
                foreach (var sizeSlot in _weaponSizeSlots)
                {
                    int slotIndex = sizeSlot.SlotIndex;
                    if (slotIndex < 0 || slotIndex >= _slots.Length)
                    {
                        continue;
                    }

                    if (_weaponSizeToSlotIndex.ContainsKey(sizeSlot.Size) == true)
                    {
                        continue;
                    }

                    WeaponSlot slot = _slots[slotIndex];
                    if (slot == null)
                    {
                        continue;
                    }

                    _weaponSizeToSlotIndex[sizeSlot.Size] = slotIndex;
                    if (slot.Size == WeaponSize.Unarmed || slot.Size == WeaponSize.Unknown)
                    {
                        slot.Size = sizeSlot.Size;
                    }
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
                slotIndex = FindSlotIndexBySize(size);
                if (slotIndex < 0)
                {
                    slotIndex = GetDefaultSlotIndex(size);
                }
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

        private int FindSlotIndexBySize(WeaponSize size)
        {
            if (_slots == null)
            {
                return -1;
            }

            for (int i = 0; i < _slots.Length; ++i)
            {
                WeaponSlot slot = _slots[i];
                if (slot != null && slot.Size == size)
                {
                    return i;
                }
            }

            return -1;
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
            return ResolveWeaponSlotForIndex(slotIndex, null);
        }

        private WeaponSlot ResolveWeaponSlotForIndex(int slotIndex, Weapon weaponOverride)
        {
            if (_slots == null || _slots.Length == 0)
                return null;

            Weapon weapon = weaponOverride;

            if (weapon == null && slotIndex >= 0 && slotIndex < _hotbar.Length)
            {
                weapon = _hotbar[slotIndex];
            }

            if (weapon != null)
            {
                if (_weaponSizeToSlotIndex.TryGetValue(weapon.Size, out int mappedIndex) == true)
                {
                    if (mappedIndex >= 0 && mappedIndex < _slots.Length)
                    {
                        WeaponSlot mappedSlot = _slots[mappedIndex];
                        if (mappedSlot != null)
                        {
                            return mappedSlot;
                        }
                    }
                }

                for (int i = 0; i < _slots.Length; ++i)
                {
                    WeaponSlot slot = _slots[i];
                    if (slot != null && slot.Size == weapon.Size)
                    {
                        return slot;
                    }
                }
            }

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

        private static string ResolveConfigurationString(NetworkString<_64> configurationHash)
        {
            string value = configurationHash.ToString();
            return string.IsNullOrEmpty(value) == false ? value : null;
        }


        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestBeginInventoryRestore(NetworkString<_32> characterId)
        {
            if (HasStateAuthority == false)
                return;

            string resolvedCharacterId = characterId.ToString();
            if (string.IsNullOrEmpty(resolvedCharacterId) == true)
            {
                resolvedCharacterId = null;
            }

            _pendingRestoreData = new PlayerCharacterInventorySaveData
            {
                CharacterId = resolvedCharacterId,
                InventorySlots = new PlayerInventoryItemData[_items.Length],
                BagSlots = new PlayerInventoryItemData[BAG_SLOT_COUNT],
                HotbarSlots = new PlayerHotbarSlotData[_hotbar.Length],
                PickaxeSlot = default,
                WoodAxeSlot = default,
                FishingPoleSlot = default,
                HeadSlot = default,
                UpperBodySlot = default,
                LowerBodySlot = default,
                PipeSlot = default,
                MountSlot = default,
                Gold = 0,
                CurrentWeaponSlot = 0,
            };
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestSetInventorySlot(byte index, int itemDefinitionId, byte quantity, NetworkString<_64> configurationHash)
        {
            if (HasStateAuthority == false || _pendingRestoreData == null)
                return;

            if (index >= _pendingRestoreData.InventorySlots.Length)
                return;

            _pendingRestoreData.InventorySlots[index] = new PlayerInventoryItemData
            {
                ItemDefinitionId = itemDefinitionId,
                Quantity = quantity,
                ConfigurationHash = ResolveConfigurationString(configurationHash),
            };
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestSetBagSlot(byte index, int itemDefinitionId, byte quantity, NetworkString<_64> configurationHash)
        {
            if (HasStateAuthority == false || _pendingRestoreData == null)
                return;

            if (index >= _pendingRestoreData.BagSlots.Length)
                return;

            _pendingRestoreData.BagSlots[index] = new PlayerInventoryItemData
            {
                ItemDefinitionId = itemDefinitionId,
                Quantity = quantity,
                ConfigurationHash = ResolveConfigurationString(configurationHash),
            };
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestSetHotbarSlot(byte index, int weaponDefinitionId, byte quantity, NetworkString<_64> configurationHash)
        {
            if (HasStateAuthority == false || _pendingRestoreData == null)
                return;

            if (index >= _pendingRestoreData.HotbarSlots.Length)
                return;

            _pendingRestoreData.HotbarSlots[index] = new PlayerHotbarSlotData
            {
                WeaponDefinitionId = weaponDefinitionId,
                Quantity = quantity,
                ConfigurationHash = ResolveConfigurationString(configurationHash),
            };
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestSetSpecialSlot(SpecialRestoreSlot slot, int itemDefinitionId, byte quantity, NetworkString<_64> configurationHash)
        {
            if (HasStateAuthority == false || _pendingRestoreData == null)
                return;

            var slotData = new PlayerInventoryItemData
            {
                ItemDefinitionId = itemDefinitionId,
                Quantity = quantity,
                ConfigurationHash = ResolveConfigurationString(configurationHash),
            };

            switch (slot)
            {
                case SpecialRestoreSlot.Pickaxe:
                    _pendingRestoreData.PickaxeSlot = slotData;
                    break;
                case SpecialRestoreSlot.WoodAxe:
                    _pendingRestoreData.WoodAxeSlot = slotData;
                    break;
                case SpecialRestoreSlot.FishingPole:
                    _pendingRestoreData.FishingPoleSlot = slotData;
                    break;
                case SpecialRestoreSlot.Head:
                    _pendingRestoreData.HeadSlot = slotData;
                    break;
                case SpecialRestoreSlot.UpperBody:
                    _pendingRestoreData.UpperBodySlot = slotData;
                    break;
                case SpecialRestoreSlot.LowerBody:
                    _pendingRestoreData.LowerBodySlot = slotData;
                    break;
                case SpecialRestoreSlot.Pipe:
                    _pendingRestoreData.PipeSlot = slotData;
                    break;
                case SpecialRestoreSlot.Mount:
                    _pendingRestoreData.MountSlot = slotData;
                    break;
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestFinalizeInventoryRestore(byte currentWeaponSlot, int gold)
        {
            if (HasStateAuthority == false || _pendingRestoreData == null)
                return;

            byte resolvedSlot = currentWeaponSlot;
            if (resolvedSlot >= _hotbar.Length)
            {
                resolvedSlot = 0;
            }

            _pendingRestoreData.CurrentWeaponSlot = resolvedSlot;
            _pendingRestoreData.Gold = gold;

            var restoreData = _pendingRestoreData;
            _pendingRestoreData = null;

            ApplySaveData(restoreData);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestAddGold(int amount)
        {
            if (amount <= 0)
                return;

            AddGold(amount);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestAddItem(int itemDefinitionId, byte quantity, NetworkString<_64> configurationHash)
        {
            if (quantity == 0)
                return;

            ItemDefinition definition = ItemDefinition.Get(itemDefinitionId);
            if (definition == null)
                return;

            AddItemInternal(definition, quantity, configurationHash);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestMoveItem(byte fromIndex, byte toIndex)
        {
            MoveItem(fromIndex, toIndex);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestEquipMount(int mountDefinitionId)
        {
            if (HasStateAuthority == false)
                return;

            EquipMountInternal(mountDefinitionId);
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

        

        private byte AddItemInternal(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash)
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
            bool isPipe = slotCategory == ESlotCategory.Pipe;
            bool isBag = slotCategory == ESlotCategory.Bag;
            bool isMount = slotCategory == ESlotCategory.Mount;

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

            if (isPipe == true && remaining > 0)
            {
                remaining = AddToPipeSlot(definition, remaining, configurationHash);
            }

            if (isBag == true && remaining > 0)
            {
                remaining = AddToBagSlots(definition, remaining, configurationHash);
            }

            if (isMount == true && remaining > 0)
            {
                remaining = AddToMountSlot(definition, remaining, configurationHash);
            }

            int generalCapacity = Mathf.Clamp(_generalInventorySize, 0, _items.Length);

            if (isMount == true)
            {
                return remaining;
            }

            for (int i = 0; i < generalCapacity && remaining > 0; i++)
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

            for (int i = 0; i < generalCapacity && remaining > 0; i++)
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
            bool fromFishingPole = fromIndex == FISHING_POLE_SLOT_INDEX;
            bool toFishingPole = toIndex == FISHING_POLE_SLOT_INDEX;
            bool fromHead = fromIndex == HEAD_SLOT_INDEX;
            bool toHead = toIndex == HEAD_SLOT_INDEX;
            bool fromUpperBody = fromIndex == UPPER_BODY_SLOT_INDEX;
            bool toUpperBody = toIndex == UPPER_BODY_SLOT_INDEX;
            bool fromLowerBody = fromIndex == LOWER_BODY_SLOT_INDEX;
            bool toLowerBody = toIndex == LOWER_BODY_SLOT_INDEX;
            bool fromPipe = fromIndex == PIPE_SLOT_INDEX;
            bool toPipe = toIndex == PIPE_SLOT_INDEX;
            bool fromMount = fromIndex == MOUNT_SLOT_INDEX;
            bool toMount = toIndex == MOUNT_SLOT_INDEX;
            bool fromBag = IsBagSlotIndex(fromIndex);
            bool toBag = IsBagSlotIndex(toIndex);
            bool fromSpecial = fromPickaxe || fromWoodAxe || fromFishingPole || fromHead || fromUpperBody || fromLowerBody || fromPipe || fromBag || fromMount;
            bool toSpecial = toPickaxe || toWoodAxe || toFishingPole || toHead || toUpperBody || toLowerBody || toPipe || toBag || toMount;
            bool generalToGeneralTransfer = fromSpecial == false && toSpecial == false;

            if (fromSpecial == false && fromIndex >= _generalInventorySize)
                return;

            if (toSpecial == false && toIndex >= _generalInventorySize)
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

                _items.Set(toIndex, fishingSourceSlot);
                UpdateWeaponDefinitionMapping(toIndex, fishingSourceSlot);

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
                    _items.Set(fromIndex, default);
                    UpdateWeaponDefinitionMapping(fromIndex, default);
                }
                else
                {
                    _items.Set(fromIndex, previousFishingPole);
                    UpdateWeaponDefinitionMapping(fromIndex, previousFishingPole);
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
                _items.Set(toIndex, headSourceSlot);
                UpdateWeaponDefinitionMapping(toIndex, headSourceSlot);

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
                    _items.Set(fromIndex, default);
                    UpdateWeaponDefinitionMapping(fromIndex, default);
                }
                else
                {
                    _items.Set(fromIndex, previousHead);
                    UpdateWeaponDefinitionMapping(fromIndex, previousHead);
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
                _items.Set(toIndex, upperBodySourceSlot);
                UpdateWeaponDefinitionMapping(toIndex, upperBodySourceSlot);

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
                    _items.Set(fromIndex, default);
                    UpdateWeaponDefinitionMapping(fromIndex, default);
                }
                else
                {
                    _items.Set(fromIndex, previousUpperBody);
                    UpdateWeaponDefinitionMapping(fromIndex, previousUpperBody);
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
                _items.Set(toIndex, lowerBodySourceSlot);
                UpdateWeaponDefinitionMapping(toIndex, lowerBodySourceSlot);

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
                    _items.Set(fromIndex, default);
                    UpdateWeaponDefinitionMapping(fromIndex, default);
                }
                else
                {
                    _items.Set(fromIndex, previousLowerBody);
                    UpdateWeaponDefinitionMapping(fromIndex, previousLowerBody);
                }

                RefreshItems();
                return;
            }

            if (fromPipe == true)
            {
                var pipeSourceSlot = _pipeSlot;
                if (pipeSourceSlot.IsEmpty == true)
                    return;
                if (toPipe == true)
                    return;
                var targetSlot = _items[toIndex];
                if (targetSlot.IsEmpty == false && IsPipeSlotItem(targetSlot) == false)
                    return;
                _items.Set(toIndex, pipeSourceSlot);
                UpdateWeaponDefinitionMapping(toIndex, pipeSourceSlot);

                if (targetSlot.IsEmpty == false && IsPipeSlotItem(targetSlot) == true)
                {
                    _pipeSlot = targetSlot;
                }
                else
                {
                    _pipeSlot = default;
                }

                RefreshPipeSlot();
                RefreshItems();
                return;
            }

            if (fromMount == true)
            {
                var mountSourceSlot = _mountSlot;
                if (mountSourceSlot.IsEmpty == true)
                    return;
                if (toMount == true)
                    return;
                var targetSlot = _items[toIndex];
                if (targetSlot.IsEmpty == false && IsMountSlotItem(targetSlot) == false)
                    return;
                _items.Set(toIndex, mountSourceSlot);
                UpdateWeaponDefinitionMapping(toIndex, mountSourceSlot);

                if (targetSlot.IsEmpty == false && IsMountSlotItem(targetSlot) == true)
                {
                    _mountSlot = targetSlot;
                }
                else
                {
                    _mountSlot = default;
                }

                RefreshMountSlot();
                RefreshItems();
                return;
            }

            if (toPipe == true)
            {
                var sourceSlot = _items[fromIndex];
                if (sourceSlot.IsEmpty == true)
                    return;
                if (IsPipeSlotItem(sourceSlot) == false)
                    return;
                var previousPipe = _pipeSlot;
                _pipeSlot = sourceSlot;
                RefreshPipeSlot();

                if (previousPipe.IsEmpty == true)
                {
                    _items.Set(fromIndex, default);
                    UpdateWeaponDefinitionMapping(fromIndex, default);
                }
                else
                {
                    _items.Set(fromIndex, previousPipe);
                    UpdateWeaponDefinitionMapping(fromIndex, previousPipe);
                }

                RefreshItems();
                return;
            }

            if (toMount == true)
            {
                var sourceSlot = _items[fromIndex];
                if (sourceSlot.IsEmpty == true)
                    return;
                if (IsMountSlotItem(sourceSlot) == false)
                    return;
                var previousMount = _mountSlot;
                _mountSlot = sourceSlot;
                RefreshMountSlot();

                if (previousMount.IsEmpty == true)
                {
                    _items.Set(fromIndex, default);
                    UpdateWeaponDefinitionMapping(fromIndex, default);
                }
                else
                {
                    _items.Set(fromIndex, previousMount);
                    UpdateWeaponDefinitionMapping(fromIndex, previousMount);
                }

                RefreshItems();
                return;
            }

            if (fromBag == true)
            {
                if (TryGetBagArrayIndex(fromIndex, out int fromBagIndex) == false)
                    return;

                var bagSourceSlot = _bagSlots[fromBagIndex];
                if (bagSourceSlot.IsEmpty == true)
                    return;

                if (toBag == true)
                {
                    if (TryGetBagArrayIndex(toIndex, out int toBagIndex) == false)
                        return;

                    var targetBagSlot = _bagSlots[toBagIndex];
                    _bagSlots.Set(fromBagIndex, targetBagSlot);
                    _bagSlots.Set(toBagIndex, bagSourceSlot);
                    RefreshBagSlots();
                    return;
                }

                if (toIndex >= _generalInventorySize)
                    return;

                var targetSlot = _items[toIndex];
                if (targetSlot.IsEmpty == false && IsBagSlotItem(targetSlot) == false)
                    return;

                _items.Set(toIndex, bagSourceSlot);
                UpdateWeaponDefinitionMapping(toIndex, bagSourceSlot);

                if (targetSlot.IsEmpty == false)
                {
                    _bagSlots.Set(fromBagIndex, targetSlot);
                }
                else
                {
                    _bagSlots.Set(fromBagIndex, default);
                }

                RefreshBagSlots();
                RefreshItems();
                return;
            }

            if (toBag == true)
            {
                if (fromIndex >= _generalInventorySize)
                    return;

                var sourceSlot = _items[fromIndex];
                if (sourceSlot.IsEmpty == true)
                    return;

                if (IsBagSlotItem(sourceSlot) == false)
                    return;

                if (TryGetBagArrayIndex(toIndex, out int toBagIndex) == false)
                    return;

                var previousBagSlot = _bagSlots[toBagIndex];
                if (previousBagSlot.IsEmpty == false && IsBagSlotItem(previousBagSlot) == false)
                    return;

                _bagSlots.Set(toBagIndex, sourceSlot);

                if (previousBagSlot.IsEmpty == true)
                {
                    _items.Set(fromIndex, default);
                    UpdateWeaponDefinitionMapping(fromIndex, default);
                }
                else
                {
                    _items.Set(fromIndex, previousBagSlot);
                    UpdateWeaponDefinitionMapping(fromIndex, previousBagSlot);
                }

                RefreshBagSlots();
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
                        if (generalToGeneralTransfer == true)
                        {
                            SuppressFeedForSlot(fromIndex);
                            SuppressFeedForSlot(toIndex);
                        }

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

            if (generalToGeneralTransfer == true)
            {
                if (generalSourceSlot.Equals(toSlot) == true)
                    return;

                SuppressFeedForSlot(fromIndex);
                SuppressFeedForSlot(toIndex);
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

            var weaponPrefab = EnsureWeaponPrefabRegistered(definition);
            if (weaponPrefab == null)
                return;

            SuppressFeedForSlot(inventoryIndex);
            if (TryDetachInventorySlot(inventoryIndex, out InventorySlot detachedSlot) == false)
            {
                ClearFeedSuppression(inventoryIndex);
                return;
            }

            var existingWeapon = _hotbar[slot];
            if (existingWeapon != null)
            {
                if (TryStoreWeapon(existingWeapon, slot) == false)
                {
                    RestoreInventorySlot(inventoryIndex, detachedSlot);
                    return;
                }
            }

            var spawnedWeapon = Runner.Spawn(weaponPrefab, inputAuthority: Object.InputAuthority);
            if (spawnedWeapon == null)
            {
                RestoreInventorySlot(inventoryIndex, detachedSlot);
                return;
            }

            spawnedWeapon.SetConfigurationHash(detachedSlot.ConfigurationHash);
            AddWeapon(spawnedWeapon, slot);

            _hotbarItems.Set(slot, detachedSlot);
            NotifyHotbarSlotStackChanged(slot);

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

            var inventorySlot = _hotbarItems[slot];
            if (inventorySlot.IsEmpty == true)
            {
                inventorySlot = new InventorySlot(definition.ID, 1, weapon.ConfigurationHash);
            }

            _items.Set(inventoryIndex, inventorySlot);
            UpdateWeaponDefinitionMapping(inventoryIndex, inventorySlot);
            RefreshItems();

            _hotbarItems.Set(slot, default);

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

            var fromHotbarItem = _hotbarItems[fromSlot];
            var toHotbarItem = _hotbarItems[toSlot];
            _hotbarItems.Set(fromSlot, toHotbarItem);
            _hotbarItems.Set(toSlot, fromHotbarItem);

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

            NotifyHotbarSlotStackChanged(fromSlot);
            NotifyHotbarSlotStackChanged(toSlot);

            NotifyHotbarSlotChanged(fromSlot);
            NotifyHotbarSlotChanged(toSlot);

            RefreshWeapons();
        }

        internal bool CanSwapHotbarSlots(int fromIndex, int toIndex)
        {
            int fromSlot = fromIndex + 1;
            int toSlot = toIndex + 1;

            if (fromSlot < 0 || fromSlot >= _hotbar.Length)
                return false;

            if (toSlot < 0 || toSlot >= _hotbar.Length)
                return false;

            if (IsValidHotbarAssignmentSlot(fromSlot) == false || IsValidHotbarAssignmentSlot(toSlot) == false)
                return false;

            if (fromSlot == toSlot)
                return false;

            var fromWeapon = _hotbar[fromSlot];
            var toWeapon = _hotbar[toSlot];

            if (CanAssignWeaponToHotbarSlot(fromWeapon, toSlot) == false)
                return false;

            if (CanAssignWeaponToHotbarSlot(toWeapon, fromSlot) == false)
                return false;

            return true;
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

            if (index == PIPE_SLOT_INDEX)
            {
                var pipeSlotData = _pipeSlot;
                if (pipeSlotData.IsEmpty == true)
                    return;
                var pipeDefinition = pipeSlotData.GetDefinition();
                if (pipeDefinition == null)
                    return;
                byte pipeQuantity = pipeSlotData.Quantity;
                if (pipeQuantity == 0)
                    return;
                _pipeSlot = default;
                RefreshPipeSlot();

                SpawnInventoryItemPickup(pipeDefinition, pipeQuantity, pipeSlotData.ConfigurationHash);
                return;
            }

            if (TryGetBagArrayIndex(index, out int bagIndex) == true)
            {
                var bagSlotData = _bagSlots[bagIndex];
                if (bagSlotData.IsEmpty == true)
                    return;
                var bagDefinition = bagSlotData.GetDefinition();
                if (bagDefinition == null)
                    return;
                byte bagQuantity = bagSlotData.Quantity;
                if (bagQuantity == 0)
                    return;

                _bagSlots.Set(bagIndex, default);
                RefreshBagSlots();

                SpawnInventoryItemPickup(bagDefinition, bagQuantity, bagSlotData.ConfigurationHash);
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

            if (index == PIPE_SLOT_INDEX)
            {
                if (quantity == 0)
                    return false;

                var pipeSlotData = _pipeSlot;
                if (pipeSlotData.IsEmpty == true)
                    return false;

                if (pipeSlotData.Quantity <= quantity)
                {
                    _pipeSlot = default;
                }
                else
                {
                    pipeSlotData.Remove(quantity);
                    _pipeSlot = pipeSlotData;
                }

                RefreshPipeSlot();
                return true;
            }

            if (TryGetHotbarSlotFromInventoryIndex(index, out int hotbarSlot) == true)
            {
                if (quantity == 0)
                    return false;

                if (hotbarSlot <= 0 || hotbarSlot >= _hotbarItems.Length)
                    return false;

                var hotbarItem = _hotbarItems[hotbarSlot];
                if (hotbarItem.IsEmpty == true)
                    return false;

                if (hotbarItem.Quantity < quantity)
                    return false;

                bool removedAll = hotbarItem.Quantity == quantity;
                if (removedAll == true)
                {
                    _hotbarItems.Set(hotbarSlot, default);
                }
                else
                {
                    hotbarItem.Remove(quantity);
                    _hotbarItems.Set(hotbarSlot, hotbarItem);
                }

                if (removedAll == true)
                {
                    if (hotbarSlot == _currentWeaponSlot)
                    {
                        byte bestWeaponSlot = _previousWeaponSlot;
                        if (bestWeaponSlot == 0 || bestWeaponSlot == _currentWeaponSlot)
                        {
                            bestWeaponSlot = FindBestWeaponSlot(_currentWeaponSlot);
                        }

                        SetCurrentWeapon(bestWeaponSlot);
                        ArmCurrentWeapon();
                    }
                    else if (_previousWeaponSlot == hotbarSlot)
                    {
                        _previousWeaponSlot = 0;
                    }

                    var weapon = _hotbar[hotbarSlot];
                    if (weapon != null)
                    {
                        RemoveWeapon(hotbarSlot);

                        if (weapon.Object != null)
                        {
                            Runner.Despawn(weapon.Object);
                        }
                    }
                }
                else
                {
                    NotifyHotbarSlotStackChanged(hotbarSlot);
                }

                return true;
            }

            if (IsGeneralInventoryIndex(index) == false)
                return false;

            var inventorySlot = _items[index];
            if (inventorySlot.IsEmpty == true)
                return false;

            bool removedSpecialItem = IsPickaxeSlotItem(inventorySlot) || IsWoodAxeSlotItem(inventorySlot) ||
                                      IsHeadSlotItem(inventorySlot) || IsUpperBodySlotItem(inventorySlot) ||
                                      IsLowerBodySlotItem(inventorySlot) || IsPipeSlotItem(inventorySlot);

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

        private bool TryDetachInventorySlot(int index, out InventorySlot slot)
        {
            slot = default;

            if (IsGeneralInventoryIndex(index) == false)
                return false;

            var currentSlot = _items[index];
            if (currentSlot.IsEmpty == true)
                return false;

            slot = currentSlot;
            _items.Set(index, default);
            UpdateWeaponDefinitionMapping(index, default);
            RefreshItems();

            return true;
        }

        private void RestoreInventorySlot(int index, InventorySlot slot)
        {
            if (IsGeneralInventoryIndex(index) == false)
                return;

            SuppressFeedForSlot(index);
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

            var storedSlot = _hotbarItems[sourceSlot];
            if (storedSlot.IsEmpty == true)
            {
                storedSlot = new InventorySlot(definition.ID, 1, weapon.ConfigurationHash);
            }

            int emptySlot = FindEmptyInventorySlot();
            if (emptySlot < 0)
                return false;

            SuppressFeedForSlot(emptySlot);
            _items.Set(emptySlot, storedSlot);
            UpdateWeaponDefinitionMapping(emptySlot, storedSlot);
            RefreshItems();

            _hotbarItems.Set(sourceSlot, default);

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
            RefreshFishingPoleSlot();
            RefreshHeadSlot();
            RefreshUpperBodySlot();
            RefreshLowerBodySlot();
            RefreshPipeSlot();
            RefreshBagSlots();
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
                WeaponSlot targetSlot = ResolveWeaponSlotForIndex(i, weapon);
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

                WeaponSlot currentSlot = ResolveWeaponSlotForIndex(_currentWeaponSlot, CurrentWeapon);
                if (currentSlot != null)
                {
                    CurrentWeaponHandle = currentSlot.Active;
                    CurrentWeaponBaseRotation = currentSlot.BaseRotation;
                }

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

        private void NotifyHotbarSlotStackChanged(int slot)
        {
            if (_lastHotbarWeapons != null && slot >= 0 && slot < _lastHotbarWeapons.Length)
            {
                _lastHotbarWeapons[slot] = null;
            }

            NotifyHotbarSlotChanged(slot);
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
            var slotData = _hotbarItems[weaponSlot];
            byte quantity = slotData.Quantity;
            NetworkString<_64> dropConfiguration = slotData.ConfigurationHash;

            if (quantity == 0)
            {
                quantity = 1;
                dropConfiguration = weapon.ConfigurationHash;
            }

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
            else if (_previousWeaponSlot == weaponSlot)
            {
                _previousWeaponSlot = 0;
            }

            _hotbarItems.Set(weaponSlot, default);

            RemoveWeapon(weaponSlot);

            if (definition != null)
            {
                SpawnInventoryItemPickup(definition, quantity, dropConfiguration);
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
            int generalCapacity = Mathf.Clamp(_generalInventorySize, 0, _items.Length);
            for (int i = 0; i < generalCapacity; i++)
            {
                if (_items[i].IsEmpty == true)
                    return i;
            }

            return -1;
        }

        private static int GetHotbarInventoryIndex(int slot)
        {
            return HOTBAR_INVENTORY_INDEX_BASE - slot;
        }

        private static bool TryGetHotbarSlotFromInventoryIndex(int index, out int slot)
        {
            int offset = HOTBAR_INVENTORY_INDEX_BASE - index;
            if (offset >= 0 && offset < HOTBAR_CAPACITY)
            {
                slot = offset;
                return true;
            }

            slot = -1;
            return false;
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

        private byte AddToPickaxeSlot(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash)
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

        private byte AddToWoodAxeSlot(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash)
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

        private byte AddToFishingPoleSlot(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash)
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

        private byte AddToHeadSlot(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash)
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

        private byte AddToUpperBodySlot(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash)
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

        private byte AddToLowerBodySlot(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash)
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

        private byte AddToPipeSlot(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash)
        {
            if (quantity == 0)
                return 0;

            var slot = _pipeSlot;

            if (slot.IsEmpty == false && IsPipeSlotItem(slot) == false)
            {
                _pipeSlot = default;
                RefreshPipeSlot();
                slot = default;
            }

            int clampedMaxStack = Mathf.Clamp((int)ItemDefinition.GetMaxStack(definition.ID), 1, byte.MaxValue);

            if (slot.IsEmpty == true)
            {
                byte addAmount = (byte)Mathf.Min(quantity, clampedMaxStack);
                if (addAmount > 0)
                {
                    slot = new InventorySlot(definition.ID, addAmount, configurationHash);
                    _pipeSlot = slot;
                    RefreshPipeSlot();
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
            _pipeSlot = slot;
            RefreshPipeSlot();

            return (byte)(quantity - space);
        }

        private byte AddToMountSlot(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash)
        {
            if (quantity == 0)
                return 0;

            var slot = _mountSlot;

            if (slot.IsEmpty == false && IsMountSlotItem(slot) == false)
            {
                _mountSlot = default;
                RefreshMountSlot();
                slot = default;
            }

            int clampedMaxStack = 1;

            if (slot.IsEmpty == true)
            {
                byte addAmount = (byte)Mathf.Min(quantity, clampedMaxStack);
                if (addAmount > 0)
                {
                    slot = new InventorySlot(definition.ID, addAmount, configurationHash);
                    _mountSlot = slot;
                    RefreshMountSlot();
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
            _mountSlot = slot;
            RefreshMountSlot();

            return (byte)(quantity - space);
        }

        private void EquipMountInternal(int mountDefinitionId)
        {
            var mountDefinition = ItemDefinition.Get(mountDefinitionId) as MountDefinition;
            if (mountDefinition == null)
                return;

            if (mountDefinition.SlotCategory != ESlotCategory.Mount)
                return;

            var newSlot = new InventorySlot(mountDefinitionId, 1, default);
            if (_mountSlot.Equals(newSlot))
                return;

            _mountSlot = newSlot;
            RefreshMountSlot();
        }

        private byte AddToBagSlots(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash)
        {
            if (quantity == 0)
                return 0;

            int clampedMaxStack = Mathf.Clamp((int)ItemDefinition.GetMaxStack(definition.ID), 1, byte.MaxValue);

            for (int i = 0; i < BAG_SLOT_COUNT && quantity > 0; i++)
            {
                var slot = _bagSlots[i];
                if (slot.IsEmpty == true)
                    continue;

                if (IsBagSlotItem(slot) == false)
                    continue;

                if (slot.ItemDefinitionId != definition.ID)
                    continue;

                if (slot.ConfigurationHash != configurationHash)
                    continue;

                if (slot.Quantity >= clampedMaxStack)
                    continue;

                byte space = (byte)Mathf.Min(clampedMaxStack - slot.Quantity, quantity);
                if (space == 0)
                    continue;

                slot.Add(space);
                _bagSlots.Set(i, slot);
                quantity -= space;
            }

            for (int i = 0; i < BAG_SLOT_COUNT && quantity > 0; i++)
            {
                var slot = _bagSlots[i];
                if (slot.IsEmpty == false)
                    continue;

                byte addAmount = (byte)Mathf.Min(clampedMaxStack, quantity);
                if (addAmount == 0)
                    continue;

                slot = new InventorySlot(definition.ID, addAmount, configurationHash);
                _bagSlots.Set(i, slot);
                quantity -= addAmount;
            }

            return quantity;
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

        private void RefreshPipeSlot()
        {
            var slot = _pipeSlot;
            if (_localPipeSlot.Equals(slot) == false)
            {
                _localPipeSlot = slot;
                ItemSlotChanged?.Invoke(PIPE_SLOT_INDEX, slot);
            }
        }

        private void RefreshMountSlot()
        {
            var slot = _mountSlot;
            if (_localMountSlot.Equals(slot) == false)
            {
                _localMountSlot = slot;
                ItemSlotChanged?.Invoke(MOUNT_SLOT_INDEX, slot);
            }
        }

        private void RefreshBagSlots()
        {
            bool sizeDirty = false;

            if (_localBagSlots == null || _localBagSlots.Length != BAG_SLOT_COUNT)
            {
                _localBagSlots = new InventorySlot[BAG_SLOT_COUNT];
                sizeDirty = true;
            }

            for (int i = 0; i < BAG_SLOT_COUNT; i++)
            {
                var slot = _bagSlots[i];
                if (_localBagSlots[i].Equals(slot) == false)
                {
                    _localBagSlots[i] = slot;
                    ItemSlotChanged?.Invoke(GetBagSlotIndex(i), slot);
                    sizeDirty = true;
                }
            }

            if (sizeDirty == true)
            {
                RecalculateGeneralInventorySize();
            }
        }

        private void RecalculateGeneralInventorySize()
        {
            int size = BASE_GENERAL_INVENTORY_SLOTS;

            for (int i = 0; i < BAG_SLOT_COUNT; i++)
            {
                var slot = _bagSlots[i];
                if (slot.IsEmpty == true)
                    continue;

                if (slot.GetDefinition() is BagDefinition bagDefinition)
                {
                    size += Mathf.Max(0, bagDefinition.Slots);
                }
            }

            size = Mathf.Clamp(size, BASE_GENERAL_INVENTORY_SLOTS, _items.Length);

            if (size == _generalInventorySize)
                return;

            int previousSize = _generalInventorySize;
            _generalInventorySize = size;

            if (HasStateAuthority == true && size < previousSize)
            {
                EnforceGeneralInventoryCapacity();
            }

            GeneralInventorySizeChanged?.Invoke(_generalInventorySize);
        }

        private void EnforceGeneralInventoryCapacity()
        {
            int capacity = Mathf.Clamp(_generalInventorySize, 0, _items.Length);

            for (int i = capacity; i < _items.Length; i++)
            {
                var slot = _items[i];
                if (slot.IsEmpty == true)
                    continue;

                byte remainingQuantity = slot.Quantity;
                var definition = slot.GetDefinition();

                if (definition != null)
                {
                    ushort maxStack = ItemDefinition.GetMaxStack(slot.ItemDefinitionId);
                    if (maxStack == 0)
                    {
                        maxStack = 1;
                    }

                    int clampedMaxStack = Mathf.Clamp(maxStack, 1, byte.MaxValue);

                    // Try to stack into existing slots
                    for (int j = 0; j < capacity && remainingQuantity > 0; j++)
                    {
                        var targetSlot = _items[j];
                        if (targetSlot.IsEmpty == true)
                            continue;

                        if (targetSlot.ItemDefinitionId != slot.ItemDefinitionId)
                            continue;

                        if (targetSlot.ConfigurationHash != slot.ConfigurationHash)
                            continue;

                        if (targetSlot.Quantity >= clampedMaxStack)
                            continue;

                        byte space = (byte)Mathf.Min(clampedMaxStack - targetSlot.Quantity, remainingQuantity);
                        if (space == 0)
                            continue;

                        targetSlot.Add(space);
                        _items.Set(j, targetSlot);
                        UpdateWeaponDefinitionMapping(j, targetSlot);
                        remainingQuantity -= space;
                    }

                    // Try to move to empty slots
                    while (remainingQuantity > 0)
                    {
                        int emptyIndex = FindFirstEmptyInventorySlot(capacity);
                        if (emptyIndex < 0)
                            break;

                        byte addAmount = (byte)Mathf.Min(clampedMaxStack, remainingQuantity);
                        var newSlot = new InventorySlot(slot.ItemDefinitionId, addAmount, slot.ConfigurationHash);
                        _items.Set(emptyIndex, newSlot);
                        UpdateWeaponDefinitionMapping(emptyIndex, newSlot);
                        remainingQuantity -= addAmount;
                    }
                }

                if (remainingQuantity > 0 && definition != null)
                {
                    SpawnInventoryItemPickup(definition, remainingQuantity, slot.ConfigurationHash);
                }

                _items.Set(i, default);
                UpdateWeaponDefinitionMapping(i, default);
            }

            RefreshItems();
        }

        private int FindFirstEmptyInventorySlot(int limit)
        {
            int length = Mathf.Clamp(limit, 0, _items.Length);
            for (int i = 0; i < length; i++)
            {
                if (_items[i].IsEmpty == true)
                    return i;
            }

            return -1;
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

            _pickaxeSlot = default;
            RefreshPickaxeSlot();
            DespawnPickaxe();
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

            _woodAxeSlot = default;
            RefreshWoodAxeSlot();
            DespawnWoodAxe();
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
                        WeaponSlot baseSlot = ResolveWeaponSlotForIndex(HOTBAR_FISHING_POLE_SLOT, _localFishingPole);
                        if (baseSlot == null)
                        {
                            int targetSlotIndex = HOTBAR_FISHING_POLE_SLOT < slotsLength ? HOTBAR_FISHING_POLE_SLOT : 0;
                            baseSlot = targetSlotIndex < _slots.Length ? _slots[targetSlotIndex] : null;
                        }

                        if (baseSlot != null)
                        {
                            equippedParent ??= baseSlot.Active;
                            unequippedParent ??= baseSlot.Inactive;
                        }
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

        private void HandleReelingMinigameResultInternal(bool wasSuccessful)
        {
            var fishingPole = _fishingPole ?? _localFishingPole;

            if (fishingPole == null)
                return;

            if (wasSuccessful == true)
            {
                fishingPole.EnterCatchPhase();
            }
            else
            {
                fishingPole.HandleReelingFailed();
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

        private bool HasAnyPickaxe()
        {
            if (IsPickaxeSlotItem(_pickaxeSlot) == true)
                return true;

            int generalCapacity = Mathf.Clamp(_generalInventorySize, 0, _items.Length);
            for (int i = 0; i < generalCapacity; i++)
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

            int generalCapacity = Mathf.Clamp(_generalInventorySize, 0, _items.Length);
            for (int i = 0; i < generalCapacity; i++)
            {
                if (IsWoodAxeSlotItem(_items[i]) == true)
                    return true;
            }

            return false;
        }

        private int FindPickaxeInInventory()
        {
            int generalCapacity = Mathf.Clamp(_generalInventorySize, 0, _items.Length);
            for (int i = 0; i < generalCapacity; i++)
            {
                if (IsPickaxeSlotItem(_items[i]) == true)
                    return i;
            }

            return -1;
        }

        private int FindWoodAxeInInventory()
        {
            int generalCapacity = Mathf.Clamp(_generalInventorySize, 0, _items.Length);
            for (int i = 0; i < generalCapacity; i++)
            {
                if (IsWoodAxeSlotItem(_items[i]) == true)
                    return i;
            }

            return -1;
        }

        private bool IsGeneralInventoryIndex(int index)
        {
            return index >= 0 && index < _generalInventorySize;
        }

        private bool IsValidInventoryIndex(int index)
        {
            if (IsGeneralInventoryIndex(index) == true)
                return true;

            if (index == PICKAXE_SLOT_INDEX || index == WOOD_AXE_SLOT_INDEX || index == FISHING_POLE_SLOT_INDEX ||
                index == HEAD_SLOT_INDEX || index == UPPER_BODY_SLOT_INDEX || index == LOWER_BODY_SLOT_INDEX || index == PIPE_SLOT_INDEX || index == MOUNT_SLOT_INDEX)
                return true;

            if (TryGetHotbarSlotFromInventoryIndex(index, out int hotbarSlot) == true)
                return hotbarSlot > 0 && hotbarSlot < _hotbar.Length;

            return IsBagSlotIndex(index);
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

        private static bool IsPipeSlotItem(InventorySlot slot)
        {
            return HasSlotCategory(slot, ESlotCategory.Pipe);
        }

        private static bool IsMountSlotItem(InventorySlot slot)
        {
            return HasSlotCategory(slot, ESlotCategory.Mount);
        }

        private static bool IsBagSlotItem(InventorySlot slot)
        {
            return HasSlotCategory(slot, ESlotCategory.Bag);
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

        private static bool TryGetBagArrayIndex(int slotIndex, out int bagIndex)
        {
            switch (slotIndex)
            {
                case BAG_SLOT_1_INDEX:
                    bagIndex = 0;
                    return true;
                case BAG_SLOT_2_INDEX:
                    bagIndex = 1;
                    return true;
                case BAG_SLOT_3_INDEX:
                    bagIndex = 2;
                    return true;
                case BAG_SLOT_4_INDEX:
                    bagIndex = 3;
                    return true;
                case BAG_SLOT_5_INDEX:
                    bagIndex = 4;
                    return true;
                default:
                    bagIndex = -1;
                    return false;
            }
        }

        public static int GetBagSlotIndex(int bagIndex)
        {
            if (bagIndex < 0 || bagIndex >= _bagSlotIndices.Length)
                return -1;

            return _bagSlotIndices[bagIndex];
        }

        public static bool IsBagSlotIndex(int slotIndex)
        {
            return TryGetBagArrayIndex(slotIndex, out _);
        }

        private void SpawnInventoryItemPickup(ItemDefinition definition, byte quantity, NetworkString<_64> configurationHash = default)
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

            float travelDuration = 0f;
            float arcHeight = 0f;
            Vector3 impulseDirection = (forward + Vector3.up * 0.5f).normalized;
            if (impulseDirection.sqrMagnitude < 0.0001f)
            {
                impulseDirection = Vector3.up;
            }

            float groundClearance = GetPickupGroundClearance();
            Vector3 finalPosition = CalculateItemDropLandingPosition(spawnPosition, impulseDirection * _itemDropImpulse, groundClearance, out travelDuration, out arcHeight);
            travelDuration = Mathf.Max(travelDuration, 0.2f);
            arcHeight = Mathf.Max(0f, arcHeight);

            var provider = Runner.Spawn(_inventoryItemPickupPrefab, finalPosition, rotation);
            if (provider == null)
                return;

            provider.Initialize(definition, quantity, configurationHash);
            provider.ConfigureSpawnAnimation(spawnPosition, travelDuration, arcHeight);
        }

        private Vector3 CalculateItemDropLandingPosition(Vector3 startPosition, Vector3 initialVelocity, float groundClearance, out float travelDuration, out float arcHeight)
        {
            PhysicsScene physicsScene = Runner != null && Runner.IsRunning == true ? Runner.GetPhysicsScene() : Physics.defaultPhysicsScene;
            Vector3 gravity = Physics.gravity;
            const float simulationStep = 0.05f;
            const float maxSimulationTime = 3f;
            Vector3 position = startPosition;
            Vector3 velocity = initialVelocity;

            travelDuration = 0f;
            arcHeight = 0f;

            float highestPoint = startPosition.y;
            RaycastHit hitInfo;

            ObjectLayer.EnsureInitialized();
            int excludedLayers = 0;
            if (ObjectLayer.Agent >= 0)
            {
                excludedLayers |= 1 << ObjectLayer.Agent;
            }
            if (ObjectLayer.AgentKCC >= 0)
            {
                excludedLayers |= 1 << ObjectLayer.AgentKCC;
            }
            if (ObjectLayer.Target >= 0)
            {
                excludedLayers |= 1 << ObjectLayer.Target;
            }
            if (ObjectLayer.Projectile >= 0)
            {
                excludedLayers |= 1 << ObjectLayer.Projectile;
            }
            if (ObjectLayer.Interaction >= 0)
            {
                excludedLayers |= 1 << ObjectLayer.Interaction;
            }
            if (ObjectLayer.Pickup >= 0)
            {
                excludedLayers |= 1 << ObjectLayer.Pickup;
            }

            int raycastMask = ~excludedLayers;

            if (velocity.sqrMagnitude <= 0f)
            {
                velocity = Vector3.zero;
            }

            if (physicsScene.Raycast(startPosition + Vector3.up * 0.1f, Vector3.down, out hitInfo, 5f, raycastMask, QueryTriggerInteraction.Ignore) == true)
            {
                if ((hitInfo.point - startPosition).sqrMagnitude <= 0.01f)
                {
                    arcHeight = 0f;
                    return hitInfo.point + Vector3.up * (groundClearance + 0.01f);
                }
            }

            for (float elapsed = 0f; elapsed < maxSimulationTime; elapsed += simulationStep)
            {
                Vector3 displacement = velocity * simulationStep + 0.5f * gravity * (simulationStep * simulationStep);
                Vector3 nextPosition = position + displacement;
                Vector3 direction = nextPosition - position;
                float distance = direction.magnitude;

                if (distance > 0f && physicsScene.Raycast(position, direction.normalized, out hitInfo, distance, raycastMask, QueryTriggerInteraction.Ignore) == true)
                {
                    float distanceFraction = distance > 0f ? hitInfo.distance / distance : 1f;
                    travelDuration += simulationStep * Mathf.Clamp01(distanceFraction);
                    arcHeight = Mathf.Max(0f, highestPoint - hitInfo.point.y);
                    return hitInfo.point + Vector3.up * (groundClearance + 0.01f);
                }

                position = nextPosition;
                velocity += gravity * simulationStep;
                travelDuration += simulationStep;
                highestPoint = Mathf.Max(highestPoint, position.y);

                if (physicsScene.Raycast(position + Vector3.up * 0.1f, Vector3.down, out hitInfo, 5f, raycastMask, QueryTriggerInteraction.Ignore) == true)
                {
                    arcHeight = Mathf.Max(0f, highestPoint - hitInfo.point.y);
                    return hitInfo.point + Vector3.up * (groundClearance + 0.01f);
                }
            }

            if (physicsScene.Raycast(position + Vector3.up * 0.1f, Vector3.down, out hitInfo, 50f, raycastMask, QueryTriggerInteraction.Ignore) == true)
            {
                arcHeight = Mathf.Max(0f, highestPoint - hitInfo.point.y);
                return hitInfo.point + Vector3.up * (groundClearance + 0.01f);
            }

            arcHeight = Mathf.Max(0f, highestPoint - position.y);
            return position + Vector3.up * (groundClearance + 0.01f);
        }

        private float GetPickupGroundClearance()
        {
            if (_inventoryItemPickupPrefab != null)
            {
                Collider collider = _inventoryItemPickupPrefab.Collider;
                if (collider != null)
                {
                    Bounds bounds = collider.bounds;
                    float centerOffset = bounds.center.y - collider.transform.position.y;
                    float clearance = bounds.extents.y - centerOffset;
                    return Mathf.Max(0f, clearance);
                }
            }

            return 0.5f;
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
        WeaponSlot slot = ResolveWeaponSlotForIndex(targetSlot, weapon);
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