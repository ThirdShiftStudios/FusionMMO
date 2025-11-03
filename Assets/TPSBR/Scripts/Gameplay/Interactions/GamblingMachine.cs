using Fusion;
using TPSBR.UI;
using UnityEngine;

namespace TPSBR
{
    public abstract class GamblingMachine : ContextBehaviour, IInteraction
    {
        [Header("Interaction")]
        [SerializeField]
        private string _interactionName = "Gambling Machine";
        [SerializeField, TextArea]
        private string _interactionDescription = "Try your luck.";
        [SerializeField]
        private Transform _hudPivot;
        [SerializeField]
        private protected Collider _interactionCollider;

        [Header("Interaction Camera")]
        [SerializeField]
        private Transform _cameraTransform;

        private Agent _cameraAgent;
        private UIGamblingView _activeView;

        string IInteraction.Name => _interactionName;
        string IInteraction.Description => _interactionDescription;
        Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
        bool IInteraction.IsActive => isActiveAndEnabled == true && (_interactionCollider == null || (_interactionCollider.enabled == true && _interactionCollider.gameObject.activeInHierarchy == true));

        protected Transform CameraTransform => _cameraTransform;
        protected Agent CurrentCameraAgent => _cameraAgent;
        protected UIGamblingView ActiveView => _activeView;

        bool IInteraction.Interact(in InteractionContext context, out string message)
        {
            Agent agent = context.Agent;

            if (agent == null)
            {
                message = "No agent available";
                return false;
            }

            return HandleInteraction(agent, out message);
        }

        protected abstract UIGamblingView ResolveView();

        protected virtual bool ConfigureView(Agent agent, UIGamblingView view)
        {
            if (view == null)
                return false;

            view.Configure(this, agent);
            return true;
        }

        protected virtual void SubscribeToViewEvents(UIGamblingView view)
        {
            _ = view;
        }

        protected virtual void UnsubscribeFromViewEvents(UIGamblingView view)
        {
            _ = view;
        }

        protected virtual void OnViewOpened(UIGamblingView view, Agent agent)
        {
            _ = view;
            _ = agent;
        }

        protected virtual void OnViewClosed(UIGamblingView view)
        {
            if (view != null)
            {
                view.ClearConfiguration(this);
            }
        }

        protected virtual bool HandleInteraction(Agent agent, out string message)
        {
            message = string.Empty;

            if (HasStateAuthority == false)
                return false;

            RPC_RequestOpen(agent.Object.InputAuthority, agent.Object.Id);
            return true;
        }

        protected bool OpenGamblingView(Agent agent)
        {
            if (agent == null)
                return false;

            if (Context == null || Context.UI == null)
                return false;

            UIGamblingView view = ResolveView();

            if (view == null)
            {
                Debug.LogWarning($"{GetType().Name} could not resolve {nameof(UIGamblingView)} via {nameof(ResolveView)} override.");
                return false;
            }

            if (ConfigureView(agent, view) == false)
                return false;

            if (_activeView != null && _activeView != view)
            {
                _activeView.HasClosed -= HandleActiveViewClosed;
                UnsubscribeFromViewEvents(_activeView);
            }

            _activeView = view;

            UnsubscribeFromViewEvents(_activeView);
            SubscribeToViewEvents(_activeView);

            _activeView.HasClosed -= HandleActiveViewClosed;
            _activeView.HasClosed += HandleActiveViewClosed;

            Context.UI.Open(_activeView);
            ApplyCameraAuthority(agent);

            OnViewOpened(_activeView, agent);

            return true;
        }

        protected void CloseGamblingView()
        {
            if (_activeView == null)
                return;

            if (Context != null && Context.UI != null)
            {
                Context.UI.Close(_activeView);
            }
            else
            {
                _activeView.Close();
            }
        }

        protected void ApplyCameraAuthority(Agent agent)
        {
            if (_cameraTransform == null || agent == null)
                return;

            if (agent.Interactions == null)
                return;

            if (_cameraAgent != null && _cameraAgent != agent)
            {
                RestoreCameraAuthority();
            }

            _cameraAgent = agent;
            agent.Interactions.SetInteractionCameraAuthority(_cameraTransform);
        }

        protected void RestoreCameraAuthority()
        {
            if (_cameraAgent == null)
                return;

            Interactions interactions = _cameraAgent.Interactions;
            if (interactions != null)
            {
                interactions.ClearInteractionCameraAuthority(_cameraTransform);
            }

            _cameraAgent = null;
        }

        protected virtual void OnDisable()
        {
            CloseGamblingView();
            RestoreCameraAuthority();
        }

        public override void Render()
        {
            base.Render();

            if (_cameraAgent != null)
            {
                ApplyCameraAuthority(_cameraAgent);
            }
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

            OpenGamblingView(agent);
        }

        private void HandleActiveViewClosed()
        {
            if (_activeView != null)
            {
                _activeView.HasClosed -= HandleActiveViewClosed;
                UnsubscribeFromViewEvents(_activeView);
                OnViewClosed(_activeView);
                _activeView = null;
            }

            RestoreCameraAuthority();
        }
    }
}
