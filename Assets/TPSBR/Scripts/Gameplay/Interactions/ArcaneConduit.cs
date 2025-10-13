using Fusion;
using TPSBR.UI;
using UnityEngine;

namespace TPSBR
{
        public sealed class ArcaneConduit : ContextBehaviour, IInteraction
        {
                [Header("Interaction")]
                [SerializeField]
                private string _interactionName = "Arcane Conduit";
                [SerializeField, TextArea]
                private string _interactionDescription = "Channel mystical energies to enhance your weapons.";
                [SerializeField]
                private Transform _hudPivot;
                [SerializeField]
                private Collider _interactionCollider;

                string  IInteraction.Name        => _interactionName;
                string  IInteraction.Description => _interactionDescription;
                Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : transform.position;
                bool    IInteraction.IsActive    => isActiveAndEnabled == true && (_interactionCollider == null || (_interactionCollider.enabled == true && _interactionCollider.gameObject.activeInHierarchy == true));

                public void Interact(Agent agent)
                {
                        if (agent == null)
                                return;

                        if (HasStateAuthority == false)
                                return;

                        RPC_RequestOpen(agent.Object.InputAuthority, agent.Object.Id);
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

                        OpenItemContextView(agent);
                }

                private void OpenItemContextView(Agent agent)
                {
                        if (Context == null || Context.UI == null)
                                return;

                        UIItemContextView view = Context.UI.Get<UIItemContextView>();

                        if (view == null)
                        {
                                Debug.LogWarning($"{nameof(UIItemContextView)} is not available in the current UI setup.");
                                return;
                        }

                        view.SetSourceAgent(agent);
                        Context.UI.Open(view);
                }

#if UNITY_EDITOR
                private void OnValidate()
                {
                        if (_interactionCollider == null)
                        {
                                _interactionCollider = GetComponent<Collider>();
                        }
                }
#endif
        }
}
