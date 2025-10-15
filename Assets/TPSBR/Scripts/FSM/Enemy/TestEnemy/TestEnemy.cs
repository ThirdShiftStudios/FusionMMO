using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using UnityEngine;

namespace TPSBR.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StateMachineController))]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(TPSBR.EnemyHealth))]
    public class TestEnemy : EnemyBehaviorController
    {
        public EnemySpawningBehavior Spawning => _spawning;
        public EnemyIdleBehavior Idle => _idle;
        public EnemyPatrolBehavior Patrol => _patrol;
        public EnemyChaseAgentBehavior ChaseAgent => _chaseAgent;
        public EnemyReturnToPatrolZoneBehavior ReturnToPatrolZone => _returnToPatrolZone;
        public EnemyDeathBehavior Death => _death;
        public EnemyDespawnBehavior Despawn => _despawn;

        [SerializeField] [Tooltip("Reference to the spawning behavior.")]
        private EnemySpawningBehavior _spawning;

        [SerializeField] [Tooltip("Reference to the idle behavior.")]
        private EnemyIdleBehavior _idle;

        [SerializeField] [Tooltip("Reference to the patrol behavior.")]
        private EnemyPatrolBehavior _patrol;

        [SerializeField] [Tooltip("Reference to the chase agent behavior.")]
        private EnemyChaseAgentBehavior _chaseAgent;

        [SerializeField] [Tooltip("Reference to the return-to-patrol-zone behavior.")]
        private EnemyReturnToPatrolZoneBehavior _returnToPatrolZone;

        [SerializeField] [Tooltip("Reference to the death behavior.")]
        private EnemyDeathBehavior _death;

        [SerializeField] [Tooltip("Reference to the despawn behavior.")]
        private EnemyDespawnBehavior _despawn;

        protected override void OnBehaviorsCached(List<EnemyBehavior> behaviors)
        {
            base.OnBehaviorsCached(behaviors);

            _spawning = ResolveBehavior(_spawning, behaviors);
            _idle = ResolveBehavior(_idle, behaviors);
            _patrol = ResolveBehavior(_patrol, behaviors);
            _chaseAgent = ResolveBehavior(_chaseAgent, behaviors);
            _returnToPatrolZone = ResolveBehavior(_returnToPatrolZone, behaviors);
            _death = ResolveBehavior(_death, behaviors);
            _despawn = ResolveBehavior(_despawn, behaviors);
        }

        protected override void ConfigureStateMachine(StateMachine<EnemyBehavior> machine)
        {
            base.ConfigureStateMachine(machine);

            if (_spawning != null)
            {
                machine.SetDefaultState(_spawning.StateId);
            }
        }

        private static TBehavior ResolveBehavior<TBehavior>(TBehavior current, IReadOnlyList<EnemyBehavior> available)
            where TBehavior : EnemyBehavior
        {
            if (current != null)
                return current;

            for (int i = 0; i < available.Count; i++)
            {
                if (available[i] is TBehavior typed)
                {
                    return typed;
                }
            }

            return null;
        }
    }
}