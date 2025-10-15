namespace TPSBR.Enemies
{
    public class EnemyReturnToPatrolZoneBehavior : EnemyBehaviorBase
    {
        protected override void OnEnterState()
        {
            base.OnEnterState();
            // Pseudocode: Plot a path back to the patrol origin and play retreat animation.
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            // Pseudocode: Move toward patrol zone and detect when arrival occurs.
            // if (reached patrol zone) -> transition back to patrol or idle depending on design.
        }

        protected override void OnExitState()
        {
            base.OnExitState();
            // Pseudocode: Clear navigation overrides once the enemy is back inside the zone.
        }
    }
}