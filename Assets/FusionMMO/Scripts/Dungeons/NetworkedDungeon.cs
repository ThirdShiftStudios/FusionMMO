using System.Collections;
using DungeonArchitect;
using Fusion;
using TPSBR;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    [RequireComponent(typeof(Dungeon))]
    public class NetworkedDungeon : NetworkBehaviour
    {
        [Networked]
        private int _seed { get; set; }

        [Networked]
        private NetworkBool _dungeonReadyToGenerate { get; set; }

        private int _lastSeedGenerated = -1;
        private Dungeon _dungeon;
        private PlayerRef _pendingTeleportPlayer = PlayerRef.None;
        private bool _dungeonGenerated;
        private Coroutine _teleportCoroutine;

        private void Awake()
        {
            CacheDungeon();
        }

        public override void Spawned()
        {
            base.Spawned();
            CacheDungeon();
            TryGenerateDungeon();
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

            if (_teleportCoroutine != null)
            {
                return;
            }

            DungeonSpawnPoint spawnPoint = GetComponentInChildren<DungeonSpawnPoint>();
            if (spawnPoint == null)
            {
                Debug.LogWarning($"{nameof(DungeonSpawnPoint)} not found under {nameof(NetworkedDungeon)}.", this);
                return;
            }

            _teleportCoroutine = StartCoroutine(TeleportPlayerAfterDelay(_pendingTeleportPlayer, spawnPoint));
        }

        private IEnumerator TeleportPlayerAfterDelay(PlayerRef playerRef, DungeonSpawnPoint spawnPoint)
        {
            yield return new WaitForSeconds(3f);

            if (Runner == null || HasStateAuthority == false)
            {
                _teleportCoroutine = null;
                yield break;
            }

            if (spawnPoint == null)
            {
                _teleportCoroutine = null;
                yield break;
            }

            if (Runner.TryGetPlayerObject(playerRef, out var playerObject) == false || playerObject == null)
            {
                _teleportCoroutine = null;
                yield break;
            }

            var player = playerObject.GetComponent<TPSBR.Player>();
            if (player == null)
            {
                _teleportCoroutine = null;
                yield break;
            }

            var agent = player.ActiveAgent;
            if (agent == null)
            {
                _teleportCoroutine = null;
                yield break;
            }

            var character = agent.Character;
            if (character == null)
            {
                _teleportCoroutine = null;
                yield break;
            }

            var controller = character.CharacterController;
            if (controller != null)
            {
                controller.SetPosition(spawnPoint.transform.position);
                controller.SetLookRotation(spawnPoint.transform.rotation);
            }

            agent.transform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);

            _pendingTeleportPlayer = PlayerRef.None;
            _teleportCoroutine = null;
        }
    }
}
