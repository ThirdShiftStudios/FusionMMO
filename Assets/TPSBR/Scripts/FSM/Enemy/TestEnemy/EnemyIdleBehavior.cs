namespace TPSBR.Enemies
{
    public class EnemyIdleBehavior : EnemyBehaviorBase
    {
        protected override void OnEnterState()
        {
            base.OnEnterState();
            // Pseudocode: Play idle animation, reset navigation targets, and listen for player detection events.
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            // Pseudocode: Continuously scan for threats or timers to transition to patrol.
            // if (playerDetected) -> transition to chase.
            // if (patrolTimerExpired) -> transition to patrol.
        }

        protected override void OnExitState()
        {
            base.OnExitState();
            // Pseudocode: Stop idle specific effects such as breathing audio loops.
        }
    }
}