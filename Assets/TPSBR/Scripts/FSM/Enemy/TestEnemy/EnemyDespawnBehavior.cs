namespace TPSBR.Enemies
{
        public class EnemyDespawnBehavior : SingleClipBehavior
        {
                protected override void OnEnterState()
                {
                        base.OnEnterState();
                        // Pseudocode: Disable colliders, play despawn VFX, and notify spawner the enemy is leaving the world.
                }

                protected override void OnFixedUpdate()
                {
                        base.OnFixedUpdate();
                        // Pseudocode: Wait for despawn animation or timer and then request pooling/destruction.
                }

                protected override void OnExitState()
                {
                        base.OnExitState();
                        // Pseudocode: This state should only exit when the object is about to be destroyed or reused.
                }
        }
}
