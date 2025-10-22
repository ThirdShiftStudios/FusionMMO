using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

        [Header("Interaction Camera")]
        [SerializeField, FormerlySerializedAs("_cameraViewTransform")]
        private Transform _cameraTransform;

        private Agent _cameraAgent;

        protected Transform CameraTransform => _cameraTransform;
        protected Agent CurrentCameraAgent => _cameraAgent;

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
    }
}
