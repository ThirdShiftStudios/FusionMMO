using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyChaseAgentBehavior : EnemyBehaviorBase
    {
        [SerializeField]
        [Tooltip("Maximum chase distance before giving up.")]
        private float _maxChaseDistance = 20f;

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (HasStateAuthority == false)
                return;

            if (Controller is not TestEnemy enemy)
                return;

            if (enemy.HasPlayerTarget == false)
            {
                enemy.StopNavigation();

                if (enemy.ReturnToPatrolZone != null)
                {
                    Machine.ForceActivateState(enemy.ReturnToPatrolZone.StateId);
                }
                else if (enemy.Patrol != null)
                {
                    Machine.ForceActivateState(enemy.Patrol.StateId);
                }

                return;
            }

            Vector3 playerPosition = enemy.GetTargetPosition();
            const float stoppingDistance = 0.25f;

            if (enemy.AttackPlayer != null && enemy.AttackPlayer.IsWithinAttackRange(enemy))
            {
                enemy.StopNavigation();
                Machine.ForceActivateState(enemy.AttackPlayer.StateId);
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
                if (enemy.ReturnToPatrolZone != null)
                {
                    Machine.ForceActivateState(enemy.ReturnToPatrolZone.StateId);
                }
                else if (enemy.Patrol != null)
                {
                    Machine.ForceActivateState(enemy.Patrol.StateId);
                }
            }
        }

        protected override void OnExitState()
        {
            if (Controller is TestEnemy enemy)
            {
                enemy.StopNavigation();
            }

            Controller.ClearTarget();
            base.OnExitState();
        }
    }
}
