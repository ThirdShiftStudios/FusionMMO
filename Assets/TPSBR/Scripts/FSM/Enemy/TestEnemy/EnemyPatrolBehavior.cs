using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyPatrolBehavior : EnemyBehaviorBase
    {
        [SerializeField]
        [Tooltip("Radius around the spawn point considered the patrol zone.")]
        private float _patrolRadius = 10f;

        private Vector3 _currentTarget;
        private bool _hasTarget;

        public float PatrolRadius => _patrolRadius;

        protected override void OnEnterState()
        {
            base.OnEnterState();

            _hasTarget = false;
            SelectNewTarget();
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

            if (_hasTarget == false)
            {
                SelectNewTarget();
            }

            if (_hasTarget == false)
                return;

            bool reached = enemy.MoveTowardsXZ(_currentTarget, enemy.MovementSpeed, Runner.DeltaTime);

            if (reached == true)
            {
                if (enemy.Idle != null)
                {
                    Machine.ForceActivateState(enemy.Idle.StateId);
                }
                else
                {
                    SelectNewTarget();
                }
            }
        }

        private void SelectNewTarget()
        {
            if (Controller is not TestEnemy enemy)
            {
                _hasTarget = false;
                return;
            }

            Vector3 spawnPosition = enemy.SpawnPosition;
            Vector2 randomOffset = Random.insideUnitCircle * _patrolRadius;
            _currentTarget = new Vector3(spawnPosition.x + randomOffset.x, spawnPosition.y, spawnPosition.z + randomOffset.y);
            _hasTarget = true;
        }
    }
}