using UnityEngine;

namespace TPSBR.Enemies
{
    public class EnemyPatrolBehavior : EnemyBehaviorBase
    {
        [SerializeField]
        [Tooltip("Radius around the spawn point considered the patrol zone.")]
        private float _patrolRadius = 10f;

        [SerializeField]
        [Tooltip("Waypoints the enemy should follow when patrolling.")]
        private Transform[] _waypoints;

        private int _currentWaypointIndex;

        protected override void OnEnterState()
        {
            base.OnEnterState();
            _currentWaypointIndex = 0;
            // Pseudocode: Acquire navigation agent, start moving towards the first waypoint, and raise patrol started events.
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            // Pseudocode: Advance through waypoints, loop when necessary, and monitor for enemies entering detection radius.
            // if (playerDetected) -> transition to chase behavior.
            // if (outside patrol radius) -> request return-to-patrol-zone behavior.
        }

        protected override void OnExitState()
        {
            base.OnExitState();
            // Pseudocode: Stop pathing and clear patrol navigation data.
        }
    }
}