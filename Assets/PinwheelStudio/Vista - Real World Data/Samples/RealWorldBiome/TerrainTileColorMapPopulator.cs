#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.UnityTerrain;

namespace Pinwheel.Vista.RealWorldData.Sample
{
    [ExecuteInEditMode]
    public class TerrainTileColorMapPopulator : MonoBehaviour
    {
        private List<RenderTexture> m_colorMapList = new List<RenderTexture>();

        public string COLOR_MAP_OUTPUT_NAME = "Terrain Color Map";
        public Material terrainMaterial;

        private void OnEnable()
        {
            VistaManager.genericTexturesPopulated += VistaManager_genericTexturesPopulated;
            m_colorMapList = new List<RenderTexture>();
        }

        private void OnDisable()
        {
            if (m_colorMapList != null)
            {
                foreach (RenderTexture rt in m_colorMapList)
                {
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }
            VistaManager.genericTexturesPopulated -= VistaManager_genericTexturesPopulated;
        }

        private void VistaManager_genericTexturesPopulated(VistaManager sender, ITile tile, List<string> labels, List<RenderTexture> textures)
        {
            if (!(tile is TerrainTile))
                return;

            int index = labels.FindIndex(s => string.Equals(s, COLOR_MAP_OUTPUT_NAME));
            if (index < 0)
                return;

            RenderTexture rt = textures[index];
            RenderTexture colorMapRt = new RenderTexture(rt);
            m_colorMapList.Add(colorMapRt);
            Drawing.Blit(rt, colorMapRt);

            Material mat = Instantiate(terrainMaterial);
            mat.mainTexture = colorMapRt;

            TerrainTile terrainTile = tile as TerrainTile;
            terrainTile.terrain.materialTemplate = mat;
        }
    }
}
#endif
