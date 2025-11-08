#if VISTA
#if GRIFFIN_2021 || GRIFFIN_3
using Pinwheel.Griffin.SplineTool;
using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Vista.Splines
{
    [RequireComponent(typeof(GSplineCreator))]
    public class PolarisSplineEvaluator : SplineEvaluatorComponentBase, ITrianglesBufferProvider
    {
        [SerializeField]
        private GSplineCreator m_spline;
        public GSplineCreator spline
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

#if GRIFFIN_2021
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
#endif

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
            m_spline = GetComponent<GSplineCreator>();
            GSplineCreator.Editor_SplineChanged += OnSplineChanged;
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            GSplineCreator.Editor_SplineChanged -= OnSplineChanged;
            ClearSplineCache();
        }

        private void Reset()
        {
            m_spline = GetComponent<GSplineCreator>();
            m_smoothness = 10;
            m_width = 10;
#if GRIFFIN_2021
            m_alignHorizontal = true;
#endif
        }

        private void OnSplineChanged(GSplineCreator changedSpline)
        {
            if (changedSpline == this.m_spline)
            {
                ClearSplineCache();
            }
        }

        public override Vector3[] GetWorldAnchors()
        {
            Vector3[] worldPoints = new Vector3[m_spline.Spline.Anchors.Count];
            int i = 0;
            foreach (GSplineAnchor anchor in m_spline.Spline.Anchors)
            {
                Vector3 wp = transform.TransformPoint(anchor.Position);
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
            Collector<Vector3> worldPoints = new Collector<Vector3>();
            for (int sIndex = 0; sIndex < m_spline.Spline.Segments.Count; ++sIndex)
            {
                float tStep = 1f / (m_smoothness - 1);
                for (int tIndex = 0; tIndex < m_smoothness - 1; ++tIndex)
                {
                    float t = tIndex * tStep;
                    Vector3 lp = m_spline.Spline.EvaluatePosition(sIndex, t);
                    Vector3 wp = transform.TransformPoint(lp);
                    worldPoints.Add(wp);
                }
            }
            m_splineWorldPoints = worldPoints.ToArray();
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
#if GRIFFIN_2021
            List<Vector3> vertices = new List<Vector3>();
            List<float> alphas = new List<float>();
            float tStep = 1f / (m_smoothness - 1);
            Vector3 l0, l1, r0, r1, c0, c1;
            Vector3 normal0, normal1;

            for (int sIndex = 0; sIndex < m_spline.Spline.Segments.Count; ++sIndex)
            {
                for (int tIndex = 0; tIndex < m_smoothness - 1; ++tIndex)
                {
                    float t0 = tIndex * tStep;
                    float t1 = (tIndex + 1) * tStep;

                    c0 = transform.TransformPoint(m_spline.Spline.EvaluatePosition(sIndex, t0));
                    c1 = transform.TransformPoint(m_spline.Spline.EvaluatePosition(sIndex, t1));

                    Matrix4x4 matrix0 = m_spline.Spline.TRS(sIndex, t0);
                    Matrix4x4 matrix1 = m_spline.Spline.TRS(sIndex, t1);

                    normal0 = transform.TransformVector(matrix0.MultiplyVector(Vector3.right));
                    normal1 = transform.TransformVector(matrix1.MultiplyVector(Vector3.right));

                    if (m_alignHorizontal)
                    {
                        normal0.y = 0;
                        normal1.y = 0;
                    }

                    l0 = c0 - normal0 * m_width * 0.5f;
                    l1 = c1 - normal1 * m_width * 0.5f;
                    r0 = c0 + normal0 * m_width * 0.5f;
                    r1 = c1 + normal1 * m_width * 0.5f;

                    vertices.Add(c0); alphas.Add(1);
                    vertices.Add(l0); alphas.Add(0);
                    vertices.Add(l1); alphas.Add(0);

                    vertices.Add(c0); alphas.Add(1);
                    vertices.Add(l1); alphas.Add(0);
                    vertices.Add(c1); alphas.Add(1);

                    vertices.Add(c0); alphas.Add(1);
                    vertices.Add(c1); alphas.Add(1);
                    vertices.Add(r1); alphas.Add(0);

                    vertices.Add(c0); alphas.Add(1);
                    vertices.Add(r1); alphas.Add(0);
                    vertices.Add(r0); alphas.Add(0);
                }
            }
            m_splineVertices = vertices.ToArray();
            m_splineAlphas = alphas.ToArray();
#elif GRIFFIN_3
            int vertexCount = spline.GetVerticesCount(smoothness, 0, width);
            bool invalidVerticesArray = m_splineVertices == null || m_splineVertices.Length != vertexCount;
            bool invalidAlphaArray = m_splineAlphas == null || m_splineAlphas.Length != vertexCount;
            if (invalidVerticesArray || invalidAlphaArray)
            {
                m_splineVertices = new Vector3[vertexCount];
                m_splineAlphas = new float[vertexCount];
            }

            spline.GenerateWorldVerticesAndAlphas(m_splineVertices, m_splineAlphas, smoothness, 0, width);
#endif
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
