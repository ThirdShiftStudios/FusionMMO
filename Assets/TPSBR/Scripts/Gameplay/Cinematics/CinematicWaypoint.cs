namespace TPSBR
{
        using UnityEngine;

        [DisallowMultipleComponent]
        public class CinematicWaypoint : MonoBehaviour
        {
                [SerializeField]
                private float _segmentDuration = 1f;

                public float SegmentDuration => Mathf.Max(_segmentDuration, 0.01f);

#if UNITY_EDITOR
                private const float DefaultWaypointRadius = 0.2f;
                private const float DefaultDirectionLength = 1f;

                private void OnValidate()
                {
                        _segmentDuration = Mathf.Max(_segmentDuration, 0.01f);
                }

                private void OnDrawGizmosSelected()
                {
                        var previousColor = Gizmos.color;
                        var position = transform.position;

                        var parentPath = GetComponentInParent<CinematicWaypointPath>();

                        var shouldDrawSphere = parentPath == null ? true : parentPath.DrawWaypointPoints;
                        var shouldDrawDirection = parentPath == null ? true : parentPath.DrawCameraDirection;

                        if (shouldDrawSphere)
                        {
                                var radius = parentPath == null ? DefaultWaypointRadius : parentPath.WaypointRadius;
                                var color = parentPath == null ? Color.yellow : parentPath.WaypointColor;

                                Gizmos.color = color;
                                Gizmos.DrawSphere(position, radius);
                        }

                        if (shouldDrawDirection)
                        {
                                var directionLength = parentPath == null ? DefaultDirectionLength : parentPath.DirectionLength;
                                var color = parentPath == null ? Color.magenta : parentPath.DirectionColor;

                                Gizmos.color = color;
                                Gizmos.DrawLine(position, position + transform.forward * directionLength);
                        }

                        Gizmos.color = previousColor;
                }
#endif
        }
}
