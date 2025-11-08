#if VISTA
#if DREAMTECK_SPLINES
using Dreamteck.Splines;
using Pinwheel.Vista.Splines;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.Splines
{
    [CustomEditor(typeof(DreamteckSplineEvaluator))]
    public class DreamteckSplineEvaluatorInspector : Editor
    {
        private DreamteckSplineEvaluator m_instance;

        private void OnEnable()
        {
            m_instance = target as DreamteckSplineEvaluator;
        }

        private static readonly GUIContent ID = new GUIContent("Id", "Id of this evaluator, pick a unique string, best to describe its usage. This id will be use to select the spline in the graph editor.");
        private static readonly GUIContent SPLINE = new GUIContent("Spline", "The Dreamteck's Spline Computer component");
        private static readonly GUIContent SMOOTHNESS = new GUIContent("Smoothness", "Approximate number of points to trace along the spline");
        private static readonly GUIContent WIDTH = new GUIContent("Width", "Width of the spline mesh");
        private static readonly GUIContent ALIGN_HORIZONTAL = new GUIContent("Align Horizontal", "This will keep the spline mesh from having super steep surface when it takes a turn");

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            string id = EditorGUILayout.TextField(ID, m_instance.id);

            GUI.enabled = false;
            EditorGUILayout.ObjectField(SPLINE, m_instance.spline, typeof(SplineComputer), true);
            GUI.enabled = true;

            int smoothness = EditorGUILayout.DelayedIntField(SMOOTHNESS, m_instance.smoothness);
            float width = EditorGUILayout.DelayedFloatField(WIDTH, m_instance.width);
            bool alignHorizontal = EditorGUILayout.Toggle(ALIGN_HORIZONTAL, m_instance.alignHorizontal);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_instance, $"Modify {m_instance.name}");
                m_instance.id = id;
                m_instance.smoothness = smoothness;
                m_instance.width = width;
                m_instance.alignHorizontal = alignHorizontal;
            }

            SplineModuleEditorUtilities.DrawSplineMeshToggle();
        }
    }
}
#endif
#endif