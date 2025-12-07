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

        public event Action<RecipeDefinition, float, float, int> CraftStarted;
        public event Action<RecipeDefinition> CraftCompleted;
        public event Action<RecipeDefinition> CraftCancelled;

        private UICraftingStationView _craftingView;
        private readonly Dictionary<NetworkId, ActiveCraft> _activeCrafts = new Dictionary<NetworkId, ActiveCraft>();
        private readonly List<NetworkId> _craftsToFinalize = new List<NetworkId>();

        private RecipeDefinition _localActiveRecipe;
        private float _localCraftStartTime;
        private float _localCraftDuration;
        private int _localCraftQuantity;
        private NetworkId _localCraftAgentId;
        private bool _localCraftInProgress;

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

        public void RequestCancelCraft(Agent agent)
        {
            if (agent == null)
                return;

            if (HasStateAuthority == true)
            {
                CancelCraft(agent);
            }
            else
            {
                RPC_RequestCancel(agent.Object.InputAuthority, agent.Object.Id);
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

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (HasStateAuthority == false)
                return;

            if (Runner == null || _activeCrafts.Count == 0)
                return;

            foreach (KeyValuePair<NetworkId, ActiveCraft> pair in _activeCrafts)
            {
                ActiveCraft craft = pair.Value;
                if (craft.Timer.IsRunning == false || craft.Timer.Expired(Runner) == true)
                {
                    _craftsToFinalize.Add(pair.Key);
                }
            }

            if (_craftsToFinalize.Count == 0)
                return;

            for (int i = 0; i < _craftsToFinalize.Count; ++i)
            {
                NetworkId agentId = _craftsToFinalize[i];
                if (_activeCrafts.TryGetValue(agentId, out ActiveCraft craft) == false)
                    continue;

                CompleteCraft(craft);
                _activeCrafts.Remove(agentId);
            }

            _craftsToFinalize.Clear();
        }

        public bool TryGetLocalCraftState(out RecipeDefinition recipe, out float startTime, out float duration, out int quantity)
        {
            recipe = _localCraftInProgress ? _localActiveRecipe : null;
            startTime = _localCraftInProgress ? _localCraftStartTime : 0f;
            duration = _localCraftInProgress ? _localCraftDuration : 0f;
            quantity = _localCraftInProgress ? _localCraftQuantity : 0;

            return _localCraftInProgress;
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

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
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

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestCancel(PlayerRef playerRef, NetworkId agentId)
        {
            if (TryResolveAgent(playerRef, agentId, out Agent agent) == false)
                return;

            CancelCraft(agent);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_CraftStarted(PlayerRef playerRef, NetworkId agentId, int recipeId, int quantity, float duration)
        {
            if (Runner == null || Runner.LocalPlayer != playerRef)
                return;

            RecipeDefinition recipe = FindRecipe(recipeId);
            if (recipe == null)
                return;

            _localCraftInProgress = true;
            _localActiveRecipe = recipe;
            _localCraftStartTime = Time.unscaledTime;
            _localCraftDuration = Mathf.Max(0f, duration);
            _localCraftQuantity = quantity;
            _localCraftAgentId = agentId;

            CraftStarted?.Invoke(recipe, _localCraftStartTime, _localCraftDuration, quantity);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_CraftCompleted(PlayerRef playerRef, NetworkId agentId, int recipeId)
        {
            if (Runner == null || Runner.LocalPlayer != playerRef)
                return;

            RecipeDefinition recipe = FindRecipe(recipeId);
            RecipeDefinition previousRecipe = _localActiveRecipe;
            bool wasActive = _localCraftInProgress == true && agentId == _localCraftAgentId;

            if (wasActive == true)
            {
                _localCraftInProgress = false;
                _localActiveRecipe = null;
                _localCraftStartTime = 0f;
                _localCraftDuration = 0f;
                _localCraftQuantity = 0;
                _localCraftAgentId = default;
            }

            RecipeDefinition targetRecipe = recipe != null ? recipe : previousRecipe;

            if (targetRecipe != null && wasActive == true)
            {
                CraftCompleted?.Invoke(targetRecipe);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_CraftCancelled(PlayerRef playerRef, NetworkId agentId, int recipeId)
        {
            if (Runner == null || Runner.LocalPlayer != playerRef)
                return;

            RecipeDefinition recipe = FindRecipe(recipeId);
            RecipeDefinition previousRecipe = _localActiveRecipe;
            bool wasActive = _localCraftInProgress == true && agentId == _localCraftAgentId;

            if (wasActive == true)
            {
                _localCraftInProgress = false;
                _localActiveRecipe = null;
                _localCraftStartTime = 0f;
                _localCraftDuration = 0f;
                _localCraftQuantity = 0;
                _localCraftAgentId = default;
            }

            RecipeDefinition targetRecipe = recipe != null ? recipe : previousRecipe;

            if (targetRecipe != null && wasActive == true)
            {
                CraftCancelled?.Invoke(targetRecipe);
            }
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

            float craftingTime = recipe.CraftingTime;

            if (craftingTime <= 0f || Runner == null)
            {
                ConsumeInputs(inventory, recipe, craftCount);
                GrantOutputs(inventory, recipe, craftCount, agent);
                return;
            }

            NetworkObject agentObject = agent.Object;
            if (agentObject == null)
                return;

            NetworkId agentId = agentObject.Id;

            if (_activeCrafts.ContainsKey(agentId) == true)
                return;

            ConsumeInputs(inventory, recipe, craftCount);

            if (StartCraft(agent, inventory, recipe, craftCount, craftingTime) == false)
            {
                ReturnInputs(inventory, recipe, craftCount);
            }
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

        private bool StartCraft(Agent agent, Inventory inventory, RecipeDefinition recipe, int crafts, float craftingTimePerCraft)
        {
            if (Runner == null || agent == null || recipe == null)
                return false;

            NetworkObject agentObject = agent.Object;
            if (agentObject == null)
                return false;

            NetworkId agentId = agentObject.Id;

            if (_activeCrafts.ContainsKey(agentId) == true)
                return false;

            float duration = Mathf.Max(0.01f, craftingTimePerCraft * crafts);

            ActiveCraft craft = new ActiveCraft
            {
                Agent = agent,
                Inventory = inventory,
                Recipe = recipe,
                Crafts = crafts,
                AgentId = agentId,
                PlayerRef = agentObject.InputAuthority,
                Duration = duration,
                Timer = TickTimer.CreateFromSeconds(Runner, duration),
            };

            _activeCrafts[agentId] = craft;

            RPC_CraftStarted(craft.PlayerRef, agentId, recipe.ID, crafts, duration);

            return true;
        }

        private void CompleteCraft(ActiveCraft craft)
        {
            if (craft == null)
                return;

            Inventory inventory = ResolveInventory(craft);

            if (inventory != null && craft.Recipe != null)
            {
                GrantOutputs(inventory, craft.Recipe, craft.Crafts, craft.Agent);
            }
            else if (craft.Recipe != null)
            {
                Debug.LogWarning($"{nameof(CraftingStation)} failed to grant crafted outputs for recipe '{craft.Recipe.Name}' because the inventory could not be resolved.");
            }

            RPC_CraftCompleted(craft.PlayerRef, craft.AgentId, craft.Recipe != null ? craft.Recipe.ID : 0);
        }

        private void CancelCraft(Agent agent)
        {
            if (agent == null || agent.Object == null)
                return;

            NetworkId agentId = agent.Object.Id;

            if (_activeCrafts.TryGetValue(agentId, out ActiveCraft craft) == false)
                return;

            Inventory inventory = ResolveInventory(craft);

            if (inventory != null && craft.Recipe != null)
            {
                ReturnInputs(inventory, craft.Recipe, craft.Crafts);
            }
            else if (craft.Recipe != null)
            {
                Debug.LogWarning($"{nameof(CraftingStation)} failed to refund inputs for recipe '{craft.Recipe.Name}' because the inventory could not be resolved.");
            }

            _activeCrafts.Remove(agentId);
            if (_craftsToFinalize.Count > 0)
            {
                _craftsToFinalize.Remove(agentId);
            }

            RPC_CraftCancelled(craft.PlayerRef, craft.AgentId, craft.Recipe != null ? craft.Recipe.ID : 0);
        }

        private sealed class ActiveCraft
        {
            public Agent Agent;
            public Inventory Inventory;
            public RecipeDefinition Recipe;
            public int Crafts;
            public NetworkId AgentId;
            public PlayerRef PlayerRef;
            public float Duration;
            public TickTimer Timer;
        }

        private Inventory ResolveInventory(ActiveCraft craft)
        {
            if (craft == null)
                return null;

            if (craft.Inventory != null)
                return craft.Inventory;

            if (craft.Agent != null && craft.Agent.Inventory != null)
            {
                craft.Inventory = craft.Agent.Inventory;
                return craft.Inventory;
            }

            if (TryResolveAgent(craft.PlayerRef, craft.AgentId, out Agent resolvedAgent) == true)
            {
                craft.Agent = resolvedAgent;
                craft.Inventory = resolvedAgent != null ? resolvedAgent.Inventory : null;
                return craft.Inventory;
            }

            return null;
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

        private void GrantOutputs(Inventory inventory, RecipeDefinition recipe, int crafts, Agent agent)
        {
            IReadOnlyList<RecipeDefinition.ItemQuantity> outputs = recipe != null ? recipe.Outputs : null;
            GrantItems(inventory, outputs, crafts, recipe, "crafted output");
            GrantProfessionExperience(agent, outputs, crafts);
        }

        private void ReturnInputs(Inventory inventory, RecipeDefinition recipe, int crafts)
        {
            GrantItems(inventory, recipe != null ? recipe.Inputs : null, crafts, recipe, "refunded input");
        }

        private void GrantItems(Inventory inventory, IReadOnlyList<RecipeDefinition.ItemQuantity> items, int crafts, RecipeDefinition recipe, string failureDescriptor)
        {
            if (inventory == null || items == null || items.Count == 0 || crafts <= 0)
                return;

            string recipeName = recipe != null ? recipe.Name : "Unknown";

            for (int i = 0; i < items.Count; ++i)
            {
                RecipeDefinition.ItemQuantity entry = items[i];
                ItemDefinition item = entry.Item;
                int perCraft = entry.Quantity;

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
                        Debug.LogWarning($"{nameof(CraftingStation)} failed to add {failureDescriptor} '{item.Name}' for recipe '{recipeName}'. Remainder: {remainder}");
                        break;
                    }
                }
            }
        }

        private void GrantProfessionExperience(Agent agent, IReadOnlyList<RecipeDefinition.ItemQuantity> outputs, int crafts)
        {
            if (agent == null || outputs == null || outputs.Count == 0 || crafts <= 0)
                return;

            for (int i = 0; i < outputs.Count; ++i)
            {
                RecipeDefinition.ItemQuantity entry = outputs[i];
                ItemDefinition item = entry.Item;

                if (item is IGrantsProfessionExperience experienceGiver == false)
                    continue;

                int quantity = entry.Quantity * crafts;
                if (quantity <= 0)
                    continue;

                agent.GrantProfessionExperience(experienceGiver, quantity);
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
