using System;
using System.Collections.Generic;
using Fusion;
using TPSBR.UI;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
        public sealed class ArcaneConduit : ContextBehaviour, IInteraction
        {
                [Header("Interaction")]
                [SerializeField]
                private string _interactionName = "Arcane Conduit";
                [SerializeField, TextArea]
                private string _interactionDescription = "Channel mystical energies to enhance your weapons.";
                [SerializeField]
                private Transform _hudPivot;
                [SerializeField]
                private Collider _interactionCollider;

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

                        OpenItemContextView(agent);
                }

                private void OpenItemContextView(Agent agent)
                {
                        if (Context == null || Context.UI == null)
                                return;

                        UIItemContextView view = Context.UI.Get<UIItemContextView>();

                        if (view == null)
                        {
                                Debug.LogWarning($"{nameof(UIItemContextView)} is not available in the current UI setup.");
                                return;
                        }

                        view.Configure(agent, destination => PopulateStaffItems(agent, destination));
                        Context.UI.Open(view);
                }

                private StaffItemStatus PopulateStaffItems(Agent agent, List<StaffItemData> destination)
                {
                        if (destination == null)
                                return StaffItemStatus.NoStaff;

                        destination.Clear();

                        if (agent == null)
                                return StaffItemStatus.NoAgent;

                        Inventory inventory = agent.Inventory;
                        if (inventory == null)
                                return StaffItemStatus.NoInventory;

                        bool hasAny = false;

                        int inventorySize = inventory.InventorySize;
                        for (int i = 0; i < inventorySize; ++i)
                        {
                                InventorySlot slot = inventory.GetItemSlot(i);
                                if (slot.IsEmpty == true)
                                        continue;

                                if (slot.GetDefinition() is WeaponDefinition weaponDefinition && weaponDefinition.WeaponPrefab != null && weaponDefinition.WeaponPrefab.Size == WeaponSize.Staff)
                                {
                                        Sprite icon = weaponDefinition.IconSprite;
                                        destination.Add(new StaffItemData(icon, slot.Quantity, StaffItemSourceType.Inventory, i, weaponDefinition, null));
                                        hasAny = true;
                                }
                        }

                        int hotbarSize = inventory.HotbarSize;
                        for (int i = 0; i < hotbarSize; ++i)
                        {
                                Weapon weapon = inventory.GetHotbarWeapon(i);
                                if (weapon == null)
                                        continue;

                                if (weapon.Size != WeaponSize.Staff)
                                        continue;

                                WeaponDefinition definition = weapon.Definition as WeaponDefinition;
                                Sprite icon = weapon.Icon;
                                if (icon == null && definition != null)
                                {
                                        icon = definition.IconSprite;
                                }

                                destination.Add(new StaffItemData(icon, 1, StaffItemSourceType.Hotbar, i, definition, weapon));
                                hasAny = true;
                        }

                        if (hasAny == false)
                                return StaffItemStatus.NoStaff;

                        return StaffItemStatus.Success;
                }

                public enum StaffItemStatus
                {
                        NoAgent,
                        NoInventory,
                        NoStaff,
                        Success
                }

                public enum StaffItemSourceType
                {
                        Inventory,
                        Hotbar
                }

                public readonly struct StaffItemData : IEquatable<StaffItemData>
                {
                        public StaffItemData(Sprite icon, int quantity, StaffItemSourceType sourceType, int sourceIndex, WeaponDefinition definition, Weapon weapon)
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
                        public StaffItemSourceType SourceType { get; }
                        public int SourceIndex { get; }
                        public WeaponDefinition Definition { get; }
                        public Weapon Weapon { get; }

                        public bool Equals(StaffItemData other)
                        {
                                return Icon == other.Icon && Quantity == other.Quantity && SourceType == other.SourceType && SourceIndex == other.SourceIndex && Definition == other.Definition && Weapon == other.Weapon;
                        }

                        public override bool Equals(object obj)
                        {
                                return obj is StaffItemData other && Equals(other);
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
        }
}
