using System.Collections.Generic;
using DungeonArchitect;
using Fusion;
using TPSBR;
using UnityEngine;
using Pathfinding;
using Pathfinding.Util;
using Pathfinding.Collections;
using Pathfinding.Graphs.Navmesh;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [System.Serializable]
    public struct NetworkedObjectMap
    {
        public GameObject monoBehavior;
        public NetworkObject networkBehavior;
    }

    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedDungeon : NetworkBehaviour
    {
        private const int REQUEST_CAPACITY = 16;

        [Networked]
        private int _seed { get; set; }

        [Networked]
        private NetworkBool _dungeonReadyToGenerate { get; set; }

        [Networked]
        private NetworkBehaviourRef<WalkInDungeonEntrance> _entrance { get; set; }

        [Networked, Capacity(REQUEST_CAPACITY)]
        private NetworkArray<TeleportRequest> _teleportRequests { get; }

        [Networked, Capacity(REQUEST_CAPACITY)]
        private NetworkArray<LoadingSceneRequest> _loadingSceneRequests { get; }

        private int _lastSeedGenerated = -1;
        [SerializeField]
        private Dungeon _dungeon;

        [SerializeField]
        private NetworkedObjectMap[] _networkedObjectMap = System.Array.Empty<NetworkedObjectMap>();

        [SerializeField]
        private Transform _networkObjectRoot;

        public Transform NetworkObjectRoot => _networkObjectRoot;

        private bool _dungeonGenerated;
        private DungeonSpawnPoint _spawnPoint;
        private AstarPath _astarPath;
        private readonly List<PlayerRef> _pendingTeleportPlayers = new List<PlayerRef>(REQUEST_CAPACITY);
        private readonly List<PendingNetworkSpawn> _pendingNetworkSpawns = new List<PendingNetworkSpawn>();
        private readonly List<NetworkObject> _spawnedNetworkObjects = new List<NetworkObject>();
        private readonly List<Transform> _dungeonTransformBuffer = new List<Transform>();

        private struct PendingNetworkSpawn
        {
            public NetworkObject Prefab;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

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
            ProcessPendingNetworkSpawns();
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
            ClearPendingNetworkSpawns();
            DespawnSpawnedNetworkObjects();
            TryGenerateDungeon();
        }

        public NetworkBehaviourRef<WalkInDungeonEntrance> Entrance => _entrance;

        public void SetEntrance(WalkInDungeonEntrance entrance)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            _entrance = entrance;
        }

        public bool TryGetEntrance(out WalkInDungeonEntrance entrance)
        {
            if (_entrance.TryResolve(out entrance) == false)
            {
                entrance = null;
                return false;
            }

            return entrance != null;
        }

        public bool TryTeleportPlayerToEntranceExit(Agent agent, out string message)
        {
            message = string.Empty;

            if (HasStateAuthority == false)
            {
                message = "No authority to teleport player.";
                return false;
            }

            if (agent == null)
            {
                message = "No agent available.";
                return false;
            }

            if (TryGetEntrance(out var entrance) == false)
            {
                message = "Dungeon entrance unavailable.";
                return false;
            }

            var exitTransform = entrance.ExitTransform;
            if (exitTransform == null)
            {
                message = "Dungeon entrance exit transform not configured.";
                return false;
            }

            var character = agent.Character;
            if (character == null)
            {
                message = "No character available.";
                return false;
            }

            var controller = character.CharacterController;
            if (controller != null)
            {
                controller.SetPosition(exitTransform.position);
                controller.SetLookRotation(exitTransform.rotation);
            }

            agent.transform.SetPositionAndRotation(exitTransform.position, exitTransform.rotation);

            if (agent.Object == null)
            {
                message = "Player object unavailable.";
                return false;
            }

            ScheduleLoadingSceneHide(agent.Object.InputAuthority);

            return true;
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

            _dungeon = GetComponentInChildren<Dungeon>();
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

            var recastGraph = GetMainRecastGraph();
            if (recastGraph == null)
            {
                return;
            }

            if (recastGraph.useTiles == false)
            {
                Debug.LogWarning("Recast graph must use tiles to stream dungeon navigation mesh.");
                return;
            }

            var astarPath = _astarPath;
            if (astarPath == null)
            {
                return;
            }

            var expandedBounds = dungeonBounds;
            float horizontalMargin = Mathf.Max(1.0f, recastGraph.characterRadius * 2.0f);
            float verticalMargin = Mathf.Max(1.0f, recastGraph.walkableHeight);
            expandedBounds.Expand(new Vector3(horizontalMargin, verticalMargin, horizontalMargin));

            bool shouldUpdate = false;

            astarPath.AddWorkItem(() =>
            {
                recastGraph.EnsureInitialized();

                var updatedForcedBounds = new Bounds(recastGraph.forcedBoundsCenter, recastGraph.forcedBoundsSize);
                if (updatedForcedBounds.size == Vector3.zero)
                {
                    updatedForcedBounds = new Bounds(expandedBounds.center, expandedBounds.size);
                }
                else
                {
                    updatedForcedBounds.Encapsulate(expandedBounds.min);
                    updatedForcedBounds.Encapsulate(expandedBounds.max);
                }

                var tileLayout = new TileLayout(recastGraph);
                var dungeonTileRect = tileLayout.GetTouchingTiles(expandedBounds);

                if (dungeonTileRect.IsValid() == false)
                {
                    return;
                }

                shouldUpdate = true;

                bool currentRectValid = recastGraph.tileXCount > 0 && recastGraph.tileZCount > 0;
                IntRect currentRect = currentRectValid
                    ? new IntRect(0, 0, recastGraph.tileXCount - 1, recastGraph.tileZCount - 1)
                    : dungeonTileRect;
                IntRect combinedRect = currentRectValid
                    ? IntRect.Union(currentRect, dungeonTileRect)
                    : dungeonTileRect;

                if (currentRectValid == false || combinedRect != currentRect)
                {
                    recastGraph.Resize(combinedRect);
                }

                updatedForcedBounds.Encapsulate(new Bounds(recastGraph.forcedBoundsCenter, recastGraph.forcedBoundsSize));
                updatedForcedBounds.size = new Vector3(
                    Mathf.Max(updatedForcedBounds.size.x, 1f),
                    Mathf.Max(updatedForcedBounds.size.y, 1f),
                    Mathf.Max(updatedForcedBounds.size.z, 1f));

                recastGraph.forcedBoundsCenter = updatedForcedBounds.center;
                recastGraph.forcedBoundsSize = updatedForcedBounds.size;
                recastGraph.transform = RecastGraph.CalculateTransform(updatedForcedBounds, Quaternion.Euler(recastGraph.rotation));

                var tiles = recastGraph.GetTiles();
                if (tiles != null)
                {
                    for (int tileIndex = 0; tileIndex < tiles.Length; ++tileIndex)
                    {
                        var tile = tiles[tileIndex];
                        if (tile == null || tile.verts.Length == 0)
                        {
                            continue;
                        }

                        var verts = tile.verts;
                        var vertsInGraphSpace = tile.vertsInGraphSpace;
                        if (vertsInGraphSpace.Length != verts.Length)
                        {
                            continue;
                        }

                        recastGraph.transform.InverseTransform(verts);
                        vertsInGraphSpace.CopyFrom(verts);
                        recastGraph.transform.Transform(verts);
                    }
                }
            });

            astarPath.FlushWorkItems();

            if (shouldUpdate == false)
            {
                return;
            }

            var updateObject = new GraphUpdateObject(expandedBounds)
            {
                updatePhysics = true,
                resetPenaltyOnPhysics = false
            };

            astarPath.UpdateGraphs(updateObject);
            astarPath.FlushGraphUpdates();
        }

        private RecastGraph GetMainRecastGraph()
        {
            if (CacheAstarPath() == false)
            {
                return null;
            }

            var data = _astarPath.data;
            if (data == null)
            {
                return null;
            }

            var recastGraph = data.recastGraph;
            if (recastGraph != null)
            {
                return recastGraph;
            }

            if (data.graphs == null)
            {
                return null;
            }

            for (int i = 0; i < data.graphs.Length; ++i)
            {
                if (data.graphs[i] is RecastGraph candidate)
                {
                    return candidate;
                }
            }

            return null;
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

            if (HasStateAuthority)
            {
                DespawnSpawnedNetworkObjects();
            }

            _dungeon.SetSeed(_seed);
            _dungeon.Build();
            UpdateAstarPath();

            HideAndQueueNetworkedObjects();

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

        public void RequestLoadingSceneHide(PlayerRef player)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            ScheduleLoadingSceneHide(player);
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

        private void ClearPendingNetworkSpawns()
        {
            _pendingNetworkSpawns.Clear();
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

        private void HideAndQueueNetworkedObjects()
        {
            if (_dungeon == null)
            {
                return;
            }

            if (_networkedObjectMap == null || _networkedObjectMap.Length == 0)
            {
                return;
            }

            if (HasStateAuthority)
            {
                _pendingNetworkSpawns.Clear();
            }

            ProcessDungeonHierarchy(_dungeon.gameObject);

            var dungeonObjects = DungeonUtils.GetDungeonObjects(_dungeon);
            for (int i = 0; i < dungeonObjects.Count; ++i)
            {
                var dungeonObject = dungeonObjects[i];
                if (dungeonObject == null || dungeonObject == _dungeon.gameObject)
                {
                    continue;
                }

                ProcessDungeonHierarchy(dungeonObject);
            }
        }

        private void ProcessPendingNetworkSpawns()
        {
            if (HasStateAuthority == false || Runner == null)
            {
                _pendingNetworkSpawns.Clear();
                return;
            }

            if (_pendingNetworkSpawns.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _pendingNetworkSpawns.Count; ++i)
            {
                var spawn = _pendingNetworkSpawns[i];
                if (spawn.Prefab == null)
                {
                    continue;
                }

                var spawnedObject = Runner.Spawn(spawn.Prefab, spawn.Position, spawn.Rotation, default, (runner, obj) =>
                {
                    obj.transform.localScale = spawn.Scale;

                    if (_networkObjectRoot != null)
                    {
                        obj.transform.SetParent(_networkObjectRoot, true);
                    }
                });

                if (spawnedObject != null)
                {
                    _spawnedNetworkObjects.Add(spawnedObject);
                }
            }

            _pendingNetworkSpawns.Clear();
        }

        private void DespawnSpawnedNetworkObjects()
        {
            if (_spawnedNetworkObjects.Count == 0)
            {
                return;
            }

            for (int i = _spawnedNetworkObjects.Count - 1; i >= 0; --i)
            {
                var spawned = _spawnedNetworkObjects[i];
                if (spawned == null)
                {
                    _spawnedNetworkObjects.RemoveAt(i);
                    continue;
                }

                if (Runner != null)
                {
                    Runner.Despawn(spawned);
                }

                _spawnedNetworkObjects.RemoveAt(i);
            }
        }

        private static bool IsMatchingPrefab(GameObject instance, GameObject prefab)
        {
            if (instance == null || prefab == null)
            {
                return false;
            }

#if UNITY_EDITOR
            var sourcePrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance);
            if (sourcePrefab != null && sourcePrefab == prefab)
            {
                return true;
            }
#endif

            if (instance.name == prefab.name)
            {
                return true;
            }

            if (instance.name.StartsWith(prefab.name + " ("))
            {
                return true;
            }

            return false;
        }

        private void ProcessDungeonHierarchy(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            _dungeonTransformBuffer.Clear();
            root.GetComponentsInChildren(true, _dungeonTransformBuffer);

            for (int transformIndex = 0; transformIndex < _dungeonTransformBuffer.Count; ++transformIndex)
            {
                var child = _dungeonTransformBuffer[transformIndex];
                if (child == null)
                {
                    continue;
                }

                var childGameObject = child.gameObject;
                if (childGameObject.activeInHierarchy == false)
                {
                    continue;
                }

                for (int mapIndex = 0; mapIndex < _networkedObjectMap.Length; ++mapIndex)
                {
                    var map = _networkedObjectMap[mapIndex];
                    if (map.monoBehavior == null)
                    {
                        continue;
                    }

                    if (IsMatchingPrefab(childGameObject, map.monoBehavior) == false)
                    {
                        continue;
                    }

                    childGameObject.SetActive(false);

                    if (HasStateAuthority && map.networkBehavior != null)
                    {
                        PendingNetworkSpawn spawn = default;
                        spawn.Prefab = map.networkBehavior;
                        spawn.Position = child.position;
                        spawn.Rotation = child.rotation;
                        spawn.Scale = child.lossyScale;
                        _pendingNetworkSpawns.Add(spawn);
                    }

                    break;
                }
            }

            _dungeonTransformBuffer.Clear();
        }
    }
}
