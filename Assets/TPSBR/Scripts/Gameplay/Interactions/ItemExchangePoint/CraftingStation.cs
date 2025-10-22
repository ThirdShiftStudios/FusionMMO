using System;
using System.Collections.Generic;
using Fusion;
using TPSBR.UI;
using TSS.Data;
using UnityEngine;

namespace TPSBR
{
    public abstract class CraftingStation : ItemExchangePoint
    {
        private const int MaxOutputSearchLimit = 1_000_000;

        public IReadOnlyList<RecipeDefinition> Recipes => _recipes ?? Array.Empty<RecipeDefinition>();

        [Header("Recipes")]
        [SerializeField]
        private RecipeDefinition[] _recipes;

        private UICraftingStationView _craftingView;

        public void Interact(Agent agent)
        {
            if (agent == null)
                return;

            if (HasStateAuthority == false)
                return;

            RPC_RequestOpen(agent.Object.InputAuthority, agent.Object.Id);
        }

        public void RequestCraft(Agent agent, RecipeDefinition recipe, int quantity)
        {
            if (agent == null || recipe == null)
                return;

            quantity = Mathf.Max(1, quantity);

            if (HasStateAuthority == true)
            {
                ProcessCraft(agent, recipe, quantity);
            }
            else
            {
                RPC_RequestCraft(agent.Object.InputAuthority, agent.Object.Id, recipe.ID, quantity);
            }
        }

