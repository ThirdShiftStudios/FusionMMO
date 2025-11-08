#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEditor;
using UnityEditor.SceneManagement;
using Pinwheel.Vista.RealWorldData;

namespace Pinwheel.VistaEditor.RealWorldData
{
    public static class EditorMenus
    {
        [MenuItem("GameObject/3D Object/Vista/Real World Biome")]
        public static void CreateRealWorldBiome(MenuCommand cmd)
        {
            VistaManager manager = null;
            if (cmd.context != null && cmd.context is GameObject root)
            {
                manager = root.GetComponentInParent<VistaManager>();
            }
            RealWorldBiome biome = RealWorldBiome.CreateInstanceInScene(manager);
            Selection.activeObject = biome;
            EditorSceneManager.MarkSceneDirty(biome.gameObject.scene);
        } 

        [MenuItem("GameObject/3D Object/Vista/Real World Biome", true)]
        public static bool ValidateCreateRealWorldBiome()
        {
            VistaManager manager = null;
            if (Selection.activeGameObject != null)
            {
                manager = Selection.activeGameObject.GetComponentInParent<VistaManager>();
            }
            return manager != null;
        }
    }
}
#endif
