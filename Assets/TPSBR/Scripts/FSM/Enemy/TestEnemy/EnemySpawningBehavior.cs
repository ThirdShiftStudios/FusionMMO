using Fusion.Addons.FSM;
using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemySpawningBehavior : EnemyBehaviorBase
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
                var next = ResolveDefaultNext<EnemyIdleBehavior>();
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
}