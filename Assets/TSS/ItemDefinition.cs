using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace TSS.Data
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "TSS/Data Definitions/Weapon")]
    public class ItemDefinition : InventoryItem
    {
        [SerializeField] private string displayName;
        [FormerlySerializedAs("icon")]
        [FormerlySerializedAs("_iconTexture")]
        [FormerlySerializedAs("_iconSprite")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private ushort maxStack = 100;
        [SerializeField] private EItemRarity _itemRarity = EItemRarity.Common;
        [SerializeField] private string _shortCode;
        [SerializeField] private ESlotCategory _slotCategory = ESlotCategory.General;

        private static Dictionary<int, ItemDefinition> _map;
        private static readonly SharedStatic<NativeHashMap<int, ushort>> _maxStacks =
            SharedStatic<NativeHashMap<int, ushort>>.GetOrCreate<MaxStacksContext>();

        private struct MaxStacksContext
        {
        }

        public override string Name => displayName;
        public override Sprite Icon => _icon;
        public virtual ushort MaxStack => maxStack;
        public virtual ESlotCategory SlotCategory => _slotCategory;
        public EItemRarity ItemRarity => _itemRarity;

        public static ushort GetMaxStack(int id)
        {
            ref var maxStacks = ref _maxStacks.Data;
            if (!maxStacks.IsCreated) return 0;
            return maxStacks.TryGetValue(id, out var max) ? max : (ushort)0;
        }

        public static ItemDefinition Get(int id)
        {
            if (_map == null)
            {
                LoadAll();
            }

            _map.TryGetValue(id, out var def);
            return def;
        }

        public static void LoadAll()
        {
            _map = new Dictionary<int, ItemDefinition>();

            ref var maxStacks = ref _maxStacks.Data;

            if (maxStacks.IsCreated)
            {
                maxStacks.Dispose();
            }

            var defs = Resources.LoadAll<ItemDefinition>(string.Empty);
            maxStacks = new NativeHashMap<int, ushort>(defs.Length, Allocator.Persistent);

            foreach (var def in defs)
            {
                if (def == null)
                {
                    continue;
                }

                _map[def.ID] = def;
                maxStacks.TryAdd(def.ID, def.MaxStack);
            }

            Debug.Log($"Total items in registry: {_map.Count}");
        }

        public ItemRarityData GetRarityResource()
        {
            var rarityResources = ItemRarityResourcesDefinition.Instance;

            if (rarityResources == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"Unable to load {nameof(ItemRarityResourcesDefinition)} when requesting rarity for {name}.");
#endif
                return default;
            }

            if (rarityResources.TryGetData(_itemRarity, out var rarityData))
            {
                return rarityData;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"No rarity data configured for {_itemRarity} in {nameof(ItemRarityResourcesDefinition)}.");
#endif

            return default;
        }
    }
}
