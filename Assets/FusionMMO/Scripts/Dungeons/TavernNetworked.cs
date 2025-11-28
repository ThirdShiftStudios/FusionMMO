using System.Collections.Generic;
using Fusion;
using UnityEngine;
using TPSBR;

namespace FusionMMO.Dungeons
{
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(NetworkObject))]
    public class TavernNetworked : NetworkBehaviour
    {
        private const int REQUEST_CAPACITY = 16;

        [Networked]
        public TavernEntrance Entrance { get; private set; }

        [Networked, Capacity(REQUEST_CAPACITY)]
        private NetworkArray<TeleportRequest> _teleportRequests { get; }

        [Networked, Capacity(REQUEST_CAPACITY)]
        private NetworkArray<LoadingSceneRequest> _loadingSceneRequests { get; }

        private TavernSpawnPoint _spawnPoint;
        private TavernExit _tavernExit;
        private readonly List<PlayerRef> _pendingTeleportPlayers = new List<PlayerRef>(REQUEST_CAPACITY);

        private void Awake()
        {
            CacheSpawnPoint();
            EnsureExitExists();
        }

        public override void Spawned()
        {
            base.Spawned();
            CacheSpawnPoint();
            EnsureExitExists();
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            ProcessTeleportRequest();
            ProcessLoadingSceneRequest();
        }

        public void SetEntrance(TavernEntrance entrance)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            Entrance = entrance;
        }

        public bool SetPendingTeleportPlayer(PlayerRef playerRef)
        {
            if (HasStateAuthority == false)
            {
                return false;
            }

            if (playerRef == PlayerRef.None)
            {
                return false;
            }

            if (IsTeleportAlreadyScheduled(playerRef))
            {
                return false;
            }

            if (_pendingTeleportPlayers.Contains(playerRef))
            {
                TryScheduleTeleport();
                return false;
            }

            _pendingTeleportPlayers.Add(playerRef);
            TryScheduleTeleport();

            return true;
        }

        private bool CacheSpawnPoint()
        {
            if (_spawnPoint != null)
            {
                return true;
            }

            _spawnPoint = GetComponentInChildren<TavernSpawnPoint>();
            return _spawnPoint != null;
        }

        private bool CacheExit()
        {
            if (_tavernExit != null)
            {
                return true;
            }

            _tavernExit = GetComponentInChildren<TavernExit>();
            return _tavernExit != null;
        }

        private void EnsureExitExists()
        {
            if (CacheExit())
            {
                return;
            }

            var exitObject = new GameObject("TavernExit");
            exitObject.transform.SetParent(transform, false);

            _tavernExit = exitObject.AddComponent<TavernExit>();
        }

        private void TryScheduleTeleport()
        {
            if (HasStateAuthority == false || Runner == null)
            {
                return;
            }

            if (_pendingTeleportPlayers.Count == 0)
            {
                return;
            }

            if (CacheSpawnPoint() == false)
            {
                Debug.LogWarning($"{nameof(TavernSpawnPoint)} not found under {nameof(TavernNetworked)}.", this);
                return;
            }

            int tickRate = TickRate.Resolve(Runner.Config.Simulation.TickRateSelection).Server;

            for (int i = _pendingTeleportPlayers.Count - 1; i >= 0; --i)
            {
                PlayerRef player = _pendingTeleportPlayers[i];

                if (player == PlayerRef.None)
                {
                    _pendingTeleportPlayers.RemoveAt(i);
                    continue;
                }

                if (IsTeleportAlreadyScheduled(player))
                {
                    _pendingTeleportPlayers.RemoveAt(i);
                    continue;
                }

                int requestIndex = FindAvailableTeleportSlot();
                if (requestIndex < 0)
                {
                    break;
                }

                TeleportRequest request = default;
                request.Player = player;
                request.TeleportTick = Runner.Tick + tickRate * 3;
                request.IsActive = true;

                _teleportRequests.Set(requestIndex, request);
                _pendingTeleportPlayers.RemoveAt(i);
            }
        }

        private void ProcessTeleportRequest()
        {
            if (HasStateAuthority == false || Runner == null)
            {
                return;
            }

            for (int i = 0, count = _teleportRequests.Length; i < count; ++i)
            {
                TeleportRequest request = _teleportRequests.Get(i);
                if (request.IsActive == false)
                {
                    continue;
                }

                if (Runner.Tick < request.TeleportTick)
                {
                    continue;
                }

                if (CacheSpawnPoint() == false)
                {
                    Debug.LogWarning($"{nameof(TavernSpawnPoint)} not found under {nameof(TavernNetworked)}.", this);
                    _teleportRequests.Set(i, default);
                    continue;
                }

                if (Runner.TryGetPlayerObject(request.Player, out var playerObject) == false || playerObject == null)
                {
                    continue;
                }

                var player = playerObject.GetComponent<TPSBR.Player>();
                if (player == null)
                {
                    _teleportRequests.Set(i, default);
                    continue;
                }

                var agent = player.ActiveAgent;
                if (agent == null)
                {
                    continue;
                }

                var character = agent.Character;
                if (character == null)
                {
                    continue;
                }

                var controller = character.CharacterController;
                if (controller != null)
                {
                    controller.SetPosition(_spawnPoint.transform.position);
                    controller.SetLookRotation(_spawnPoint.transform.rotation);
                }

                agent.transform.SetPositionAndRotation(_spawnPoint.transform.position, _spawnPoint.transform.rotation);

                ScheduleLoadingSceneHide(request.Player);

                _teleportRequests.Set(i, default);
            }

            if (_pendingTeleportPlayers.Count > 0)
            {
                TryScheduleTeleport();
            }
        }

        private void ProcessLoadingSceneRequest()
        {
            if (HasStateAuthority == false || Runner == null)
            {
                return;
            }

            for (int i = 0, count = _loadingSceneRequests.Length; i < count; ++i)
            {
                LoadingSceneRequest request = _loadingSceneRequests.Get(i);
                if (request.IsActive == false)
                {
                    continue;
                }

                if (Runner.Tick < request.HideTick)
                {
                    continue;
                }

                _loadingSceneRequests.Set(i, default);

                RPC_HideLoadingScene(request.Player);
            }
        }

        public void ScheduleLoadingSceneHide(PlayerRef player)
        {
            if (Runner == null || player == PlayerRef.None)
            {
                return;
            }

            int tickRate = TickRate.Resolve(Runner.Config.Simulation.TickRateSelection).Server;
            int requestIndex = FindAvailableLoadingSceneSlot();
            if (requestIndex < 0)
            {
                return;
            }

            LoadingSceneRequest request = default;
            request.Player = player;
            request.HideTick = Runner.Tick + tickRate * 3;
            request.IsActive = true;

            _loadingSceneRequests.Set(requestIndex, request);
        }

        private bool IsTeleportAlreadyScheduled(PlayerRef player)
        {
            for (int i = 0, count = _teleportRequests.Length; i < count; ++i)
            {
                TeleportRequest request = _teleportRequests.Get(i);
                if (request.IsActive && request.Player == player)
                {
                    return true;
                }
            }

            return false;
        }

        private int FindAvailableTeleportSlot()
        {
            for (int i = 0, count = _teleportRequests.Length; i < count; ++i)
            {
                if (_teleportRequests.Get(i).IsActive == false)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindAvailableLoadingSceneSlot()
        {
            for (int i = 0, count = _loadingSceneRequests.Length; i < count; ++i)
            {
                if (_loadingSceneRequests.Get(i).IsActive == false)
                {
                    return i;
                }
            }

            return -1;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_HideLoadingScene(PlayerRef targetPlayer)
        {
            if (Runner == null || Runner.LocalPlayer != targetPlayer)
            {
                return;
            }

            var networking = TPSBR.Global.Networking;
            if (networking != null)
            {
                networking.RequestLoadingScene(false, 0f);
            }
        }
    }
}
