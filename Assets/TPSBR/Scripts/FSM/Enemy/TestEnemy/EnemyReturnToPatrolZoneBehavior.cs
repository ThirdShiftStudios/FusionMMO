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
            enemy.MoveTowardsXZ(spawnPosition, enemy.MovementSpeed, Runner.DeltaTime);

            if (enemy.IsWithinPatrolRadius(enemy.transform.position) == true && enemy.Patrol != null)
            {
                Machine.ForceActivateState(enemy.Patrol.StateId);
            }
        }
    }
}
