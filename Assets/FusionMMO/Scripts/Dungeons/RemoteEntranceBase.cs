using Fusion;
using FusionMMO.Loading;
using TPSBR;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    public abstract class RemoteEntranceBase : NetworkBehaviour, IInteraction
    {
        [Header("Interaction")]
        [SerializeField] private string _interactionName = "Enter";
        [SerializeField, TextArea] private string _interactionDescription = "Enter the remote location.";
        [SerializeField] private Transform _hudPivot;
        [SerializeField] private Collider _interactionCollider;

        [Header("References")]
        [SerializeField] private Transform _entrance;

        [SerializeField]
        private LoadingScreenDefinition _loadingScreenDefinition;

        public Transform EntranceTransform => _entrance;

        protected LoadingScreenDefinition LoadingScreen => _loadingScreenDefinition;

        public void RequestLoadingScene(PlayerRef playerRef)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            if (Runner == null)
            {
                return;
            }

            RPC_ShowLoadingScene(playerRef);
        }

        string IInteraction.Name => _interactionName;
        string IInteraction.Description => _interactionDescription;
        Vector3 IInteraction.HUDPosition => _hudPivot != null ? _hudPivot.position : (_entrance != null ? _entrance.position : transform.position);
        bool IInteraction.IsActive => isActiveAndEnabled == true && (_interactionCollider == null || (_interactionCollider.enabled == true && _interactionCollider.gameObject.activeInHierarchy == true));

        bool IInteraction.Interact(in InteractionContext context, out string message)
        {
            message = string.Empty;

            if (HasStateAuthority == false)
            {
                return false;
            }

            var agent = context.Agent;
            if (agent == null)
            {
                return false;
            }

            var agentObject = agent.Object;
            if (agentObject == null)
            {
                return false;
            }

            PlayerRef playerRef = agentObject.InputAuthority;
            if (playerRef == PlayerRef.None)
            {
                return false;
            }

            if (TryHandleEntry(playerRef))
            {
                RPC_ShowLoadingScene(playerRef);
                return true;
            }

            message = "Unable to enter.";
            return false;
        }

        protected abstract bool TryHandleEntry(PlayerRef playerRef);

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ShowLoadingScene(PlayerRef targetPlayer)
        {
            if (Runner == null || Runner.LocalPlayer != targetPlayer)
            {
                return;
            }

            var networking = TPSBR.Global.Networking;
            if (networking != null)
            {
                if (_loadingScreenDefinition != null)
                {
                    var loadingSprite = _loadingScreenDefinition.GetRandomLoadingScreen();
                    if (loadingSprite != null)
                    {
                        networking.SetLoadingSceneSprite(loadingSprite);
                    }
                }

                networking.RequestLoadingScene(true);
            }
        }
    }
}
