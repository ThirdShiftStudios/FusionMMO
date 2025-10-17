using Fusion;
using UnityEngine;

namespace TPSBR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyNetworkBehavior : ContextBehaviour
    {
        [SerializeField]
        private EnemyHealth _health;

        private PrototypeEnemySpawner _spawner;

        public override void Spawned()
        {
            base.Spawned();

            if (_health == null)
            {
                _health = GetComponent<EnemyHealth>();
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false)
                return;

            if (_health == null)
                return;

            if (_health.CurrentHealth <= 0f)
            {
                DespawnEnemy();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _spawner?.NotifyEnemyDespawned(this);
            _spawner = null;

            base.Despawned(runner, hasState);
        }

        public void SetSpawner(PrototypeEnemySpawner spawner)
        {
            _spawner = spawner;
        }

        private void DespawnEnemy()
        {
            if (Runner == null)
                return;

            if (Object == null || Object.IsValid == false)
                return;

            Runner.Despawn(Object);
        }
    }
}
