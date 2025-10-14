using Fusion.Addons.FSM;
using UnityEngine;

namespace TPSBR.Enemies
{
        public abstract class TestEnemyBehaviorBase : EnemyBehavior
        {
                [SerializeField]
                [Tooltip("Optional explicit next behavior to transition into when this state completes.")]
                private EnemyBehavior _defaultNext;

                protected EnemyBehavior DefaultNext => _defaultNext;

                protected EnemyBehavior ResolveDefaultNext<TBehavior>() where TBehavior : EnemyBehavior
                {
                        if (_defaultNext != null)
                                return _defaultNext;

                        if (Controller == null)
                                return null;

                        for (int i = 0; i < Controller.Machine.States.Length; i++)
                        {
                                if (Controller.Machine.States[i] is TBehavior)
                                {
                                        return Controller.Machine.States[i];
                                }
                        }

                        return null;
                }
        }

        public class TestEnemySpawningBehavior : TestEnemyBehaviorBase
        {
                [SerializeField]
                [Tooltip("How long the spawn presentation should take before transitioning to idle.")]
                private float _spawnDuration = 2f;

                private float _spawnTimer;

                protected override void OnEnterState()
                {
                        base.OnEnterState();
                        _spawnTimer = _spawnDuration;

                        // Pseudocode: Play spawn animation, enable invulnerability, and notify systems that the enemy is spawning.
                }

                protected override void OnFixedUpdate()
                {
                        base.OnFixedUpdate();

                        // Pseudocode: Count down spawn timer and monitor for spawn completion events.
                        if (_spawnTimer > 0f)
                        {
                                _spawnTimer -= Runner.DeltaTime;
                        }

                        if (_spawnTimer <= 0f)
                        {
                                // Pseudocode: Determine if we should go to idle or patrol depending on world state.
                                var next = ResolveDefaultNext<TestEnemyIdleBehavior>();
                                if (next != null)
                                {
                                        Machine.ForceActivateState(next);
                                }
                        }
                }

                protected override void OnExitState()
                {
                        base.OnExitState();

                        // Pseudocode: Remove spawn-only effects like invulnerability and VFX.
                }
        }

        public class TestEnemyIdleBehavior : TestEnemyBehaviorBase
        {
                protected override void OnEnterState()
                {
                        base.OnEnterState();
                        // Pseudocode: Play idle animation, reset navigation targets, and listen for player detection events.
                }

                protected override void OnFixedUpdate()
                {
                        base.OnFixedUpdate();
                        // Pseudocode: Continuously scan for threats or timers to transition to patrol.
                        // if (playerDetected) -> transition to chase.
                        // if (patrolTimerExpired) -> transition to patrol.
                }

                protected override void OnExitState()
                {
                        base.OnExitState();
                        // Pseudocode: Stop idle specific effects such as breathing audio loops.
                }
        }

        public class TestEnemyPatrolBehavior : TestEnemyBehaviorBase
        {
                [SerializeField]
                [Tooltip("Radius around the spawn point considered the patrol zone.")]
                private float _patrolRadius = 10f;

                [SerializeField]
                [Tooltip("Waypoints the enemy should follow when patrolling.")]
                private Transform[] _waypoints;

                private int _currentWaypointIndex;

                protected override void OnEnterState()
                {
                        base.OnEnterState();
                        _currentWaypointIndex = 0;
                        // Pseudocode: Acquire navigation agent, start moving towards the first waypoint, and raise patrol started events.
                }

                protected override void OnFixedUpdate()
                {
                        base.OnFixedUpdate();
                        // Pseudocode: Advance through waypoints, loop when necessary, and monitor for enemies entering detection radius.
                        // if (playerDetected) -> transition to chase behavior.
                        // if (outside patrol radius) -> request return-to-patrol-zone behavior.
                }

                protected override void OnExitState()
                {
                        base.OnExitState();
                        // Pseudocode: Stop pathing and clear patrol navigation data.
                }
        }

        public class TestEnemyChaseAgentBehavior : TestEnemyBehaviorBase
        {
                [SerializeField]
                [Tooltip("Maximum chase distance before giving up.")]
                private float _maxChaseDistance = 25f;

                protected override void OnEnterState()
                {
                        base.OnEnterState();
                        // Pseudocode: Lock onto the detected agent, increase movement speed, and play chase animation.
                }

                protected override void OnFixedUpdate()
                {
                        base.OnFixedUpdate();
                        // Pseudocode: Pursue the target, update pathing, and evaluate attack opportunities.
                        // if (target lost or too far) -> transition to return-to-patrol-zone.
                        // if (target defeated) -> transition to patrol or idle.
                }

                protected override void OnExitState()
                {
                        base.OnExitState();
                        // Pseudocode: Reset chase modifiers and release target references.
                }
        }

        public class TestEnemyReturnToPatrolZoneBehavior : TestEnemyBehaviorBase
        {
                protected override void OnEnterState()
                {
                        base.OnEnterState();
                        // Pseudocode: Plot a path back to the patrol origin and play retreat animation.
                }

                protected override void OnFixedUpdate()
                {
                        base.OnFixedUpdate();
                        // Pseudocode: Move toward patrol zone and detect when arrival occurs.
                        // if (reached patrol zone) -> transition back to patrol or idle depending on design.
                }

                protected override void OnExitState()
                {
                        base.OnExitState();
                        // Pseudocode: Clear navigation overrides once the enemy is back inside the zone.
                }
        }

        public class TestEnemyDeathBehavior : TestEnemyBehaviorBase
        {
                protected override void OnEnterState()
                {
                        base.OnEnterState();
                        // Pseudocode: Trigger ragdoll, stop AI updates, and broadcast death events.
                }

                protected override void OnFixedUpdate()
                {
                        base.OnFixedUpdate();
                        // Pseudocode: Wait for death animation or ragdoll settling before allowing despawn.
                }

                protected override void OnExitState()
                {
                        base.OnExitState();
                        // Pseudocode: Clean up death VFX or detach loot drops.
                }
        }

        public class TestEnemyDespawnBehavior : TestEnemyBehaviorBase
        {
                protected override void OnEnterState()
                {
                        base.OnEnterState();
                        // Pseudocode: Disable colliders, play despawn VFX, and notify spawner the enemy is leaving the world.
                }

                protected override void OnFixedUpdate()
                {
                        base.OnFixedUpdate();
                        // Pseudocode: Wait for despawn animation or timer and then request pooling/destruction.
                }

                protected override void OnExitState()
                {
                        base.OnExitState();
                        // Pseudocode: This state should only exit when the object is about to be destroyed or reused.
                }
        }
}
