using System;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using TSS.Data;

namespace TPSBR
{
        public enum ItemStatus
        {
                NoAgent,
                NoInventory,
                NoItems,
                Success
        }

        public enum ItemSourceType
        {
                None,
                Inventory,
                Hotbar,
                Vendor
        }

        public readonly struct ItemData : IEquatable<ItemData>
        {
                public ItemData(Sprite icon, int quantity, ItemSourceType sourceType, int sourceIndex, ItemDefinition definition, Weapon weapon, string configurationHash)
                {
                        Icon = icon;
                        Quantity = quantity;
                        SourceType = sourceType;
                        SourceIndex = sourceIndex;
                        Definition = definition;
                        Weapon = weapon;
                        ConfigurationHash = configurationHash;
                }

                public Sprite Icon { get; }
                public int Quantity { get; }
                public ItemSourceType SourceType { get; }
                public int SourceIndex { get; }
                public ItemDefinition Definition { get; }
                public Weapon Weapon { get; }
                public string ConfigurationHash { get; }

                public bool Equals(ItemData other)
                {
                        return Icon == other.Icon && Quantity == other.Quantity && SourceType == other.SourceType && SourceIndex == other.SourceIndex && Definition == other.Definition && Weapon == other.Weapon && ConfigurationHash == other.ConfigurationHash;
                }

                public override bool Equals(object obj)
                {
                        return obj is ItemData other && Equals(other);
                }

                public override int GetHashCode()
                {
                        unchecked
                        {
                                int hashCode = Icon != null ? Icon.GetHashCode() : 0;
                                hashCode = (hashCode * 397) ^ Quantity;
                                hashCode = (hashCode * 397) ^ (int)SourceType;
                                hashCode = (hashCode * 397) ^ SourceIndex;
                                hashCode = (hashCode * 397) ^ (Definition != null ? Definition.GetHashCode() : 0);
                                hashCode = (hashCode * 397) ^ (Weapon != null ? Weapon.GetHashCode() : 0);
                                hashCode = (hashCode * 397) ^ (ConfigurationHash != null ? ConfigurationHash.GetHashCode() : 0);
                                return hashCode;
                        }
                }
        }
}
