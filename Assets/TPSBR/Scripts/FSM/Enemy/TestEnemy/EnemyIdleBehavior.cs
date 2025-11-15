using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyIdleBehavior : EnemyBehaviorBase
    {
        [SerializeField]
        [Tooltip("Time the enemy waits before resuming patrol.")]
        private float _idleDuration = 3f;

        private float _idleTimer;

        public bool IsIdleDurationComplete => _idleTimer <= 0f;

        protected override void OnEnterState()
        {
            base.OnEnterState();
            _idleTimer = _idleDuration;

            if (Controller is TestEnemy enemy)
            {
                enemy.StopNavigation();
            }
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (HasStateAuthority == false)
                return;

            if (Controller is not TestEnemy)
                return;

            if (_idleTimer > 0f)
            {
                _idleTimer = Mathf.Max(0f, _idleTimer - Runner.DeltaTime);
            }
        }
    }
}
