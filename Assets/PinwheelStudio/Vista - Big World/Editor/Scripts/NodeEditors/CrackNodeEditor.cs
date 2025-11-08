#if VISTA
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.Graph
{
    [NodeEditor(typeof(CrackNode))]
    public class CrackNodeEditor : ImageNodeEditorBase
    {
        private static readonly GUIContent SMOOTHNESS = new GUIContent("Smoothness", "Lower value creates jaggy cracks, higher value creates smoother one");
        private static readonly GUIContent WIDTH = new GUIContent("Width", "Width of the cracks");
        private static readonly GUIContent LENGTH = new GUIContent("Length", "Length of the cracks");
        private static readonly GUIContent DEPTH = new GUIContent("Depth", "Depth of the cracks");
        private static readonly GUIContent ANGLE_LIMIT = new GUIContent("Angle Limit", "The surface angle limit");
        private static readonly GUIContent ITERATION_COUNT = new GUIContent("Iteration Count", "The number of iteration to run the simulation");

        public override void OnGUI(INode node)
        {
            CrackNode n = node as CrackNode;
            EditorGUI.BeginChangeCheck();
            float smoothness = EditorGUILayout.Slider(SMOOTHNESS, n.smoothness, 0f, 1f);
            float width = EditorGUILayout.FloatField(WIDTH, n.width);
            float length = EditorGUILayout.FloatField(LENGTH, n.length);
            float depth = EditorGUILayout.FloatField(DEPTH, n.depth);
            float angleLimit = EditorGUILayout.Slider(ANGLE_LIMIT, n.angleLimit, 0f, 90f);
            int iterationCount = EditorGUILayout.IntField(ITERATION_COUNT, n.iterationCount);
            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.smoothness = smoothness;
                n.width = width;
                n.length = length;
                n.depth = depth;
                n.angleLimit = angleLimit;
                n.iterationCount = iterationCount;
            }
        }
    }
}
#endif
