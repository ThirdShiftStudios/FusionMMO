using UnityEngine;

namespace TPSBR
{
    public abstract class Climbable : MonoBehaviour
    {
        [SerializeField]
        private float _activationDistance = 1.5f;

        [SerializeField]
        private float _exitDistance = 0.75f;

        public float ActivationDistance => Mathf.Max(0.0f, _activationDistance);
        public float ExitDistance => Mathf.Max(0.0f, _exitDistance);

        public abstract int WaypointCount { get; }
        public abstract Vector3 GetWaypointPosition(int index);
        public abstract Vector3 GetSegmentDirection(int index);
        public abstract Vector3 ProjectOnSegment(int index, Vector3 position);
        public abstract float GetNormalizedProgress(int index, Vector3 position);

        public Vector3 StartPoint => WaypointCount > 0 ? GetWaypointPosition(0) : transform.position;
        public Vector3 EndPoint => WaypointCount > 0 ? GetWaypointPosition(WaypointCount - 1) : transform.position;
    }
}
