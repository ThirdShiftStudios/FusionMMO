#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.Graph;

namespace Pinwheel.Vista.RealWorldData
{
    [System.Serializable]
    public class RWBInputProvider : IExternalInputProvider, ITerrainGraphArgumentsFiller
    {
        private RealWorldBiome m_biome;
        private RealWorldBiome biome
        {
            get
            {
                if (m_biome == null)
                {
                    foreach (RealWorldBiome b in RealWorldBiome.allInstances)
                    {
                        if (b.m_guid == m_biomeInstanceGuid)
                        {
                            m_biome = b;
                            break;
                        }
                    }
                }
                return m_biome;
            }
        }

        [SerializeField]
        private string m_biomeInstanceGuid;
        [SerializeField]
        public Vector2Int heightMapSize;
        [SerializeField]
        public float[] heightMapData;
        [SerializeField]
        public Vector2Int colorMapSize;
        [SerializeField]
        public Color32[] colorMapData;

        private List<GraphRenderTexture> m_textures;
        private List<GraphBuffer> m_buffers;

        public RWBInputProvider(RealWorldBiome b)
        {
            m_biome = b;
            m_biomeInstanceGuid = b.m_guid;
        }

        public void SetInput(GraphInputContainer inputContainer)
        {
            m_textures = new List<GraphRenderTexture>();
            m_buffers = new List<GraphBuffer>();

            GraphRenderTexture biomeMask = new GraphRenderTexture(1, 1, RenderTextureFormat.RFloat);
            biomeMask.identifier = Pinwheel.Vista.Graph.GraphConstants.BIOME_MASK_INPUT_NAME;
            Drawing.Blit(Texture2D.whiteTexture, biomeMask);
            m_textures.Add(biomeMask);

            if (Utilities.IsTextureDataValid(heightMapData, heightMapSize))
            {
                Texture2D rwHeightMap2D = new Texture2D(heightMapSize.x, heightMapSize.y, TextureFormat.RFloat, false, true);
                rwHeightMap2D.SetPixelData(heightMapData, 0, 0);
                rwHeightMap2D.Apply();

                GraphRenderTexture heightRT = new RenderTexture(rwHeightMap2D.width, rwHeightMap2D.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                heightRT.identifier = Pinwheel.Vista.RealWorldData.Graph.GraphConstants.REAL_WORLD_HEIGHT_INPUT_NAME;
                Drawing.Blit(rwHeightMap2D, heightRT);
                Object.DestroyImmediate(rwHeightMap2D);

                m_textures.Add(heightRT);
            }

            if (Utilities.IsTextureDataValid(colorMapData, colorMapSize))
            {
                Texture2D rwColorMap2D = new Texture2D(colorMapSize.x, colorMapSize.y, TextureFormat.RGBA32, false, false);
                rwColorMap2D.SetPixelData(colorMapData, 0, 0);
                rwColorMap2D.Apply();

                GraphRenderTexture colorRT = new RenderTexture(rwColorMap2D.width, rwColorMap2D.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                colorRT.identifier = Pinwheel.Vista.RealWorldData.Graph.GraphConstants.REAL_WORLD_COLOR_INPUT_NAME;
                Drawing.Blit(rwColorMap2D, colorRT);
                Object.DestroyImmediate(rwColorMap2D);

                m_textures.Add(colorRT);
            }

            //TODO: Add more input to m_textures and m_buffers here
            //...

            //Copy to inputContainer
            foreach (GraphRenderTexture rt in m_textures)
            {
                inputContainer.AddTexture(rt.identifier, rt.renderTexture);
            }
            foreach (GraphBuffer b in m_buffers)
            {
                inputContainer.AddBuffer(b.identifier, b.buffer);
            }
        }

        public void CleanUp()
        {
            foreach (GraphRenderTexture rt in m_textures)
            {
                if (rt != null)
                {
                    rt.Dispose();
                }
            }
            m_textures = null;

            foreach (GraphBuffer b in m_buffers)
            {
                if (b != null)
                {
                    b.Dispose();
                }
            }
            m_buffers = null;
        }

        public GraphRenderTexture RemoveTexture(string identifier)
        {
            GraphRenderTexture rt = m_textures.Find(t => t.identifier.Equals(identifier));
            if (rt != null)
            {
                m_textures.Remove(rt);
                return rt;
            }
            else
            {
                return null;
            }
        }

        public void FillTerrainGraphArguments(TerrainGraph graph, IDictionary<int, Args> args)
        {
        }
    }
}
#endif
