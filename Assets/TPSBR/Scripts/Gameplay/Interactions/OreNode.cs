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
        public event Action<Agent> MiningImpact;

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

        public void TriggerMiningImpact(Agent agent)
        {
            NetworkId agentId = GetAgentId(agent);

            if (Runner == null)
            {
                MiningImpact?.Invoke(agent);
                return;
            }

            if (HasStateAuthority == true)
            {
                RPC_PlayMiningImpact(agentId);
            }
            else
            {
                RPC_RequestMiningImpact(agentId);
            }
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

        private NetworkId GetAgentId(Agent agent)
        {
            NetworkObject networkObject = agent != null ? agent.Object : null;

            if (networkObject == null)
                return NetworkId.None;

            return networkObject.Id;
        }

        private Agent ResolveAgent(NetworkId agentId)
        {
            if (Runner == null)
                return null;

            if (agentId == NetworkId.None)
                return null;

            if (Runner.TryFindObject(agentId, out NetworkObject networkObject) == false)
                return null;

            return networkObject.GetComponent<Agent>();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestMiningImpact(NetworkId agentId)
        {
            if (HasStateAuthority == false)
                return;

            RPC_PlayMiningImpact(agentId);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_PlayMiningImpact(NetworkId agentId)
        {
            Agent agent = ResolveAgent(agentId);
            MiningImpact?.Invoke(agent);
        }
    }
}
