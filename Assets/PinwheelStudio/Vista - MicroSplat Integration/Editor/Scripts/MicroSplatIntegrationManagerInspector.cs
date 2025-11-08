#if VISTA
#if __MICROSPLAT__
using JBooth.MicroSplat;
using Pinwheel.Vista;
using Pinwheel.Vista.MicroSplatIntegration;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.MicroSplatIntegration
{
    [CustomEditor(typeof(MicroSplatIntegrationManager))]
    public class MicroSplatIntegrationManagerInspector : Editor
    {
        private MicroSplatIntegrationManager m_instance;

        private void OnEnable()
        {
            m_instance = target as MicroSplatIntegrationManager;
        }

        public override void OnInspectorGUI()
        {
            DrawGeneralGUI();
            DrawTextureArrayConfigGUI();
            DrawTerrainFxGUI();
        }

        private class GeneralGUI
        {
            public static readonly string ID = "pinwheel.vistaeditor.msintegration.manager.general";
            public static readonly GUIContent LABEL = new GUIContent("General");

            public static readonly GUIContent VISTA_MANAGER = new GUIContent("Vista Manager", "The Vista Manager instance associates with this object");
            public static readonly string CULL_BIOMES_WARNING = "Cull Biomes option on the VM should be turned off";
        }

        private void DrawGeneralGUI()
        {
            if (EditorCommon.BeginFoldout(GeneralGUI.ID, GeneralGUI.LABEL, null, true))
            {
                EditorGUILayout.ObjectField(GeneralGUI.VISTA_MANAGER, m_instance.vistaManager, typeof(VistaManager), true);
                if (m_instance.vistaManager != null && m_instance.vistaManager.shouldCullBiomes)
                {
                    EditorCommon.DrawWarning(GeneralGUI.CULL_BIOMES_WARNING, true); 
                }
            }
            EditorCommon.EndFoldout();
        }

        private class TextureArrayGUI
        {
            public static readonly string ID = "pinwheel.vistaeditor.msintegration.manager.texturearray";
            public static readonly GUIContent LABEL = new GUIContent("Texture Array");

            public static readonly GUIContent TEXTURE_ARRAY_CONFIGS = new GUIContent("Texture Array Configs", "The Texture Array Configs asset that was created by MicroSplat");
            public static readonly GUIContent UPDATE_AFTER_GENERATION = new GUIContent("Update After Generation", "Should it update the texture array assets after the terrain generation done? You only need to check this in case you are going to make some change to terrain layers, otherwise turn it off will be fine.");
        }

        private void DrawTextureArrayConfigGUI()
        {
            if (EditorCommon.BeginFoldout(TextureArrayGUI.ID, TextureArrayGUI.LABEL, null, true))
            {
                EditorGUI.BeginChangeCheck();
                TextureArrayConfig taConfig = EditorGUILayout.ObjectField(TextureArrayGUI.TEXTURE_ARRAY_CONFIGS, m_instance.textureArrayConfig, typeof(TextureArrayConfig), false) as TextureArrayConfig;
                bool updateTextureArrays = EditorGUILayout.Toggle(TextureArrayGUI.UPDATE_AFTER_GENERATION, m_instance.updateTextureArraysAfterGenerating);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_instance, "Change Texture Array Configs settings");
                    m_instance.textureArrayConfig = taConfig;
                    m_instance.updateTextureArraysAfterGenerating = updateTextureArrays;
                }
            }
            EditorCommon.EndFoldout();
        }

        private class TerrainFxGUI
        {
            public static readonly string ID = "pinwheel.vistaeditor.msintegration.manager.terrainfx";
            public static readonly GUIContent LABEL = new GUIContent("Terrain FX");

            public static readonly GUIContent FX_MAP_DIRECTORY = new GUIContent("FX Map Directory", "The folder to store all generated FX maps.");
            public static readonly GUIContent UPDATE_AFTER_GENERATION = new GUIContent("Update After Generation", "Should it update the terrain FX maps after the terrain generation done? You only need to check this in case you are going to make some change to the terrain fx such as wetness, puddles, streams, lava, otherwise turn it off so it will not do the asset reimport tasks.");
        }

        private void DrawTerrainFxGUI()
        {
            if (EditorCommon.BeginFoldout(TerrainFxGUI.ID, TerrainFxGUI.LABEL, null, true))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                string directory = EditorGUILayout.DelayedTextField(TerrainFxGUI.FX_MAP_DIRECTORY, m_instance.fxMapDirectory);
                if (GUILayout.Button("...", GUILayout.Width(25)))
                {
                    directory = EditorUtility.OpenFolderPanel("Select Folder", "Assets/", "");
                }
                EditorGUILayout.EndHorizontal();
                bool updateAfterGenerating = EditorGUILayout.Toggle(TerrainFxGUI.UPDATE_AFTER_GENERATION, m_instance.updateFxMapsAfterGenerating);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_instance, "Change Terrain FX settings");

                    if (!string.IsNullOrEmpty(directory))
                    {
                        string relativePath = null;
                        if (!directory.StartsWith("Assets/"))
                        {
                            relativePath = FileUtil.GetProjectRelativePath(directory);
                        }
                        else
                        {
                            relativePath = directory;
                        }
                        if (string.IsNullOrEmpty(relativePath))
                        {
                            relativePath = "Assets/";
                        }
                        m_instance.fxMapDirectory = relativePath;
                    }
                    m_instance.updateFxMapsAfterGenerating = updateAfterGenerating;
                }
            }
            EditorCommon.EndFoldout();
        }
    }
}
#endif
#endif