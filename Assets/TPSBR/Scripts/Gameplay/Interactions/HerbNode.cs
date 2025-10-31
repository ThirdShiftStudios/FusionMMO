using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class HerbNode : ResourceNode
    {
        public event Action<Agent> HarvestStarted;
        public event Action<Agent> HarvestCancelled;
        public event Action<Agent> HarvestCompleted;

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
            return "Harvest Herbs";
        }

        protected override string GetDefaultInteractionDescription()
        {
            return "Hold interact to harvest these herbs.";
        }

        protected override int GetToolSpeed(Agent agent)
        {
            return 0;
        }

        protected override bool TryStartAnimatedInteraction(in InteractionContext context)
        {
            CharacterAnimationController animationController = context.AnimationController;

            if (animationController == null)
                return false;

            float cancelMoveDistance = context.Interactions != null ? context.Interactions.HerbCancelMoveDistance : 0f;
            float cancelInputThreshold = context.Interactions != null ? context.Interactions.HerbCancelInputThreshold : 0f;

            return animationController.TryStartHerbInteraction(this, cancelMoveDistance, cancelInputThreshold);
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
            EvaluateLootTable(agent);
        }
    }
}
