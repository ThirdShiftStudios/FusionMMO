#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.RealWorldData;
using UnityEditor;

namespace Pinwheel.VistaEditor.RealWorldData
{
    [InitializeOnLoad]
    public static class GlobalSceneGUIDrawer
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            SceneView.duringSceneGui += DuringSceneGui;
        }

        private static void DuringSceneGui(SceneView sv)
        {
            DrawBiomeWorkingIcon(sv);
        }

        private static void DrawBiomeWorkingIcon(SceneView sv)
        {
            foreach (RealWorldBiome biome in RealWorldBiome.allInstances)
            {
                if (biome.currentlyProcessingTileBounds != null)
                {
                    Bounds b = biome.currentlyProcessingTileBounds.Value;
                    Handles.color = Color.yellow;
                    Handles.DrawWireCube(b.center, b.size);
                    Handles.Label(b.center, WaitIconProvider.GetTexture());
                }
            }
        }
    }
}
#endif
