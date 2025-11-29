using System.Collections.Generic;
using System.Linq;
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

        private readonly struct AlchemyIngredient
        {
            public ItemDefinition Definition { get; }
            public NetworkString<_64> ConfigurationHash { get; }
            public int Quantity { get; }

            public AlchemyIngredient(ItemDefinition definition, NetworkString<_64> configurationHash, int quantity)
            {
                Definition = definition;
                ConfigurationHash = configurationHash;
                Quantity = quantity;
            }
        }

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

        public void RequestAlchemize(Agent agent, IReadOnlyList<UIAlchemyStationView.InventoryEntry> items)
        {
            if (agent == null)
                return;

            if (items == null || items.Count == 0)
                return;

            List<int> slotIndices = new List<int>(items.Count);
            for (int i = 0; i < items.Count; ++i)
            {
                slotIndices.Add(items[i].SlotIndex);
            }

            if (HasStateAuthority == true)
            {
                ProcessAlchemy(agent, slotIndices);
            }
            else
            {
                RPC_RequestAlchemize(agent.Object.InputAuthority, agent.Object.Id, slotIndices.ToArray());
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestAlchemize(PlayerRef playerRef, NetworkId agentId, int[] slotIndices)
        {
            if (TryResolveAgent(playerRef, agentId, out Agent agent) == false)
                return;

            ProcessAlchemy(agent, slotIndices);
        }

        private void ProcessAlchemy(Agent agent, IReadOnlyList<int> slotIndices)
        {
            if (HasStateAuthority == false || _alchemyDefinition == null)
                return;

            Inventory inventory = agent != null ? agent.Inventory : null;
            if (inventory == null)
                return;

            if (slotIndices == null || slotIndices.Count == 0)
                return;

            Dictionary<int, int> slotQuantities = new Dictionary<int, int>();
            for (int i = 0; i < slotIndices.Count; ++i)
            {
                int slotIndex = slotIndices[i];
                if (slotQuantities.ContainsKey(slotIndex) == true)
                {
                    ++slotQuantities[slotIndex];
                }
                else
                {
                    slotQuantities[slotIndex] = 1;
                }
            }

            Dictionary<int, AlchemyIngredient> slotIngredients = new Dictionary<int, AlchemyIngredient>(slotQuantities.Count);
            List<AlchemyIngredient> ingredients = new List<AlchemyIngredient>(slotQuantities.Count);
            int floraCount = 0;
            int essenceCount = 0;
            int oreCount = 0;
            int liquidCount = 0;

            foreach (KeyValuePair<int, int> slotQuantity in slotQuantities)
            {
                if (TryGetIngredient(inventory, slotQuantity.Key, out InventorySlot slot, out ItemDefinition definition, out AlchemyCategory category) == false)
                    return;

                int quantity = slotQuantity.Value;

                switch (category)
                {
                    case AlchemyCategory.Flora:
                        floraCount += quantity;
                        break;
                    case AlchemyCategory.Essence:
                        essenceCount += quantity;
                        break;
                    case AlchemyCategory.Ore:
                        oreCount += quantity;
                        break;
                    case AlchemyCategory.BaseLiquid:
                        liquidCount += quantity;
                        break;
                }

                AlchemyIngredient ingredient = new AlchemyIngredient(definition, slot.ConfigurationHash, quantity);
                ingredients.Add(ingredient);
                slotIngredients[slotQuantity.Key] = ingredient;
            }

            if (floraCount == 0 || essenceCount == 0 || oreCount == 0 || liquidCount == 0)
                return;

            List<InventorySlot> consumed = new List<InventorySlot>(ingredients.Count);

            foreach (KeyValuePair<int, AlchemyIngredient> slotIngredient in slotIngredients)
            {
                if (TryConsumeIngredient(inventory, slotIngredient.Key, slotIngredient.Value, consumed) == false)
                {
                    RestoreConsumed(inventory, consumed);
                    return;
                }
            }

            string configurationHash = GenerateConfigurationHash(ingredients);

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

        private static bool TryGetIngredient(Inventory inventory, int slotIndex, out InventorySlot slot, out ItemDefinition definition, out AlchemyCategory category)
        {
            slot = default;
            definition = null;
            category = default;

            if (slotIndex < 0 || slotIndex >= inventory.InventorySize)
                return false;

            slot = inventory.GetItemSlot(slotIndex);
            if (slot.IsEmpty == true)
                return false;

            definition = ItemDefinition.Get(slot.ItemDefinitionId);
            if (definition == null)
                return false;

            if (definition is FloraResource)
            {
                category = AlchemyCategory.Flora;
            }
            else if (definition is EssenceResource)
            {
                category = AlchemyCategory.Essence;
            }
            else if (definition is OreResource)
            {
                category = AlchemyCategory.Ore;
            }
            else if (definition is BaseLiquid)
            {
                category = AlchemyCategory.BaseLiquid;
            }
            else
            {
                return false;
            }

            return true;
        }

        private static bool TryConsumeIngredient(Inventory inventory, int slotIndex, AlchemyIngredient ingredient, List<InventorySlot> consumed)
        {
            if (ingredient.Quantity <= 0)
                return false;

            int remaining = ingredient.Quantity;
            while (remaining > 0)
            {
                byte removeQuantity = (byte)Mathf.Min(byte.MaxValue, remaining);
                if (inventory.TryExtractInventoryItem(slotIndex, removeQuantity, out InventorySlot removedSlot) == false)
                    return false;

                if (removedSlot.Quantity == 0)
                    return false;

                if (removedSlot.ItemDefinitionId != (ingredient.Definition != null ? ingredient.Definition.ID : 0))
                {
                    ItemDefinition removedDefinition = ItemDefinition.Get(removedSlot.ItemDefinitionId);
                    if (removedDefinition != null)
                    {
                        inventory.AddItem(removedDefinition, removedSlot.Quantity, removedSlot.ConfigurationHash);
                    }

                    return false;
                }

                consumed?.Add(removedSlot);

                remaining -= removeQuantity;
            }
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

        private static string GenerateConfigurationHash(List<AlchemyIngredient> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
                return string.Empty;

            var parts = ingredients
                .Where(ingredient => ingredient.Quantity > 0)
                .GroupBy(ingredient => new
                {
                    ingredient.Definition,
                    Configuration = ingredient.ConfigurationHash.ToString(),
                })
                .Select(group => BuildPart(group.Key.Definition, group.Key.Configuration, group.Sum(ingredient => ingredient.Quantity)))
                .Where(part => string.IsNullOrWhiteSpace(part) == false)
                .OrderBy(part => part, System.StringComparer.Ordinal)
                .ToArray();

            string combined = string.Join("|", parts);
            return Hash128.Compute(combined).ToString();
        }

        private static string BuildPart(ItemDefinition definition, string configurationHash, int quantity)
        {
            if (quantity <= 0)
                return string.Empty;

            string idPart = definition != null ? definition.ID.ToString() : "0";

            if (string.IsNullOrWhiteSpace(configurationHash) == true)
                return $"{idPart}x{quantity}";

            return $"{idPart}:{configurationHash}x{quantity}";
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
