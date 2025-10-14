using System;
using System.Collections.Generic;
using Fusion;
using TSS.Data;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
        public abstract class CraftingStation : ItemContextInteraction
        {
                [SerializeField]
                private Transform _weaponViewTransform;

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

                public override void Spawned()
                {
                        base.Spawned();

                        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
                        UpdateSelectedItemSlotCache();
                        UpdateWeaponPreviewFromDefinitionId(SelectedItemDefinitionId);
                }

                protected override void OnDisable()
                {
                        base.OnDisable();

                        if (HasStateAuthority == false)
                        {
                                _currentSelectedSourceType = ItemSourceType.None;
                                _currentSelectedSourceIndex = -1;
                        }
                }

                protected override ItemStatus PopulateItems(Agent agent, List<ItemData> destination)
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
                                        destination.Add(new ItemData(icon, slot.Quantity, ItemSourceType.Inventory, i, weaponDefinition, null, null));
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

                                destination.Add(new ItemData(icon, 1, ItemSourceType.Hotbar, i, definition, weapon, null));
                                hasAny = true;
                        }

                        if (hasAny == false)
                                return ItemStatus.NoItems;

                        return ItemStatus.Success;
                }

                protected override void OnItemSelected(ItemData data)
                {
                        UpdateLocalItemPreview(data.Definition);
                        RequestSetSelectedItem(data.Definition != null ? data.Definition.ID : 0, data.SourceType, data.SourceIndex);
                        base.OnItemSelected(data);
                }

                protected override void OnItemContextViewClosed()
                {
                        UpdateLocalItemPreview(null);
                        RequestSetSelectedItem(0, ItemSourceType.None, -1);
                        base.OnItemContextViewClosed();
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

                private void UpdateLocalItemPreview(ItemDefinition definition)
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

                        if (definition is WeaponDefinition weaponDefinition)
                        {
                                ShowWeaponPreview(weaponDefinition);
                        }
                        else
                        {
                                ClearWeaponPreview();
                        }
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

                private void ShowWeaponPreview(WeaponDefinition definition)
                {
                        if (_weaponViewTransform == null || definition == null || definition.WeaponPrefab == null)
                        {
                                ClearWeaponPreview();
                                return;
                        }

                        ClearWeaponPreview();

                        _weaponPreviewInstance = UnityEngine.Object.Instantiate(definition.WeaponPrefab, _weaponViewTransform);

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
                                UnityEngine.Object.Destroy(_weaponPreviewInstance.gameObject);
                                _weaponPreviewInstance = null;
                        }

                        _currentPreviewDefinitionId = 0;
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
                                UnityEngine.Object.Destroy(networkObject);
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
        }
}
