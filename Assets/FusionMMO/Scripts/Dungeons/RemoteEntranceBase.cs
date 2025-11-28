using Fusion;
using FusionMMO.Loading;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    public abstract class RemoteEntranceBase : NetworkBehaviour
    {
        [SerializeField]
        private Transform _entrance;

        [SerializeField]
        private float _activationDistance = 5f;

        [SerializeField]
        private LoadingScreenDefinition _loadingScreenDefinition;

        public Transform EntranceTransform => _entrance;

        protected LoadingScreenDefinition LoadingScreen => _loadingScreenDefinition;

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (HasStateAuthority == false || _entrance == null || Runner == null)
            {
                return;
            }

            float sqrActivationDistance = _activationDistance * _activationDistance;
            foreach (var player in Runner.ActivePlayers)
            {
                if (Runner.TryGetPlayerObject(player, out var playerObject) == false || playerObject == null)
                {
                    continue;
                }

                var playerComponent = playerObject.GetComponent<TPSBR.Player>();
                if (playerComponent == null)
                {
                    continue;
                }

                var agent = playerComponent.ActiveAgent;
                if (agent == null)
                {
                    continue;
                }

                Vector3 toEntrance = agent.transform.position - _entrance.position;
                if (toEntrance.sqrMagnitude > sqrActivationDistance)
                {
                    continue;
                }

                if (TryHandleEntry(player))
                {
                    RPC_ShowLoadingScene(player);
                }
            }
        }

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
