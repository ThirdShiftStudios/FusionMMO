using System;
using System.Collections.Generic;
using DungeonArchitect;
using Fusion;
using TPSBR;
using UnityEngine;
using Pathfinding;
using Pathfinding.Util;

namespace FusionMMO.Dungeons
{
    public struct TeleportRequest : INetworkStruct
    {
        public PlayerRef Player;
        public int TeleportTick;
        public NetworkBool IsActive;
    }

    public struct LoadingSceneRequest : INetworkStruct
    {
        public PlayerRef Player;
        public int HideTick;
        public NetworkBool IsActive;
    }

    [RequireComponent(typeof(Dungeon))]
    public class NetworkedDungeon : NetworkBehaviour
    {
        private const int REQUEST_CAPACITY = 16;

        [Networked]
        private int _seed { get; set; }

        [Networked]
        private NetworkBool _dungeonReadyToGenerate { get; set; }

        [Networked, Capacity(REQUEST_CAPACITY)]
        private NetworkArray<TeleportRequest> _teleportRequests { get; }

        [Networked, Capacity(REQUEST_CAPACITY)]
        private NetworkArray<LoadingSceneRequest> _loadingSceneRequests { get; }

        private int _lastSeedGenerated = -1;
        private Dungeon _dungeon;
        private bool _dungeonGenerated;
        private DungeonSpawnPoint _spawnPoint;
        private AstarPath _astarPath;
        private GridGraph _dungeonGridGraph;
        private Bounds? _dungeonGridBounds;
        private float _dungeonGridNodeSize = 1f;
        private readonly List<PlayerRef> _pendingTeleportPlayers = new List<PlayerRef>(REQUEST_CAPACITY);

        private const string RuntimeGridGraphName = "RuntimeDungeonGrid";

        private void Awake()
        {
            CacheDungeon();
            CacheSpawnPoint();
            CacheAstarPath();
        }

        public override void Spawned()
        {
            base.Spawned();
            CacheDungeon();
            CacheSpawnPoint();
            CacheAstarPath();
            TryGenerateDungeon();
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            TryScheduleTeleport();
            ProcessTeleportRequest();
            ProcessLoadingSceneRequest();
        }

        public override void Render()
        {
            base.Render();
            TryGenerateDungeon();
        }

        public void RandomizeSeed()
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            if (CacheDungeon() == false)
            {
                Debug.LogWarning($"{nameof(NetworkedDungeon)} requires a {nameof(Dungeon)} component on the same GameObject.", this);
                return;
            }

            _dungeon.RandomizeSeed();

            var config = _dungeon.Config;
            if (config != null)
            {
                _seed = (int)config.Seed;
            }

            _dungeonGenerated = false;
            _dungeonReadyToGenerate = true;
            ClearTeleportRequests();
            ClearLoadingSceneRequests();
            _pendingTeleportPlayers.Clear();
            TryGenerateDungeon();
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

        private bool CacheDungeon()
        {
            if (_dungeon != null)
            {
                return true;
            }

            _dungeon = GetComponent<Dungeon>();
            return _dungeon != null;
        }

        private bool CacheSpawnPoint()
        {
            if (_spawnPoint != null)
            {
                return true;
            }

            _spawnPoint = GetComponentInChildren<DungeonSpawnPoint>();
            return _spawnPoint != null;
        }

        private bool CacheAstarPath()
        {
            var activeAstar = AstarPath.active;
            if (activeAstar == null)
            {
                _astarPath = null;
                return false;
            }

            _astarPath = activeAstar;
            EnsureDungeonGridGraph();
            return true;
        }

        private void UpdateAstarPath()
        {
            if (CacheAstarPath() == false)
            {
                return;
            }

            if (_dungeon == null)
            {
                return;
            }

            var dungeonBounds = DungeonUtils.GetDungeonBounds(_dungeon);
            if (dungeonBounds.size == Vector3.zero)
            {
                return;
            }

            var data = _astarPath.data;
            if (data == null || data.graphs == null)
            {
                return;
            }

            List<NavGraph> graphsToScan = ListPool<NavGraph>.Claim();

            foreach (var graph in data.graphs)
            {
                if (graph is RecastGraph recastGraph)
                {
                    Vector3 size = dungeonBounds.size;
                    size.y = Mathf.Max(size.y, 1f);

                    recastGraph.forcedBoundsCenter = dungeonBounds.center;
                    recastGraph.forcedBoundsSize = size;
                    graphsToScan.Add(recastGraph);
                }
            }

            bool dungeonGridUpdated = UpdateDungeonGridGraph(dungeonBounds);

            if (dungeonGridUpdated && _dungeonGridGraph != null && graphsToScan.Contains(_dungeonGridGraph) == false)
            {
                graphsToScan.Add(_dungeonGridGraph);
            }

            if (graphsToScan.Count > 0)
            {
                _astarPath.Scan(graphsToScan.ToArray());
            }

            ListPool<NavGraph>.Release(ref graphsToScan);
        }

