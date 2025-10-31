using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class RuneNode : ResourceNode
    {
        public event Action<Agent> HarvestStarted;
        public event Action<Agent> HarvestCancelled;
        public event Action<Agent> HarvestCompleted;

        [SerializeField]
        private RuneResourceDefinition _rewardDefinition;
        [SerializeField, Min(1)]
        private byte _rewardQuantity = 1;

        public bool TryBeginHarvesting(Agent agent)
        {
            return TryBeginInteraction(agent);
        }

        public void CancelHarvesting(Agent agent)
        {
            CancelInteraction(agent);
        }

        public bool TickHarvesting(float deltaTime, Agent agent)
        {
            return TickInteraction(deltaTime, agent);
        }

        public void ResetNode()
        {
            ResetResource();
        }

        protected override string GetDefaultInteractionName()
        {
            return "Harvest Runes";
        }

        protected override string GetDefaultInteractionDescription()
        {
            return "Hold interact to harvest this rune node.";
        }

        protected override int GetToolSpeed(Agent agent)
        {
            return agent?.Inventory?.GetPickaxeSpeed() ?? 0;
        }

        protected override void OnInteractionStarted(Agent agent)
        {
            base.OnInteractionStarted(agent);
            HarvestStarted?.Invoke(agent);
        }

        protected override void OnInteractionCancelled(Agent agent)
        {
            base.OnInteractionCancelled(agent);
            HarvestCancelled?.Invoke(agent);
        }

        protected override void OnInteractionCompleted(Agent agent)
        {
            base.OnInteractionCompleted(agent);
            HarvestCompleted?.Invoke(agent);
            GrantReward(agent);
            EvaluateLootTable(agent);
        }

        private void GrantReward(Agent agent)
        {
            if (_rewardDefinition == null)
                return;

            if (_rewardQuantity == 0)
                return;

            if (agent == null)
                return;

            Inventory inventory = agent.Inventory;
            if (inventory == null)
                return;

            inventory.AddItem(_rewardDefinition, _rewardQuantity);
            agent.GrantProfessionExperience(_rewardDefinition, _rewardQuantity);
        }
    }
}
