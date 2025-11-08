#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.Vista.NavMeshUtilities;

namespace Pinwheel.VistaEditor.NavMeshUtilities
{
    [CustomEditor(typeof(NavAreaMeshGenerator))]
    public class NavAreaMeshGeneratorInspector : Editor
    {
        private NavAreaMeshGenerator m_instance;

        private void OnEnable()
        {
            m_instance = target as NavAreaMeshGenerator;
        }

        private static readonly GUIContent OUTPUT_NAMES = new GUIContent("Output Names", "Name of the area mask outputs, defined in your terrain graph Output Nodes");
        private static readonly GUIContent NAV_AREA = new GUIContent("Nav Area", "The navmesh area to generate meshes for");

        private static readonly GUIContent AUTO_MESH_OPTIONS = new GUIContent("Auto Mesh Options", "Use the suggested mesh generation options");
        private static readonly GUIContent MESH_MIN_SUBDIV = new GUIContent("Min Subdiv", "The minimum subdiv level of the mesh, deciding polygon density at plain area");
        private static readonly GUIContent MESH_MAX_SUBDIV = new GUIContent("Max Subdiv", "The maximum subdiv level of the mesh, deciding polygon density at rough area");
        private static readonly GUIContent MESH_GRID_SIZE = new GUIContent("Chunk Grid Size", "This will split the mesh into a grid of several smaller mesh, to get rid of the 65k vertices limit and better navmesh result");

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            GUIContent[] areaNames;
            int[] areaIndices;
            NavEditorUtilities.GetNavAreas(out areaNames, out areaIndices);
            int areaIndex = EditorGUILayout.IntPopup(NAV_AREA, m_instance.navAreaIndex, areaNames, areaIndices);
            bool autoMeshOptions = EditorGUILayout.Toggle(AUTO_MESH_OPTIONS, m_instance.useAutoMeshOptions);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_instance, $"Modify {m_instance.name}");
                m_instance.navAreaIndex = areaIndex;
                m_instance.useAutoMeshOptions = autoMeshOptions;
                EditorUtility.SetDirty(m_instance);
            }

            if (!autoMeshOptions)
            {
                EditorGUI.indentLevel += 1;
                EditorGUI.BeginChangeCheck();
                int minSubdiv = EditorGUILayout.DelayedIntField(MESH_MIN_SUBDIV, m_instance.meshMinSubdiv);
                int maxSubdiv = EditorGUILayout.DelayedIntField(MESH_MAX_SUBDIV, m_instance.meshMaxSubdiv);
                int gridSize = EditorGUILayout.DelayedIntField(MESH_GRID_SIZE, m_instance.meshChunkGridSize);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_instance, $"Modify {m_instance.name}");
                    m_instance.meshMinSubdiv = minSubdiv;
                    m_instance.meshMaxSubdiv = maxSubdiv;
                    m_instance.meshChunkGridSize = gridSize;
                    EditorUtility.SetDirty(m_instance);
                }
                EditorGUI.indentLevel -= 1;
            }

            EditorGUI.BeginChangeCheck();
            SerializedProperty outputNames = serializedObject.FindProperty("m_outputNames");
            EditorGUILayout.PropertyField(outputNames, OUTPUT_NAMES);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(m_instance);
            }
        }
    }
}
#endif
