using System;
using System.Collections.Generic;
using TSS.Data;
using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "RecipeDefinition", menuName = "TSS/Data Definitions/Recipe")]
    public class RecipeDefinition : DataDefinition
    {
        [Header("Display")]
        [SerializeField]
        private string _displayName;
        [SerializeField]
        private Texture2D _iconTexture;
        [SerializeField]
        private Sprite _iconSprite;

        [Header("Crafting")]
        [SerializeField]
        private ItemQuantity[] _inputs;
        [SerializeField]
        private ItemQuantity[] _outputs;
        [SerializeField, Min(0f)]
        private float _craftingTime = 0f;
        [SerializeField]
        private Professions.ProfessionIndex _requiredProfession = Professions.ProfessionIndex.Mining;
        [SerializeField, Min(0)]
        private int _minimumProfessionLevel = Professions.MinLevel;

        [NonSerialized]
        private Sprite _generatedSprite;

        public override string Name => _displayName;
        public override Texture2D Icon => _iconSprite != null ? _iconSprite.texture : _iconTexture;
        public Sprite IconSprite => _iconSprite != null ? _iconSprite : GetOrCreateSprite();

        public IReadOnlyList<ItemQuantity> Inputs => _inputs ?? Array.Empty<ItemQuantity>();
        public IReadOnlyList<ItemQuantity> Outputs => _outputs ?? Array.Empty<ItemQuantity>();
        public float CraftingTime => Mathf.Max(0f, _craftingTime);
        public Professions.ProfessionIndex RequiredProfession => _requiredProfession;
        public int MinimumProfessionLevel => Mathf.Max(0, _minimumProfessionLevel);
        public bool HasProfessionRequirement => MinimumProfessionLevel > 0;

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

        [Serializable]
        public struct ItemQuantity
        {
            [SerializeField]
            private ItemDefinition _item;
            [SerializeField, Min(1)]
            private int _quantity;

            public ItemDefinition Item => _item;
            public int Quantity => _quantity < 0 ? 0 : _quantity;
            public bool IsValid => _item != null && _quantity > 0;
        }
    }
}
