using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using Pathfinding;
using UnityEngine;

namespace TPSBR
{
    [DisallowMultipleComponent]
    public sealed class PrototypeEnemySpawner : NetworkBehaviour
    {
        [Header("Spawning")]
        [SerializeField]
        private EnemyBehaviorController[] _enemyPrefabs;

        [SerializeField, Min(0)]
        private int _initialEnemyCount = 1;

        [SerializeField, Min(0)]
        private int _maxEnemies = 5;

        [SerializeField, Min(0f)]
        private float _spawnInterval = 5f;

        private readonly List<EnemyNetworkBehavior> _spawnedEnemies = new();

        [Networked]
        private TickTimer SpawnTimer { get; set; }

        public override void Spawned()
        {
            base.Spawned();

            if (HasStateAuthority == false)
                return;

            CleanupSpawnedEnemies();

            int targetInitialCount = Mathf.Clamp(_initialEnemyCount, 0, _maxEnemies);
            FillToCount(targetInitialCount);

            if (_spawnInterval > 0f && _spawnedEnemies.Count < _maxEnemies)
            {
                StartSpawnTimer();
            }
            else
            {
                SpawnTimer = default;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false)
                return;

            CleanupSpawnedEnemies();

            if (_maxEnemies <= 0)
            {
                SpawnTimer = default;
                return;
            }

            if (_spawnedEnemies.Count >= _maxEnemies)
            {
                SpawnTimer = default;
                return;
            }

            if (_spawnInterval <= 0f)
            {
                FillToCount(_maxEnemies);
                return;
            }

            if (SpawnTimer.ExpiredOrNotRunning(Runner) == false)
                return;

            TrySpawnEnemy();

            if (_spawnedEnemies.Count < _maxEnemies)
            {
                StartSpawnTimer();
            }
            else
            {
                SpawnTimer = default;
            }
        }

        internal void NotifyEnemyDespawned(EnemyNetworkBehavior enemy)
        {
            if (enemy == null)
                return;

            _spawnedEnemies.Remove(enemy);

            if (HasStateAuthority == false)
                return;

            if (_spawnInterval > 0f && _spawnedEnemies.Count < _maxEnemies)
            {
                StartSpawnTimer();
            }
            else if (_spawnInterval <= 0f)
            {
                FillToCount(_maxEnemies);
            }
        }

        private void FillToCount(int targetCount)
        {
            if (Runner == null)
                return;

            while (_spawnedEnemies.Count < targetCount)
            {
                if (TrySpawnEnemy() == false)
                    break;
            }
        }

        private bool TrySpawnEnemy()
        {
            if (Runner == null)
                return false;

            if (_enemyPrefabs == null || _enemyPrefabs.Length == 0)
                return false;

            var astar = AstarPath.active;
            if (astar == null)
                return false;

            var constraint = NNConstraint.Default;
            constraint.constrainWalkability = true;
            constraint.walkable = true;

            var nearest = astar.GetNearest(transform.position, constraint);
            if (nearest.node == null)
                return false;

            Vector3 spawnPosition = nearest.position;
            Quaternion spawnRotation = transform.rotation;

            int prefabIndex = Random.Range(0, _enemyPrefabs.Length);
            var prefab = _enemyPrefabs[prefabIndex];
            if (prefab == null)
                return false;

            var enemyInstance = Runner.Spawn(prefab, spawnPosition, spawnRotation);
            if (enemyInstance == null)
                return false;

            var networkBehavior = enemyInstance.GetComponent<EnemyNetworkBehavior>();
            if (networkBehavior == null)
            {
                if (enemyInstance.Object != null && enemyInstance.Object.IsValid == true)
                {
                    Runner.Despawn(enemyInstance.Object);
                }

                return false;
            }

            networkBehavior.SetSpawner(this);
            _spawnedEnemies.Add(networkBehavior);

            return true;
        }

        private void CleanupSpawnedEnemies()
        {
            for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _spawnedEnemies[i];
                if (enemy == null || enemy.Object == null || enemy.Object.IsValid == false)
                {
                    _spawnedEnemies.RemoveAt(i);
                }
            }
        }

        private void StartSpawnTimer()
        {
            if (Runner == null)
                return;

            if (_spawnInterval <= 0f)
            {
                SpawnTimer = default;
                return;
            }

            SpawnTimer = TickTimer.CreateFromSeconds(Runner, _spawnInterval);
        }
    }
}
