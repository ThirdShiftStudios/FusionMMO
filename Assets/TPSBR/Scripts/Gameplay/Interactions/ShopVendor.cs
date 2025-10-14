using System.Collections.Generic;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using Random = UnityEngine.Random;
using TSS.Data;

namespace TPSBR
{
        public abstract class ShopVendor : ItemContextInteraction
        {
                [Header("Vendor Stock")]
                [SerializeField, Min(1)]
                private int _itemsPerDefinition = 3;
                [SerializeField]
                private Vector2Int _quantityRange = new Vector2Int(1, 1);
                [SerializeField]
                private bool _refreshStockOnClose = true;

                private readonly List<VendorOffer> _currentStock = new List<VendorOffer>();
                private bool _stockGenerated;

                protected IReadOnlyList<VendorOffer> CurrentStock => _currentStock;

                protected override ItemStatus PopulateItems(Agent agent, List<ItemData> destination)
                {
                        if (destination == null)
                                return ItemStatus.NoItems;

                        EnsureStock();

                        destination.Clear();

                        if (_currentStock.Count == 0)
                                return ItemStatus.NoItems;

                        for (int i = 0; i < _currentStock.Count; ++i)
                        {
                                VendorOffer offer = _currentStock[i];
                                destination.Add(new ItemData(offer.Icon, offer.Quantity, ItemSourceType.Vendor, i, offer.Definition, null, offer.ConfigurationHash));
                        }

                        return ItemStatus.Success;
                }

                protected override void OnItemSelected(ItemData data)
                {
                        base.OnItemSelected(data);

                        if (data.SourceType != ItemSourceType.Vendor)
                                return;

                        if (TryGetOffer(data.SourceIndex, out VendorOffer offer) == false)
                                return;

                        OnOfferSelected(CurrentAgent, offer);
                }

                protected override void OnItemContextViewClosed()
                {
                        if (_refreshStockOnClose == true)
                        {
                                InvalidateStock();
                        }

                        base.OnItemContextViewClosed();
                }

                protected virtual void OnOfferSelected(Agent agent, VendorOffer offer)
                {
                        _ = agent;
                        _ = offer;
                }

                protected virtual void EnsureStock()
                {
                        if (_stockGenerated == true)
                                return;

                        InvalidateStock();

                        DataDefinition[] definitions = FilterDefinitions;
                        if (definitions == null || definitions.Length == 0)
                                return;

                        int offersPerDefinition = Mathf.Max(0, _itemsPerDefinition);
                        if (offersPerDefinition <= 0)
                                return;

                        for (int i = 0; i < definitions.Length; ++i)
                        {
                                ItemDefinition itemDefinition = definitions[i] as ItemDefinition;
                                if (itemDefinition == null)
                                        continue;

                                for (int j = 0; j < offersPerDefinition; ++j)
                                {
                                        int quantity = GetRandomQuantity(itemDefinition);
                                        string configuration = GenerateConfiguration(itemDefinition);
                                        Sprite icon = itemDefinition.IconSprite;
                                        _currentStock.Add(new VendorOffer(itemDefinition, icon, quantity, configuration));
                                }
                        }

                        _stockGenerated = true;
                }

                protected virtual int GetRandomQuantity(ItemDefinition definition)
                {
                        _ = definition;

                        int min = Mathf.Max(1, _quantityRange.x);
                        int max = Mathf.Max(min, _quantityRange.y);
                        return Random.Range(min, max + 1);
                }

                protected virtual string GenerateConfiguration(ItemDefinition definition)
                {
                        if (definition is WeaponDefinition weaponDefinition && weaponDefinition.WeaponPrefab != null)
                        {
                                string randomStats = weaponDefinition.WeaponPrefab.GenerateRandomStats();
                                if (string.IsNullOrWhiteSpace(randomStats) == false)
                                        return randomStats;
                        }
                        else if (definition is PickaxeDefinition pickaxeDefinition && pickaxeDefinition.PickaxePrefab != null)
                        {
                                string randomStats = pickaxeDefinition.PickaxePrefab.GenerateRandomStats();
                                if (string.IsNullOrWhiteSpace(randomStats) == false)
                                        return randomStats;
                        }
                        else if (definition is WoodAxeDefinition woodAxeDefinition && woodAxeDefinition.WoodAxePrefab != null)
                        {
                                string randomStats = woodAxeDefinition.WoodAxePrefab.GenerateRandomStats();
                                if (string.IsNullOrWhiteSpace(randomStats) == false)
                                        return randomStats;
                        }

                        return null;
                }

                protected bool TryGetOffer(int index, out VendorOffer offer)
                {
                        if (index < 0 || index >= _currentStock.Count)
                        {
                                offer = default;
                                return false;
                        }

                        offer = _currentStock[index];
                        return true;
                }

                protected void InvalidateStock()
                {
                        _currentStock.Clear();
                        _stockGenerated = false;
                }

                protected readonly struct VendorOffer
                {
                        public VendorOffer(ItemDefinition definition, Sprite icon, int quantity, string configurationHash)
                        {
                                Definition = definition;
                                Icon = icon;
                                Quantity = quantity;
                                ConfigurationHash = configurationHash;
                        }

                        public ItemDefinition Definition { get; }
                        public Sprite Icon { get; }
                        public int Quantity { get; }
                        public string ConfigurationHash { get; }
                }
        }
}
