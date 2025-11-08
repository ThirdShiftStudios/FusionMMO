#if VISTA
using Pinwheel.Vista.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pinwheel.Vista.BigWorld
{
    internal static class BiomeTextureBlend
    {
        private static readonly string TEXTURE_BLEND_SHADER_NAME = "Vista/Shaders/BiomeTextureBlend";
        private static readonly int SRC_TEXTURE = Shader.PropertyToID("_SrcTexture");
        private static readonly int DEST_TEXTURE = Shader.PropertyToID("_DestTexture");
        private static readonly int BIOME_MASK_TEXTURE = Shader.PropertyToID("_BiomeMaskTexture");
        private static readonly int DEST_RESOLUTION = Shader.PropertyToID("_DestResolution");

        private static readonly string KW_SRC_IS_NULL = "SRC_IS_NULL";

        internal static void BlendSingleTexture(
            BiomeData destData, List<BiomeData> srcDatas,
            Func<BiomeData, RenderTexture> textureGetter, Action<BiomeData, RenderTexture> textureSetter,
            List<BiomeBlendOptions> blendOptions, Func<BiomeBlendOptions, BiomeBlendOptions.TextureBlendMode> blendModeGetter)
        {
            //Find the max resolution
            int res = int.MinValue;
            RenderTextureFormat format = RenderTextureFormat.RFloat;
            for (int i = srcDatas.Count - 1; i >= 0; --i)
            {
                BiomeData srcData = srcDatas[i];
                RenderTexture tex = textureGetter.Invoke(srcData);
                if (tex != null)
                {
                    res = Mathf.Max(res, tex.width);
                    format = tex.format;
                }
            }

            //all source textures is null
            if (res <= 0)
                return;

            //Blend
            ComputeShader textureBlendShader = Resources.Load<ComputeShader>(TEXTURE_BLEND_SHADER_NAME);
            RenderTexture destTex = new RenderTexture(res, res, 0, format, RenderTextureReadWrite.Linear);
            destTex.filterMode = FilterMode.Bilinear;
            destTex.wrapMode = TextureWrapMode.Clamp;
            destTex.enableRandomWrite = true;
            destTex.Create();

#if !UNITY_EDITOR            
            Drawing.Blit(Texture2D.blackTexture, destTex);
#endif

            for (int i = 0; i < srcDatas.Count; ++i)
            {
                BiomeData srcData = srcDatas[i];
                RenderTexture srcTex = textureGetter.Invoke(srcData);
                BiomeBlendOptions.TextureBlendMode blendMode = blendModeGetter.Invoke(blendOptions[i]);
                Blend(textureBlendShader, srcTex, destTex, srcData.biomeMaskMap, blendMode);
            }
            textureSetter.Invoke(destData, destTex);
            Resources.UnloadAsset(textureBlendShader);
        }

        internal static void BlendTextureWeights(BiomeData destData, List<BiomeData> srcDatas)
        {
            List<Texture> srcMasks = new List<Texture>();
            int res = int.MinValue;
            RenderTextureFormat format = RenderTextureFormat.RFloat;
            foreach (BiomeData srcData in srcDatas)
            {
                if (srcData.GetLayerCount() > 0 && srcData.biomeMaskMap != null)
                {
                    res = Mathf.Max(res, srcData.biomeMaskMap.width);
                    format = srcData.biomeMaskMap.format;
                    srcMasks.Add(srcData.biomeMaskMap);
                }
                else
                {
                    srcMasks.Add(Texture2D.blackTexture);
                }
            }

            if (res <= 0)
                return;

            ComputeShader textureBlendShader = Resources.Load<ComputeShader>(TEXTURE_BLEND_SHADER_NAME);
            res = int.MinValue;
            format = RenderTextureFormat.RFloat;
            List<TerrainLayer> srcLayers = new List<TerrainLayer>();
            List<RenderTexture> srcWeights = new List<RenderTexture>();
            List<RenderTexture> blendedMaskByLayer = new List<RenderTexture>();
            for (int iBiome = 0; iBiome < srcDatas.Count; ++iBiome)
            {
                BiomeData srcData = srcDatas[iBiome];
                res = Mathf.Max(res, GetMaxLayerWeightResolution(srcData));
                List<TerrainLayer> layers = new List<TerrainLayer>();
                List<RenderTexture> weights = new List<RenderTexture>();
                srcData.GetLayerWeights(layers, weights);
                srcLayers.AddRange(layers);
                srcWeights.AddRange(weights);

                RenderTexture blendedMask = RenderTexture.GetTemporary(res, res, 0, format, RenderTextureReadWrite.Linear);
                blendedMask.filterMode = FilterMode.Bilinear;
                blendedMask.wrapMode = TextureWrapMode.Clamp;
                blendedMask.enableRandomWrite = true;
                blendedMask.Create();
                Drawing.Blit(srcMasks[iBiome], blendedMask);

                for (int jBiome = iBiome + 1; jBiome < srcDatas.Count; ++jBiome)
                {
                    Blend(textureBlendShader, Texture2D.blackTexture, blendedMask, srcMasks[jBiome], BiomeBlendOptions.TextureBlendMode.Linear);
                }

                for (int iLayer = 0; iLayer < layers.Count; ++iLayer)
                {
                    blendedMaskByLayer.Add(blendedMask);
                }
            }
            Resources.UnloadAsset(textureBlendShader);

            if (res <= 0)
                return;

            List<RenderTexture> destWeights = new List<RenderTexture>();
            for (int i = 0; i < srcWeights.Count; ++i)
            {
                RenderTexture destWeight = new RenderTexture(res, res, 0, format, RenderTextureReadWrite.Linear);
                destWeight.filterMode = FilterMode.Bilinear;
                destWeight.wrapMode = TextureWrapMode.Clamp;
                destWeight.enableRandomWrite = true;
                destWeight.Create();
                destWeights.Add(destWeight);
            }

            WeightsBlend.Blend(destWeights.ToArray(), srcWeights.ToArray(), blendedMaskByLayer.ToArray());

            for (int i = 0; i < blendedMaskByLayer.Count; ++i)
            {
                RenderTexture.ReleaseTemporary(blendedMaskByLayer[i]);
            }

            for (int i = 0; i < srcLayers.Count; ++i)
            {
                destData.AddTextureLayer(srcLayers[i], destWeights[i]);
            }
        }

        internal static void BlendDensityMaps(
            BiomeData destData, List<BiomeData> srcDatas,
            List<BiomeBlendOptions> blendOptions)
        {
            List<DetailTemplate> srcTemplates = new List<DetailTemplate>();
            foreach (BiomeData srcData in srcDatas)
            {
                srcTemplates.AddRange(srcData.m_detailTemplates_Density);
            }
            srcTemplates = srcTemplates.Distinct().ToList();

            RenderTexture dummyBlack = RenderTexture.GetTemporary(1, 1, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);

            foreach (DetailTemplate t in srcTemplates)
            {
                DetailTemplate template = t;
                Func<BiomeData, RenderTexture> textureGetter = (b) =>
                {
                    int index = b.m_detailTemplates_Density.IndexOf(template);
                    if (index >= 0)
                    {
                        return b.m_detailDensityMaps.At(index);
                    }
                    else
                    {
                        return dummyBlack;
                    }
                };
                Action<BiomeData, RenderTexture> textureSetter = (b, rt) =>
                {
                    b.AddDetailDensity(template, rt);
                };

                BlendSingleTexture(destData, srcDatas, textureGetter, textureSetter, blendOptions, (o) => { return o.detailDensityBlendMode; });
            }

            RenderTexture.ReleaseTemporary(dummyBlack);
        }

        internal static void BlendGenericTextures(BiomeData destData, List<BiomeData> srcDatas, List<BiomeBlendOptions> blendOptions)
        {
            List<string> srcLabels = new List<string>();
            foreach (BiomeData srcData in srcDatas)
            {
                srcLabels.AddRange(srcData.m_genericTextureLabels);
            }

            RenderTexture dummyBlack = RenderTexture.GetTemporary(1, 1, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            srcLabels = srcLabels.Distinct().ToList();
            foreach (string s in srcLabels)
            {
                string label = s;
                Func<BiomeData, RenderTexture> textureGetter = (b) =>
                {
                    int index = b.m_genericTextureLabels.IndexOf(label);
                    if (index >= 0)
                    {
                        return b.m_genericTextures.At(index);
                    }
                    else
                    {
                        return dummyBlack;
                    }
                };
                Action<BiomeData, RenderTexture> textureSetter = (b, rt) =>
                {
                    b.AddGenericTexture(label, rt);
                };

                BlendSingleTexture(destData, srcDatas, textureGetter, textureSetter, blendOptions, (o) => { return BiomeBlendOptions.TextureBlendMode.Linear; });
            }
            RenderTexture.ReleaseTemporary(dummyBlack);
        }

        private static void Blend(ComputeShader blendShader, Texture src, RenderTexture dest, Texture biomeMask, BiomeBlendOptions.TextureBlendMode blendMode)
        {
            if (dest == null)
            {
                throw new ArgumentException("dest cannot be null");
            }

            int kernel = (int)blendMode;
            if (src != null)
            {
                blendShader.SetTexture(kernel, SRC_TEXTURE, src);
                blendShader.DisableKeyword(KW_SRC_IS_NULL);
            }
            else
            {
                blendShader.EnableKeyword(KW_SRC_IS_NULL);
            }
            blendShader.SetTexture(kernel, DEST_TEXTURE, dest);
            blendShader.SetTexture(kernel, BIOME_MASK_TEXTURE, biomeMask != null ? biomeMask : Texture2D.blackTexture);
            blendShader.SetVector(DEST_RESOLUTION, new Vector2(dest.width, dest.height));

            blendShader.Dispatch(kernel, (dest.width + 7) / 8, (dest.height + 7) / 8, 1);
        }

        internal static int GetMaxLayerWeightResolution(BiomeData data)
        {
            int res = int.MinValue;
            if (data.m_layerWeights == null)
                return res;
            foreach (RenderTexture rt in data.m_layerWeights)
            {
                res = Mathf.Max(res, rt.width);
            }
            return res;
        }
    }
}
#endif
