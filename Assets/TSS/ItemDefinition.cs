using System.Collections.Generic;
using System;
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
        [SerializeField] private Texture2D _iconTexture;
        [SerializeField] private Sprite _iconSprite;
        [SerializeField] private ushort maxStack = 100;

        [NonSerialized]
        private Sprite _generatedSprite;

        private static Dictionary<int, ItemDefinition> _map;
        private static readonly SharedStatic<NativeHashMap<int, ushort>> _maxStacks =
            SharedStatic<NativeHashMap<int, ushort>>.GetOrCreate<MaxStacksContext>();

        private struct MaxStacksContext
        {
        }

        public override string Name => displayName;
        public override Texture2D Icon => _iconSprite != null ? _iconSprite.texture : _iconTexture;
        public virtual ushort MaxStack => maxStack;
        public Sprite IconSprite => _iconSprite != null ? _iconSprite : GetOrCreateSprite();

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

        private Sprite GetOrCreateSprite()
        {
            if (_iconSprite != null)
            {
                return _iconSprite;
            }

            if (_iconTexture == null)
            {
                return null;
            }

            if (_generatedSprite != null)
            {
                return _generatedSprite;
            }

            _generatedSprite = Sprite.Create(
                _iconTexture,
                new Rect(0f, 0f, _iconTexture.width, _iconTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            _generatedSprite.name = $"{name}_Icon";
            return _generatedSprite;
        }
    }
}
