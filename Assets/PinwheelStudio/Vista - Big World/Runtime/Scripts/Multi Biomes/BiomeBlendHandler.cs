#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pinwheel.Vista.BigWorld
{
    public static class BiomeBlendHandler
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        private static void OnInitialize()
        {
            VistaManager.blendBiomeDataCallback += Blend;
        }

        public static BiomeData Blend(List<BiomeData> srcDatas, List<BiomeBlendOptions> blendOptions)
        {
            BiomeData data = new BiomeData();
            BiomeTextureBlend.BlendSingleTexture(data, srcDatas, (b) => { return b.heightMap; }, (b, t) => { b.heightMap = t; }, blendOptions, (o) => { return o.heightMapBlendMode; });
            BiomeTextureBlend.BlendSingleTexture(data, srcDatas, (b) => { return b.holeMap; }, (b, t) => { b.holeMap = t; }, blendOptions, (o) => { return BiomeBlendOptions.TextureBlendMode.Linear; });
            BiomeTextureBlend.BlendSingleTexture(data, srcDatas, (b) => { return b.meshDensityMap; }, (b, t) => { b.meshDensityMap = t; }, blendOptions, (o) => { return BiomeBlendOptions.TextureBlendMode.Linear; });
            BiomeTextureBlend.BlendSingleTexture(data, srcDatas, (b) => { return b.albedoMap; }, (b, t) => { b.albedoMap = t; }, blendOptions, (o) => { return BiomeBlendOptions.TextureBlendMode.Linear; });
            BiomeTextureBlend.BlendSingleTexture(data, srcDatas, (b) => { return b.metallicMap; }, (b, t) => { b.metallicMap = t; }, blendOptions, (o) => { return BiomeBlendOptions.TextureBlendMode.Linear; });
            BiomeTextureBlend.BlendTextureWeights(data, srcDatas);
            BiomeTextureBlend.BlendDensityMaps(data, srcDatas, blendOptions);
            BiomeTextureBlend.BlendGenericTextures(data, srcDatas, blendOptions);

            BiomeBufferBlend.BlendTreeBuffer(data, srcDatas, blendOptions);
            BiomeBufferBlend.BlendDetailInstanceBuffer(data, srcDatas, blendOptions);
            BiomeBufferBlend.BlendObjectBuffer(data, srcDatas, blendOptions);
            BiomeBufferBlend.BlendGenericBuffer(data, srcDatas, blendOptions);

            return data;
        }
    }
}
#endif
