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
    [NodeEditor(typeof(ErosionNode))]
    public class ErosionNodeEditor : ImageNodeEditorBase
    {
        private static readonly GUIContent GENERAL_HEADER = new GUIContent("General");
        private static readonly GUIContent ITERATION_COUNT = new GUIContent("Iteration", "The number of simulation steps to perform");
        private static readonly GUIContent ITERATION_PER_FRAME = new GUIContent("Iteration Per Frame", "The number of steps to perform in a single frame");

        private static readonly GUIContent HYDRAULIC_EROSION_HEADER = new GUIContent("Hydraulic Erosion");
        private static readonly GUIContent RAIN_RATE = new GUIContent("Rain", "The rain amount, higher value erodes more. This value should be adjusted in conjunction with Evaporation. If Evaporation is too low, lower/sunken/hollow part of the terrain will less likely to be eroded because water movement is not active there, think of a lake.");
        private static readonly GUIContent RAIN_OVER_TIME = new GUIContent("Rain Over Time", "Multiplier for the rain amount during simulation. Creative use of this curve can create interesting eroded patterns.");
        private static readonly GUIContent SEDIMENT_CAPACITY = new GUIContent("Sediment Capacity", "How much sediment the water can pickup & transport downhill. Higher value create deeper trenches.");
        private static readonly GUIContent EROSION_RATE = new GUIContent("Erosion", "Erosion strength multiplier, higher value erode the terrain more.");
        private static readonly GUIContent DEPOSITION_RATE = new GUIContent("Deposition", "Deposition strength multiplier, lower value makes sediment to be transported further from the source.");
        private static readonly GUIContent EVAPORATION_RATE = new GUIContent("Evaporation", "How fast the water evaporated/removed from the simulation. Low value makes lower parts of the terrain less eroded, higher value produces minor eroded trails.");

        private static readonly GUIContent HEADER_THERMAL_EROSION = new GUIContent("Thermal Erosion");
        private static readonly GUIContent TALUS_ANGLE = new GUIContent("Talus Angle", "The angle where soil stop sliding downhill");
        private static readonly GUIContent THERMAL_EROSION_PROPORTION = new GUIContent("Proportion", "How many hydraulic erosion pass to be performed before doing 1 thermal erosion pass");

        private static readonly GUIContent HEADER_OPTIMIZATION = new GUIContent("Optimization");
        private static readonly GUIContent USE_MULTI_RESOLUTION = new GUIContent("Multi Resolution", "Recommended to be turned on. Perform simulation on many canvases of different size and combined them. This makes it faster on higher resolution while preserving result similarity.");
        private static readonly GUIContent ENABLE_FILE_SAVING = new GUIContent("Save To File", "Editor only. Save simulation results to file as texture. You can load them with Load Texture node to perform other tasks without rerun the simulation.");

        public override void OnGUI(INode node)
        {
            ErosionNode n = node as ErosionNode;
            EditorGUI.BeginChangeCheck();

            EditorCommon.Header(GENERAL_HEADER);
            int iterationCount = EditorGUILayout.DelayedIntField(ITERATION_COUNT, n.iterationCount);
            int iterationPerFrame = EditorGUILayout.DelayedIntField(ITERATION_PER_FRAME, n.iterationPerFrame);

            EditorCommon.Header(HYDRAULIC_EROSION_HEADER);
            float rainRate = EditorGUILayout.DelayedFloatField(RAIN_RATE, n.rainRate);
            AnimationCurve rainOverTime = EditorGUILayout.CurveField(RAIN_OVER_TIME, n.rainOverTime, Color.cyan, new Rect(0, 0, 1, 1));
            float sedimentCapacity = EditorGUILayout.DelayedFloatField(SEDIMENT_CAPACITY, n.sedimentCapacity);
            float erosionRate = EditorGUILayout.DelayedFloatField(EROSION_RATE, n.erosionRate);
            float depositionRate = EditorGUILayout.DelayedFloatField(DEPOSITION_RATE, n.depositionRate);
            float evaporationRate = EditorGUILayout.DelayedFloatField(EVAPORATION_RATE, n.evaporationRate);

            EditorCommon.Header(HEADER_THERMAL_EROSION);
            float talusAngle = EditorGUILayout.DelayedFloatField(TALUS_ANGLE, n.talusAngle);
            int thermalErosionProportion = EditorGUILayout.DelayedIntField(THERMAL_EROSION_PROPORTION, n.thermalErosionProportion);
            EditorGUILayout.LabelField(" ", $"1 TE after {thermalErosionProportion} HE", EditorCommon.Styles.grayMiniLabel);

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

                n.rainRate = rainRate;
                n.rainOverTime = rainOverTime;
                n.sedimentCapacity = sedimentCapacity;
                n.erosionRate = erosionRate;
                n.depositionRate = depositionRate;
                n.evaporationRate = evaporationRate;

                n.talusAngle = talusAngle;
                n.thermalErosionProportion = thermalErosionProportion;

                n.useMultiResolution = multiResolution;
                n.enableFileSaving = enableFileSaving;
            }

        }
    }
}
#endif
