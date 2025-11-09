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
        private readonly Dictionary<PlayerRef, float> _loadingScreenHideTimers = new Dictionary<PlayerRef, float>();
        private readonly List<PlayerRef> _loadingScreenHideScratch = new List<PlayerRef>();

        private NetworkedDungeon _spawnedDungeon;
        private Transform _cachedDungeonSpawnPoint;
        private Vector3 _activationOrigin;
        private bool _activationOriginInitialized;
        private bool _isGeneratingDungeon;
        private bool _localImmediateLoadingActive;
        private bool _authoritativeLoadingActive;

        public override void Spawned()
        {
            base.Spawned();
            CacheActivationOrigin();
        }

        public override void FixedUpdateNetwork()
        {
            CacheActivationOrigin();

            if (Runner != null)
            {
                UpdateLocalPlayerLoadingPreview();
            }

            if (HasStateAuthority == false)
            {
                return;
            }

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

            if (_loadingScreenHideTimers.Count > 0)
            {
                UpdateLoadingScreenHideTimers();
            }
        }

        private void OnDisable()
        {
            if (_spawnedDungeon != null)
            {
                _spawnedDungeon.DungeonGenerated -= OnDungeonGenerated;
            }

            if (_localImmediateLoadingActive == true || _authoritativeLoadingActive == true)
            {
                ForceHideLocalLoadingScreen();
            }

            if (_loadingScreenHideTimers.Count > 0)
            {
                _loadingScreenHideTimers.Clear();
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

            ShowLoadingScreen(playerRef, 0f);

            if (TryTeleportPlayer(playerRef, playerComponent, LoadingScreenHideDelay) == false)
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

            ScheduleLoadingScreenHide(playerRef, loadingHideDelay);
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

            if (_loadingScreenHideTimers.Count > 0)
            {
                _loadingScreenHideTimers.Clear();
            }

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
            CancelLoadingScreenHide(playerRef);

            if (_loadingScreenPlayers.Add(playerRef) == true)
            {
                RPC_SetDungeonLoadingScreen(playerRef, true, additionalTime);
            }
        }

        private void HideLoadingScreen(PlayerRef playerRef, float additionalTime)
        {
            CancelLoadingScreenHide(playerRef);

            if (_loadingScreenPlayers.Remove(playerRef) == true)
            {
                RPC_SetDungeonLoadingScreen(playerRef, false, additionalTime);
            }
        }

        private void ScheduleLoadingScreenHide(PlayerRef playerRef, float delay)
        {
            if (delay <= 0f)
            {
                HideLoadingScreen(playerRef, 0f);
                return;
            }

            _loadingScreenHideTimers[playerRef] = delay;
        }

        private void CancelLoadingScreenHide(PlayerRef playerRef)
        {
            _loadingScreenHideTimers.Remove(playerRef);
        }

        private void UpdateLoadingScreenHideTimers()
        {
            if (Runner == null)
            {
                _loadingScreenHideTimers.Clear();
                return;
            }

            float deltaTime = Runner.DeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            _loadingScreenHideScratch.Clear();

            foreach (var pair in _loadingScreenHideTimers)
            {
                _loadingScreenHideScratch.Add(pair.Key);
            }

            for (int i = 0; i < _loadingScreenHideScratch.Count; ++i)
            {
                var playerRef = _loadingScreenHideScratch[i];

                if (_loadingScreenHideTimers.TryGetValue(playerRef, out float remainingTime) == false)
                {
                    continue;
                }

                remainingTime -= deltaTime;

                if (remainingTime <= 0f)
                {
                    _loadingScreenHideTimers.Remove(playerRef);
                    HideLoadingScreen(playerRef, 0f);
                }
                else
                {
                    _loadingScreenHideTimers[playerRef] = remainingTime;
                }
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

            if (show == true)
            {
                _authoritativeLoadingActive = true;
                _localImmediateLoadingActive = false;
            }
            else
            {
                _authoritativeLoadingActive = false;
                _localImmediateLoadingActive = false;
            }
        }

        private void UpdateLocalPlayerLoadingPreview()
        {
            if (_entrance == null)
            {
                return;
            }

            PlayerRef localPlayer = Runner.LocalPlayer;
            if (localPlayer == PlayerRef.None)
            {
                return;
            }

            if (Runner.TryGetPlayerObject(localPlayer, out var playerObject) == false || playerObject == null)
            {
                return;
            }

            var playerComponent = playerObject.GetComponent<Player>();
            var agent = playerComponent != null ? playerComponent.ActiveAgent : null;
            if (agent == null)
            {
                return;
            }

            float sqrActivationDistance = _activationDistance * _activationDistance;
            Vector3 toEntrance = agent.transform.position - _activationOrigin;
            bool isInside = toEntrance.sqrMagnitude <= sqrActivationDistance;

            if (isInside == true)
            {
                if (_authoritativeLoadingActive == false && _localImmediateLoadingActive == false)
                {
                    ShowLocalImmediateLoadingScreen();
                }
            }
            else
            {
                if (_authoritativeLoadingActive == false && _localImmediateLoadingActive == true)
                {
                    HideLocalImmediateLoadingScreen();
                }
            }
        }

        private void ShowLocalImmediateLoadingScreen()
        {
            if (_localImmediateLoadingActive == true)
            {
                return;
            }

            var networking = Global.Networking;
            if (networking == null)
            {
                return;
            }

            networking.SetDungeonLoadingScreen(true, 0f);
            _localImmediateLoadingActive = true;
        }

        private void HideLocalImmediateLoadingScreen()
        {
            if (_localImmediateLoadingActive == false)
            {
                return;
            }

            var networking = Global.Networking;
            if (networking != null)
            {
                networking.SetDungeonLoadingScreen(false, 0f);
            }

            _localImmediateLoadingActive = false;
        }

        private void ForceHideLocalLoadingScreen()
        {
            if (Runner == null)
            {
                _localImmediateLoadingActive = false;
                _authoritativeLoadingActive = false;
                return;
            }

            PlayerRef localPlayer = Runner.LocalPlayer;
            if (localPlayer == PlayerRef.None)
            {
                _localImmediateLoadingActive = false;
                _authoritativeLoadingActive = false;
                return;
            }

            var networking = Global.Networking;
            if (networking != null)
            {
                networking.SetDungeonLoadingScreen(false, 0f);
            }

            _localImmediateLoadingActive = false;
            _authoritativeLoadingActive = false;
        }
    }
}
