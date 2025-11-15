using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyChaseAgentBehavior : EnemyBehaviorBase
    {
        [SerializeField]
        [Tooltip("Maximum chase distance before giving up.")]
        private float _maxChaseDistance = 20f;

        private bool _shouldReturnToPatrolZone;
        private bool _shouldAttackPlayer;

        public bool ShouldReturnToPatrolZone => _shouldReturnToPatrolZone;
        public bool ShouldAttackPlayer => _shouldAttackPlayer;

        protected override void OnEnterState()
        {
            base.OnEnterState();

            _shouldReturnToPatrolZone = false;
            _shouldAttackPlayer = false;
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (HasStateAuthority == false)
                return;

            if (Controller is not TestEnemy enemy)
                return;

            _shouldAttackPlayer = false;
            _shouldReturnToPatrolZone = false;

            if (enemy.HasPlayerTarget == false)
            {
                enemy.StopNavigation();
                _shouldReturnToPatrolZone = true;
                return;
            }

            Vector3 playerPosition = enemy.GetTargetPosition();
            const float stoppingDistance = 0.25f;

            if (enemy.AttackPlayer != null && enemy.AttackPlayer.IsWithinAttackRange(enemy))
            {
                enemy.StopNavigation();
                _shouldAttackPlayer = true;
                return;
            }

            if (enemy.AIPath != null && enemy.Seeker != null)
            {
                enemy.NavigateTo(playerPosition, stoppingDistance);
            }
            else
            {
                enemy.MoveTowardsXZ(playerPosition, enemy.MovementSpeed, Runner.DeltaTime);
            }

            float distanceFromSpawn = enemy.GetHorizontalDistanceFromSpawn();
            if (distanceFromSpawn >= _maxChaseDistance)
            {
                enemy.StopNavigation();
                _shouldReturnToPatrolZone = true;
            }
        }

        protected override void OnExitState()
        {
            _shouldReturnToPatrolZone = false;
            _shouldAttackPlayer = false;

            if (Controller is TestEnemy enemy)
            {
                enemy.StopNavigation();
            }

            Controller.ClearTarget();
            base.OnExitState();
        }
    }
}
