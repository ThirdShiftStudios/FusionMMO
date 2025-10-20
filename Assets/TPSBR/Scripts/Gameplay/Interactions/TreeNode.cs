using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class TreeNode : ResourceNode
    {
        public event Action<Agent> ChoppingStarted;
        public event Action<Agent> ChoppingCancelled;
        public event Action<Agent> ChoppingCompleted;

        public bool TryBeginChopping(Agent agent)
        {
            return TryBeginInteraction(agent);
        }

        public void CancelChopping(Agent agent)
        {
            CancelInteraction(agent);
        }

        public bool TickChopping(float deltaTime, Agent agent)
        {
            return TickInteraction(deltaTime, agent);
        }

        public void ResetTree()
        {
            ResetResource();
        }

        protected override string GetDefaultInteractionName()
        {
            return "Chop Tree";
        }

        protected override string GetDefaultInteractionDescription()
        {
            return "Hold interact to chop down this tree.";
        }

        protected override int GetToolSpeed(Agent agent)
        {
            return agent?.Inventory?.GetWoodAxeSpeed() ?? 0;
        }

        protected override void OnInteractionStarted(Agent agent)
        {
            base.OnInteractionStarted(agent);
            ChoppingStarted?.Invoke(agent);
        }

        protected override void OnInteractionCancelled(Agent agent)
        {
            base.OnInteractionCancelled(agent);
            ChoppingCancelled?.Invoke(agent);
        }

        protected override void OnInteractionCompleted(Agent agent)
        {
            base.OnInteractionCompleted(agent);
            EvaluateLootTable(agent);
            ChoppingCompleted?.Invoke(agent);
        }
    }
}
