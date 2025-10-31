using UnityEngine;
using UnityEngine.Serialization;
using TPSBR.UI;

namespace TPSBR
{
    public abstract class ItemExchangePoint : ContextBehaviour, IInteraction
    {
        [Header("Interaction")]
        [SerializeField]
        private string _interactionName = "Item Vendor";
        [SerializeField, TextArea]
        private string _interactionDescription = "Browse and purchase configured items.";
        [SerializeField]
        private Transform _hudPivot;
        [SerializeField]
        private protected Collider _interactionCollider;

        string IInteraction.Name => _interactionName;
        string IInteraction.Description => _interactionDescription;
        Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
        bool IInteraction.IsActive => isActiveAndEnabled == true && (_interactionCollider == null || (_interactionCollider.enabled == true && _interactionCollider.gameObject.activeInHierarchy == true));

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

        [Header("Interaction Camera")]
        [SerializeField, FormerlySerializedAs("_cameraViewTransform")]
        private Transform _cameraTransform;

        private Agent _cameraAgent;
        private UIView _activeUIView;

        protected Transform CameraTransform => _cameraTransform;
        protected Agent CurrentCameraAgent => _cameraAgent;
        protected virtual UIView _uiView => null;
        protected UIView ActiveUIView => _activeUIView;

        protected abstract bool HandleInteraction(Agent agent, out string message);

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

        protected bool OpenExchangeView(Agent agent)
        {
            if (agent == null)
                return false;

            if (Context == null || Context.UI == null)
                return false;

            UIView view = _uiView;

            if (view == null)
            {
                Debug.LogWarning($"{GetType().Name} could not resolve {nameof(UIView)} via {nameof(_uiView)} override.");
                return false;
            }

            if (ConfigureExchangeView(agent, view) == false)
                return false;

            if (_activeUIView != null && _activeUIView != view)
            {
                _activeUIView.HasClosed -= HandleActiveViewClosed;
                UnsubscribeFromViewEvents(_activeUIView);
            }

            _activeUIView = view;

            UnsubscribeFromViewEvents(_activeUIView);
            SubscribeToViewEvents(_activeUIView);

            _activeUIView.HasClosed -= HandleActiveViewClosed;
            _activeUIView.HasClosed += HandleActiveViewClosed;

            Context.UI.Open(_activeUIView);
            ApplyCameraAuthority(agent);

            OnExchangeViewOpened(_activeUIView, agent);

            return true;
        }

        protected void CloseExchangeView()
        {
            if (_activeUIView == null)
                return;

            if (Context != null && Context.UI != null)
            {
                Context.UI.Close(_activeUIView);
            }
            else
            {
                _activeUIView.Close();
            }
        }

        protected virtual bool ConfigureExchangeView(Agent agent, UIView view)
        {
            _ = agent;
            _ = view;
            return true;
        }

        protected virtual void SubscribeToViewEvents(UIView view)
        {
            _ = view;
        }

        protected virtual void UnsubscribeFromViewEvents(UIView view)
        {
            _ = view;
        }

        protected virtual void OnExchangeViewOpened(UIView view, Agent agent)
        {
            _ = view;
            _ = agent;
        }

        protected virtual void OnExchangeViewClosed(UIView view)
        {
            _ = view;
        }

        protected virtual void OnDisable()
        {
            CloseExchangeView();
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

        private void HandleActiveViewClosed()
        {
            if (_activeUIView != null)
            {
                _activeUIView.HasClosed -= HandleActiveViewClosed;
                UnsubscribeFromViewEvents(_activeUIView);
                OnExchangeViewClosed(_activeUIView);
                _activeUIView = null;
            }

            RestoreCameraAuthority();
        }
    }
}
