using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class OreNode : ResourceNode
    {
        private ShatterStone.OreNode _oreNode;
        public event Action<Agent> MiningStarted;
        public event Action<Agent> MiningCancelled;
        public event Action<Agent> MiningCompleted;

        public bool TryBeginMining(Agent agent)
        {
            return TryBeginInteraction(agent);
        }

        public void CancelMining(Agent agent)
        {
            CancelInteraction(agent);
        }

        public bool TickMining(float deltaTime, Agent agent)
        {
            return TickInteraction(deltaTime, agent);
        }

        public override void Spawned()
        {
            base.Spawned();
            _oreNode ??= GetComponentInChildren<ShatterStone.OreNode>();
            _oreNode.ResetNode(0);
        }

        public void ResetNode()
        {
            ResetResource();
        }

        protected override string GetDefaultInteractionName()
        {
            return "Mine Ore";
        }

        protected override string GetDefaultInteractionDescription()
        {
            return "Hold interact to mine this ore node.";
        }

        protected override int GetToolSpeed(Agent agent)
        {
            return agent?.Inventory?.GetPickaxeSpeed() ?? 0;
        }

        protected override bool TryStartAnimatedInteraction(in InteractionContext context)
        {
            CharacterAnimationController animationController = context.AnimationController;

            if (animationController == null)
                return false;

            float cancelMoveDistance = context.Interactions != null ? context.Interactions.OreCancelMoveDistance : 0f;
            float cancelInputThreshold = context.Interactions != null ? context.Interactions.OreCancelInputThreshold : 0f;

            return animationController.TryStartOreInteraction(this, cancelMoveDistance, cancelInputThreshold);
        }

        protected override void OnInteractionStarted(Agent agent)
        {
            base.OnInteractionStarted(agent);
            MiningStarted?.Invoke(agent);
        }

        protected override void OnInteractionCancelled(Agent agent)
        {
            base.OnInteractionCancelled(agent);
            MiningCancelled?.Invoke(agent);
        }

        protected override void OnInteractionCompleted(Agent agent)
        {
            base.OnInteractionCompleted(agent);
            MiningCompleted?.Invoke(agent);
            EvaluateLootTable(agent);

            _oreNode ??= GetComponentInChildren<ShatterStone.OreNode>();
            _oreNode.DestroyNode();
        }

        public void PlayHitEffect()
        {
            Debug.Log($"{this.name} PlayEffect");
            var node = GetComponentInChildren<ShatterStone.OreNode>();
            if (node)
            {
                node.Interact(1);
            }
        }
    }
}
