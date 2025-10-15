using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyChaseAgentBehavior : EnemyBehaviorBase
    {
        [SerializeField]
        [Tooltip("Maximum chase distance before giving up.")]
        private float _maxChaseDistance = 25f;

        protected override void OnEnterState()
        {
            base.OnEnterState();
            // Pseudocode: Lock onto the detected agent, increase movement speed, and play chase animation.
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            // Pseudocode: Pursue the target, update pathing, and evaluate attack opportunities.
            // if (target lost or too far) -> transition to return-to-patrol-zone.
            // if (target defeated) -> transition to patrol or idle.
        }

        protected override void OnExitState()
        {
            base.OnExitState();
            // Pseudocode: Reset chase modifiers and release target references.
        }
    }
}