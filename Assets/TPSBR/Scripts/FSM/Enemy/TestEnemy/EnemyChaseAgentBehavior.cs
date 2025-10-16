using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyChaseAgentBehavior : EnemyBehaviorBase
    {
        [SerializeField]
        [Tooltip("Maximum chase distance before giving up.")]
        private float _maxChaseDistance = 20f;

        protected override void OnEnterState()
        {
            base.OnEnterState();

            if (Controller is TestEnemy enemy)
            {
                enemy.ResetPathfinding();
            }
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (HasStateAuthority == false)
                return;

            if (Controller is not TestEnemy enemy)
                return;

            if (enemy.HasPlayerTarget == false)
            {
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
            enemy.MoveTowardsXZ(playerPosition, enemy.MovementSpeed, Runner.DeltaTime);

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
                enemy.ResetPathfinding();
            }

            Controller.ClearTarget();
            base.OnExitState();
        }
    }
}
