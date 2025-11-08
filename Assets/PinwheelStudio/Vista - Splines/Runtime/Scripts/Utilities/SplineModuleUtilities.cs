#if VISTA
using UnityEngine;
using System.Collections.Generic;
using Pinwheel.Vista.Graph;

namespace Pinwheel.Vista.Splines
{
    public static class SplineModuleUtilities
    {
        public delegate void CollectSplinesHandler(string id, Collector<ISplineEvaluator> splines);
        public static event CollectSplinesHandler collectSplines;

        public delegate void CollectAllSplinesHandler(Collector<ISplineEvaluator> splines);
        public static event CollectAllSplinesHandler collectAllSplines;

        public static ISplineEvaluator GetFirstSplineWithId(string id)
        {
            if (collectSplines != null)
            {
                Collector<ISplineEvaluator> splines = new Collector<ISplineEvaluator>();
                collectSplines.Invoke(id, splines);
                if (splines.Count > 0)
                {
                    return splines.At(0);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static ISplineEvaluator[] GetSplinesWithId(string id)
        {
            if (collectSplines != null)
            {
                Collector<ISplineEvaluator> splines = new Collector<ISplineEvaluator>();
                collectSplines.Invoke(id, splines);
                if (splines.Count > 0)
                {
                    return splines.ToArray();
                }
                else
                {
                    return new ISplineEvaluator[0];
                }
            }
            else
            {
                return new ISplineEvaluator[0];
            }
        }

        public static ISplineEvaluator[] GetAllSplines()
        {
            Collector<ISplineEvaluator> splines = new Collector<ISplineEvaluator>();
            collectAllSplines?.Invoke(splines);
            return splines.ToArray();
        }

        public static Vector3[] GetWorldAnchors(ISplineEvaluator[] splines)
        {
            List<Vector3> anchors = new List<Vector3>();
            foreach (ISplineEvaluator s in splines)
            {
                anchors.AddRange(s.GetWorldAnchors());
            }
            return anchors.ToArray();
        }

        public static Vector3[] GetWorldPoints(ISplineEvaluator[] splines)
        {
            List<Vector3> anchors = new List<Vector3>();
            foreach (ISplineEvaluator s in splines)
            {
                anchors.AddRange(s.GetWorldPoints());
            }
            return anchors.ToArray();
        }

        public static void GetWorldTrianglesAndAlphas(ISplineEvaluator[] splines, out Vector3[] vertices, out float[] alphas)
        {
            List<Vector3> verticesList = new List<Vector3>();
            List<float> alphasList = new List<float>();
            foreach (ISplineEvaluator s in splines)
            {
                Vector3[] v;
                float[] a;
                s.GetWorldTrianglesAndAlphas(out v, out a);
                verticesList.AddRange(v);
                alphasList.AddRange(a);
            }
            vertices = verticesList.ToArray();
            alphas = alphasList.ToArray();
        }

        public static Vector4 CalculateWorldBounds(GraphContext context)
        {
            if (context.GetArg(Args.BIOME_SPACE).intValue == (int)Space.Self)
            {
                return context.GetArg(Args.BIOME_WORLD_BOUNDS).vectorValue;
            }
            else
            {
                return context.GetArg(Args.WORLD_BOUNDS).vectorValue;
            }
        }
    }
}
#endif
