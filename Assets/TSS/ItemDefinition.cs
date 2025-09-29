using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace TSS.Data
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "TSS/Data Definitions/Weapon")]
    public class ItemDefinition : DataDefinition
    {
        [SerializeField] private string displayName;
        [SerializeField] private Texture2D icon;
        [SerializeField] private ushort maxStack = 100;
        public GameObject WorldVisuals; // prefab for world/item pickup visuals
        
        static Dictionary<int, ItemDefinition> _map;
        static readonly SharedStatic<NativeHashMap<int, ushort>> _maxStacks =
            SharedStatic<NativeHashMap<int, ushort>>.GetOrCreate<MaxStacksContext>();
        struct MaxStacksContext
        {
        }
        public override string Name => displayName;
        public override Texture2D Icon => icon;
        public ushort MaxStack => maxStack;
        
        
        public static ushort GetMaxStack(int id)
        {
            ref var maxStacks = ref _maxStacks.Data;
            if (!maxStacks.IsCreated) return 0;
            return maxStacks.TryGetValue(id, out var max) ? max : (ushort)0;
        }
        public static ItemDefinition Get(int id)
        {
            if (_map == null)
                LoadAll();
            _map.TryGetValue(id, out var def);
            return def;
        }
        public static void LoadAll()
        {
            _map = new Dictionary<int, ItemDefinition>();

            ref var maxStacks = ref _maxStacks.Data;

            // Dispose previous hash map if reloading.
            if (maxStacks.IsCreated)
                maxStacks.Dispose();

            var defs = Resources.LoadAll<ItemDefinition>("");
            maxStacks = new NativeHashMap<int, ushort>(defs.Length, Allocator.Persistent);

            foreach (var def in defs)
            {
                if (def == null) continue;
                _map[def.ID] = def;
                maxStacks.TryAdd(def.ID, def.MaxStack);
            }
            
            Debug.Log($"Total items in registry: {_map.Count}");
        }
    }
}