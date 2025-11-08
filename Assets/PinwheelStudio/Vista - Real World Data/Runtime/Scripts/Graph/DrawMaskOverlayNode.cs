#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.Graph;

namespace Pinwheel.Vista.RealWorldData.Graph
{
    [NodeMetadata(
        title = "Draw Mask Overlay",
        path = "General/Draw Mask Overlay",
        description = "Utility node for overlaying a mask onto a color texture. Useful for visualizing mask extracted from color map using HSL Selector")]
    public class DrawMaskOverlayNode : ImageNodeBase
    {
        public readonly ColorTextureSlot colorMapSlot = new ColorTextureSlot("Color Map", SlotDirection.Input, 0);
        public readonly MaskSlot maskSlot = new MaskSlot("Mask", SlotDirection.Input, 1);

        public readonly ColorTextureSlot outputSlot = new ColorTextureSlot("Output", SlotDirection.Output, 100);

        private static readonly string SHADER_NAME = "Hidden/Vista/RealWorldData/Graph/DrawMaskOverlay";
        private static readonly int COLOR_MAP = Shader.PropertyToID("_ColorMap");
        private static readonly int MASK_MAP = Shader.PropertyToID("_MaskMap");
        private static readonly int PASS = 0;

        public DrawMaskOverlayNode() : base()
        {

        }

        public override void ExecuteImmediate(GraphContext context)
        {
            int baseResolution = context.GetArg(Args.RESOLUTION).intValue;
            SlotRef colorMapRefLink = context.GetInputLink(m_id, colorMapSlot.id);
            Texture colorMapTexture = context.GetTexture(colorMapRefLink);
            int inputResolution;
            if (colorMapTexture == null)
            {
                colorMapTexture = Texture2D.blackTexture;
                inputResolution = baseResolution;
            }
            else
            {
                inputResolution = colorMapTexture.width;
            }

            SlotRef maskRefLink = context.GetInputLink(m_id, maskSlot.id);
            Texture maskTexture = context.GetTexture(maskRefLink);
            if (maskTexture == null)
            {
                maskTexture = Texture2D.blackTexture;
            }

            int resolution = this.CalculateResolution(baseResolution, inputResolution);
            DataPool.RtDescriptor desc = DataPool.RtDescriptor.Create(resolution, resolution, RenderTextureFormat.ARGB32);
            SlotRef outputRef = new SlotRef(m_id, outputSlot.id);
            RenderTexture targetRt = context.CreateRenderTarget(desc, outputRef);

            Material mat = new Material(ShaderUtilities.Find(SHADER_NAME));
            mat.SetTexture(COLOR_MAP, colorMapTexture);
            mat.SetTexture(MASK_MAP, maskTexture);

            Drawing.DrawQuad(targetRt, mat, PASS);
            context.ReleaseReference(colorMapRefLink);
            context.ReleaseReference(maskRefLink);
            Object.DestroyImmediate(mat);
        }
    }
}
#endif
