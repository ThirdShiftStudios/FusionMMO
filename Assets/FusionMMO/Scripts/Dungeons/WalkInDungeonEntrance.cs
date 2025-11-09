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

        private const float DungeonLoadingHideDelay = 3f;

        [Networked]
        private NetworkObjectRef _spawnedDungeonRef { get; set; }

        private NetworkedDungeon _spawnedDungeon;
        private PlayerRef _activatingPlayerRef = PlayerRef.None;
        private int _dungeonRequestId;

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            if (_entrance == null || _dungeonPrefab == null || Runner == null)
            {
                return;
            }

            TryResolveSpawnedDungeon();

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

                HandlePlayerAtEntrance(playerComponent);
                break;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            AttachToDungeon(null);
        }

        private void HandlePlayerAtEntrance(TPSBR.Player player)
        {
            if (player == null)
            {
                return;
            }

            if (_spawnedDungeon == null)
            {
                StartDungeonGeneration(player);
                return;
            }

            if (_spawnedDungeon.IsGenerated == false)
            {
                return;
            }

            MovePlayerToDungeon(player);
        }

        private void StartDungeonGeneration(TPSBR.Player player)
        {
            if (Runner == null || _dungeonPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = new Vector3(1000f, 1000f, 1000f);
            Quaternion spawnRotation = Quaternion.identity;

            var spawnedDungeon = Runner.Spawn(_dungeonPrefab, spawnPosition, spawnRotation);

            AttachToDungeon(spawnedDungeon);

            if (spawnedDungeon != null)
            {
                if (HasStateAuthority == true)
                {
                    _spawnedDungeonRef = spawnedDungeon.Object;
                }
                _activatingPlayerRef = player.Object.InputAuthority;
                _dungeonRequestId = unchecked(_dungeonRequestId + 1);
                player.RequestDungeonLoadingScreen(DungeonLoadingHideDelay, _dungeonRequestId);
                spawnedDungeon.RandomizeSeed();
            }
        }

        private void TryResolveSpawnedDungeon()
        {
            if (_spawnedDungeon != null)
            {
                return;
            }

            if (Runner == null || _spawnedDungeonRef.IsValid == false)
            {
                return;
            }

            if (Runner.TryFindObject(_spawnedDungeonRef, out var networkObject) == false)
            {
                return;
            }

            AttachToDungeon(networkObject.GetComponent<NetworkedDungeon>());
        }

        private void AttachToDungeon(NetworkedDungeon dungeon)
        {
            if (_spawnedDungeon == dungeon)
            {
                return;
            }

            if (_spawnedDungeon != null)
            {
                _spawnedDungeon.DungeonGenerated -= OnDungeonGenerated;
            }

            _spawnedDungeon = dungeon;

            if (_spawnedDungeon == null)
            {
                if (HasStateAuthority == true)
                {
                    _spawnedDungeonRef = default;
                }
                return;
            }

            _spawnedDungeon.DungeonGenerated += OnDungeonGenerated;

            if (HasStateAuthority == true && _spawnedDungeonRef.IsValid == false && _spawnedDungeon.Object != null)
            {
                _spawnedDungeonRef = _spawnedDungeon.Object;
            }
        }

        private void OnDungeonGenerated(NetworkedDungeon dungeon)
        {
            if (Runner == null || dungeon != _spawnedDungeon)
            {
                return;
            }

            if (_activatingPlayerRef.IsValid == true && Runner.TryGetPlayerObject(_activatingPlayerRef, out var playerObject) == true && playerObject != null)
            {
                var player = playerObject.GetComponent<TPSBR.Player>();
                MovePlayerToDungeon(player);
                player?.NotifyDungeonGenerationComplete(_dungeonRequestId);
            }

            _activatingPlayerRef = PlayerRef.None;
        }

        private void MovePlayerToDungeon(TPSBR.Player player)
        {
            if (player == null || _spawnedDungeon == null)
            {
                return;
            }

            var agent = player.ActiveAgent;
            if (agent == null)
            {
                return;
            }

            var spawnPoint = _spawnedDungeon.GetComponentInChildren<TPSBR.DungeonSpawnPoint>();
            if (spawnPoint == null)
            {
                return;
            }

            var character = agent.Character;
            var controller = character != null ? character.CharacterController : null;
            if (controller == null)
            {
                return;
            }

            Transform spawnTransform = spawnPoint.transform;
            controller.SetPosition(spawnTransform.position);
            controller.SetLookRotation(spawnTransform.rotation);
            agent.transform.SetPositionAndRotation(spawnTransform.position, spawnTransform.rotation);
        }
    }
}
