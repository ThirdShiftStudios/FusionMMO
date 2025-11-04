using System;
using UnityEngine;

namespace TSS.Data
{
    public enum EItemRarity
    {
        None = 0,
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public struct ItemRarityData
    {
        [SerializeField] private EItemRarity _rarity;
<<<<<<< HEAD
        [SerializeField] private Color _primaryTextColor;
        [SerializeField] private Color _secondaryTextColor;
=======
        [SerializeField] private Color _primaryTextColor = Color.white;
        [SerializeField] private Color _secondaryTextColor = Color.white;
>>>>>>> origin/codex/add-eitemrarity-enum-and-itemrarityresourcesdefinition
        [SerializeField] private GameObject _pickupVisuals;

        public EItemRarity Rarity => _rarity;
        public Color PrimaryTextColor => _primaryTextColor;
        public Color SecondaryTextColor => _secondaryTextColor;
        public GameObject PickupVisuals => _pickupVisuals;
    }

    [CreateAssetMenu(fileName = "ItemRarityResources", menuName = "TSS/Data Definitions/Item Rarity Resources")]
    public class ItemRarityResourcesDefinition : DataDefinition
    {
        private const string ResourcePath = "ItemRarityResources";
        private static ItemRarityResourcesDefinition _instance;

        [SerializeField] private Texture2D _icon;
        [SerializeField] private string _displayName = "Item Rarity Resources";
        [SerializeField] private ItemRarityData[] _rarities = Array.Empty<ItemRarityData>();

        public static ItemRarityResourcesDefinition Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ItemRarityResourcesDefinition>(ResourcePath);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (_instance == null)
                    {
                        Debug.LogWarning($"Unable to locate {nameof(ItemRarityResourcesDefinition)} asset at Resources/{ResourcePath}.");
                    }
#endif
                }

                return _instance;
            }
        }

        public override string Name => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public override Texture2D Icon => _icon;

        public ItemRarityData[] Rarities => _rarities;

        public bool TryGetData(EItemRarity rarity, out ItemRarityData rarityData)
        {
            for (var i = 0; i < _rarities.Length; i++)
            {
                if (_rarities[i].Rarity == rarity)
                {
                    rarityData = _rarities[i];
                    return true;
                }
            }

            rarityData = default;
            return false;
        }
    }
}
