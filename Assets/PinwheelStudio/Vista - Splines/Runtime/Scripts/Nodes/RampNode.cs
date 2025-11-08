#if VISTA
using Pinwheel.Vista.Graph;
using Pinwheel.Vista.Graphics;
using UnityEngine;

namespace Pinwheel.Vista.Splines.Graph
{
    [NodeMetadata(
        title = "Ramp",
        path = "General/Ramp",
        icon = "",
        keywords = "road",
        documentation = "",
        description = "Conform the terrain height to a spline, useful for matching with the road mesh. Note that it doesn't generate any road mesh.")]
    public class RampNode : ImageNodeBase
    {
        public readonly MaskSlot heightInputSlot = new MaskSlot("Height", SlotDirection.Input, 0);
        public readonly MaskSlot falloffDetailSlot = new MaskSlot("Falloff Detail", SlotDirection.Input, 1);

        public readonly MaskSlot heightOutputSlot = new MaskSlot("Height", SlotDirection.Output, 100);
        public readonly MaskSlot rampMaskOutputSlot = new MaskSlot("Ramp Mask", SlotDirection.Output, 101);

        [SerializeField]
        private string m_splineId;
        public string splineId
        {
            get
            {
                return m_splineId;
            }
            set
            {
                m_splineId = value;
            }
        }

        [SerializeField]
        private AnimationCurve m_falloff;
        public AnimationCurve falloff
        {
            get
            {
                return m_falloff;
            }
            set
            {
                m_falloff = value;
            }
        }

        private static string SHADER_NAME = "Hidden/Vista/Graph/Ramp";
        private static int TERRAIN_HEIGHT_MAP = Shader.PropertyToID("_TerrainHeightMap");
        private static int SPLINE_FALLOFF_DETAIL_MAP = Shader.PropertyToID("_FalloffDetailMap");
        private static int SPLINE_FALLOFF_MASK = Shader.PropertyToID("_SplineFalloffMask");
        private static int FALLOFF_CURVE_MAP = Shader.PropertyToID("_FalloffCurveMap");
        private static int SPLINE_HEIGHT_MAP = Shader.PropertyToID("_SplineHeightMap");

        private static readonly int PASS_RAMP = 0;
        private static readonly int PASS_RAMP_MASK = 1;

        private static readonly string TEMP_VERTICES_BUFFER_NAME = "~TempVerticesBuffer";
        private static readonly string TEMP_ALPHAS_BUFFER_NAME = "~TempAlphasBuffer";
        private static readonly string TEMP_FALLOFF_MASK_NAME = "~TempFalloffMask";
        private static readonly string TEMP_SPLINE_HEIGHT_MAP_NAME = "~TempSplineHeightMap";

        private Material m_material;

        public RampNode() : base()
        {
            m_splineId = "PICK_A_SPLINE_ID";
            m_falloff = CreateDefaultFalloff();
        }

        private AnimationCurve CreateDefaultFalloff()
        {
            Keyframe[] keys = new Keyframe[]
            {
                new Keyframe(0,0),
                new Keyframe(0.5f, 1),
                new Keyframe(1,1)
            };
            return new AnimationCurve(keys);
        }

        public override void ExecuteImmediate(GraphContext context)
        {
            int baseResolution = context.GetArg(Args.RESOLUTION).intValue;
            SlotRef inputHeightRefLink = context.GetInputLink(m_id, heightInputSlot.id);
            Texture inputHeightTexture = context.GetTexture(inputHeightRefLink);
            int inputResolution;
            if (inputHeightTexture == null)
            {
                inputHeightTexture = Texture2D.blackTexture;
                inputResolution = baseResolution;
            }
            else
            {
                inputResolution = inputHeightTexture.width;
            }

            SlotRef falloffDetailRefLink = context.GetInputLink(m_id, falloffDetailSlot.id);
            Texture falloffDetailTexture = context.GetTexture(falloffDetailRefLink);
            if (falloffDetailTexture == null)
            {
                falloffDetailTexture = Texture2D.whiteTexture;
            }

            int resolution = this.CalculateResolution(baseResolution, inputResolution);
            DataPool.RtDescriptor heightOutputDesc = DataPool.RtDescriptor.Create(resolution, resolution);
            SlotRef heightOutputRef = new SlotRef(m_id, heightOutputSlot.id);
            RenderTexture heightOutputRt = context.CreateRenderTarget(heightOutputDesc, heightOutputRef);

            ISplineEvaluator[] splineEvaluators = SplineModuleUtilities.GetSplinesWithId(m_splineId);
            if (splineEvaluators.Length == 0)
            {
                Drawing.Blit(inputHeightTexture, heightOutputRt);
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

                    DataPool.RtDescriptor splineFalloffDesc = DataPool.RtDescriptor.Create(resolution, resolution);
                    RenderTexture splineFalloffMask = context.CreateTemporaryRT(splineFalloffDesc, TEMP_FALLOFF_MASK_NAME);
                    SplineExtractUtilities.RenderFalloffMask(splineFalloffMask, vertBuffer, alphaBuffer, worldVertices.Length, worldBounds);

                    float maxHeight = context.GetArg(Args.TERRAIN_HEIGHT).floatValue;
                    DataPool.RtDescriptor splineHeightMapDesc = DataPool.RtDescriptor.Create(resolution, resolution);
                    RenderTexture splineHeightMap = context.CreateTemporaryRT(splineHeightMapDesc, TEMP_SPLINE_HEIGHT_MAP_NAME);
                    SplineExtractUtilities.RenderHeightMap(splineHeightMap, vertBuffer, alphaBuffer, worldVertices.Length, worldBounds, maxHeight);

                    Texture2D falloffCurveMap = Utilities.TextureFromCurve(m_falloff);

                    m_material = new Material(Shader.Find(SHADER_NAME));
                    m_material.SetTexture(TERRAIN_HEIGHT_MAP, inputHeightTexture);
                    m_material.SetTexture(FALLOFF_CURVE_MAP, falloffCurveMap);
                    m_material.SetTexture(SPLINE_FALLOFF_DETAIL_MAP, falloffDetailTexture);
                    m_material.SetTexture(SPLINE_FALLOFF_MASK, splineFalloffMask);
                    m_material.SetTexture(SPLINE_HEIGHT_MAP, splineHeightMap);

                    SlotRef rampMaskRef = new SlotRef(m_id, rampMaskOutputSlot.id);
                    if (context.GetReferenceCount(rampMaskRef) > 0)
                    {
                        DataPool.RtDescriptor rampMaskDesc = DataPool.RtDescriptor.Create(resolution, resolution);
                        RenderTexture rampMaskRt = context.CreateRenderTarget(rampMaskDesc, rampMaskRef);

                        Drawing.DrawQuad(rampMaskRt, m_material, PASS_RAMP_MASK);
                    }

                    Drawing.DrawQuad(heightOutputRt, m_material, PASS_RAMP);
                    Object.DestroyImmediate(m_material);

                    Object.DestroyImmediate(falloffCurveMap);
                    context.ReleaseTemporary(TEMP_VERTICES_BUFFER_NAME);
                    context.ReleaseTemporary(TEMP_ALPHAS_BUFFER_NAME);
                    context.ReleaseTemporary(TEMP_FALLOFF_MASK_NAME);
                    context.ReleaseTemporary(TEMP_SPLINE_HEIGHT_MAP_NAME);
                }
            }

            context.ReleaseReference(inputHeightRefLink);
            context.ReleaseReference(falloffDetailRefLink);
        }
    }
}
#endif
