namespace TPSBR
{
        using System.Collections.Generic;
        using UnityEngine;

        [DisallowMultipleComponent]
        public class CinematicWaypointPath : MonoBehaviour
        {
                [SerializeField]
                private List<CinematicWaypoint> _waypoints = new List<CinematicWaypoint>();

#if UNITY_EDITOR
                [SerializeField]
                private bool _drawWaypointPoints = true;

                [SerializeField]
                private bool _drawPathConnections = true;

                [SerializeField]
                private bool _drawCameraDirection = true;

                [SerializeField]
                private float _waypointRadius = 0.2f;

                [SerializeField]
                private float _directionLength = 1f;

                [SerializeField]
                private Color _waypointColor = Color.yellow;

                [SerializeField]
                private Color _pathColor = Color.cyan;

                [SerializeField]
                private Color _directionColor = Color.magenta;

                public bool DrawWaypointPoints => _drawWaypointPoints;

                public bool DrawPathConnections => _drawPathConnections;

                public bool DrawCameraDirection => _drawCameraDirection;

                public float WaypointRadius => _waypointRadius;

                public float DirectionLength => _directionLength;

                public Color WaypointColor => _waypointColor;

                public Color PathColor => _pathColor;

                public Color DirectionColor => _directionColor;
#endif

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
                        _waypointRadius = Mathf.Max(_waypointRadius, 0.01f);
                        _directionLength = Mathf.Max(_directionLength, 0f);

                        RefreshWaypoints();
                }

                private void OnDrawGizmosSelected()
                {
                        RefreshWaypoints();

                        if (_waypoints == null || _waypoints.Count == 0)
                                return;

                        var previousColor = Gizmos.color;

                        if (_drawWaypointPoints)
                        {
                                Gizmos.color = _waypointColor;

                                foreach (var waypoint in _waypoints)
                                {
                                        if (waypoint == null)
                                                continue;

                                        Gizmos.DrawSphere(waypoint.transform.position, _waypointRadius);
                                }
                        }

                        if (_drawPathConnections)
                        {
                                Gizmos.color = _pathColor;

                                for (int i = 0; i < _waypoints.Count - 1; ++i)
                                {
                                        var current = _waypoints[i];
                                        var next = _waypoints[i + 1];

                                        if (current == null || next == null)
                                                continue;

                                        Gizmos.DrawLine(current.transform.position, next.transform.position);
                                }
                        }

                        if (_drawCameraDirection)
                        {
                                Gizmos.color = _directionColor;

                                foreach (var waypoint in _waypoints)
                                {
                                        if (waypoint == null)
                                                continue;

                                        var position = waypoint.transform.position;
                                        var direction = waypoint.transform.forward * _directionLength;

                                        Gizmos.DrawLine(position, position + direction);
                                }
                        }

                        Gizmos.color = previousColor;
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
