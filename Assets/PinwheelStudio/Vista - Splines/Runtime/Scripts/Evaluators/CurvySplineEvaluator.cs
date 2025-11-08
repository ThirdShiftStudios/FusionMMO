#if VISTA
#if VISTA_CURVY_SPLINE
using FluffyUnderware.Curvy;
using UnityEngine;

namespace Pinwheel.Vista.Splines
{
    [RequireComponent(typeof(CurvySpline))]
    public class CurvySplineEvaluator : SplineEvaluatorComponentBase, ITrianglesBufferProvider
    {
        [SerializeField]
        private CurvySpline m_spline;
        public CurvySpline spline
        {
            get
            {
                return m_spline;
            }
        }

        [SerializeField]
        private int m_smoothness;
        public int smoothness
        {
            get
            {
                return m_smoothness;
            }
            set
            {
                int oldValue = m_smoothness;
                int newValue = value;
                if (oldValue != newValue)
                {
                    m_smoothness = Mathf.Max(2, newValue);
                    ClearSplineCache();
                }
            }
        }

        [SerializeField]
        private float m_width;
        public float width
        {
            get
            {
                return m_width;
            }
            set
            {
                float oldValue = m_width;
                float newValue = value;
                if (oldValue != newValue)
                {
                    m_width = Mathf.Max(0, newValue);
                    ClearSplineCache();
                }
            }
        }

        [SerializeField]
        private bool m_alignHorizontal;
        public bool alignHorizontal
        {
            get
            {
                return m_alignHorizontal;
            }
            set
            {
                bool oldValue = m_alignHorizontal;
                bool newValue = value;
                if (oldValue != newValue)
                {
                    m_alignHorizontal = value;
                    ClearSplineCache();
                }
            }
        }

        private ComputeBuffer m_trianglesBuffer;
        public ComputeBuffer trianglesBuffer
        {
            get
            {
                if (m_trianglesBuffer == null)
                {
                    if (m_splineVertices == null || m_splineVertices.Length == 0)
                    {
                        ExtractVerticesAndAlphas();
                    }
                    if (m_splineVertices?.Length > 0)
                    {
                        m_trianglesBuffer = new ComputeBuffer(m_splineVertices.Length, sizeof(float) * 3);
                        m_trianglesBuffer.SetData(m_splineVertices);
                    }
                }
                return m_trianglesBuffer;
            }
        }

        private Vector3[] m_splineWorldPoints;
        private Vector3[] m_splineVertices;
        private float[] m_splineAlphas;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_spline = GetComponent<CurvySpline>();
            m_spline.OnRefresh.AddListener(OnSplineRefreshCallback);
            m_spline.OnGlobalCoordinatesChanged += OnGlobalCoordinatesChangedCallback;
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            ClearSplineCache();
            m_spline.OnRefresh.RemoveListener(OnSplineRefreshCallback);
            m_spline.OnGlobalCoordinatesChanged -= OnGlobalCoordinatesChangedCallback;
        }

        private void Reset()
        {
            m_spline = GetComponent<CurvySpline>();
            m_smoothness = 100;
            m_width = 10;
            m_alignHorizontal = true;
        }

        private void OnSplineRefreshCallback(CurvySplineEventArgs arg)
        {
            ClearSplineCache();
        }

        private void OnGlobalCoordinatesChangedCallback(CurvySpline spline)
        {
            ClearSplineCache();
        }

        public override Vector3[] GetWorldAnchors()
        {
            Vector3[] worldPoints = new Vector3[m_spline.ControlPointCount];
            int i = 0;
            foreach (CurvySplineSegment controlPoint in m_spline.ControlPointsList)
            {
                worldPoints[i] = controlPoint.transform.position;
                i += 1;
            }

            return worldPoints;
        }

        public override Vector3[] GetWorldPoints()
        {
            if (m_splineWorldPoints == null || m_splineWorldPoints.Length == 0)
            {
                ExtractWorldPoints();
            }
            Vector3[] worldPointClone = new Vector3[m_splineWorldPoints.Length];
            m_splineWorldPoints.CopyTo(worldPointClone, 0);
            return worldPointClone;
        }

