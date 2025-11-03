using System;
using UnityEngine;

namespace TPSBR
{
    public sealed class Ladder : Climbable
    {
        [SerializeField]
        private Transform[] _waypoints = Array.Empty<Transform>();

        [SerializeField]
        private float _waypointSnapDistance = 0.1f;

        private float[] _cumulativeDistances = Array.Empty<float>();
        private float _totalLength;

        public override int WaypointCount => _waypoints != null ? _waypoints.Length : 0;
        public float WaypointSnapDistance => Mathf.Max(0.01f, _waypointSnapDistance);
        public float TotalLength => _totalLength;

        private void Awake()
        {
            RebuildDistanceCache();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildDistanceCache();
        }
#endif

        public override Vector3 GetWaypointPosition(int index)
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                return transform.position;
            }

            index = Mathf.Clamp(index, 0, _waypoints.Length - 1);
            Transform waypoint = _waypoints[index];
            return waypoint != null ? waypoint.position : transform.position;
        }

        public override Vector3 GetSegmentDirection(int index)
        {
            if (WaypointCount < 2)
            {
                return transform.forward;
            }

            int clampedIndex = Mathf.Clamp(index, 0, WaypointCount - 2);
            Vector3 from = GetWaypointPosition(clampedIndex);
            Vector3 to = GetWaypointPosition(clampedIndex + 1);
            Vector3 direction = to - from;

            if (direction.sqrMagnitude < Mathf.Epsilon)
            {
                return transform.forward;
            }

            return direction.normalized;
        }

        public override Vector3 ProjectOnSegment(int index, Vector3 position)
        {
            if (WaypointCount < 2)
            {
                return transform.position;
            }

            int clampedIndex = Mathf.Clamp(index, 0, WaypointCount - 2);
            Vector3 from = GetWaypointPosition(clampedIndex);
            Vector3 to = GetWaypointPosition(clampedIndex + 1);
            Vector3 segment = to - from;

            if (segment.sqrMagnitude < Mathf.Epsilon)
            {
                return from;
            }

            float projection = Vector3.Dot(position - from, segment) / segment.sqrMagnitude;
            projection = Mathf.Clamp01(projection);

            return from + segment * projection;
        }

        public override float GetNormalizedProgress(int index, Vector3 position)
        {
            if (WaypointCount < 2 || _totalLength <= Mathf.Epsilon)
            {
                return 0f;
            }

            int clampedIndex = Mathf.Clamp(index, 0, WaypointCount - 2);
            Vector3 from = GetWaypointPosition(clampedIndex);
            Vector3 to = GetWaypointPosition(clampedIndex + 1);
            Vector3 segment = to - from;
            float segmentLength = segment.magnitude;

            float baseDistance = clampedIndex < _cumulativeDistances.Length ? _cumulativeDistances[clampedIndex] : 0f;
            float distanceAlongSegment = 0f;

            if (segmentLength > Mathf.Epsilon)
            {
                Vector3 local = position - from;
                float dot = Vector3.Dot(local, segment.normalized);
                distanceAlongSegment = Mathf.Clamp(dot, 0f, segmentLength);
            }

            return Mathf.Clamp01((baseDistance + distanceAlongSegment) / _totalLength);
        }

        private void RebuildDistanceCache()
        {
            if (WaypointCount < 2)
            {
                _totalLength = 0f;
                _cumulativeDistances = Array.Empty<float>();
                return;
            }

            if (_cumulativeDistances == null || _cumulativeDistances.Length != WaypointCount)
            {
                _cumulativeDistances = new float[WaypointCount];
            }

            _cumulativeDistances[0] = 0f;
            float runningDistance = 0f;

            for (int i = 1; i < WaypointCount; ++i)
            {
                Vector3 from = GetWaypointPosition(i - 1);
                Vector3 to = GetWaypointPosition(i);
                runningDistance += Vector3.Distance(from, to);
                _cumulativeDistances[i] = runningDistance;
            }

            _totalLength = runningDistance;
        }
    }
}
