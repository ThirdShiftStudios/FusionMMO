#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Pinwheel.VistaEditor.NavMeshUtilities
{
    public static class NavEditorUtilities
    {
        public static void GetNavAreas(out GUIContent[] names, out int[] indices)
        {
            string[] areas =
#if UNITY_6000_0_OR_NEWER
                UnityEngine.AI.NavMesh.GetAreaNames();
#else
                GameObjectUtility.GetNavMeshAreaNames();
#endif
            List<GUIContent> areaNames = new List<GUIContent>();
            List<int> areaIndices = new List<int>();

            for (int i = 0; i < areas.Length; ++i)
            {
                if (!string.IsNullOrEmpty(areas[i]))
                {
                    areaNames.Add(new GUIContent(areas[i]));
#if UNITY_6000_0_OR_NEWER
                    areaIndices.Add(UnityEngine.AI.NavMesh.GetAreaFromName(areas[i]));
#else
                    areaIndices.Add(GameObjectUtility.GetNavMeshAreaFromName(areas[i]));
#endif
                }
            }

            names = areaNames.ToArray();
            indices = areaIndices.ToArray();
        }
    }
}
#endif
