namespace TPSBR.Enemies
{
    public class EnemyDeathBehavior : EnemyBehaviorBase
    {
        protected override void OnEnterState()
        {
            base.OnEnterState();
            // Pseudocode: Trigger ragdoll, stop AI updates, and broadcast death events.
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            // Pseudocode: Wait for death animation or ragdoll settling before allowing despawn.
        }

        protected override void OnExitState()
        {
            base.OnExitState();
            // Pseudocode: Clean up death VFX or detach loot drops.
        }
    }
}