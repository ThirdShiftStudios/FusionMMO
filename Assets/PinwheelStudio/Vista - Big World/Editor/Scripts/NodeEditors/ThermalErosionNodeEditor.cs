#if VISTA
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.Graph
{
    [NodeEditor(typeof(ThermalErosionNode))]
    public class ThermalErosionNodeEditor : ImageNodeEditorBase
    {
        private static readonly GUIContent GENERAL_HEADER = new GUIContent("General");       
        private static readonly GUIContent HIGH_QUALITY = new GUIContent("High Quality", "If true, it will simulate soil transport in 8 directions, otherwise 4");
        private static readonly GUIContent DETAIL_LEVEL = new GUIContent("Detail Level", "Smaller value runs faster and produces larger features, while larger value is more expensive but produces more micro details");
        private static readonly GUIContent ITERATION_COUNT = new GUIContent("Iteration", "The number of simulation step to perform");
        private static readonly GUIContent ITERATION_PER_FRAME = new GUIContent("Iteration Per Frame", "The number of step to perform in a single frame");

        private static readonly GUIContent SIMULATION_HEADER = new GUIContent("Simulation");
        private static readonly GUIContent EROSION_RATE = new GUIContent("Erosion Rate", "Strength of the erosion, higher value causes more soil to slide down the slope");
        private static readonly GUIContent EROSION_MULTIPLIER = new GUIContent(" ", "Overall multiplier. Change the erosion strength without modifying its base value");

        private static readonly GUIContent RESTING_ANGLE = new GUIContent("Resting Angle", "The angle in degree where soil stop sliding");
        private static readonly GUIContent RESTING_ANGLE_MULTIPLIER = new GUIContent(" ", "Overall multiplier. Change the resting angle without modifying its base value");

        private static readonly GUIContent ARTISTIC_HEADER = new GUIContent("Artistic Controls");
        private static readonly GUIContent HEIGHT_SCALE = new GUIContent("Height Scale", "A multiplier to terrain height to further enhance the erosion effect");
        private static readonly GUIContent DETAIL_HEIGHT_SCALE = new GUIContent("Detail Height Scale", "A multiplier to the detail height map to randomize the water flow and create more eroded features on the very flat areas");
        private static readonly GUIContent EROSION_BOOST = new GUIContent("Erosion Boost", "A multiplier to enhance the erosion effect");
        private static readonly GUIContent DEPOSITION_BOOST = new GUIContent("Deposition Boost", "A multiplier to enhance the deposition effect");

        public override void OnGUI(INode node)
        {
            ThermalErosionNode n = node as ThermalErosionNode;

            EditorGUI.BeginChangeCheck();
            EditorCommon.Header(GENERAL_HEADER);
            bool highQuality = EditorGUILayout.Toggle(HIGH_QUALITY, n.highQualityMode);
            float detailLevel = EditorGUILayout.Slider(DETAIL_LEVEL, n.detailLevel, 0f, 1f);
            int iterationCount = EditorGUILayout.IntField(ITERATION_COUNT, n.iterationCount);
            int iterationPerFrame = EditorGUILayout.IntField(ITERATION_PER_FRAME, n.iterationPerFrame);

            EditorCommon.Header(SIMULATION_HEADER);
            float erosionRate = EditorGUILayout.FloatField(EROSION_RATE, n.erosionRate);
            float erosionMultiplier = EditorGUILayout.Slider(EROSION_MULTIPLIER, n.erosionMultiplier, 0f, 2f);

            float restingAngle = EditorGUILayout.FloatField(RESTING_ANGLE, n.restingAngle);
            float restingAngleMultiplier = EditorGUILayout.Slider(RESTING_ANGLE_MULTIPLIER, n.restingAngleMultiplier, 0f, 2f);

            EditorCommon.Header(ARTISTIC_HEADER);
            float heightScale = EditorGUILayout.FloatField(HEIGHT_SCALE, n.heightScale);
            float erosionBoost = EditorGUILayout.FloatField(EROSION_BOOST, n.erosionBoost);
            float depositionBoost = EditorGUILayout.FloatField(DEPOSITION_BOOST, n.depositionBoost);

            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.highQualityMode = highQuality;
                n.detailLevel = detailLevel;
                n.iterationCount = iterationCount;
                n.iterationPerFrame = iterationPerFrame;

                n.erosionRate = erosionRate;
                n.erosionMultiplier = erosionMultiplier;

                n.restingAngle = restingAngle;
                n.restingAngleMultiplier = restingAngleMultiplier;

                n.heightScale = heightScale;
                n.erosionBoost = erosionBoost;
                n.depositionBoost = depositionBoost;
            }
        }
    }
}
#endif
