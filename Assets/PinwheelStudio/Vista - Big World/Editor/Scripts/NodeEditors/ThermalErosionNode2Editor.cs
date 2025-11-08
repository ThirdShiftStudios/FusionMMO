#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.Vista;
using Pinwheel.VistaEditor;
using Pinwheel.Vista.Graph;
using Pinwheel.VistaEditor.Graph;

namespace Pinwheel.VistaEditor.Graph
{
    [NodeEditor(typeof(ThermalErosionNode2))]
    public class ThermalErosionNode2Editor : ImageNodeEditorBase
    {
        private static readonly GUIContent GENERAL_HEADER = new GUIContent("General");
        private static readonly GUIContent ITERATION_COUNT = new GUIContent("Iteration", "The number of simulation steps to perform");
        private static readonly GUIContent ITERATION_PER_FRAME = new GUIContent("Iteration Per Frame", "The number of steps to perform in a single frame");
                
        private static readonly GUIContent HEADER_SIMULATION = new GUIContent("Simulation");
        private static readonly GUIContent EROSION_RATE = new GUIContent("Erosion", "Erosion strength multiplier, higher value erode the terrain more.");       
        private static readonly GUIContent TALUS_ANGLE = new GUIContent("Talus Angle", "The angle where soil stop sliding downhill");

        private static readonly GUIContent HEADER_OPTIMIZATION = new GUIContent("Optimization");
        private static readonly GUIContent USE_MULTI_RESOLUTION = new GUIContent("Multi Resolution", "Recommended to be turned on. Perform simulation on many canvases of different size and combined them. This makes it faster on higher resolution while preserving result similarity.");
        private static readonly GUIContent ENABLE_FILE_SAVING = new GUIContent("Save To File", "Editor only. Save simulation results to file as texture. You can load them with Load Texture node to perform other tasks without rerun the simulation.");

        public override void OnGUI(INode node)
        {
            ThermalErosionNode2 n = node as ThermalErosionNode2;
            EditorGUI.BeginChangeCheck();

            EditorCommon.Header(GENERAL_HEADER);
            int iterationCount = EditorGUILayout.DelayedIntField(ITERATION_COUNT, n.iterationCount);
            int iterationPerFrame = EditorGUILayout.DelayedIntField(ITERATION_PER_FRAME, n.iterationPerFrame);

            EditorCommon.Header(HEADER_SIMULATION);
            float erosionRate = EditorGUILayout.DelayedFloatField(EROSION_RATE, n.erosionRate);
            float talusAngle = EditorGUILayout.DelayedFloatField(TALUS_ANGLE, n.talusAngle);

            EditorCommon.Header(HEADER_OPTIMIZATION);
            bool multiResolution = EditorGUILayout.Toggle(USE_MULTI_RESOLUTION, n.useMultiResolution);
            bool enableFileSaving = EditorGUILayout.Toggle(ENABLE_FILE_SAVING, n.enableFileSaving);
            if (enableFileSaving)
            {
                EditorSettings editorSettings = EditorSettings.Get();
                EditorGUILayout.LabelField(" ", $"Editor only! Files will be saved at {editorSettings.graphEditorSettings.fileExportDirectory}", EditorCommon.Styles.grayMiniLabel);
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.iterationCount = iterationCount;
                n.iterationPerFrame = iterationPerFrame;

                n.erosionRate = erosionRate;
                n.talusAngle = talusAngle;

                n.useMultiResolution = multiResolution;
                n.enableFileSaving = enableFileSaving;
            }

        }
    }
}
#endif
