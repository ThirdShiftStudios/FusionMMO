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
                public TestEnemySpawningBehavior Spawning => _spawning;
                public TestEnemyIdleBehavior Idle => _idle;
                public TestEnemyPatrolBehavior Patrol => _patrol;
                public TestEnemyChaseAgentBehavior ChaseAgent => _chaseAgent;
                public TestEnemyReturnToPatrolZoneBehavior ReturnToPatrolZone => _returnToPatrolZone;
                public TestEnemyDeathBehavior Death => _death;
                public TestEnemyDespawnBehavior Despawn => _despawn;

                [SerializeField]
                [Tooltip("Reference to the spawning behavior.")]
                private TestEnemySpawningBehavior _spawning;

                [SerializeField]
                [Tooltip("Reference to the idle behavior.")]
                private TestEnemyIdleBehavior _idle;

                [SerializeField]
                [Tooltip("Reference to the patrol behavior.")]
                private TestEnemyPatrolBehavior _patrol;

                [SerializeField]
                [Tooltip("Reference to the chase agent behavior.")]
                private TestEnemyChaseAgentBehavior _chaseAgent;

                [SerializeField]
                [Tooltip("Reference to the return-to-patrol-zone behavior.")]
                private TestEnemyReturnToPatrolZoneBehavior _returnToPatrolZone;

                [SerializeField]
                [Tooltip("Reference to the death behavior.")]
                private TestEnemyDeathBehavior _death;

                [SerializeField]
                [Tooltip("Reference to the despawn behavior.")]
                private TestEnemyDespawnBehavior _despawn;

#if UNITY_EDITOR
                private void Reset()
                {
                        EnsureComponent<NetworkObject>();
                        EnsureComponent<NetworkTransform>();
                        EnsureComponent<StateMachineController>();
                        EnsureComponent<TPSBR.EnemyHealth>();
                }
#endif

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

#if UNITY_EDITOR
                private void EnsureComponent<T>() where T : Component
                {
                        if (GetComponent<T>() == null)
                        {
                                gameObject.AddComponent<T>();
                        }
                }
#endif
        }
}
