#if VISTA
using UnityEngine;

namespace Pinwheel.Vista.Splines
{
    public interface ISplineEvaluator
    {
        public string id { get; }
        public Vector3[] GetWorldAnchors();
        public Vector3[] GetWorldPoints();
        public void GetWorldTrianglesAndAlphas(out Vector3[] vertices, out float[] alphas);
    }
}
#endif
