using Fusion;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    public class WalkInDungeonEntrance : NetworkedSpaceEntranceBase
    {
        [SerializeField]
        private Transform _exit;

        [SerializeField]
        private NetworkedDungeon _dungeonPrefab;

        private NetworkedDungeon _spawnedDungeon;
        public Transform ExitTransform => _exit;

        protected override bool TryQueueEntry(PlayerRef playerRef)
        {
            if (_dungeonPrefab == null)
            {
                return false;
            }

            if (_spawnedDungeon == null || _spawnedDungeon.Object == null)
            {
                return SpawnDungeon(playerRef);
            }

            return _spawnedDungeon.SetPendingTeleportPlayer(playerRef);
        }

        private bool SpawnDungeon(PlayerRef playerRef)
        {
            if (Runner == null || _dungeonPrefab == null)
            {
                return false;
            }

            Vector3 spawnPosition = NetworkedSpaceSpawnManager.AllocatePosition();
            Quaternion spawnRotation = Quaternion.identity;

            _spawnedDungeon = Runner.Spawn(_dungeonPrefab, spawnPosition, spawnRotation);

            if (_spawnedDungeon != null)
            {
                _spawnedDungeon.SetEntrance(this);
                _spawnedDungeon.RandomizeSeed();
                return _spawnedDungeon.SetPendingTeleportPlayer(playerRef);
            }

            return false;
        }
    }
}
