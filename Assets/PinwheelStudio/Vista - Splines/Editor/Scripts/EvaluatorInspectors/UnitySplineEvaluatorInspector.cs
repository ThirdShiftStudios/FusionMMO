#if VISTA
#if VISTA_UNITY_SPLINE
using Pinwheel.Vista.Splines;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace Pinwheel.VistaEditor.Splines
{
    [CustomEditor(typeof(UnitySplineEvaluator))]
    public class UnitySplineEvaluatorInspector : Editor
    {
        private UnitySplineEvaluator m_instance;

        private void OnEnable()
        {
            m_instance = target as UnitySplineEvaluator;
        }

        private static readonly GUIContent ID = new GUIContent("Id", "Id of this evaluator, best to describe its usage. This id will be use to select the spline in the graph editor. Evaluators with the same ID will be grouped together in the graph.");
        private static readonly GUIContent SPLINE = new GUIContent("Spline", "The SplineContainer component");
        private static readonly GUIContent SMOOTHNESS = new GUIContent("Smoothness", "Number of points to trace along the spline");
        private static readonly GUIContent WIDTH = new GUIContent("Width", "Width of the spline mesh");
        private static readonly GUIContent ALIGN_HORIZONTAL = new GUIContent("Align Horizontal", "This will keep the spline mesh from having super steep surface when it takes a turn");

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            string id = EditorGUILayout.TextField(ID, m_instance.id);

            GUI.enabled = false;
            EditorGUILayout.ObjectField(SPLINE, m_instance.spline, typeof(SplineContainer), true);
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