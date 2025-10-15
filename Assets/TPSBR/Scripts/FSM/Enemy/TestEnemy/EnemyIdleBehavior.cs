using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyIdleBehavior : EnemyBehaviorBase
    {
        [SerializeField]
        [Tooltip("Time the enemy waits before resuming patrol.")]
        private float _idleDuration = 3f;

        private float _idleTimer;

        protected override void OnEnterState()
        {
            base.OnEnterState();
            _idleTimer = _idleDuration;
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (HasStateAuthority == false)
                return;

            if (Controller is not TestEnemy enemy)
                return;

            if (enemy.HasPlayerTarget == true && enemy.ChaseAgent != null)
            {
                Machine.ForceActivateState(enemy.ChaseAgent.StateId);
                return;
            }

            if (_idleTimer > 0f)
            {
                _idleTimer -= Runner.DeltaTime;
            }

            if (_idleTimer <= 0f && enemy.Patrol != null)
            {
                Machine.ForceActivateState(enemy.Patrol.StateId);
            }
        }
    }
}
