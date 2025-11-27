using Fusion;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    public class TavernEntrance : NetworkedSpaceEntranceBase
    {
        [SerializeField]
        private TavernNetworked _tavernPrefab;

        private TavernNetworked _spawnedTavern;

        protected override bool TryQueueEntry(PlayerRef playerRef)
        {
            if (_tavernPrefab == null)
            {
                return false;
            }

            if (_spawnedTavern == null || _spawnedTavern.Object == null)
            {
                return SpawnTavern(playerRef);
            }

            return _spawnedTavern.SetPendingTeleportPlayer(playerRef);
        }

        private bool SpawnTavern(PlayerRef playerRef)
        {
            if (Runner == null || _tavernPrefab == null)
            {
                return false;
            }

            Vector3 spawnPosition = NetworkedSpaceSpawnManager.AllocatePosition();
            Quaternion spawnRotation = Quaternion.identity;

            _spawnedTavern = Runner.Spawn(_tavernPrefab, spawnPosition, spawnRotation);

            if (_spawnedTavern != null)
            {
                _spawnedTavern.SetEntrance(this);
                return _spawnedTavern.SetPendingTeleportPlayer(playerRef);
            }

            return false;
        }
    }
}
