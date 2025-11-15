using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyPatrolBehavior : EnemyBehaviorBase
    {
        [SerializeField]
        [Tooltip("Radius around the spawn point considered the patrol zone.")]
        private float _patrolRadius = 10f;

        [SerializeField]
        [Tooltip("Distance from the patrol destination at which it is considered reached.")]
        private float _patrolStoppingDistance = 0.5f;

        private Vector3 _currentTarget;
        private bool _hasTarget;
        private bool _readyToIdle;

        public float PatrolRadius => _patrolRadius;
        public bool ReadyToIdle => _readyToIdle;

        protected override void OnEnterState()
        {
            base.OnEnterState();

            _hasTarget = false;
            _readyToIdle = false;
            SelectNewTarget();
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (HasStateAuthority == false)
                return;

            if (Controller is not TestEnemy enemy)
                return;

            if (enemy.HasPlayerTarget == true)
            {
                enemy.StopNavigation();
                return;
            }

            if (_hasTarget == false)
            {
                SelectNewTarget();
            }

            if (_hasTarget == false)
                return;

            if (enemy.AIPath != null && enemy.Seeker != null)
            {
                enemy.NavigateTo(_currentTarget, _patrolStoppingDistance);

                if (enemy.HasReachedDestination(_patrolStoppingDistance) == true)
                {
                    if (enemy.Idle != null)
                    {
                        _readyToIdle = true;
                    }
                    else
                    {
                        SelectNewTarget();
                    }
                }
            }
            else
            {
                bool reached = enemy.MoveTowardsXZ(_currentTarget, enemy.MovementSpeed, Runner.DeltaTime);

                if (reached == true)
                {
                    if (enemy.Idle != null)
                    {
                        _readyToIdle = true;
                    }
                    else
                    {
                        SelectNewTarget();
                    }
                }
            }
        }

        protected override void OnExitState()
        {
            base.OnExitState();

            _readyToIdle = false;
        }

        private void SelectNewTarget()
        {
            if (Controller is not TestEnemy enemy)
            {
                _hasTarget = false;
                _readyToIdle = false;
                return;
            }

            Vector3 spawnPosition = enemy.SpawnPosition;
            Vector2 randomOffset = Random.insideUnitCircle * _patrolRadius;
            _currentTarget = new Vector3(spawnPosition.x + randomOffset.x, spawnPosition.y, spawnPosition.z + randomOffset.y);
            _hasTarget = true;
            _readyToIdle = false;

            if (enemy.AIPath != null && enemy.Seeker != null)
            {
                enemy.NavigateTo(_currentTarget, _patrolStoppingDistance);
            }
        }
    }
}