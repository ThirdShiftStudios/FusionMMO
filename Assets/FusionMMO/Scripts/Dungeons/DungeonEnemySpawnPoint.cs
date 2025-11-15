using System;
using Fusion.Addons.FSM;
using UnityEngine;

namespace FusionMMO.Dungeons
{
    [DisallowMultipleComponent]
    public class DungeonEnemySpawnPoint : MonoBehaviour
    {
        [Serializable]
        public struct EnemySpawnData
        {
            [SerializeField]
            public int _spawnCount;

            [SerializeField]
            public EnemyBehaviorController _enemy;
        }

        [SerializeField]
        private EnemySpawnData[] _spawnData = Array.Empty<EnemySpawnData>();

        [SerializeField]
        private float _radius = 0f;

        public EnemySpawnData[] SpawnData => _spawnData;

        public float Radius => Mathf.Max(0f, _radius);

        public Vector3 GetRandomSpawnPosition()
        {
            var position = transform.position;

            if (Radius <= 0f)
            {
                return position;
            }

            var offset = UnityEngine.Random.insideUnitCircle * Radius;
            position.x += offset.x;
            position.z += offset.y;

            return position;
        }
    }
}