        public int GetCraftableCount(Agent agent, RecipeDefinition recipe)
        {
            if (agent == null || recipe == null)
                return 0;

            Inventory inventory = agent.Inventory;
            if (inventory == null)
                return 0;

            return CalculateCraftableCount(inventory, recipe);
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

            OpenExchangeView(agent);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestCraft(PlayerRef playerRef, NetworkId agentId, int recipeId, int quantity)
        {
            if (TryResolveAgent(playerRef, agentId, out Agent agent) == false)
                return;

            RecipeDefinition recipe = FindRecipe(recipeId);
            if (recipe == null)
                return;

            quantity = Mathf.Max(1, quantity);

            ProcessCraft(agent, recipe, quantity);
        }

        protected override UIView _uiView => GetCraftingView();

private UICraftingStationView GetCraftingView()
{
if (_craftingView == null && Context != null && Context.UI != null)
{
_craftingView = Context.UI.Get<UICraftingStationView>();

if (_craftingView == null)
{
_craftingView = Context.UI.CreateViewFromResource<UICraftingStationView>(UICraftingStationView.ResourcePath);
}
}

return _craftingView;
}

        protected override bool ConfigureExchangeView(Agent agent, UIView view)
        {
            if (view is UICraftingStationView craftingView)
            {
                craftingView.Configure(this, agent);
                return true;
            }

            Debug.LogWarning($"{nameof(UICraftingStationView)} is not available in the current UI setup.");
            return false;
        }

        private void ProcessCraft(Agent agent, RecipeDefinition recipe, int requestedQuantity)
        {
            if (HasStateAuthority == false)
                return;

            if (agent == null || recipe == null)
                return;

            Inventory inventory = agent.Inventory;
            if (inventory == null)
                return;

            int availableQuantity = CalculateCraftableCount(inventory, recipe);
            if (availableQuantity <= 0)
                return;

            int craftCount = Mathf.Clamp(requestedQuantity, 1, availableQuantity);
            if (craftCount <= 0)
                return;

            ConsumeInputs(inventory, recipe, craftCount);
            GrantOutputs(inventory, recipe, craftCount);
        }

        private int CalculateCraftableCount(Inventory inventory, RecipeDefinition recipe)
        {
            if (inventory == null || recipe == null)
                return 0;

            IReadOnlyList<RecipeDefinition.ItemQuantity> inputs = recipe.Inputs;
            IReadOnlyList<RecipeDefinition.ItemQuantity> outputs = recipe.Outputs;

            int inputBound = int.MaxValue;

            if (inputs != null && inputs.Count > 0)
            {
                inputBound = int.MaxValue;

                for (int i = 0; i < inputs.Count; ++i)
                {
                    RecipeDefinition.ItemQuantity requirement = inputs[i];
                    ItemDefinition item = requirement.Item;
                    int requiredPerCraft = requirement.Quantity;

                    if (item == null || requiredPerCraft <= 0)
                        return 0;

                    int available = CountInventoryQuantity(inventory, item.ID);
                    int possible = available / requiredPerCraft;
                    if (possible < inputBound)
                    {
                        inputBound = possible;
                    }

                    if (inputBound == 0)
                        break;
                }
            }

            if (inputBound <= 0)
                return 0;

            int outputBound = GetMaxCraftableByOutputs(inventory, outputs, inputBound);

            if (inputBound == int.MaxValue)
            {
                return outputBound;
            }

            return Mathf.Min(inputBound, outputBound);
        }

        private static int GetMaxCraftableByOutputs(Inventory inventory, IReadOnlyList<RecipeDefinition.ItemQuantity> outputs, int inputBound)
        {
            if (outputs == null || outputs.Count == 0)
                return inputBound;

            if (inputBound == 0)
                return 0;

            int upperBound = inputBound;

            if (inputBound == int.MaxValue)
            {
                upperBound = DetermineOutputUpperBound(inventory, outputs);
                if (upperBound <= 0)
                    return 0;
                upperBound = Mathf.Min(upperBound, MaxOutputSearchLimit);
            }

            int low = 1;
            int high = Mathf.Max(upperBound, 1);
            int best = 0;

            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                if (CanStoreOutputs(inventory, outputs, mid))
                {
                    best = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return best;
        }

        private static bool CanStoreOutputs(Inventory inventory, IReadOnlyList<RecipeDefinition.ItemQuantity> outputs, int crafts)
        {
            if (crafts <= 0)
                return false;

            if (outputs == null || outputs.Count == 0)
                return true;

            Dictionary<int, int> availableStackSpace = new Dictionary<int, int>();
            int emptySlots = 0;

            int slotCount = inventory.InventorySize;
            for (int i = 0; i < slotCount; ++i)
            {
                InventorySlot slot = inventory.GetItemSlot(i);
                if (slot.IsEmpty == true)
                {
                    emptySlots++;
                    continue;
                }

                ItemDefinition existingDefinition = slot.GetDefinition();
                if (existingDefinition == null)
                    continue;

                int available = Mathf.Max(0, existingDefinition.MaxStack - slot.Quantity);
                if (available <= 0)
                    continue;

                int defId = existingDefinition.ID;
                if (availableStackSpace.TryGetValue(defId, out int current))
                {
                    availableStackSpace[defId] = current + available;
                }
                else
                {
                    availableStackSpace[defId] = available;
                }
            }

            int remainingEmptySlots = emptySlots;

            for (int i = 0; i < outputs.Count; ++i)
            {
                RecipeDefinition.ItemQuantity output = outputs[i];
                ItemDefinition item = output.Item;
                int perCraft = output.Quantity;

                if (item == null || perCraft <= 0)
                    continue;

                int requiredTotal = perCraft * crafts;
                int defId = item.ID;

                if (availableStackSpace.TryGetValue(defId, out int stackSpace) == true && stackSpace > 0)
                {
                    int usedStackSpace = Mathf.Min(stackSpace, requiredTotal);
                    requiredTotal -= usedStackSpace;
                    availableStackSpace[defId] = stackSpace - usedStackSpace;
                }

                if (requiredTotal <= 0)
                    continue;

                int maxPerSlot = Mathf.Max(1, item.MaxStack);
                int slotsNeeded = (requiredTotal + maxPerSlot - 1) / maxPerSlot;

                if (slotsNeeded > remainingEmptySlots)
                    return false;

                remainingEmptySlots -= slotsNeeded;
            }

            return true;
        }

        private static int DetermineOutputUpperBound(Inventory inventory, IReadOnlyList<RecipeDefinition.ItemQuantity> outputs)
        {
            if (outputs == null || outputs.Count == 0)
                return int.MaxValue;

            Dictionary<int, int> availableStackSpace = new Dictionary<int, int>();
            int emptySlots = 0;

            int slotCount = inventory.InventorySize;
            for (int i = 0; i < slotCount; ++i)
            {
                InventorySlot slot = inventory.GetItemSlot(i);
                if (slot.IsEmpty == true)
                {
                    emptySlots++;
                    continue;
                }

                ItemDefinition definition = slot.GetDefinition();
                if (definition == null)
                    continue;

                int available = Mathf.Max(0, definition.MaxStack - slot.Quantity);
                if (available <= 0)
                    continue;

                int defId = definition.ID;
                if (availableStackSpace.TryGetValue(defId, out int current))
                {
                    availableStackSpace[defId] = current + available;
                }
                else
                {
                    availableStackSpace[defId] = available;
                }
            }

            bool hasValidOutput = false;
            int upperBound = int.MaxValue;

            for (int i = 0; i < outputs.Count; ++i)
            {
                RecipeDefinition.ItemQuantity output = outputs[i];
                ItemDefinition item = output.Item;
                int perCraft = output.Quantity;

                if (item == null || perCraft <= 0)
                    continue;

                hasValidOutput = true;

                int defId = item.ID;
                availableStackSpace.TryGetValue(defId, out int stackSpace);

                long capacityFromStacks = stackSpace;
                long capacityFromEmptySlots = (long)emptySlots * Mathf.Max(1, item.MaxStack);
                long totalCapacity = capacityFromStacks + capacityFromEmptySlots;

                long possible = perCraft > 0 ? totalCapacity / perCraft : 0;
                if (possible > int.MaxValue)
                {
                    possible = int.MaxValue;
                }

                int possibleInt = (int)Mathf.Max(0, (int)possible);

                if (possibleInt < upperBound)
                {
                    upperBound = possibleInt;
                }
            }

            if (hasValidOutput == false)
                return int.MaxValue;

            return Mathf.Max(upperBound, 0);
        }

        private static int CountInventoryQuantity(Inventory inventory, int definitionId)
        {
            if (definitionId == 0)
                return 0;

            int total = 0;
            int slotCount = inventory.InventorySize;

            for (int i = 0; i < slotCount; ++i)
            {
                InventorySlot slot = inventory.GetItemSlot(i);
                if (slot.IsEmpty == true)
                    continue;

                if (slot.ItemDefinitionId == definitionId)
                {
                    total += slot.Quantity;
                }
            }

            return total;
        }

        private void ConsumeInputs(Inventory inventory, RecipeDefinition recipe, int crafts)
        {
            IReadOnlyList<RecipeDefinition.ItemQuantity> inputs = recipe.Inputs;
            if (inputs == null || inputs.Count == 0)
                return;

            for (int i = 0; i < inputs.Count; ++i)
            {
                RecipeDefinition.ItemQuantity input = inputs[i];
                ItemDefinition item = input.Item;
                int perCraft = input.Quantity;

                if (item == null || perCraft <= 0)
                    continue;

                int remaining = perCraft * crafts;

                for (int slotIndex = 0; slotIndex < inventory.InventorySize && remaining > 0; ++slotIndex)
                {
                    InventorySlot slot = inventory.GetItemSlot(slotIndex);
                    if (slot.IsEmpty == true)
                        continue;

                    if (slot.ItemDefinitionId != item.ID)
                        continue;

                    int removalAmount = Mathf.Min(remaining, slot.Quantity);
                    if (removalAmount <= 0)
                        continue;

                    byte removeByte = (byte)Mathf.Clamp(removalAmount, 0, byte.MaxValue);
                    if (inventory.TryExtractInventoryItem(slotIndex, removeByte, out InventorySlot removedSlot) == false)
                        continue;

                    remaining -= removedSlot.Quantity;
                }
            }
        }

        private void GrantOutputs(Inventory inventory, RecipeDefinition recipe, int crafts)
        {
            IReadOnlyList<RecipeDefinition.ItemQuantity> outputs = recipe.Outputs;
            if (outputs == null || outputs.Count == 0)
                return;

            for (int i = 0; i < outputs.Count; ++i)
            {
                RecipeDefinition.ItemQuantity output = outputs[i];
                ItemDefinition item = output.Item;
                int perCraft = output.Quantity;

                if (item == null || perCraft <= 0)
                    continue;

                int remaining = perCraft * crafts;

                while (remaining > 0)
                {
                    int toAdd = Mathf.Min(remaining, byte.MaxValue);
                    byte remainder = inventory.AddItem(item, (byte)toAdd);
                    int added = toAdd - remainder;
                    remaining -= added;

                    if (remainder > 0)
                    {
                        Debug.LogWarning($"{nameof(CraftingStation)} failed to add crafted output '{item.Name}' for recipe '{recipe.Name}'. Remainder: {remainder}");
                        break;
                    }
                }
            }
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

        private RecipeDefinition FindRecipe(int id)
        {
            if (_recipes == null || _recipes.Length == 0)
                return null;

            for (int i = 0; i < _recipes.Length; ++i)
            {
                RecipeDefinition recipe = _recipes[i];
                if (recipe == null)
                    continue;

                if (recipe.ID == id)
                    return recipe;
            }

            return null;
        }
    }
}
