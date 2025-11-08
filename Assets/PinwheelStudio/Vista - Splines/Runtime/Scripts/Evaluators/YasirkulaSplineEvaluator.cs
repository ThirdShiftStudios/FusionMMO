#if VISTA
#if VISTA_YASIRKULA_SPLINE
using BezierSolution;
using UnityEngine;

namespace Pinwheel.Vista.Splines
{
    [RequireComponent(typeof(BezierSpline))]
    public class YasirkulaSplineEvaluator : SplineEvaluatorComponentBase, ITrianglesBufferProvider
    {
        [SerializeField]
        private BezierSpline m_spline;
        public BezierSpline spline
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
            m_spline = GetComponent<BezierSpline>();
            m_spline.onSplineChanged += OnSplineChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearSplineCache();
            m_spline.onSplineChanged -= OnSplineChanged;
        }

        private void OnSplineChanged(BezierSpline spline, DirtyFlags dirtyFlags)
        {
            ClearSplineCache();
        }

        private void Reset()
        {
            m_spline = GetComponent<BezierSpline>();
            m_smoothness = 100;
            m_width = 10;
            m_alignHorizontal = true;
        }

        public override Vector3[] GetWorldAnchors()
        {
            Vector3[] worldPoints = new Vector3[m_spline.Count];
            int i = 0;
            foreach (BezierPoint point in m_spline)
            {
                Vector3 wp = point.transform.position;
                worldPoints[i] = wp;
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
                Vector3 wp = m_spline.GetPoint(i * step);
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

            Vector3 c0, normal0;
            Vector3 c1, normal1;
            Vector3 l0, l1, r0, r1;

            m_splineVertices = new Vector3[vertCount];
            m_splineAlphas = new float[vertCount];

            for (int i = 0; i < segmentCount; ++i)
            {
                float t0 = i * step;
                float t1 = (i + 1) * step;

                c0 = m_spline.GetPoint(t0);
                c1 = m_spline.GetPoint(t1);

                normal0 = m_spline.GetNormal(t0);
                normal1 = m_spline.GetNormal(t1);

                if (m_alignHorizontal)
                {
                    normal0.y = 0;
                    normal0 = normal0.normalized;
                    normal1.y = 0;
                    normal1 = normal1.normalized;
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