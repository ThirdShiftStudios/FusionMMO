#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEditor;
using Pinwheel.Vista.NavMeshUtilities;
using UnityEditor.SceneManagement;

namespace Pinwheel.VistaEditor.NavMeshUtilities
{
    public static class EditorMenus
    {
        [MenuItem("GameObject/3D Object/Vista/Nav Area Generator")]
        private static NavAreaMeshGenerator CreateNavAreaMeshGenerator(MenuCommand cmd)
        {
            GameObject go = new GameObject("Nav Area Generator");
            NavAreaMeshGenerator generator = go.AddComponent<NavAreaMeshGenerator>();
            generator.outputNames = new string[]
            {
                "Put Output Name from Graph Output Node here"
            };

            if (cmd.context != null && cmd.context is GameObject root)
            {
                go.transform.parent = root.transform;
            }

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
            return generator;
        }
    }
}
#endif
