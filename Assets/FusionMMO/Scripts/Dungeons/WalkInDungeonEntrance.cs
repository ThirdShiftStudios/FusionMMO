using Fusion;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    public class WalkInDungeonEntrance : NetworkBehaviour
    {
        [SerializeField]
        private Transform _entrance;

        [SerializeField]
        private NetworkedDungeon _dungeonPrefab;

        [SerializeField]
        private float _activationDistance = 5f;

        private NetworkedDungeon _spawnedDungeon;

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            if (_spawnedDungeon != null)
            {
                return;
            }

            if (_entrance == null || _dungeonPrefab == null || Runner == null)
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
                if (playerComponent == false) { continue; }
                var agent = playerComponent.ActiveAgent;
                if (agent == false) { continue; }

            

                Vector3 toEntrance = agent.transform.position - _entrance.position;
                if (toEntrance.sqrMagnitude > sqrActivationDistance)
                {
                    continue;
                }

                SpawnDungeon();
                break;
            }
        }

        private void SpawnDungeon()
        {
            if (Runner == null || _dungeonPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = new Vector3(1000f, 1000f, 1000f);
            Quaternion spawnRotation = Quaternion.identity;

            _spawnedDungeon = Runner.Spawn(_dungeonPrefab, spawnPosition, spawnRotation);

            if (_spawnedDungeon != null)
            {
                _spawnedDungeon.RandomizeSeed();
            }

            RPC_ShowLoadingScene();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ShowLoadingScene()
        {
            var networking = TPSBR.Global.Networking;
            if (networking != null)
            {
                networking.RequestLoadingScene(true);
            }
        }
    }
}
