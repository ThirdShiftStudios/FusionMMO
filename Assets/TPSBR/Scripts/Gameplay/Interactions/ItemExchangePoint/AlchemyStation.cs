using System.Collections.Generic;
using Fusion;
using TPSBR.UI;
using TSS.Data;
using UnityEngine;

namespace TPSBR
{
    public sealed class AlchemyStation : ItemExchangePoint
    {
        [Header("Alchemy")]
        [SerializeField]
        private AlchemyDefinition _alchemyDefinition;

        private UIAlchemyStationView _alchemyView;

        protected override UIView _uiView => GetAlchemyView();

        protected override bool HandleInteraction(Agent agent, out string message)
        {
            message = string.Empty;

            if (agent == null)
                return false;

            if (HasStateAuthority == false)
                return false;

            RPC_RequestOpen(agent.Object.InputAuthority, agent.Object.Id);
            return true;
        }

        protected override bool ConfigureExchangeView(Agent agent, UIView view)
        {
            if (view is UIAlchemyStationView alchemyView)
            {
                alchemyView.Configure(agent, this);
                return true;
            }

            Debug.LogWarning($"{nameof(UIAlchemyStationView)} is not available in the current UI setup.");
            return false;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_RequestOpen(PlayerRef playerRef, NetworkId agentId)
        {
            if (Runner == null || Runner.LocalPlayer != playerRef)
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

            OpenExchangeView(agent);
        }

        private UIAlchemyStationView GetAlchemyView()
        {
            if (_alchemyView == null && Context != null && Context.UI != null)
            {
                _alchemyView = Context.UI.Get<UIAlchemyStationView>();
            }

            return _alchemyView;
        }

        public void RequestAlchemize(Agent agent, int floraSlotIndex, int essenceSlotIndex, int oreSlotIndex, int liquidSlotIndex)
        {
            if (agent == null)
                return;

            if (HasStateAuthority == true)
            {
                ProcessAlchemy(agent, floraSlotIndex, essenceSlotIndex, oreSlotIndex, liquidSlotIndex);
            }
            else
            {
                RPC_RequestAlchemize(agent.Object.InputAuthority, agent.Object.Id, floraSlotIndex, essenceSlotIndex, oreSlotIndex, liquidSlotIndex);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestAlchemize(PlayerRef playerRef, NetworkId agentId, int floraSlotIndex, int essenceSlotIndex, int oreSlotIndex, int liquidSlotIndex)
        {
            if (TryResolveAgent(playerRef, agentId, out Agent agent) == false)
                return;

            ProcessAlchemy(agent, floraSlotIndex, essenceSlotIndex, oreSlotIndex, liquidSlotIndex);
        }

        private void ProcessAlchemy(Agent agent, int floraSlotIndex, int essenceSlotIndex, int oreSlotIndex, int liquidSlotIndex)
        {
            if (HasStateAuthority == false || _alchemyDefinition == null)
                return;

            Inventory inventory = agent != null ? agent.Inventory : null;
            if (inventory == null)
                return;

            if (TryGetIngredient(inventory, floraSlotIndex, typeof(FloraResource), out InventorySlot floraSlot, out ItemDefinition floraDefinition) == false)
                return;

            if (TryGetIngredient(inventory, essenceSlotIndex, typeof(EssenceResource), out InventorySlot essenceSlot, out ItemDefinition essenceDefinition) == false)
                return;

            if (TryGetIngredient(inventory, oreSlotIndex, typeof(OreResource), out InventorySlot oreSlot, out ItemDefinition oreDefinition) == false)
                return;

            if (TryGetIngredient(inventory, liquidSlotIndex, typeof(BaseLiquid), out InventorySlot liquidSlot, out ItemDefinition liquidDefinition) == false)
                return;

            List<InventorySlot> consumed = new List<InventorySlot>(4);

            if (TryConsumeIngredient(inventory, floraSlotIndex, floraDefinition, consumed) == false)
                return;

            if (TryConsumeIngredient(inventory, essenceSlotIndex, essenceDefinition, consumed) == false)
            {
                RestoreConsumed(inventory, consumed);
                return;
            }

            if (TryConsumeIngredient(inventory, oreSlotIndex, oreDefinition, consumed) == false)
            {
                RestoreConsumed(inventory, consumed);
                return;
            }

            if (TryConsumeIngredient(inventory, liquidSlotIndex, liquidDefinition, consumed) == false)
            {
                RestoreConsumed(inventory, consumed);
                return;
            }

            string configurationHash = GenerateConfigurationHash(floraDefinition, floraSlot.ConfigurationHash,
                essenceDefinition, essenceSlot.ConfigurationHash, oreDefinition, oreSlot.ConfigurationHash,
                liquidDefinition, liquidSlot.ConfigurationHash);

            NetworkString<_64> networkHash = default;
            if (string.IsNullOrWhiteSpace(configurationHash) == false)
            {
                networkHash = configurationHash;
            }

            byte leftover = inventory.AddItem(_alchemyDefinition, 1, networkHash);

            if (leftover > 0)
            {
                RestoreConsumed(inventory, consumed);
            }
        }

        private static bool TryGetIngredient(Inventory inventory, int slotIndex, System.Type expectedType, out InventorySlot slot, out ItemDefinition definition)
        {
            slot = default;
            definition = null;

            if (slotIndex < 0 || slotIndex >= inventory.InventorySize)
                return false;

            slot = inventory.GetItemSlot(slotIndex);
            if (slot.IsEmpty == true)
                return false;

            definition = ItemDefinition.Get(slot.ItemDefinitionId);
            if (definition == null || expectedType.IsInstanceOfType(definition) == false)
                return false;

            return true;
        }

        private static bool TryConsumeIngredient(Inventory inventory, int slotIndex, ItemDefinition expectedDefinition, List<InventorySlot> consumed)
        {
            if (expectedDefinition == null)
                return false;

            if (inventory.TryExtractInventoryItem(slotIndex, 1, out InventorySlot removedSlot) == false)
                return false;

            if (removedSlot.Quantity == 0)
                return false;

            if (removedSlot.ItemDefinitionId != expectedDefinition.ID)
            {
                ItemDefinition removedDefinition = ItemDefinition.Get(removedSlot.ItemDefinitionId);
                if (removedDefinition != null)
                {
                    inventory.AddItem(removedDefinition, removedSlot.Quantity, removedSlot.ConfigurationHash);
                }

                return false;
            }

            consumed?.Add(removedSlot);
            return true;
        }

        private void RestoreConsumed(Inventory inventory, List<InventorySlot> consumed)
        {
            if (inventory == null || consumed == null || consumed.Count == 0)
                return;

            for (int i = 0; i < consumed.Count; ++i)
            {
                InventorySlot slot = consumed[i];
                ItemDefinition definition = ItemDefinition.Get(slot.ItemDefinitionId);
                if (definition == null || slot.Quantity == 0)
                    continue;

                inventory.AddItem(definition, slot.Quantity, slot.ConfigurationHash);
            }
        }

        private static string GenerateConfigurationHash(ItemDefinition flora, NetworkString<_64> floraConfig,
            ItemDefinition essence, NetworkString<_64> essenceConfig, ItemDefinition ore, NetworkString<_64> oreConfig,
            ItemDefinition liquid, NetworkString<_64> liquidConfig)
        {
            string floraPart = BuildPart(flora, floraConfig);
            string essencePart = BuildPart(essence, essenceConfig);
            string orePart = BuildPart(ore, oreConfig);
            string liquidPart = BuildPart(liquid, liquidConfig);

            string combined = string.Join("|", floraPart, essencePart, orePart, liquidPart);
            return Hash128.Compute(combined).ToString();
        }

        private static string BuildPart(ItemDefinition definition, NetworkString<_64> configurationHash)
        {
            string config = configurationHash.ToString();
            string idPart = definition != null ? definition.ID.ToString() : "0";

            if (string.IsNullOrWhiteSpace(config) == true)
                return idPart;

            return $"{idPart}:{config}";
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
