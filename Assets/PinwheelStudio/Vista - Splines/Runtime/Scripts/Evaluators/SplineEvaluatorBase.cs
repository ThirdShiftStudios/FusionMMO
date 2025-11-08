#if VISTA
using UnityEngine;
using System.Collections.Generic;

namespace Pinwheel.Vista.Splines
{
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    public abstract class SplineEvaluatorComponentBase : MonoBehaviour, ISplineEvaluator
    {
        [SerializeField]
        private string m_id;
        public string id
        {
            get
            {
                return m_id;
            }
            set
            {
                m_id = value;
            }
        }

        protected virtual void OnEnable()
        {
            SplineModuleUtilities.collectSplines += OnCollectSplines;
            SplineModuleUtilities.collectAllSplines += OnCollectAllSplines;
        }

        protected virtual void OnDisable()
        {
            SplineModuleUtilities.collectSplines -= OnCollectSplines;
            SplineModuleUtilities.collectAllSplines -= OnCollectAllSplines;
        }

        private void OnCollectSplines(string id, Collector<ISplineEvaluator> splines)
        {
            if (!string.IsNullOrEmpty(m_id))
            {
                if (string.Equals(m_id, id))
                {
                    splines.Add(this);
                }
            }
        }

        private void OnCollectAllSplines(Collector<ISplineEvaluator> splines)
        {
            if (!string.IsNullOrEmpty(m_id))
            {
                splines.Add(this);
            }
        }

        public virtual Vector3[] GetWorldAnchors()
        {
            return null;
        }

        public virtual Vector3[] GetWorldPoints()
        {
            return null;
        }

        public virtual void GetWorldTrianglesAndAlphas(out Vector3[] vertices, out float[] alphas)
        {
            vertices = null;
            alphas = null;
        }
    }
}
#endif
