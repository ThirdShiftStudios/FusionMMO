using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class OreNode : ResourceNode
    {
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

            if (agent != null)
            {
                Professions professions = agent.GetComponent<Professions>();
                professions?.AddExperience(Professions.ProfessionIndex.Mining, 100);
            }
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
