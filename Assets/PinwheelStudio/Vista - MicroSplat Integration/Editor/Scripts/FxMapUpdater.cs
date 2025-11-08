#if VISTA
#if __MICROSPLAT__
using JBooth.MicroSplat;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.MicroSplatIntegration;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.MicroSplatIntegration
{
    public static class FxMapUpdater
    {
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            MicroSplatIntegrationManager.updateFxMapCallback += OnUpdateFxMap;
            MicroSplatIntegrationManager.finishingUpCallback += OnFinishingUp;
        }

        private static void OnUpdateFxMap(MicroSplatIntegrationManager msim, ITile tile, List<string> labels, List<RenderTexture> textures)
        {
            if (msim.updateFxMapsAfterGenerating == false)
            {
                return;
            }

            MicroSplatObject mso = tile.gameObject.GetComponent<MicroSplatObject>();
            if (mso == null)
            {
                return;
            }
#if __MICROSPLAT_STREAMS__
            UpdateFxTexture(msim, tile, labels, textures, mso, GraphConstants.STREAM_DATA_OUTPUT_NAME, "stream_data", (m, t) => { m.streamTexture = t; });
#endif
#if __MICROSPLAT_SNOW__
            UpdateFxTexture(msim, tile, labels, textures, mso, GraphConstants.SNOW_MASK_OUTPUT_NAME, "snowmask", (m, t) => { m.snowMaskOverride = t; });
#endif
#if __MICROSPLAT_SCATTER__
            UpdateFxTexture(msim, tile, labels, textures, mso, GraphConstants.SCATTER_OUTPUT_NAME, "scatter", (m, t) => { m.scatterMapOverride = t; });
#endif
#if __MICROSPLAT_PROCTEX__
            UpdateFxTexture(msim, tile, labels, textures, mso, GraphConstants.CAVITY_OUTPUT_NAME, "cavity", (m, t) => { m.cavityMap = t; });
#endif
#if __MICROSPLAT_GLOBALTEXTURE__
            UpdateFxTexture(msim, tile, labels, textures, mso, GraphConstants.GLOBAL_TINT_OUTPUT_NAME, "tint", (m, t) => { m.tintMapOverride = t; });
            UpdateFxTexture(msim, tile, labels, textures, mso, GraphConstants.GLOBAL_SAOM_OUTPUT_NAME, "saom", (m, t) => { m.globalSAOMOverride = t; });
            UpdateFxTexture(msim, tile, labels, textures, mso, GraphConstants.GLOBAL_EMIS_OUTPUT_NAME, "emis", (m, t) => { m.globalEmisOverride = t; });
#endif
        }

        private static void UpdateFxTexture(MicroSplatIntegrationManager msim, ITile tile, List<string> labels, List<RenderTexture> textures, MicroSplatObject mso, string textureLabel, string assetNameSuffix, System.Action<MicroSplatObject, Texture2D> textureSetter)
        {
            RenderTexture tex = null;
            for (int i = 0; i < labels.Count; ++i)
            {
                if (string.Equals(labels[i], textureLabel))
                {
                    tex = textures[i];
                    break;
                }
            }

            if (tex != null)
            {
                RenderTexture rtToRead;
                if (tex.format == RenderTextureFormat.ARGB32)
                {
                    rtToRead = tex;
                }
                else
                {
                    RenderTexture clonedRt = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                    Drawing.Blit(tex, clonedRt);
                    rtToRead = clonedRt;
                }

                Texture2D tex2D = new Texture2D(rtToRead.width, rtToRead.height, TextureFormat.ARGB32, false, true);
                tex2D.name = $"{tile.gameObject.name}_{assetNameSuffix}";

                RenderTexture.active = rtToRead;
                tex2D.ReadPixels(new Rect(0, 0, rtToRead.width, rtToRead.height), 0, 0);
                tex2D.Apply();
                RenderTexture.active = null;

                if (rtToRead != tex)
                {
                    rtToRead.Release();
                    Object.DestroyImmediate(rtToRead);
                }

                if (!Directory.Exists(msim.fxMapDirectory))
                {
                    Directory.CreateDirectory(msim.fxMapDirectory);
                }

                string filePath = Path.Combine(msim.fxMapDirectory, tex2D.name + ".tga");
                byte[] imageData = tex2D.EncodeToTGA();
                File.WriteAllBytes(filePath, imageData);
                Object.DestroyImmediate(tex2D);

                AssetDatabase.ImportAsset(filePath);
                Texture2D tex2dAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
                textureSetter.Invoke(mso, tex2dAsset);
            }
        }

        private static void OnFinishingUp(MicroSplatIntegrationManager sender)
        {
            if (sender.updateFxMapsAfterGenerating == false)
            {
                return;
            }

            MicroSplatObject.SyncAll();
        }
    }
}
#endif
#endif