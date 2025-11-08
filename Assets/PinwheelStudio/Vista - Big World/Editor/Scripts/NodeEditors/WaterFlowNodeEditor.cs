#if VISTA
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.Graph
{
    [NodeEditor(typeof(WaterFlowNode))]
    public class WaterFlowNodeEditor : ImageNodeEditorBase
    {
        private static readonly GUIContent WATER_SOURCE_AMOUNT = new GUIContent("Water Source", "The amount of water pour into the system in each iteration");
        private static readonly GUIContent WATER_SOURCE_MULTIPLIER = new GUIContent(" ", "Overall multiplier. Change the water source amount without modifying its base value");

        private static readonly GUIContent RAIN_RATE = new GUIContent("Rain Rate", "Rain probability");
        private static readonly GUIContent RAIN_MULTIPLIER = new GUIContent(" ", "Overall multiplier. Change the rain amount without modifying its base value");

        private static readonly GUIContent FLOW_RATE = new GUIContent("Flow Rate", "Water flow speed. Default value is fine, too high may cause numerical error");
        private static readonly GUIContent FLOW_MULTIPLIER = new GUIContent(" ", "Overall multiplier. Change the flow speed without modifying its base value");

        private static readonly GUIContent EVAPORATION_RATE = new GUIContent("Evaporation Rate", "Strength of the evaporation that remove water from the system");
        private static readonly GUIContent EVAPORATION_MULTIPLIER = new GUIContent(" ", "Overall multiplier. Change the evaporation strength without modifying its base value");

        private static readonly GUIContent HIGH_QUALITY = new GUIContent("High Quality", "If true, it will simulate water flow in 8 directions, otherwise 4");
        private static readonly GUIContent DETAIL_LEVEL = new GUIContent("Detail Level", "Smaller value runs faster and produces larger features, while larger value is more expensive but produces more micro details");
        private static readonly GUIContent ITERATION_COUNT = new GUIContent("Iteration", "The number of simulation step to perform");
        private static readonly GUIContent ITERATION_PER_FRAME = new GUIContent("Iteration Per Frame", "The number of step to perform in a single frame");

        public override void OnGUI(INode node)
        {
            WaterFlowNode n = node as WaterFlowNode;
            EditorGUI.BeginChangeCheck();
            float waterSourceAmount = EditorGUILayout.FloatField(WATER_SOURCE_AMOUNT, n.waterSourceAmount);
            float waterSourceMultiplier = EditorGUILayout.Slider(WATER_SOURCE_MULTIPLIER, n.waterSourceMultiplier, 0f, 2f);

            float rainRate = EditorGUILayout.FloatField(RAIN_RATE, n.rainRate);
            float rainMultiplier = EditorGUILayout.Slider(RAIN_MULTIPLIER, n.rainMultiplier, 0f, 2f);

            float flowRate = EditorGUILayout.FloatField(FLOW_RATE, n.flowRate);
            float flowMultiplier = EditorGUILayout.Slider(FLOW_MULTIPLIER, n.flowMultiplier, 0f, 2f);

            float evaporationRate = EditorGUILayout.FloatField(EVAPORATION_RATE, n.evaporationRate);
            float evaporationMultiplier = EditorGUILayout.Slider(EVAPORATION_MULTIPLIER, n.evaporationMultiplier, 0f, 2f);

            bool highQuality = EditorGUILayout.Toggle(HIGH_QUALITY, n.highQualityMode);
            float detailLevel = EditorGUILayout.Slider(DETAIL_LEVEL, n.detailLevel, 0f, 1f);
            int iterationCount = EditorGUILayout.IntField(ITERATION_COUNT, n.iterationCount);
            int iterationPerFrame = EditorGUILayout.IntField(ITERATION_PER_FRAME, n.iterationPerFrame);

            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.waterSourceAmount = waterSourceAmount;
                n.waterSourceMultiplier = waterSourceMultiplier;

                n.rainRate = rainRate;
                n.rainMultiplier = rainMultiplier;

                n.flowRate = flowRate;
                n.flowMultiplier = flowMultiplier;

                n.evaporationRate = evaporationRate;
                n.evaporationMultiplier = evaporationMultiplier;

                n.highQualityMode = highQuality;
                n.detailLevel = detailLevel;
                n.iterationCount = iterationCount;
                n.iterationPerFrame = iterationPerFrame;
            }
        }
    }
}
#endif
