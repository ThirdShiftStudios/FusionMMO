using Animancer;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(AnimancerComponent))]
    public sealed class EnemyNetworkBehavior : ContextBehaviour
    {
        [SerializeField]
        private EnemyHealth _health;

        [SerializeField]
        private AnimancerComponent _animancer;

        private PrototypeEnemySpawner _spawner;

        public AnimancerComponent Animancer => _animancer;

        public override void Spawned()
        {
            base.Spawned();

            if (_health == null)
            {
                _health = GetComponent<EnemyHealth>();
            }

            if (_animancer == null)
            {
                _animancer = GetComponent<AnimancerComponent>();
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
