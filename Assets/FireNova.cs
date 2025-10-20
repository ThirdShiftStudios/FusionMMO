
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class FireNova : ContextBehaviour
    {
        private EnemyNetworkBehavior[] _enemiesNearby;
        [SerializeField] private Transform _visualsRoot;
        [SerializeField] private Transform _scalarRoot;
        
        public override void Spawned()
        {
            base.Spawned();
            _enemiesNearby = GetNearbyEnemies();
        }

        private EnemyNetworkBehavior[] GetNearbyEnemies()
        {
            return default;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _enemiesNearby = default;
            base.Despawned(runner, hasState);
        }
    }
}
