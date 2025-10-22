using System;
using System.Collections.Generic;
using Fusion;
using TPSBR.UI;
using TSS.Data;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using UnityEngine.Serialization;

namespace TPSBR
{
        public sealed class ItemVendor : ItemExchangePoint
        {
              
                [Header("Filtering")]
                [FormerlySerializedAs("ItemDefinitions")]
                public DataDefinition[] FilterDefinitions;
                [SerializeField]
                private int _itemsPerDefinition = 3;

                [Networked, Capacity(64)]
                private NetworkLinkedList<InventorySlot> _availableItems { get; }

                public const int ITEM_COST = 10;
                private const int SELL_REWARD = 5;

                private UIVendorView _activeVendorView;

                private static WeaponDefinition[] _cachedWeaponDefinitions;
                private static PickaxeDefinition[] _cachedPickaxeDefinitions;
                private static WoodAxeDefinition[] _cachedWoodAxeDefinitions;

                public override void Spawned()
                {
                        base.Spawned();

                        if (HasStateAuthority == true)
                        {
                                EnsureGeneratedItems();
                        }
                }

                public void Interact(Agent agent)
                {
                        if (agent == null)
                                return;

                        if (HasStateAuthority == false)
                                return;

                        RPC_RequestOpen(agent.Object.InputAuthority, agent.Object.Id);
                }

