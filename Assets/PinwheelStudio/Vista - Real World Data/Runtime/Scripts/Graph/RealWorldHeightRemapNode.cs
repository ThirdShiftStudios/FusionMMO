#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.Graph;
using Pinwheel.Vista.RealWorldData.USGS;

namespace Pinwheel.Vista.RealWorldData.Graph
{
    [NodeMetadata(
        title = "Real World Height Remap",
        path = "Adjustments/Real World Height Remap",
        description = "Remap downloaded height map to the desired range in meters. Downloaded height map usually remapped from [serviceMinHeight,serviceMaxHeight] to [0,1]. This node is used to convert that [0,1] data back to meters. If you don't use this node, height data will be remapped from [0,1] to [0,graphHeight]")]
    public class RealWorldHeightRemapNode : ImageNodeBase
    {
        public readonly MaskSlot inputSlot = new MaskSlot("Input", SlotDirection.Input, 0);
        public readonly MaskSlot outputSlot = new MaskSlot("Output", SlotDirection.Output, 100);

        [SerializeField]
        private float m_destMinHeight;
        public float destMinHeight
        {
            get
            {
                return m_destMinHeight;
            }
            set
            {
                m_destMinHeight = value;
            }
        }

        [SerializeField]
        private float m_destMaxHeight;
        public float destMaxHeight
        {
            get
            {
                return m_destMaxHeight;
            }
            set
            {
                m_destMaxHeight = value;
            }
        }

        private static readonly string SHADER_NAME = "Hidden/Vista/RealWorldData/RealWorldHeightRemap";
        private static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");
        private static readonly int REMAP_MIN_HEIGHT = Shader.PropertyToID("_RemapMinHeight");
        private static readonly int REMAP_MAX_HEIGHT = Shader.PropertyToID("_RemapMaxHeight");
        private static readonly int GRAPH_MAX_HEIGHT = Shader.PropertyToID("_GraphMaxHeight");

        public RealWorldHeightRemapNode() : base()
        {
            m_destMinHeight = USGSDataProvider.MIN_HEIGHT;
            m_destMaxHeight = USGSDataProvider.MAX_HEIGHT;
        }

        public override void ExecuteImmediate(GraphContext context)
        {
            int baseResolution = context.GetArg(Args.RESOLUTION).intValue;
            SlotRef inputRefLink = context.GetInputLink(m_id, inputSlot.id);
            Texture inputTexture = context.GetTexture(inputRefLink);
            int inputResolution;
            if (inputTexture == null)
            {
                inputTexture = Texture2D.blackTexture;
                inputResolution = baseResolution;
            }
            else
            {
                inputResolution = inputTexture.width;
            }

            int resolution = this.CalculateResolution(baseResolution, inputResolution);
            DataPool.RtDescriptor desc = DataPool.RtDescriptor.Create(resolution, resolution);
            SlotRef outputRef = new SlotRef(m_id, outputSlot.id);
            RenderTexture targetRt = context.CreateRenderTarget(desc, outputRef);

            Material mat = new Material(ShaderUtilities.Find(SHADER_NAME));
            mat.SetTexture(MAIN_TEX, inputTexture);
            mat.SetFloat(REMAP_MIN_HEIGHT, m_destMinHeight);
            mat.SetFloat(REMAP_MAX_HEIGHT, m_destMaxHeight);
            mat.SetFloat(GRAPH_MAX_HEIGHT, context.GetArg(Args.TERRAIN_HEIGHT).floatValue);

            Drawing.DrawQuad(targetRt, mat, 0);
            context.ReleaseReference(inputRefLink);
            Object.DestroyImmediate(mat);
        }
    }
}
#endif
