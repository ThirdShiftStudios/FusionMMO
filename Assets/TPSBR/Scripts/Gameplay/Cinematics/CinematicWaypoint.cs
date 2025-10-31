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
                private void OnValidate()
                {
                        _segmentDuration = Mathf.Max(_segmentDuration, 0.01f);
                }
#endif
        }
}
