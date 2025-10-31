namespace TPSBR
{
        using System.Collections.Generic;
        using UnityEngine;

        [DisallowMultipleComponent]
        public class CinematicWaypointPath : MonoBehaviour
        {
                [SerializeField]
                private List<CinematicWaypoint> _waypoints = new List<CinematicWaypoint>();

                public IReadOnlyList<CinematicWaypoint> Waypoints => _waypoints;

                private void Awake()
                {
                        RefreshWaypoints();
                }

                private void OnTransformChildrenChanged()
                {
                        RefreshWaypoints();
                }

#if UNITY_EDITOR
                private void OnValidate()
                {
                        RefreshWaypoints();
                }
#endif

                public void RefreshWaypoints()
                {
                        _waypoints.Clear();

                        var children = GetComponentsInChildren<CinematicWaypoint>(true);
                        foreach (var waypoint in children)
                        {
                                if (waypoint == null || waypoint.transform == transform)
                                        continue;

                                _waypoints.Add(waypoint);
                        }
                }
        }
}
