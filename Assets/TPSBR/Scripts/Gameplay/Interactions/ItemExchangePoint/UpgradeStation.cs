using System;
using System.Collections.Generic;
using Fusion;
using TPSBR.UI;
using TSS.Data;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public abstract class UpgradeStation : ItemExchangePoint
    {

        [SerializeField]
        private Transform _weaponViewTransform;

        [Header("Filtering")]
        public DataDefinition[] FilterDefinitions;
        private UIItemContextView _itemContextView;

        [Networked]
        private int SelectedItemDefinitionId { get; set; }
        [Networked]
        private ItemSourceType SelectedItemSourceType { get; set; }
        [Networked]
        private int SelectedItemSourceIndex { get; set; }

        private Weapon _weaponPreviewInstance;
        private int _currentPreviewDefinitionId;
        private ItemSourceType _currentSelectedSourceType = ItemSourceType.None;
        private int _currentSelectedSourceIndex = -1;
        private ChangeDetector _changeDetector;


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

            OpenItemContextView(agent);
        }

        private void OpenItemContextView(Agent agent)
        {
            OpenExchangeView(agent);
        }

        protected override UIView _uiView => GetItemContextView();

        private UIItemContextView GetItemContextView()
        {
            if (_itemContextView == null && Context != null && Context.UI != null)
            {
                _itemContextView = Context.UI.Get<UIItemContextView>();
            }

            return _itemContextView;
        }

        protected override bool ConfigureExchangeView(Agent agent, UIView view)
        {
            if (view is UIItemContextView itemContextView)
            {
                itemContextView.Configure(agent, destination => PopulateItems(agent, destination));
                return true;
            }

            Debug.LogWarning($"{nameof(UIItemContextView)} is not available in the current UI setup.");
            return false;
        }

        protected override void SubscribeToViewEvents(UIView view)
        {
            if (view is UIItemContextView itemContextView)
            {
                itemContextView.ItemSelected += HandleItemSelected;
            }
        }

        protected override void UnsubscribeFromViewEvents(UIView view)
        {
            if (view is UIItemContextView itemContextView)
            {
                itemContextView.ItemSelected -= HandleItemSelected;
            }
        }

        protected override void OnExchangeViewClosed(UIView view)
        {
            UpdateLocalWeaponPreview(null);
            RequestSetSelectedItem(0, ItemSourceType.None, -1);

            if (HasStateAuthority == false)
            {
                _currentSelectedSourceType = ItemSourceType.None;
                _currentSelectedSourceIndex = -1;
            }

            base.OnExchangeViewClosed(view);
        }

        private ItemStatus PopulateItems(Agent agent, List<ItemData> destination)
        {
            if (destination == null)
                return ItemStatus.NoItems;

            destination.Clear();

            if (agent == null)
                return ItemStatus.NoAgent;

            Inventory inventory = agent.Inventory;
            if (inventory == null)
                return ItemStatus.NoInventory;

            bool hasAny = false;

            int inventorySize = inventory.InventorySize;
            for (int i = 0; i < inventorySize; ++i)
            {
                InventorySlot slot = inventory.GetItemSlot(i);
                if (slot.IsEmpty == true)
                    continue;

                if (slot.GetDefinition() is WeaponDefinition weaponDefinition && weaponDefinition.WeaponPrefab != null)
                {
                    if (MatchesFilter(weaponDefinition) == false)
                        continue;

                    Sprite icon = weaponDefinition.IconSprite;
                    destination.Add(new ItemData(icon, slot.Quantity, ItemSourceType.Inventory, i, weaponDefinition, null));
                    hasAny = true;
                }
            }

            int hotbarSize = inventory.HotbarSize;
            for (int i = 0; i < hotbarSize; ++i)
            {
                Weapon weapon = inventory.GetHotbarWeapon(i);
                if (weapon == null)
                    continue;

                WeaponDefinition definition = weapon.Definition as WeaponDefinition;
                if (MatchesFilter(definition) == false)
                    continue;

                Sprite icon = weapon.Icon;
                if (icon == null && definition != null)
                {
                    icon = definition.IconSprite;
                }

                destination.Add(new ItemData(icon, 1, ItemSourceType.Hotbar, i, definition, weapon));
                hasAny = true;
            }

            if (hasAny == false)
                return ItemStatus.NoItems;

            return ItemStatus.Success;
        }

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
            Hotbar
        }

        public readonly struct ItemData : IEquatable<ItemData>
        {
            public ItemData(Sprite icon, int quantity, ItemSourceType sourceType, int sourceIndex, WeaponDefinition definition, Weapon weapon)
            {
                Icon = icon;
                Quantity = quantity;
                SourceType = sourceType;
                SourceIndex = sourceIndex;
                Definition = definition;
                Weapon = weapon;
            }

            public Sprite Icon { get; }
            public int Quantity { get; }
            public ItemSourceType SourceType { get; }
            public int SourceIndex { get; }
            public WeaponDefinition Definition { get; }
            public Weapon Weapon { get; }

            public bool Equals(ItemData other)
            {
                return Icon == other.Icon && Quantity == other.Quantity && SourceType == other.SourceType && SourceIndex == other.SourceIndex && Definition == other.Definition && Weapon == other.Weapon;
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
                    return hashCode;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_interactionCollider == null)
            {
                _interactionCollider = GetComponent<Collider>();
            }
        }