        private bool UpdateDungeonGridGraph(Bounds dungeonBounds)
        {
            if (dungeonBounds.size == Vector3.zero)
            {
                return false;
            }

            if (EnsureDungeonGridGraph() == false || _dungeonGridGraph == null)
            {
                return false;
            }

            float nodeSize = Mathf.Max(_dungeonGridNodeSize, 0.1f);

            Bounds expandedBounds = dungeonBounds;
            expandedBounds.Expand(new Vector3(nodeSize, 0f, nodeSize));

            bool boundsChanged = false;
            if (_dungeonGridBounds.HasValue)
            {
                Bounds combined = _dungeonGridBounds.Value;
                if (EncapsulateIfNeeded(ref combined, expandedBounds))
                {
                    _dungeonGridBounds = combined;
                    boundsChanged = true;
                }
            }
            else
            {
                _dungeonGridBounds = expandedBounds;
                boundsChanged = true;
            }

            if (_dungeonGridBounds.HasValue == false)
            {
                return false;
            }

            Bounds targetBounds = _dungeonGridBounds.Value;
            targetBounds.size = new Vector3(
                Mathf.Max(targetBounds.size.x, nodeSize),
                Mathf.Max(targetBounds.size.y, 1f),
                Mathf.Max(targetBounds.size.z, nodeSize));
            _dungeonGridBounds = targetBounds;

            int width = Mathf.Max(1, Mathf.CeilToInt(targetBounds.size.x / nodeSize));
            int depth = Mathf.Max(1, Mathf.CeilToInt(targetBounds.size.z / nodeSize));

            bool centerChanged = (_dungeonGridGraph.center - targetBounds.center).sqrMagnitude > 0.001f;
            bool sizeChanged = _dungeonGridGraph.width != width || _dungeonGridGraph.depth != depth;

            if (centerChanged)
            {
                _dungeonGridGraph.center = targetBounds.center;
            }

            if (sizeChanged)
            {
                _dungeonGridGraph.SetDimensions(width, depth, nodeSize);
            }

            return true;
        }

        private static bool EncapsulateIfNeeded(ref Bounds current, Bounds addition)
        {
            Vector3 originalMin = current.min;
            Vector3 originalMax = current.max;
            current.Encapsulate(addition);
            return originalMin != current.min || originalMax != current.max;
        }

        private bool EnsureDungeonGridGraph()
        {
            if (_astarPath == null)
            {
                return false;
            }

            if (_dungeonGridGraph != null)
            {
                return true;
            }

            var data = _astarPath.data;
            if (data == null)
            {
                return false;
            }

            foreach (var graph in data.graphs)
            {
                if (graph is GridGraph existing && string.Equals(existing.name, RuntimeGridGraphName, StringComparison.Ordinal))
                {
                    _dungeonGridGraph = existing;
                    _dungeonGridNodeSize = Mathf.Max(existing.nodeSize, 0.1f);
                    return true;
                }
            }

            GridGraph template = null;
            foreach (var graph in data.graphs)
            {
                if (graph is GridGraph candidate && string.Equals(candidate.name, RuntimeGridGraphName, StringComparison.Ordinal) == false)
                {
                    template = candidate;
                    break;
                }
            }

            var newGraph = data.AddGraph(typeof(GridGraph)) as GridGraph;
            if (newGraph == null)
            {
                return false;
            }

            var originalIndex = newGraph.graphIndex;
            var originalActive = newGraph.active;

            if (template != null)
            {
                string serialized = JsonUtility.ToJson(template);
                JsonUtility.FromJsonOverwrite(serialized, newGraph);
                newGraph.graphIndex = originalIndex;
                newGraph.active = originalActive;
                newGraph.nodes = null;
            }

            newGraph.name = RuntimeGridGraphName;
            newGraph.guid = Guid.NewGuid();
            _dungeonGridNodeSize = Mathf.Max(newGraph.nodeSize, 0.1f);

            if (newGraph.width <= 0 || newGraph.depth <= 0)
            {
                newGraph.center = Vector3.zero;
                newGraph.SetDimensions(1, 1, _dungeonGridNodeSize);
            }

            _dungeonGridGraph = newGraph;
            _dungeonGridBounds = null;
            return true;
        }

        private void TryGenerateDungeon()
        {
            if (_dungeonReadyToGenerate == false)
            {
                return;
            }

            if (CacheDungeon() == false)
            {
                return;
            }

            if (_lastSeedGenerated == _seed)
            {
                return;
            }

            _dungeon.SetSeed(_seed);
            _dungeon.Build();
            UpdateAstarPath();

            _lastSeedGenerated = _seed;
            _dungeonGenerated = true;
            TryScheduleTeleport();
        }

        private void TryScheduleTeleport()
        {
            if (HasStateAuthority == false || Runner == null)
            {
                return;
            }

            if (_dungeonGenerated == false)
            {
                return;
            }

            if (_pendingTeleportPlayers.Count == 0)
            {
                return;
            }

            if (CacheSpawnPoint() == false)
            {
                Debug.LogWarning($"{nameof(DungeonSpawnPoint)} not found under {nameof(NetworkedDungeon)}.", this);
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
                    Debug.LogWarning($"{nameof(DungeonSpawnPoint)} not found under {nameof(NetworkedDungeon)}.", this);
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

        private void ScheduleLoadingSceneHide(PlayerRef player)
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

        private void ClearTeleportRequests()
        {
            for (int i = 0, count = _teleportRequests.Length; i < count; ++i)
            {
                _teleportRequests.Set(i, default);
            }
        }

        private void ClearLoadingSceneRequests()
        {
            for (int i = 0, count = _loadingSceneRequests.Length; i < count; ++i)
            {
                _loadingSceneRequests.Set(i, default);
            }
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