                [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
                private void RPC_RequestOpen(PlayerRef playerRef, NetworkId agentId)
                {
                        if (Runner == null)
                                return;

                        if (Runner.LocalPlayer != playerRef)
                                return;

                        Agent agent = null;

                        if (Runner.TryFindObject(agentId, out NetworkObject agentObject) == true)
                        {
                                agent = agentObject.GetComponent<Agent>();
                        }

                        if (agent == null && Context != null)
                        {
                                agent = Context.ObservedAgent;
                        }

                        if (agent == null)
                                return;

                        OpenVendorView(agent);
                }

                private void OpenVendorView(Agent agent)
                {
                        if (Context == null || Context.UI == null)
                                return;

                        UIVendorView view = Context.UI.Get<UIVendorView>();

                        if (view == null)
                        {
                                Debug.LogWarning($"{nameof(UIVendorView)} is not available in the current UI setup.");
                                return;
                        }

                        view.Configure(agent, this, PopulateItems);

                        if (_activeVendorView != null)
                        {
                                _activeVendorView.ItemSelected -= HandleItemSelected;
                                _activeVendorView.HasClosed    -= HandleVendorViewClosed;
                        }

                        _activeVendorView = view;
                        _activeVendorView.ItemSelected += HandleItemSelected;
                        _activeVendorView.HasClosed    += HandleVendorViewClosed;

                        Context.UI.Open(view);
                        ApplyCameraAuthority(agent);
                }

                private VendorItemStatus PopulateItems(List<VendorItemData> destination)
                {
                        if (destination == null)
                                return VendorItemStatus.NoItems;

                        destination.Clear();

                        if (FilterDefinitions == null || FilterDefinitions.Length == 0)
                                return VendorItemStatus.NoDefinitions;

                        if (HasStateAuthority == true)
                        {
                                EnsureGeneratedItems();
                        }

                        if (_availableItems.Count == 0)
                                return VendorItemStatus.NoItems;

                        for (int i = 0; i < _availableItems.Count; ++i)
                        {
                                InventorySlot slot = _availableItems.Get(i);

                                if (slot.IsEmpty == true)
                                        continue;

                                ItemDefinition definition = ItemDefinition.Get(slot.ItemDefinitionId);
                                if (definition == null)
                                        continue;

                                Sprite icon = definition.IconSprite;
                                string configurationHash = slot.ConfigurationHash.ToString();
                                destination.Add(new VendorItemData(icon, slot.Quantity, definition, configurationHash, i));
                        }

                        return destination.Count > 0 ? VendorItemStatus.Success : VendorItemStatus.NoItems;
                }

                private void EnsureGeneratedItems()
                {
                        if (HasStateAuthority == false)
                                return;

                        if (_availableItems.Count > 0)
                                return;

                        if (FilterDefinitions == null || FilterDefinitions.Length == 0)
                                return;

                        int itemsPerDefinition = Mathf.Max(1, _itemsPerDefinition);

                        for (int i = 0; i < FilterDefinitions.Length; ++i)
                        {
                                DataDefinition filter = FilterDefinitions[i];
                                if (filter == null)
                                        continue;

                                GenerateItemsForFilter(filter, itemsPerDefinition);
                        }
                }

                public void RequestPurchase(Agent agent, int vendorIndex)
                {
                        if (agent == null)
                                return;

                        if (HasStateAuthority == true)
                        {
                                ProcessPurchase(agent, vendorIndex);
                        }
                        else
                        {
                                RPC_RequestPurchase(agent.Object.InputAuthority, agent.Object.Id, vendorIndex);
                        }
                }

                public void RequestSell(Agent agent, int inventoryIndex)
                {
                        if (agent == null)
                                return;

                        if (HasStateAuthority == true)
                        {
                                ProcessSell(agent, inventoryIndex);
                        }
                        else
                        {
                                RPC_RequestSell(agent.Object.InputAuthority, agent.Object.Id, inventoryIndex);
                        }
                }

                [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
                private void RPC_RequestPurchase(PlayerRef playerRef, NetworkId agentId, int vendorIndex)
                {
                        if (TryResolveAgent(playerRef, agentId, out Agent agent) == false)
                                return;

                        ProcessPurchase(agent, vendorIndex);
                }

                [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
                private void RPC_RequestSell(PlayerRef playerRef, NetworkId agentId, int inventoryIndex)
                {
                        if (TryResolveAgent(playerRef, agentId, out Agent agent) == false)
                                return;

                        ProcessSell(agent, inventoryIndex);
                }

                private bool TryResolveAgent(PlayerRef playerRef, NetworkId agentId, out Agent agent)
                {
                        agent = null;

                        if (Runner == null)
                                return false;

                        if (Runner.TryFindObject(agentId, out NetworkObject agentObject) == false)
                                return false;

                        agent = agentObject.GetComponent<Agent>();

                        if (agent == null || agent.Object == null)
                                return false;

                        if (agent.Object.InputAuthority != playerRef)
                                return false;

                        return true;
                }

                private void ProcessPurchase(Agent agent, int vendorIndex)
                {
                        if (HasStateAuthority == false || agent == null)
                                return;

                        if (vendorIndex < 0 || vendorIndex >= _availableItems.Count)
                                return;

                        Inventory inventory = agent.Inventory;
                        if (inventory == null)
                                return;

                        InventorySlot vendorSlot = _availableItems.Get(vendorIndex);
                        if (vendorSlot.IsEmpty == true)
                                return;

                        ItemDefinition definition = ItemDefinition.Get(vendorSlot.ItemDefinitionId);
                        if (definition == null)
                                return;

                        if (inventory.Gold < ITEM_COST)
                                return;

                        if (HasEmptyInventorySlot(inventory) == false)
                                return;

                        if (inventory.TrySpendGold(ITEM_COST) == false)
                                return;

                        byte quantity = vendorSlot.Quantity;
                        if (quantity == 0)
                        {
                                quantity = 1;
                        }

                        byte remainder = inventory.AddItem(definition, quantity, vendorSlot.ConfigurationHash);

                        if (remainder > 0)
                        {
                                inventory.AddGold(ITEM_COST);
                                return;
                        }

                        _availableItems.Remove(vendorSlot);
                }

                private void ProcessSell(Agent agent, int inventoryIndex)
                {
                        if (HasStateAuthority == false || agent == null)
                                return;

                        if (_availableItems.Count >= _availableItems.Capacity)
                                return;

                        Inventory inventory = agent.Inventory;
                        if (inventory == null)
                                return;

                        InventorySlot inventorySlot = inventory.GetItemSlot(inventoryIndex);
                        if (inventorySlot.IsEmpty == true)
                                return;

                        ItemDefinition definition = ItemDefinition.Get(inventorySlot.ItemDefinitionId);
                        if (definition == null)
                                return;

                        byte quantity = inventorySlot.Quantity;
                        if (quantity == 0)
                                return;

                        if (inventory.TryExtractInventoryItem(inventoryIndex, quantity, out InventorySlot removedSlot) == false)
                                return;

                        if (removedSlot.Quantity == 0)
                                return;

                        _availableItems.Add(removedSlot);
                        inventory.AddGold(SELL_REWARD);
                }

                private static bool HasEmptyInventorySlot(Inventory inventory)
                {
                        if (inventory == null)
                                return false;

                        int slotCount = inventory.InventorySize;

                        for (int i = 0; i < slotCount; ++i)
                        {
                                InventorySlot slot = inventory.GetItemSlot(i);

                                if (slot.IsEmpty == true)
                                        return true;
                        }

                        return false;
                }

                private void GenerateItemsForFilter(DataDefinition filter, int count)
                {
                        if (count <= 0)
                                return;

                        if (filter is WeaponDefinition)
                        {
                                WeaponDefinition[] definitions = GetCachedDefinitions(ref _cachedWeaponDefinitions);
                                if (AddRandomItems(definitions, count, filter.GetType()) == false)
                                {
                                        AddSpecificDefinition(filter as ItemDefinition, count);
                                }
                        }
                        else if (filter is PickaxeDefinition)
                        {
                                PickaxeDefinition[] definitions = GetCachedDefinitions(ref _cachedPickaxeDefinitions);
                                if (AddRandomItems(definitions, count, filter.GetType()) == false)
                                {
                                        AddSpecificDefinition(filter as ItemDefinition, count);
                                }
                        }
                        else if (filter is WoodAxeDefinition)
                        {
                                WoodAxeDefinition[] definitions = GetCachedDefinitions(ref _cachedWoodAxeDefinitions);
                                if (AddRandomItems(definitions, count, filter.GetType()) == false)
                                {
                                        AddSpecificDefinition(filter as ItemDefinition, count);
                                }
                        }
                        else if (filter is ItemDefinition itemDefinition)
                        {
                                AddSpecificDefinition(itemDefinition, count);
                        }
                }

                private bool AddRandomItems<TDefinition>(TDefinition[] definitions, int count, Type requiredType) where TDefinition : ItemDefinition
                {
                        if (definitions == null || definitions.Length == 0)
                                return false;

                        bool addedAny = false;

                        for (int i = 0; i < count; ++i)
                        {
                                TDefinition definition = GetRandomDefinition(definitions, requiredType);
                                if (definition == null)
                                        continue;

                                AddSpecificDefinition(definition, 1);
                                addedAny = true;
                        }

                        return addedAny;
                }

                private static TDefinition GetRandomDefinition<TDefinition>(TDefinition[] definitions, Type requiredType) where TDefinition : ItemDefinition
                {
                        if (definitions == null || definitions.Length == 0)
                                return null;

                        if (requiredType == null || requiredType == typeof(TDefinition))
                        {
                                return definitions[UnityEngine.Random.Range(0, definitions.Length)];
                        }

                        int startIndex = UnityEngine.Random.Range(0, definitions.Length);

                        for (int i = 0; i < definitions.Length; ++i)
                        {
                                int index = (startIndex + i) % definitions.Length;
                                TDefinition candidate = definitions[index];

                                if (candidate != null && candidate.GetType() == requiredType)
                                {
                                        return candidate;
                                }
                        }

                        return null;
                }

                private void AddSpecificDefinition(ItemDefinition definition, int count)
                {
                        if (definition == null)
                                return;

                        for (int i = 0; i < count; ++i)
                        {
                                string configurationHash = GenerateConfigurationHash(definition);
                                NetworkString<_32> networkHash = default;

                                if (string.IsNullOrWhiteSpace(configurationHash) == false)
                                {
                                        networkHash = configurationHash;
                                }

                                if (_availableItems.Count >= _availableItems.Capacity)
                                        return;

                                _availableItems.Add(new InventorySlot(definition.ID, 1, networkHash));
                        }
                }

                private static TDefinition[] GetCachedDefinitions<TDefinition>(ref TDefinition[] cache) where TDefinition : ItemDefinition
                {
                        if (cache == null || cache.Length == 0)
                        {
                                cache = Resources.LoadAll<TDefinition>(string.Empty);
                        }

                        return cache;
                }

                private string GenerateConfigurationHash(ItemDefinition definition)
                {
                        if (definition is WeaponDefinition weaponDefinition && weaponDefinition.WeaponPrefab != null)
                        {
                                string randomStats = weaponDefinition.WeaponPrefab.GenerateRandomStats();
                                if (string.IsNullOrWhiteSpace(randomStats) == false)
                                {
                                        return randomStats;
                                }
                        }
                        else if (definition is PickaxeDefinition pickaxeDefinition && pickaxeDefinition.PickaxePrefab != null)
                        {
                                string randomStats = pickaxeDefinition.PickaxePrefab.GenerateRandomStats();
                                if (string.IsNullOrWhiteSpace(randomStats) == false)
                                {
                                        return randomStats;
                                }
                        }
                        else if (definition is WoodAxeDefinition woodAxeDefinition && woodAxeDefinition.WoodAxePrefab != null)
                        {
                                string randomStats = woodAxeDefinition.WoodAxePrefab.GenerateRandomStats();
                                if (string.IsNullOrWhiteSpace(randomStats) == false)
                                {
                                        return randomStats;
                                }
                        }

                        return string.Empty;
                }

                private void HandleItemSelected(VendorItemData data)
                {
                        _ = data;
                        ApplyCameraAuthority(CurrentCameraAgent);
                }

                private void HandleVendorViewClosed()
                {
                        if (_activeVendorView != null)
                        {
                                _activeVendorView.ItemSelected -= HandleItemSelected;
                                _activeVendorView.HasClosed    -= HandleVendorViewClosed;
                                _activeVendorView = null;
                        }

                        RestoreCameraAuthority();
                }

                protected override void OnDisable()
                {
                        if (_activeVendorView != null)
                        {
                                _activeVendorView.ItemSelected -= HandleItemSelected;
                                _activeVendorView.HasClosed    -= HandleVendorViewClosed;
                                _activeVendorView = null;
                        }

                        RestoreCameraAuthority();
                        if (HasStateAuthority == true)
                        {
                                _availableItems.Clear();
                        }

                        base.OnDisable();
                }

                public enum VendorItemStatus
                {
                        NoDefinitions,
                        NoItems,
                        Success
                }

                public readonly struct VendorItemData : IEquatable<VendorItemData>
                {
                        public VendorItemData(Sprite icon, int quantity, ItemDefinition definition, string configurationHash, int sourceIndex)
                        {
                                Icon = icon;
                                Quantity = quantity;
                                Definition = definition;
                                ConfigurationHash = configurationHash;
                                SourceIndex = sourceIndex;
                        }

                        public Sprite Icon { get; }
                        public int Quantity { get; }
                        public ItemDefinition Definition { get; }
                        public string ConfigurationHash { get; }
                        public int SourceIndex { get; }

                        public bool Equals(VendorItemData other)
                        {
                                return Icon == other.Icon && Quantity == other.Quantity && Definition == other.Definition && string.Equals(ConfigurationHash, other.ConfigurationHash, StringComparison.Ordinal) && SourceIndex == other.SourceIndex;
                        }

                        public override bool Equals(object obj)
                        {
                                return obj is VendorItemData other && Equals(other);
                        }

                        public override int GetHashCode()
                        {
                                unchecked
                                {
                                        int hashCode = Icon != null ? Icon.GetHashCode() : 0;
                                        hashCode = (hashCode * 397) ^ Quantity;
                                        hashCode = (hashCode * 397) ^ (Definition != null ? Definition.GetHashCode() : 0);
                                        hashCode = (hashCode * 397) ^ (ConfigurationHash != null ? ConfigurationHash.GetHashCode() : 0);
                                        hashCode = (hashCode * 397) ^ SourceIndex;
                                        return hashCode;
                                }
                        }
                }
        }
}
