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
        public sealed class ItemVendor : ContextBehaviour, IInteraction
        {
                [Header("Interaction")]
                [SerializeField]
                private string _interactionName = "Item Vendor";
                [SerializeField, TextArea]
                private string _interactionDescription = "Browse and purchase configured items.";
                [SerializeField]
                private Transform _hudPivot;
                [SerializeField]
                private Collider _interactionCollider;
                [SerializeField]
                private Transform _cameraTransform;

                [Header("Filtering")]
                [FormerlySerializedAs("ItemDefinitions")]
                public DataDefinition[] FilterDefinitions;
                [SerializeField]
                private int _itemsPerDefinition = 3;

                private readonly List<VendorItemData> _generatedItems = new List<VendorItemData>();
                private bool _itemsDirty = true;

                private UIVendorView _activeVendorView;
                private Agent _cameraAgent;

                private static WeaponDefinition[] _cachedWeaponDefinitions;
                private static PickaxeDefinition[] _cachedPickaxeDefinitions;
                private static WoodAxeDefinition[] _cachedWoodAxeDefinitions;

                string  IInteraction.Name        => _interactionName;
                string  IInteraction.Description => _interactionDescription;
                Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
                bool    IInteraction.IsActive    => isActiveAndEnabled == true && (_interactionCollider == null || (_interactionCollider.enabled == true && _interactionCollider.gameObject.activeInHierarchy == true));

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

                        _itemsDirty = true;
                        view.Configure(agent, PopulateItems);

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

                        EnsureGeneratedItems();

                        if (_generatedItems.Count == 0)
                                return VendorItemStatus.NoItems;

                        destination.AddRange(_generatedItems);
                        return VendorItemStatus.Success;
                }

                private void EnsureGeneratedItems()
                {
                        if (_itemsDirty == false && _generatedItems.Count > 0)
                                return;

                        _generatedItems.Clear();
                        _itemsDirty = false;

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

                        Sprite icon = definition.IconSprite;

                        for (int i = 0; i < count; ++i)
                        {
                                string configurationHash = GenerateConfigurationHash(definition);
                                _generatedItems.Add(new VendorItemData(icon, 1, definition, configurationHash));
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
                        ApplyCameraAuthority(_cameraAgent);
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
                        _itemsDirty = true;
                        _generatedItems.Clear();
                }

                private void OnDisable()
                {
                        if (_activeVendorView != null)
                        {
                                _activeVendorView.ItemSelected -= HandleItemSelected;
                                _activeVendorView.HasClosed    -= HandleVendorViewClosed;
                                _activeVendorView = null;
                        }

                        RestoreCameraAuthority();
                        _generatedItems.Clear();
                        _itemsDirty = true;
                }

                private void ApplyCameraAuthority(Agent agent)
                {
                        if (_cameraTransform == null || agent == null)
                                return;

                        if (agent.Interactions == null)
                                return;

                        if (_cameraAgent != null && _cameraAgent != agent)
                        {
                                RestoreCameraAuthority();
                        }

                        _cameraAgent = agent;
                        agent.Interactions.SetInteractionCameraAuthority(_cameraTransform);
                }

                private void RestoreCameraAuthority()
                {
                        if (_cameraAgent == null)
                                return;

                        Interactions interactions = _cameraAgent.Interactions;
                        if (interactions != null)
                        {
                                interactions.ClearInteractionCameraAuthority(_cameraTransform);
                        }

                        _cameraAgent = null;
                }

                public override void Render()
                {
                        base.Render();

                        ApplyCameraAuthority(_cameraAgent);
                }

                public enum VendorItemStatus
                {
                        NoDefinitions,
                        NoItems,
                        Success
                }

                public readonly struct VendorItemData : IEquatable<VendorItemData>
                {
                        public VendorItemData(Sprite icon, int quantity, ItemDefinition definition, string configurationHash)
                        {
                                Icon = icon;
                                Quantity = quantity;
                                Definition = definition;
                                ConfigurationHash = configurationHash;
                        }

                        public Sprite Icon { get; }
                        public int Quantity { get; }
                        public ItemDefinition Definition { get; }
                        public string ConfigurationHash { get; }

                        public bool Equals(VendorItemData other)
                        {
                                return Icon == other.Icon && Quantity == other.Quantity && Definition == other.Definition && string.Equals(ConfigurationHash, other.ConfigurationHash, StringComparison.Ordinal);
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
                                        return hashCode;
                                }
                        }
                }
        }
}
