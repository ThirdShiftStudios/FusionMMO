#if VISTA
using Pinwheel.Vista.Splines;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Pinwheel.VistaEditor.Splines
{
    public static class SplineModuleEditorUtilities
    {
        public static readonly GUIContent SPLINE_ID = new GUIContent("Spline Id", "Id of the spline evaluators in the scene to extract. Spline evaluators with the same ID will be grouped together.");
        public static readonly string ADD_SPLINE_EVALUATOR_HELP =
            "Add an appropriate Spline Evaluator to the spline object.\n" +
            "Edit spline properties using the evaluator component.";
        public static readonly string WARNING_SPLINE_NOT_FOUND = "Spline Evaluator object not found. Try another ID";
        public static readonly GUIContent DRAW_SPLINE_MESH_LABEL = new GUIContent("Visualize", "Draw the spline mesh in the scene view");

        private const string DRAW_SPLINE_MESH_KEY = "vista-draw-spline-mesh-in-scene-view";
        public static bool drawSplineMeshInSceneView
        {
            get
            {
                return EditorPrefs.GetBool(DRAW_SPLINE_MESH_KEY);
            }
            set
            {
                EditorPrefs.SetBool(DRAW_SPLINE_MESH_KEY, value);
            }
        }

        public static List<string> GetAllSplinesId()
        {
            ISplineEvaluator[] splineEvaluators = SplineModuleUtilities.GetAllSplines();
            List<string> splineIds = new List<string>();
            for (int i = 0; i < splineEvaluators.Length; ++i)
            {
                string n = splineEvaluators[i].id;
                if (!splineIds.Contains(n))
                {
                    splineIds.Add(splineEvaluators[i].id);
                }

            }
            return splineIds;
        }

        public static void DrawSplineMeshToggle()
        {
            drawSplineMeshInSceneView = EditorGUILayout.Toggle(DRAW_SPLINE_MESH_LABEL, drawSplineMeshInSceneView);
        }
    }
}
#endif
