#if VISTA
#if __MICROSPLAT__
using JBooth.MicroSplat;
using Pinwheel.Vista;
using Pinwheel.Vista.MicroSplatIntegration;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.MicroSplatIntegration
{
    public static class TextureArraysUpdater
    {
        [InitializeOnLoadMethod]
        public static void OnInitialize()
        {
            MicroSplatIntegrationManager.updateTextureArrayCallback += OnUpdateTextureArray;
        }

        private static void OnUpdateTextureArray(MicroSplatIntegrationManager sender)
        {
            if (sender.vistaManager == null || sender.textureArrayConfig == null)
                return;

            List<ITile> tiles = sender.vistaManager.GetTiles();
            TerrainLayer[] layers = GetLayersFromTiles(tiles);
            UpdateTextureArrayConfig(sender.textureArrayConfig, layers);
            TextureArrayConfigEditor.CompileConfig(sender.textureArrayConfig);
        }

        private static TerrainLayer[] GetLayersFromTiles(List<ITile> tiles)
        {
            foreach (ITile t in tiles)
            {
                if (t is ILayerWeightsPopulator lwp)
                {
                    return lwp.terrainLayers;
                }
            }
            return null;
        }

        private static void UpdateTextureArrayConfig(TextureArrayConfig cfg, TerrainLayer[] layers)
        {
            cfg.sourceTextures.Clear();
            int maxTexSize = 256;
            int count = layers.Length;
            for (int i = 0; i < count; ++i)
            {
                // Metalic, AO, Height, Smooth
                TerrainLayer proto = layers[i];
                var e = new TextureArrayConfig.TextureEntry();
                if (proto != null)
                {
                    e.diffuse = proto.diffuseTexture;
                    e.normal = proto.normalMapTexture;
                    e.metal = proto.maskMapTexture;
                    e.metalChannel = TextureArrayConfig.TextureChannel.R;
                    e.height = proto.maskMapTexture;
                    e.heightChannel = TextureArrayConfig.TextureChannel.B;
                    e.smoothness = proto.maskMapTexture;
                    e.smoothnessChannel = TextureArrayConfig.TextureChannel.A;
                    e.ao = proto.maskMapTexture;
                    e.aoChannel = TextureArrayConfig.TextureChannel.G;
                }
                if (e.smoothness != null)
                {
                    cfg.allTextureChannelAO = TextureArrayConfig.AllTextureChannel.G;
                    cfg.allTextureChannelHeight = TextureArrayConfig.AllTextureChannel.B;
                    cfg.allTextureChannelSmoothness = TextureArrayConfig.AllTextureChannel.A;
                }
                cfg.sourceTextures.Add(e);
                if (proto != null && proto.diffuseTexture != null && proto.diffuseTexture.width > maxTexSize)
                {
                    maxTexSize = proto.diffuseTexture.width;
                }
            }
            SetDefaultTextureSize(cfg, maxTexSize);
        }

        private static void SetDefaultTextureSize(TextureArrayConfig cfg, int size)
        {
            if (size > 2048)
            {
                cfg.defaultTextureSettings.diffuseSettings.textureSize = TextureArrayConfig.TextureSize.k4096;
                cfg.defaultTextureSettings.normalSettings.textureSize = TextureArrayConfig.TextureSize.k4096;
            }
            else if (size > 1024)
            {
                cfg.defaultTextureSettings.diffuseSettings.textureSize = TextureArrayConfig.TextureSize.k2048;
                cfg.defaultTextureSettings.normalSettings.textureSize = TextureArrayConfig.TextureSize.k2048;
            }
            else if (size > 512)
            {
                cfg.defaultTextureSettings.diffuseSettings.textureSize = TextureArrayConfig.TextureSize.k1024;
                cfg.defaultTextureSettings.normalSettings.textureSize = TextureArrayConfig.TextureSize.k1024;
            }
            else if (size > 256)
            {
                cfg.defaultTextureSettings.diffuseSettings.textureSize = TextureArrayConfig.TextureSize.k512;
                cfg.defaultTextureSettings.normalSettings.textureSize = TextureArrayConfig.TextureSize.k512;
            }
            else
            {
                cfg.defaultTextureSettings.diffuseSettings.textureSize = TextureArrayConfig.TextureSize.k256;
                cfg.defaultTextureSettings.normalSettings.textureSize = TextureArrayConfig.TextureSize.k256;
            }
        }
    }
}
#endif
#endif