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
                        var parentPath = GetComponentInParent<CinematicWaypointPath>();

                        if (parentPath != null)
                        {
                                parentPath.DrawPathGizmos(this);
                                return;
                        }

                        var previousColor = Gizmos.color;
                        var position = transform.position;

                        Gizmos.color = Color.yellow;
                        Gizmos.DrawSphere(position, DefaultWaypointRadius);

                        Gizmos.color = Color.magenta;
                        Gizmos.DrawLine(position, position + transform.forward * DefaultDirectionLength);

                        Gizmos.color = previousColor;
                }
#endif
        }
}
