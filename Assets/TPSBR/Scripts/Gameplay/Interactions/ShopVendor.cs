using System;
using System.Collections.Generic;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using TSS.Data;

namespace TPSBR
{
        public sealed class ShopVendor : CraftingStation
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

                protected override void OnItemContextViewClosed()
                {
                        base.OnItemContextViewClosed();

                        if (_refreshStockOnClose == true)
                        {
                                _currentStock.Clear();
                                _stockGenerated = false;
                        }
                }

                private void EnsureStock()
                {
                        if (_stockGenerated == true)
                                return;

                        _currentStock.Clear();

                        DataDefinition[] definitions = FilterDefinitions;
                        if (definitions == null || definitions.Length == 0)
                        {
                                _stockGenerated = true;
                                return;
                        }

                        int offersPerDefinition = Mathf.Max(0, _itemsPerDefinition);
                        if (offersPerDefinition <= 0)
                        {
                                _stockGenerated = true;
                                return;
                        }

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

                private int GetRandomQuantity(ItemDefinition definition)
                {
                        _ = definition;

                        int min = Mathf.Max(1, _quantityRange.x);
                        int max = Mathf.Max(min, _quantityRange.y);
                        return Random.Range(min, max + 1);
                }

                private static string GenerateConfiguration(ItemDefinition definition)
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

                private readonly struct VendorOffer
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
