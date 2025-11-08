#if VISTA
using Pinwheel.Vista.Graph;
using Pinwheel.Vista.Graphics;
using UnityEngine;

namespace Pinwheel.Vista.Splines.Graph
{
    public static class NodeLibraryUtilities
    {
        public static class PathNode
        {
            private static string SHADER_NAME = "Hidden/Vista/Graph/Path";
            private static int SPLINE_FALLOFF_DETAIL_MAP = Shader.PropertyToID("_FalloffDetailMap");
            private static int SPLINE_FALLOFF_MASK = Shader.PropertyToID("_SplineFalloffMask");
            private static int FALLOFF_CURVE_MAP = Shader.PropertyToID("_FalloffCurveMap");

            private static int PASS = 0;

            private static readonly string TEMP_VERTICES_BUFFER_NAME = "~TempVerticesBuffer";
            private static readonly string TEMP_ALPHAS_BUFFER_NAME = "~TempAlphasBuffer";
            private static readonly string TEMP_FALLOFF_MASK_NAME = "~TempFalloffMask";

            private static Material s_material;

            public static void Execute(GraphContext context, string splineId, AnimationCurve falloffCurve, Texture inputFalloffDetailTexture, RenderTexture outputRt)
            {
                ISplineEvaluator[] splineEvaluators = SplineModuleUtilities.GetSplinesWithId(splineId);
                if (splineEvaluators.Length==0)
                {
                    Drawing.Blit(Texture2D.blackTexture, outputRt);
                }
                else
                {
                    Vector3[] worldVertices;
                    float[] alphas;
                    SplineModuleUtilities.GetWorldTrianglesAndAlphas(splineEvaluators, out worldVertices, out alphas);

                    if (worldVertices.Length > 0)
                    {
                        Vector4 worldBounds = SplineModuleUtilities.CalculateWorldBounds(context);

                        DataPool.BufferDescriptor vertBufferDesc = DataPool.BufferDescriptor.Create(worldVertices.Length * 3);
                        ComputeBuffer vertBuffer = context.CreateTemporaryBuffer(vertBufferDesc, TEMP_VERTICES_BUFFER_NAME, false);
                        vertBuffer.SetData(worldVertices);

                        DataPool.BufferDescriptor alphaBufferDesc = DataPool.BufferDescriptor.Create(worldVertices.Length * 1);
                        ComputeBuffer alphaBuffer = context.CreateTemporaryBuffer(alphaBufferDesc, TEMP_ALPHAS_BUFFER_NAME, false);
                        alphaBuffer.SetData(alphas);

                        DataPool.RtDescriptor splineFalloffDesc = DataPool.RtDescriptor.Create(outputRt.width, outputRt.height);
                        RenderTexture splineFalloffMask = context.CreateTemporaryRT(splineFalloffDesc, TEMP_FALLOFF_MASK_NAME);
                        SplineExtractUtilities.RenderFalloffMask(splineFalloffMask, vertBuffer, alphaBuffer, worldVertices.Length, worldBounds);

                        Texture2D falloffCurveMap = Utilities.TextureFromCurve(falloffCurve);

                        s_material = new Material(Shader.Find(SHADER_NAME));
                        s_material.SetTexture(FALLOFF_CURVE_MAP, falloffCurveMap);
                        s_material.SetTexture(SPLINE_FALLOFF_DETAIL_MAP, inputFalloffDetailTexture);
                        s_material.SetTexture(SPLINE_FALLOFF_MASK, splineFalloffMask);

                        Drawing.DrawQuad(outputRt, s_material, PASS);

                        Object.DestroyImmediate(falloffCurveMap);
                        context.ReleaseTemporary(TEMP_VERTICES_BUFFER_NAME);
                        context.ReleaseTemporary(TEMP_ALPHAS_BUFFER_NAME);
                        context.ReleaseTemporary(TEMP_FALLOFF_MASK_NAME);
                        Object.DestroyImmediate(s_material);
                    }
                }
            }
        }
    }
}
#endif
