using DungeonArchitect;
using Fusion;
using TPSBR;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    public struct TeleportRequest : INetworkStruct
    {
        public PlayerRef Player;
        public int TeleportTick;
        public NetworkBool IsActive;
    }

    [RequireComponent(typeof(Dungeon))]
    public class NetworkedDungeon : NetworkBehaviour
    {
        [Networked]
        private int _seed { get; set; }

        [Networked]
        private NetworkBool _dungeonReadyToGenerate { get; set; }

        [Networked]
        private TeleportRequest _teleportRequest { get; set; }

        private int _lastSeedGenerated = -1;
        private Dungeon _dungeon;
        private PlayerRef _pendingTeleportPlayer = PlayerRef.None;
        private bool _dungeonGenerated;
        private DungeonSpawnPoint _spawnPoint;

        private void Awake()
        {
            CacheDungeon();
            CacheSpawnPoint();
        }

        public override void Spawned()
        {
            base.Spawned();
            CacheDungeon();
            CacheSpawnPoint();
            TryGenerateDungeon();
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            ProcessTeleportRequest();
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
            _teleportRequest = default;
            TryGenerateDungeon();
        }

        public void SetPendingTeleportPlayer(PlayerRef playerRef)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            if (playerRef == PlayerRef.None)
            {
                return;
            }

            _pendingTeleportPlayer = playerRef;
            TryScheduleTeleport();
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

            _lastSeedGenerated = _seed;
            _dungeonGenerated = true;
            TryScheduleTeleport();
        }

        private void TryScheduleTeleport()
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            if (_pendingTeleportPlayer == PlayerRef.None)
            {
                return;
            }

            if (_dungeonGenerated == false)
            {
                return;
            }

            if (CacheSpawnPoint() == false)
            {
                Debug.LogWarning($"{nameof(DungeonSpawnPoint)} not found under {nameof(NetworkedDungeon)}.", this);
                return;
            }

            TeleportRequest request = _teleportRequest;
            if (request.IsActive)
            {
                return;
            }

            if (Runner == null)
            {
                return;
            }

            int tickRate = TickRate.Resolve(Runner.Config.Simulation.TickRateSelection).Server;
            int teleportTick = Runner.Tick + tickRate * 3;

            request.Player = _pendingTeleportPlayer;
            request.TeleportTick = teleportTick;
            request.IsActive = true;

            _teleportRequest = request;
        }

        private void ProcessTeleportRequest()
        {
            if (HasStateAuthority == false || Runner == null)
            {
                return;
            }

            TeleportRequest request = _teleportRequest;
            if (request.IsActive == false)
            {
                return;
            }

            if (Runner.Tick < request.TeleportTick)
            {
                return;
            }

            if (CacheSpawnPoint() == false)
            {
                Debug.LogWarning($"{nameof(DungeonSpawnPoint)} not found under {nameof(NetworkedDungeon)}.", this);
                _teleportRequest = default;
                _pendingTeleportPlayer = PlayerRef.None;
                return;
            }

            if (Runner.TryGetPlayerObject(request.Player, out var playerObject) == false || playerObject == null)
            {
                return;
            }

            var player = playerObject.GetComponent<TPSBR.Player>();
            if (player == null)
            {
                _teleportRequest = default;
                _pendingTeleportPlayer = PlayerRef.None;
                return;
            }

            var agent = player.ActiveAgent;
            if (agent == null)
            {
                return;
            }

            var character = agent.Character;
            if (character == null)
            {
                return;
            }

            var controller = character.CharacterController;
            if (controller != null)
            {
                controller.SetPosition(_spawnPoint.transform.position);
                controller.SetLookRotation(_spawnPoint.transform.rotation);
            }

            agent.transform.SetPositionAndRotation(_spawnPoint.transform.position, _spawnPoint.transform.rotation);

            _teleportRequest = default;
            _pendingTeleportPlayer = PlayerRef.None;
        }
    }
}
