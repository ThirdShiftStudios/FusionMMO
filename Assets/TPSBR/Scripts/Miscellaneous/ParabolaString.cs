using UnityEngine;

namespace TPSBR
{
    public class ParabolaString : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer _lineRenderer;
        [SerializeField]
        private int _segmentCount = 20;
        [SerializeField]
        private float _height = 1f;

        private Transform _start;
        private Transform _end;

        private void Awake()
        {
            HideLine();
        }

        private void LateUpdate()
        {
            if (_lineRenderer == null || _start == null || _end == null)
            {
                HideLine();
                return;
            }

            int segmentCount = Mathf.Max(2, _segmentCount);
            float count = segmentCount - 1f;

            Vector3 start = _start.position;
            Vector3 end = _end.position;

            if (_lineRenderer.positionCount != segmentCount)
            {
                _lineRenderer.positionCount = segmentCount;
            }

            for (int i = 0; i < segmentCount; ++i)
            {
                float t = i / count;
                Vector3 position = SampleParabola(start, end, _height, t);
                _lineRenderer.SetPosition(i, position);
            }

            if (_lineRenderer.enabled == false)
            {
                _lineRenderer.enabled = true;
            }
        }

        public void SetEndpoints(Transform start, Transform end)
        {
            _start = start;
            _end = end;
        }

        public void ClearEndpoints()
        {
            _start = null;
            _end = null;
            HideLine();
        }

        private void HideLine()
        {
            if (_lineRenderer == null)
                return;

            _lineRenderer.enabled = false;
            _lineRenderer.positionCount = 0;
        }

        private Vector3 SampleParabola(Vector3 start, Vector3 end, float height, float t)
        {
            if (Mathf.Abs(start.y - end.y) < 0.1f)
            {
                Vector3 travelDirection = end - start;
                Vector3 result = start + t * travelDirection;
                result.y += Mathf.Sin(t * Mathf.PI) * height;
                return result;
            }

            Vector3 travelDirection = end - start;
            Vector3 levelDirection = end - new Vector3(start.x, end.y, start.z);
            Vector3 right = Vector3.Cross(travelDirection, levelDirection);
            Vector3 up = Vector3.Cross(right, levelDirection);

            if (end.y > start.y)
            {
                up = -up;
            }

            Vector3 result = start + t * travelDirection;
            result += Mathf.Sin(t * Mathf.PI) * height * up.normalized;
            return result;
        }
    }
}
