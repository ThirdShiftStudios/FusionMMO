#if VISTA
using Pinwheel.Vista.Graph;
using Pinwheel.Vista.Graphics;
using UnityEngine;

namespace Pinwheel.Vista.HandPainting.Graph
{
    [NodeMetadata(
        title = "Paint Mask",
        path = "General/Paint Mask",
        icon = "",
        documentation = "",
        keywords = "",
        description = "Perform painting on a 2D canvas with grayscale value [0-1]")]
    public class PaintMaskNode : ExecutableNodeBase, ISerializationCallbackReceiver
    {
        public readonly MaskSlot outputSlot = new MaskSlot("Output", SlotDirection.Output, 100);

        [SerializeField]
        private int m_resolution;
        [NonExposable]
        public int resolution
        {
            get
            {
                return m_resolution;
            }
            set
            {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                int oldValue = m_resolution;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                int newValue = Mathf.Clamp(Utilities.MultipleOf8(value), Constants.RES_MIN, Constants.K1K24);
                m_resolution = newValue;
            }
        }

        [SerializeField]
        private float m_brushRadius;
        [NonExposable]
        public float brushRadius
        {
            get
            {
                return m_brushRadius;
            }
            set
            {
                m_brushRadius = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float m_brushFalloff;
        [NonExposable]
        public float brushFalloff
        {
            get
            {
                return m_brushFalloff;
            }
            set
            {
                m_brushFalloff = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float m_brushOpacity;
        [NonExposable]
        public float brushOpacity
        {
            get
            {
                return m_brushOpacity;
            }
            set
            {
                m_brushOpacity = Mathf.Clamp01(value);
            }
        }

        [System.NonSerialized]
        private Texture2D m_texture;
        public Texture2D texture
        {
            get
            {
                if (m_texture == null)
                {
                    m_texture = new Texture2D(m_resolution, m_resolution, TextureFormat.RFloat, false, true);
                    m_texture.wrapMode = TextureWrapMode.Clamp;
                    m_texture.filterMode = FilterMode.Bilinear;

                    if (m_textureData != null && m_textureData.Length == m_resolution * m_resolution)
                    {
                        Color[] colors = new Color[m_textureData.Length];
                        for (int i = 0; i < m_textureData.Length; ++i)
                        {
                            colors[i] = new Color(m_textureData[i], 0, 0, 0);
                        }

                        m_texture.SetPixels(colors);
                        m_texture.Apply();
                    }
                    else
                    {
                        int length = m_resolution * m_resolution;
                        Color[] colors = new Color[length];
                        for (int i = 0; i < length; ++i)
                        {
                            colors[i] = Color.black;
                        }

                        m_texture.SetPixels(colors);
                        m_texture.Apply();
                    }
                }
                return m_texture;
            }
        }

        [SerializeField]
        private float[] m_textureData;

        public PaintMaskNode() : base()
        {
            m_resolution = 128;
            m_brushRadius = 0.5f;
            m_brushFalloff = 0.5f;
            m_brushOpacity = 0.5f;
        }

        public override void ExecuteImmediate(GraphContext context)
        {
            DataPool.RtDescriptor desc = DataPool.RtDescriptor.Create(m_resolution, m_resolution);
            SlotRef outputRef = new SlotRef(m_id, outputSlot.id);
            RenderTexture targetRt = context.CreateRenderTarget(desc, outputRef);
            Drawing.Blit(texture, targetRt);
        }

        public void OnBeforeSerialize()
        {
            if (m_texture != null)
            {
                Color[] colors = m_texture.GetPixels();
                m_textureData = new float[colors.Length];
                for (int i = 0; i < colors.Length; ++i)
                {
                    m_textureData[i] = colors[i].r;
                }

                Object.DestroyImmediate(m_texture);
            }
        }

        public void OnAfterDeserialize()
        {

        }
    }
}
#endif
