#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.ExposeProperty;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pinwheel.Vista.Graph
{
    public static class OverrideGraphHandler
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        private static void OnInitialize()
        {
            LocalProceduralBiome.cloneAndOverrideGraphCallback += OnCloneAndOverrideGraph;
        }

        private static TerrainGraph OnCloneAndOverrideGraph(TerrainGraph src, IEnumerable<PropertyOverride> overrides)
        {
            return CloneAndOverrideGraph(src, overrides);
        }

        private static T CloneAndOverrideGraph<T>(T src, IEnumerable<PropertyOverride> overrides) where T : GraphAsset
        {
            T clonedGraph = Object.Instantiate(src);

            foreach (PropertyOverride po in overrides)
            {
                po.OverrideValue(clonedGraph);
            }
            return clonedGraph;
        }
    }
}
#endif
