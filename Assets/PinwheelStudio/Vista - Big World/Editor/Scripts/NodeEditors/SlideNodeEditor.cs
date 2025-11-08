#if VISTA
using Pinwheel.Vista.Graph;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.Graph
{
    [NodeEditor(typeof(SlideNode))]
    public class SlideNodeEditor : ExecutableNodeEditorBase
    {
        private static readonly GUIContent ITERATION_COUNT = new GUIContent("Iteration", "Number of iteration to simulate");
        private static readonly GUIContent TRAIL_CURVATURE = new GUIContent("Trail Curvature", "Control how smooth/curvy the trails are");

        public override void OnGUI(INode node)
        {
            SlideNode n = node as SlideNode;
            EditorGUI.BeginChangeCheck();
            int iterationCount = EditorGUILayout.IntField(ITERATION_COUNT, n.iterationCount);
            float trailCurvature = EditorGUILayout.Slider(TRAIL_CURVATURE, n.trailCurvature, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.iterationCount = iterationCount;
                n.trailCurvature = trailCurvature;
            }
        }
    }
}
#endif