#endif

        public override void Spawned()
        {
            base.Spawned();

            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            UpdateSelectedItemSlotCache();
            UpdateWeaponPreviewFromDefinitionId(SelectedItemDefinitionId);
        }

        protected override void OnDisable()
        {
            UpdateLocalWeaponPreview(null);

            if (HasStateAuthority == true)
            {
                RequestSetSelectedItem(0, ItemSourceType.None, -1);
            }
            else
            {
                _currentSelectedSourceType = ItemSourceType.None;
                _currentSelectedSourceIndex = -1;
            }

            base.OnDisable();
        }

        private void UpdateLocalWeaponPreview(WeaponDefinition definition)
        {
            if (definition == null)
            {
                if (_currentPreviewDefinitionId != 0)
                {
                    ClearWeaponPreview();
                }

                return;
            }

            if (_currentPreviewDefinitionId == definition.ID)
                return;

            ShowWeaponPreview(definition);
        }

        private void HandleItemSelected(ItemData data)
        {
            UpdateLocalWeaponPreview(data.Definition);
            RequestSetSelectedItem(data.Definition != null ? data.Definition.ID : 0, data.SourceType, data.SourceIndex);
            ApplyCameraAuthority(CurrentCameraAgent);
        }

        private void ShowWeaponPreview(WeaponDefinition definition)
        {
            if (_weaponViewTransform == null || definition == null || definition.WeaponPrefab == null)
            {
                ClearWeaponPreview();
                return;
            }

            ClearWeaponPreview();

            _weaponPreviewInstance = Instantiate(definition.WeaponPrefab, _weaponViewTransform);

            DisableNetworkingForPreview(_weaponPreviewInstance);

            Transform previewTransform = _weaponPreviewInstance.transform;
            previewTransform.localPosition = Vector3.zero;
            previewTransform.localRotation = Quaternion.identity;
            previewTransform.localScale = Vector3.one;
            _currentPreviewDefinitionId = definition.ID;
        }

        private void ClearWeaponPreview()
        {
            if (_weaponPreviewInstance != null)
            {
                Destroy(_weaponPreviewInstance.gameObject);
                _weaponPreviewInstance = null;
            }

            _currentPreviewDefinitionId = 0;
        }

        private void RequestSetSelectedItem(int definitionId, ItemSourceType sourceType, int sourceIndex)
        {
            definitionId = Mathf.Max(0, definitionId);
            sourceIndex = Mathf.Max(-1, sourceIndex);

            if (HasStateAuthority == true)
            {
                SetSelectedItem(definitionId, sourceType, sourceIndex);
                return;
            }

            RPC_RequestSetSelectedItem(definitionId, sourceType, sourceIndex);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestSetSelectedItem(int definitionId, ItemSourceType sourceType, int sourceIndex)
        {
            SetSelectedItem(definitionId, sourceType, sourceIndex);
        }

        private void SetSelectedItem(int definitionId, ItemSourceType sourceType, int sourceIndex)
        {
            definitionId = Mathf.Max(0, definitionId);
            sourceIndex = Mathf.Max(-1, sourceIndex);

            SelectedItemDefinitionId = definitionId;
            SelectedItemSourceType = sourceType;
            SelectedItemSourceIndex = sourceIndex;

            UpdateWeaponPreviewFromDefinitionId(definitionId);
            UpdateSelectedItemSlotCache();
        }

        private void UpdateWeaponPreviewFromDefinitionId(int definitionId)
        {
            if (definitionId <= 0)
            {
                if (_currentPreviewDefinitionId != 0)
                {
                    ClearWeaponPreview();
                }

                return;
            }

            ItemDefinition itemDefinition = ItemDefinition.Get(definitionId);
            if (itemDefinition is WeaponDefinition weaponDefinition)
            {
                if (_currentPreviewDefinitionId == weaponDefinition.ID)
                    return;

                ShowWeaponPreview(weaponDefinition);
            }
            else if (_currentPreviewDefinitionId != 0)
            {
                ClearWeaponPreview();
            }
        }

        private void DisableNetworkingForPreview(Weapon previewWeapon)
        {
            if (previewWeapon == null)
                return;

            NetworkBehaviour[] networkBehaviours = previewWeapon.GetComponentsInChildren<NetworkBehaviour>(true);
            for (int i = 0; i < networkBehaviours.Length; ++i)
            {
                NetworkBehaviour behaviour = networkBehaviours[i];
                behaviour.enabled = false;
            }

            NetworkObject[] networkObjects = previewWeapon.GetComponentsInChildren<NetworkObject>(true);
            for (int i = 0; i < networkObjects.Length; ++i)
            {
                NetworkObject networkObject = networkObjects[i];
                Destroy(networkObject);
            }
        }

        public override void Render()
        {
            base.Render();

            if (_changeDetector != null)
            {
                foreach (var change in _changeDetector.DetectChanges(this))
                {
                    switch (change)
                    {
                        case nameof(SelectedItemDefinitionId):
                            UpdateWeaponPreviewFromDefinitionId(SelectedItemDefinitionId);
                            break;
                        case nameof(SelectedItemSourceType):
                        case nameof(SelectedItemSourceIndex):
                            UpdateSelectedItemSlotCache();
                            break;
                    }
                }
            }
        }

        private void UpdateSelectedItemSlotCache()
        {
            _currentSelectedSourceType = SelectedItemSourceType;
            _currentSelectedSourceIndex = SelectedItemSourceIndex;
        }

        private bool MatchesFilter(DataDefinition definition)
        {
            if (FilterDefinitions == null || FilterDefinitions.Length == 0)
                return true;

            if (definition == null)
                return false;

            int definitionId = definition.ID;

            for (int i = 0; i < FilterDefinitions.Length; ++i)
            {
                DataDefinition filter = FilterDefinitions[i];
                if (filter == null)
                    continue;

                if (filter.ID == definitionId)
                    return true;
            }

            return false;
        }
    }
}
