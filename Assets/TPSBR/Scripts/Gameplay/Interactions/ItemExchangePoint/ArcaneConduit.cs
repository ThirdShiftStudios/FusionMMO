using System;
using System.Collections.Generic;
using Fusion;
using TPSBR.Abilities;
using TPSBR.UI;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public sealed class ArcaneConduit : UpgradeStation
    {
        [SerializeField]
        private int _abilityCost = 50;

        private UIArcaneConduitView _arcaneView;
        private Agent _activeAgent;
        private Inventory _trackedInventory;
        private readonly List<AbilityOption> _abilityOptions = new List<AbilityOption>();

        public readonly struct AbilityOption
        {
            public AbilityOption(int index, AbilityDefinition definition, bool isUnlocked, bool canPurchase, int cost)
            {
                Index = index;
                Definition = definition;
                IsUnlocked = isUnlocked;
                CanPurchase = canPurchase;
                Cost = cost;
            }

            public int Index { get; }
            public AbilityDefinition Definition { get; }
            public bool IsUnlocked { get; }
            public bool CanPurchase { get; }
            public int Cost { get; }
        }

        protected override UIView _uiView => GetArcaneView() ?? base._uiView;

        private UIArcaneConduitView GetArcaneView()
        {
            if (_arcaneView == null && Context != null && Context.UI != null)
            {
                _arcaneView = Context.UI.Get<UIArcaneConduitView>();
            }

            return _arcaneView;
        }

        protected override bool ConfigureExchangeView(Agent agent, UIView view)
        {
            bool configured = base.ConfigureExchangeView(agent, view);

            if (configured == true && view is UIArcaneConduitView arcaneView)
            {
                arcaneView.SetConduit(this);
            }

            return configured;
        }

        protected override void SubscribeToViewEvents(UIView view)
        {
            base.SubscribeToViewEvents(view);

            if (view is UIArcaneConduitView arcaneView)
            {
                arcaneView.ItemSelected += HandleItemContextSelection;
                arcaneView.AbilityPurchaseRequested += HandleAbilityPurchaseRequested;
                arcaneView.AbilityAssignmentRequested += HandleAbilityAssignmentRequested;
            }
        }

        protected override void UnsubscribeFromViewEvents(UIView view)
        {
            if (view is UIArcaneConduitView arcaneView)
            {
                arcaneView.ItemSelected -= HandleItemContextSelection;
                arcaneView.AbilityPurchaseRequested -= HandleAbilityPurchaseRequested;
                arcaneView.AbilityAssignmentRequested -= HandleAbilityAssignmentRequested;
            }

            base.UnsubscribeFromViewEvents(view);
        }

        protected override void OnExchangeViewOpened(UIView view, Agent agent)
        {
            base.OnExchangeViewOpened(view, agent);

            _activeAgent = agent;
            SubscribeToInventory(_activeAgent != null ? _activeAgent.Inventory : null);
            RefreshAbilityOptions();
        }

        protected override void OnExchangeViewClosed(UIView view)
        {
            base.OnExchangeViewClosed(view);

            UnsubscribeFromInventory();
            _activeAgent = null;
            _abilityOptions.Clear();

            if (_arcaneView != null)
            {
                _arcaneView.ClearAbilityOptions();
                _arcaneView.ClearAbilityAssignments();
            }
        }

        public void RequestPurchaseAbility(Agent agent, int abilityIndex)
        {
            if (agent == null)
                return;

            abilityIndex = Mathf.Max(0, abilityIndex);

            if (HasStateAuthority == true)
            {
                ProcessAbilityPurchase(agent, abilityIndex);
            }
            else
            {
                RPC_RequestPurchaseAbility(agent.Object.InputAuthority, agent.Object.Id, abilityIndex);
            }
        }

        public void RequestAbilityAssignment(Agent agent, StaffWeapon.AbilityControlSlot slot, int abilityIndex)
        {
            if (agent == null)
                return;

            if (HasStateAuthority == true)
            {
                ProcessAbilityAssignment(agent, slot, abilityIndex);
            }
            else
            {
                RPC_RequestAbilityAssignment(agent.Object.InputAuthority, agent.Object.Id, (byte)slot, abilityIndex);
            }
        }

        private void HandleItemContextSelection(UpgradeStation.ItemData data)
        {
            _ = data;
            RefreshAbilityOptions();
        }

        private void HandleAbilityPurchaseRequested(int abilityIndex)
        {
            if (_activeAgent == null)
                return;

            RequestPurchaseAbility(_activeAgent, abilityIndex);
        }

        private void HandleAbilityAssignmentRequested(StaffWeapon.AbilityControlSlot slot, int abilityIndex)
        {
            if (_activeAgent == null)
                return;

            RequestAbilityAssignment(_activeAgent, slot, abilityIndex);
        }

        private void SubscribeToInventory(Inventory inventory)
        {
            if (_trackedInventory == inventory)
                return;

            UnsubscribeFromInventory();

            if (inventory == null)
                return;

            _trackedInventory = inventory;
            _trackedInventory.ItemSlotChanged += HandleInventorySlotChanged;
            _trackedInventory.HotbarSlotChanged += HandleHotbarSlotChanged;
            _trackedInventory.GoldChanged += HandleGoldChanged;
        }

        private void UnsubscribeFromInventory()
        {
            if (_trackedInventory == null)
                return;

            _trackedInventory.ItemSlotChanged -= HandleInventorySlotChanged;
            _trackedInventory.HotbarSlotChanged -= HandleHotbarSlotChanged;
            _trackedInventory.GoldChanged -= HandleGoldChanged;
            _trackedInventory = null;
        }

        private void HandleInventorySlotChanged(int index, InventorySlot slot)
        {
            _ = slot;

            if (_currentSelectedSourceType != ItemSourceType.Inventory)
                return;

            if (index != _currentSelectedSourceIndex)
                return;

            RefreshAbilityOptions();
        }

        private void HandleHotbarSlotChanged(int index, Weapon weapon)
        {
            _ = weapon;

            if (_currentSelectedSourceType != ItemSourceType.Hotbar)
                return;

            if (index != _currentSelectedSourceIndex)
                return;
            
            RefreshAbilityOptions();
        }

        private void HandleGoldChanged(int gold)
        {
            _ = gold;

            if (_activeAgent == null)
                return;

            RefreshAbilityOptions();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestPurchaseAbility(PlayerRef playerRef, NetworkId agentId, int abilityIndex)
        {
            if (TryResolveAgent(playerRef, agentId, out Agent agent) == false)
                return;

            ProcessAbilityPurchase(agent, abilityIndex);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestAbilityAssignment(PlayerRef playerRef, NetworkId agentId, byte slotValue, int abilityIndex)
        {
            if (TryResolveAgent(playerRef, agentId, out Agent agent) == false)
                return;

            int clampedSlot = Mathf.Clamp(slotValue, 0, StaffWeapon.GetAbilityControlSlotCount() - 1);
            StaffWeapon.AbilityControlSlot slot = (StaffWeapon.AbilityControlSlot)clampedSlot;
            ProcessAbilityAssignment(agent, slot, abilityIndex);
        }

        private void ProcessAbilityPurchase(Agent agent, int abilityIndex)
        {
            if (HasStateAuthority == false)
                return;

            if (agent == null)
                return;

            Inventory inventory = agent.Inventory;
            if (inventory == null)
                return;

            UpgradeStation.ItemSourceType sourceType = SelectedItemSourceType;
            int sourceIndex = SelectedItemSourceIndex;

            if (sourceType == UpgradeStation.ItemSourceType.None || sourceIndex < 0)
                return;

            if (TryResolveSelection(agent, sourceType, sourceIndex, out WeaponDefinition definition, out NetworkString<_32> configurationHash, out Weapon weapon) == false)
                return;

            IReadOnlyList<AbilityDefinition> availableAbilities = definition.AvailableAbilities;
            if (availableAbilities == null || abilityIndex < 0 || abilityIndex >= availableAbilities.Count)
                return;

            AbilityDefinition abilityDefinition = availableAbilities[abilityIndex];
            if (abilityDefinition == null)
                return;

            if (StaffWeapon.TryGetAbilityIndexes(configurationHash, out int[] existingIndexes) == false)
            {
                existingIndexes = Array.Empty<int>();
            }

            var configuredIndexes = new List<int>(existingIndexes ?? Array.Empty<int>());
            if (configuredIndexes.Contains(abilityIndex) == true)
                return;

            if (configuredIndexes.Count >= StaffWeapon.MaxConfiguredAbilities)
                return;

            if (inventory.Gold < _abilityCost)
                return;

            configuredIndexes.Add(abilityIndex);
            configuredIndexes.Sort();

            if (StaffWeapon.TryApplyAbilityIndexes(configurationHash, configuredIndexes, out NetworkString<_32> updatedHash) == false)
                return;

            if (inventory.TrySpendGold(_abilityCost) == false)
                return;

            bool applied = ApplyConfigurationToSelection(inventory, sourceType, sourceIndex, updatedHash, weapon);

            if (applied == false)
            {
                inventory.AddGold(_abilityCost);
                return;
            }

            RefreshAbilityOptions();
        }

        private void ProcessAbilityAssignment(Agent agent, StaffWeapon.AbilityControlSlot slot, int abilityIndex)
        {
            if (HasStateAuthority == false)
                return;

            if (agent == null)
                return;

            Inventory inventory = agent.Inventory;
            if (inventory == null)
                return;

            UpgradeStation.ItemSourceType sourceType = SelectedItemSourceType;
            int sourceIndex = SelectedItemSourceIndex;

            if (sourceType == UpgradeStation.ItemSourceType.None || sourceIndex < 0)
                return;

            if (TryResolveSelection(agent, sourceType, sourceIndex, out WeaponDefinition definition, out NetworkString<_32> configurationHash, out Weapon weapon) == false)
                return;

            IReadOnlyList<AbilityDefinition> availableAbilities = definition.AvailableAbilities;

            if (StaffWeapon.TryGetAbilityConfiguration(definition, configurationHash, out StaffWeapon.AbilityConfiguration abilityConfiguration) == false)
            {
                abilityConfiguration = new StaffWeapon.AbilityConfiguration(Array.Empty<int>(), null);
            }

            IReadOnlyList<int> unlockedIndexes = abilityConfiguration.UnlockedIndexes;
            int slotIndex = (int)slot;
            int slotCount = StaffWeapon.GetAbilityControlSlotCount();

            if (slotIndex < 0 || slotIndex >= slotCount)
                return;

            if (abilityIndex >= 0)
            {
                if (availableAbilities == null || abilityIndex >= availableAbilities.Count)
                    return;

                bool isUnlocked = false;

                if (unlockedIndexes != null)
                {
                    for (int i = 0; i < unlockedIndexes.Count; ++i)
                    {
                        if (unlockedIndexes[i] == abilityIndex)
                        {
                            isUnlocked = true;
                            break;
                        }
                    }
                }

                if (isUnlocked == false)
                    return;
            }

            int[] assignments = CopyAssignments(abilityConfiguration.SlotAssignments);

            if (abilityIndex >= 0)
            {
                for (int i = 0; i < assignments.Length; ++i)
                {
                    if (assignments[i] == abilityIndex)
                    {
                        assignments[i] = -1;
                    }
                }

                assignments[slotIndex] = abilityIndex;
            }
            else
            {
                assignments[slotIndex] = -1;
            }

            if (StaffWeapon.TryApplyAbilityAssignments(configurationHash, assignments, out NetworkString<_32> updatedHash) == false)
                return;

            bool applied = ApplyConfigurationToSelection(inventory, sourceType, sourceIndex, updatedHash, weapon);

            if (applied == false)
                return;

            RefreshAbilityOptions();
        }

        private bool ApplyConfigurationToSelection(Inventory inventory, UpgradeStation.ItemSourceType sourceType, int sourceIndex, NetworkString<_32> configurationHash, Weapon weapon)
        {
            switch (sourceType)
            {
                case UpgradeStation.ItemSourceType.Inventory:
                    return inventory.TrySetInventorySlotConfiguration(sourceIndex, configurationHash);

                case UpgradeStation.ItemSourceType.Hotbar:
                    if (inventory.TrySetHotbarConfiguration(sourceIndex, configurationHash) == false)
                        return false;

                    if (weapon != null && weapon.ConfigurationHash != configurationHash)
                    {
                        weapon.SetConfigurationHash(configurationHash);
                    }

                    return true;
            }

            return false;
        }

        private void RefreshAbilityOptions()
        {
            _abilityOptions.Clear();

            if (_arcaneView == null)
                GetArcaneView();

            if (_arcaneView == null)
                return;

            if (_activeAgent == null)
            {
                _arcaneView.ClearAbilityOptions();
                _arcaneView.ClearAbilityAssignments();
                return;
            }

            if (TryResolveSelection(_activeAgent, _currentSelectedSourceType, _currentSelectedSourceIndex, out WeaponDefinition definition, out NetworkString<_32> configurationHash, out _) == false)
            {
                _arcaneView.ClearAbilityOptions();
                _arcaneView.ClearAbilityAssignments();
                return;
            }

            IReadOnlyList<AbilityDefinition> abilities = definition.AvailableAbilities;
            if (abilities == null || abilities.Count == 0)
            {
                _arcaneView.ClearAbilityOptions();
                _arcaneView.ClearAbilityAssignments();
                return;
            }

            if (StaffWeapon.TryGetAbilityConfiguration(definition, configurationHash, out StaffWeapon.AbilityConfiguration abilityConfiguration) == false)
            {
                abilityConfiguration = new StaffWeapon.AbilityConfiguration(Array.Empty<int>(), null);
            }

            IReadOnlyList<int> unlockedIndexList = abilityConfiguration.UnlockedIndexes;
            var unlockedIndexes = unlockedIndexList != null ? new HashSet<int>(unlockedIndexList) : new HashSet<int>();
            int unlockedCount = unlockedIndexes.Count;
            int gold = _activeAgent.Inventory != null ? _activeAgent.Inventory.Gold : 0;
            bool canAfford = gold >= _abilityCost;
            int maxConfigured = StaffWeapon.MaxConfiguredAbilities;

            for (int i = 0; i < abilities.Count; ++i)
            {
                AbilityDefinition ability = abilities[i];
                if (ability == null)
                    continue;

                bool isUnlocked = unlockedIndexes.Contains(i);
                bool canPurchase = canAfford && isUnlocked == false && unlockedCount < maxConfigured;

                _abilityOptions.Add(new AbilityOption(i, ability, isUnlocked, canPurchase, _abilityCost));
            }

            _arcaneView.SetAbilityOptions(_abilityOptions);
            _arcaneView.SetAbilityAssignments(CopyAssignments(abilityConfiguration.SlotAssignments));
        }

        private static int[] CopyAssignments(IReadOnlyList<int> source)
        {
            int slotCount = StaffWeapon.GetAbilityControlSlotCount();
            int[] assignments = new int[slotCount];

            for (int i = 0; i < slotCount; ++i)
            {
                assignments[i] = source != null && i < source.Count ? source[i] : -1;
            }

            return assignments;
        }

        private bool TryResolveSelection(Agent agent, UpgradeStation.ItemSourceType sourceType, int sourceIndex, out WeaponDefinition definition, out NetworkString<_32> configurationHash, out Weapon weapon)
        {
            definition = null;
            configurationHash = default;
            weapon = null;

            if (agent == null)
                return false;

            Inventory inventory = agent.Inventory;
            if (inventory == null)
                return false;

            switch (sourceType)
            {
                case UpgradeStation.ItemSourceType.Inventory:
                {
                    InventorySlot slot = inventory.GetItemSlot(sourceIndex);
                    if (slot.IsEmpty == true)
                        return false;

                    definition = slot.GetDefinition() as WeaponDefinition;
                    configurationHash = slot.ConfigurationHash;
                    return definition != null;
                }

                case UpgradeStation.ItemSourceType.Hotbar:
                {
                    weapon = inventory.GetHotbarWeapon(sourceIndex);
                    if (weapon == null)
                        return false;

                    definition = weapon.Definition as WeaponDefinition;
                    configurationHash = weapon.ConfigurationHash;
                    return definition != null;
                }
            }

            return false;
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
    }
}
