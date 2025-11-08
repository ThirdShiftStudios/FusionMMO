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
        title = "HSL Selector",
        path = "Masking/HSL Selector",
        description = "Generate a mask based on an input color image. Pixels that match HSL ranges will return white, otherwise black. Useful when you want to mask out terrain features by color (eg. green->tree)")]
    public class HSLSelectorNode : ImageNodeBase
    {
        public readonly ColorTextureSlot inputSlot = new ColorTextureSlot("Input", SlotDirection.Input, 0);
        public readonly MaskSlot outputSlot = new MaskSlot("Output", SlotDirection.Output, 100);

        [SerializeField]
        private float m_minHue;
        public float minHue
        {
            get
            {
                return m_minHue;
            }
            set
            {
                m_minHue = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float m_maxHue;
        public float maxHue
        {
            get
            {
                return m_maxHue;
            }
            set
            {
                m_maxHue = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float m_minSaturation;
        public float minSaturation
        {
            get
            {
                return m_minSaturation;
            }
            set
            {
                m_minSaturation = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float m_maxSaturation;
        public float maxSaturation
        {
            get
            {
                return m_maxSaturation;
            }
            set
            {
                m_maxSaturation = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float m_minLightness;
        public float minLightness
        {
            get
            {
                return m_minLightness;
            }
            set
            {
                m_minLightness = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float m_maxLightness;
        public float maxLightness
        {
            get
            {
                return m_maxLightness;
            }
            set
            {
                m_maxLightness = Mathf.Clamp01(value);
            }
        }

        private static readonly string SHADER = "Hidden/Vista/RealWorldData/Graph/HSLSelector";
        private static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");
        private static readonly int MIN_H = Shader.PropertyToID("_MinH");
        private static readonly int MAX_H = Shader.PropertyToID("_MaxH");
        private static readonly int MIN_S = Shader.PropertyToID("_MinS");
        private static readonly int MAX_S = Shader.PropertyToID("_MaxS");
        private static readonly int MIN_L = Shader.PropertyToID("_MinL");
        private static readonly int MAX_L = Shader.PropertyToID("_MaxL");
        private static readonly int PASS = 0;

        private Material m_material;

        public HSLSelectorNode() : base()
        {
            m_minHue = 0.225f;
            m_maxHue = 0.45f;
            m_minSaturation = 0.25f;
            m_maxSaturation = 1f;
            m_minLightness = 0.25f;
            m_maxLightness = 1f;
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
            DataPool.RtDescriptor desc = DataPool.RtDescriptor.Create(resolution, resolution, RenderTextureFormat.RFloat);
            SlotRef outputRef = new SlotRef(m_id, outputSlot.id);
            RenderTexture targetRt = context.CreateRenderTarget(desc, outputRef);

            m_material = new Material(ShaderUtilities.Find(SHADER));
            m_material.SetTexture(MAIN_TEX, inputTexture);
            m_material.SetFloat(MIN_H, m_minHue);
            m_material.SetFloat(MAX_H, m_maxHue);
            m_material.SetFloat(MIN_S, m_minSaturation);
            m_material.SetFloat(MAX_S, m_maxSaturation);
            m_material.SetFloat(MIN_L, m_minLightness);
            m_material.SetFloat(MAX_L, m_maxLightness);

            Drawing.DrawQuad(targetRt, m_material, PASS);
            context.ReleaseReference(inputRefLink);
            Object.DestroyImmediate(m_material);
        }
    }
}
#endif