        private void ExtractWorldPoints()
        {
            m_splineWorldPoints = new Vector3[m_smoothness];
            float step = 1f / (m_smoothness - 1);
            for (int i = 0; i < m_smoothness; ++i)
            {
                Vector3 wp = m_spline.Interpolate(i * step, Space.World);
                m_splineWorldPoints[i] = wp;
            }
        }

        public override void GetWorldTrianglesAndAlphas(out Vector3[] vertices, out float[] alphas)
        {
            if (m_splineVertices == null || m_splineAlphas == null ||
                m_splineVertices.Length == 0 || m_splineAlphas.Length == 0)
            {
                ExtractVerticesAndAlphas();
            }

            vertices = new Vector3[m_splineVertices.Length];
            m_splineVertices.CopyTo(vertices, 0);

            alphas = new float[m_splineAlphas.Length];
            m_splineAlphas.CopyTo(alphas, 0);
        }

        private void ExtractVerticesAndAlphas()
        {
            int segmentCount = m_smoothness - 1;
            int trisCount = segmentCount * 4;
            int vertCount = trisCount * 3;
            float step = 1f / (m_smoothness - 1);

            Vector3 c0, up0, tangent0, normal0;
            Vector3 c1, up1, tangent1, normal1;
            Vector3 l0, l1, r0, r1;

            m_splineVertices = new Vector3[vertCount];
            m_splineAlphas = new float[vertCount];

            for (int i = 0; i < segmentCount; ++i)
            {
                float t0 = i * step;
                float t1 = (i + 1) * step;

                m_spline.InterpolateAndGetTangentFast(t0, out c0, out tangent0, Space.World);
                up0 = m_spline.GetOrientationUpFast(t0, Space.World);

                m_spline.InterpolateAndGetTangentFast(t1, out c1, out tangent1, Space.World);
                up1 = m_spline.GetOrientationUpFast(t1, Space.World);

                normal0 = Vector3.Cross(tangent0, up0).normalized;
                normal1 = Vector3.Cross(tangent1, up1).normalized;

                if (m_alignHorizontal)
                {
                    normal0.y = 0;
                    normal1.y = 0;
                }

                l0 = c0 - normal0 * m_width * 0.5f;
                l1 = c1 - normal1 * m_width * 0.5f;
                r0 = c0 + normal0 * m_width * 0.5f;
                r1 = c1 + normal1 * m_width * 0.5f;

                m_splineVertices[i * 12 + 0] = c0; m_splineAlphas[i * 12 + 0] = 1;
                m_splineVertices[i * 12 + 1] = l0; m_splineAlphas[i * 12 + 1] = 0;
                m_splineVertices[i * 12 + 2] = l1; m_splineAlphas[i * 12 + 2] = 0;

                m_splineVertices[i * 12 + 3] = c0; m_splineAlphas[i * 12 + 3] = 1;
                m_splineVertices[i * 12 + 4] = l1; m_splineAlphas[i * 12 + 4] = 0;
                m_splineVertices[i * 12 + 5] = c1; m_splineAlphas[i * 12 + 5] = 1;

                m_splineVertices[i * 12 + 6] = c0; m_splineAlphas[i * 12 + 6] = 1;
                m_splineVertices[i * 12 + 7] = c1; m_splineAlphas[i * 12 + 7] = 1;
                m_splineVertices[i * 12 + 8] = r1; m_splineAlphas[i * 12 + 8] = 0;

                m_splineVertices[i * 12 + 9] = c0; m_splineAlphas[i * 12 + 9] = 1;
                m_splineVertices[i * 12 + 10] = r1; m_splineAlphas[i * 12 + 10] = 0;
                m_splineVertices[i * 12 + 11] = r0; m_splineAlphas[i * 12 + 11] = 0;
            }
        }

        private void ClearSplineCache()
        {
            m_splineWorldPoints = null;
            m_splineVertices = null;
            m_splineAlphas = null; 
            if (m_trianglesBuffer != null)
            {
                m_trianglesBuffer.Release();
                m_trianglesBuffer = null;
            }
        }
    }
}
#endif
#endif