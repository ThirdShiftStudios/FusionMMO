using UnityEngine;
using UnityEngine.Serialization;

namespace TPSBR
{
        public abstract class ItemExchangePoint : ContextBehaviour
        {
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
