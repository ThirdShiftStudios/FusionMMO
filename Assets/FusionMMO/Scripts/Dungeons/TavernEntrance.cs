using Fusion;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    public class TavernEntrance : RemoteEntranceBase
    {
        [SerializeField]
        private TavernNetworked _tavernPrefab;

        [SerializeField]
        private Transform _exit;

        private TavernNetworked _spawnedTavern;

        public Transform ExitTransform => _exit;

        protected override bool TryHandleEntry(PlayerRef playerRef)
        {
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

            Vector3 spawnPosition = RemoteSpawnManager.GetOrReservePosition(this);
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
