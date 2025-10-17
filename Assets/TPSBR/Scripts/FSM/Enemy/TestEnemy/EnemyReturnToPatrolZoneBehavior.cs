using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyReturnToPatrolZoneBehavior : EnemyBehaviorBase
    {
        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (HasStateAuthority == false)
                return;

            if (Controller is not TestEnemy enemy)
                return;

            Vector3 spawnPosition = enemy.SpawnPosition;

            if (enemy.AIPath != null && enemy.Seeker != null)
            {
                const float stoppingDistance = 0.5f;
                enemy.NavigateTo(spawnPosition, stoppingDistance);

                bool reachedDestination = enemy.HasReachedDestination(stoppingDistance);
                if ((reachedDestination == true || enemy.IsWithinPatrolRadius(enemy.transform.position) == true) && enemy.Patrol != null)
                {
                    Machine.ForceActivateState(enemy.Patrol.StateId);
                }
            }
            else
            {
                bool reached = enemy.MoveTowardsXZ(spawnPosition, enemy.MovementSpeed, Runner.DeltaTime);

                if ((reached == true || enemy.IsWithinPatrolRadius(enemy.transform.position) == true) && enemy.Patrol != null)
                {
                    Machine.ForceActivateState(enemy.Patrol.StateId);
                }
            }
        }
    }
}
