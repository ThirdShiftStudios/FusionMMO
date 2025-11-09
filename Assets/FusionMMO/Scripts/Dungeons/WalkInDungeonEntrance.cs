using System.Collections.Generic;
using Fusion;
using TPSBR;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    public class WalkInDungeonEntrance : NetworkBehaviour
    {
        private const float LoadingScreenHideDelay = 3f;

        [SerializeField]
        private Transform _entrance;

        [SerializeField]
        private NetworkedDungeon _dungeonPrefab;

        [SerializeField]
        private float _activationDistance = 5f;

        private readonly List<PlayerRef> _queuedTeleportPlayers = new List<PlayerRef>();
        private readonly HashSet<PlayerRef> _loadingScreenPlayers = new HashSet<PlayerRef>();

        private NetworkedDungeon _spawnedDungeon;
        private Transform _cachedDungeonSpawnPoint;
        private Vector3 _activationOrigin;
        private bool _activationOriginInitialized;
        private bool _isGeneratingDungeon;

        public override void Spawned()
        {
            base.Spawned();
            CacheActivationOrigin();
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            CacheActivationOrigin();

            if (_entrance == null || _dungeonPrefab == null || Runner == null)
            {
                return;
            }

            IsDungeonAvailable();

            float sqrActivationDistance = _activationDistance * _activationDistance;

            foreach (var player in Runner.ActivePlayers)
            {
                if (Runner.TryGetPlayerObject(player, out var playerObject) == false || playerObject == null)
                {
                    continue;
                }

                var playerComponent = playerObject.GetComponent<Player>();
                if (playerComponent == null)
                {
                    continue;
                }

                var agent = playerComponent.ActiveAgent;
                if (agent == null)
                {
                    continue;
                }

                Vector3 toEntrance = agent.transform.position - _activationOrigin;
                if (toEntrance.sqrMagnitude > sqrActivationDistance)
                {
                    continue;
                }

                HandlePlayerEntered(playerComponent);
                break;
            }

            if (_isGeneratingDungeon == false && IsDungeonAvailable())
            {
                TeleportPendingPlayers();
            }
        }

        private void OnDisable()
        {
            if (_spawnedDungeon != null)
            {
                _spawnedDungeon.DungeonGenerated -= OnDungeonGenerated;
            }
        }

        private void CacheActivationOrigin()
        {
            if (_activationOriginInitialized == true)
            {
                return;
            }

            if (_entrance != null)
            {
                _activationOrigin = _entrance.position;
            }
            else
            {
                _activationOrigin = transform.position;
            }

            _activationOriginInitialized = true;
        }

        private void HandlePlayerEntered(Player playerComponent)
        {
            PlayerRef playerRef = playerComponent.Object.InputAuthority;

            if (_isGeneratingDungeon == true)
            {
                QueueTeleportPlayer(playerRef);
                ShowLoadingScreen(playerRef, 0f);
                return;
            }

            if (IsDungeonAvailable() == false)
            {
                _isGeneratingDungeon = true;
                QueueTeleportPlayer(playerRef);
                ShowLoadingScreen(playerRef, 0f);
                SpawnDungeon();
                return;
            }

            if (TryTeleportPlayer(playerRef, playerComponent, 0f) == false)
            {
                QueueTeleportPlayer(playerRef);
            }
        }

        private void SpawnDungeon()
        {
            if (Runner == null || _dungeonPrefab == null)
            {
                OnDungeonGenerationFailed();
                return;
            }

            Vector3 spawnPosition = new Vector3(1000f, 1000f, 1000f);
            Quaternion spawnRotation = Quaternion.identity;

            _spawnedDungeon = Runner.Spawn(_dungeonPrefab, spawnPosition, spawnRotation);

            if (_spawnedDungeon != null)
            {
                _cachedDungeonSpawnPoint = null;
                _spawnedDungeon.DungeonGenerated -= OnDungeonGenerated;
                _spawnedDungeon.DungeonGenerated += OnDungeonGenerated;
                _spawnedDungeon.RandomizeSeed();
            }
            else
            {
                OnDungeonGenerationFailed();
            }
        }

        private void OnDungeonGenerated(NetworkedDungeon dungeon)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            if (_spawnedDungeon != dungeon)
            {
                return;
            }

            UpdateCachedSpawnPoint();

            _isGeneratingDungeon = false;

            TeleportPendingPlayers();
        }

        private void UpdateCachedSpawnPoint()
        {
            if (IsDungeonAvailable() == false)
            {
                _cachedDungeonSpawnPoint = null;
                return;
            }

            var spawnPoint = _spawnedDungeon.GetComponentInChildren<DungeonSpawnPoint>();
            if (spawnPoint != null)
            {
                _cachedDungeonSpawnPoint = spawnPoint.transform;

                if (_entrance != null)
                {
                    _entrance.SetPositionAndRotation(_cachedDungeonSpawnPoint.position, _cachedDungeonSpawnPoint.rotation);
                }
            }
        }

        private void TeleportPendingPlayers()
        {
            for (int i = _queuedTeleportPlayers.Count - 1; i >= 0; --i)
            {
                PlayerRef playerRef = _queuedTeleportPlayers[i];

                if (Runner.TryGetPlayerObject(playerRef, out var playerObject) == false || playerObject == null)
                {
                    _queuedTeleportPlayers.RemoveAt(i);
                    HideLoadingScreen(playerRef, 0f);
                    continue;
                }

                var playerComponent = playerObject.GetComponent<Player>();
                if (TryTeleportPlayer(playerRef, playerComponent, LoadingScreenHideDelay) == true)
                {
                    _queuedTeleportPlayers.RemoveAt(i);
                }
            }
        }

        private bool TryTeleportPlayer(PlayerRef playerRef, Player playerComponent, float loadingHideDelay)
        {
            if (playerComponent == null)
            {
                return false;
            }

            var agent = playerComponent.ActiveAgent;
            if (agent == null)
            {
                return false;
            }

            var character = agent.Character;
            if (character == null)
            {
                return false;
            }

            var kcc = character.CharacterController;
            if (kcc == null)
            {
                return false;
            }

            if (TryGetTeleportTransform(out Vector3 targetPosition, out Quaternion targetRotation) == false)
            {
                return false;
            }

            kcc.SetPosition(targetPosition, true);
            kcc.SetLookRotation(targetRotation, false, false);
            kcc.SetDynamicVelocity(Vector3.zero);
            kcc.SetKinematicVelocity(Vector3.zero);

            HideLoadingScreen(playerRef, loadingHideDelay);
            return true;
        }

        private bool TryGetTeleportTransform(out Vector3 position, out Quaternion rotation)
        {
            if (_cachedDungeonSpawnPoint == null)
            {
                UpdateCachedSpawnPoint();
            }

            if (_cachedDungeonSpawnPoint != null)
            {
                position = _cachedDungeonSpawnPoint.position;
                rotation = _cachedDungeonSpawnPoint.rotation;
                return true;
            }

            if (_entrance != null)
            {
                position = _entrance.position;
                rotation = _entrance.rotation;
                return true;
            }

            position = _activationOrigin;
            rotation = transform.rotation;
            return true;
        }

        private void QueueTeleportPlayer(PlayerRef playerRef)
        {
            if (_queuedTeleportPlayers.Contains(playerRef) == false)
            {
                _queuedTeleportPlayers.Add(playerRef);
            }
        }

        private bool IsDungeonAvailable()
        {
            if (_spawnedDungeon == null)
            {
                return false;
            }

            if (_spawnedDungeon.Object == null || _spawnedDungeon.Object.IsValid == false)
            {
                _spawnedDungeon.DungeonGenerated -= OnDungeonGenerated;
                _spawnedDungeon = null;
                _cachedDungeonSpawnPoint = null;
                return false;
            }

            return true;
        }

        private void OnDungeonGenerationFailed()
        {
            _isGeneratingDungeon = false;

            if (_loadingScreenPlayers.Count > 0)
            {
                var players = new List<PlayerRef>(_loadingScreenPlayers);
                foreach (var playerRef in players)
                {
                    HideLoadingScreen(playerRef, 0f);
                }
            }

            _queuedTeleportPlayers.Clear();
        }

        private void ShowLoadingScreen(PlayerRef playerRef, float additionalTime)
        {
            if (_loadingScreenPlayers.Add(playerRef) == true)
            {
                RPC_SetDungeonLoadingScreen(playerRef, true, additionalTime);
            }
        }

        private void HideLoadingScreen(PlayerRef playerRef, float additionalTime)
        {
            if (_loadingScreenPlayers.Remove(playerRef) == true)
            {
                RPC_SetDungeonLoadingScreen(playerRef, false, additionalTime);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_SetDungeonLoadingScreen(PlayerRef playerRef, bool show, float additionalTime)
        {
            if (Runner == null)
            {
                return;
            }

            if (Runner.LocalPlayer != playerRef)
            {
                return;
            }

            var networking = Global.Networking;
            if (networking == null)
            {
                return;
            }

            networking.SetDungeonLoadingScreen(show, additionalTime);
        }
    }
}
