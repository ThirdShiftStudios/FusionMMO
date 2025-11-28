using Fusion;
using TPSBR.UI;
using UnityEngine;

namespace TPSBR
{
    public sealed class AlchemyStation : ItemExchangePoint
    {
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
                alchemyView.Configure(agent);
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
    }
}
