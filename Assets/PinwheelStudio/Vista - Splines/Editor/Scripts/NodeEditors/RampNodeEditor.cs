#if VISTA
using Pinwheel.Vista.Graph;
using Pinwheel.Vista.Splines;
using Pinwheel.Vista.Splines.Graph;
using Pinwheel.VistaEditor.Graph;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.Splines.Graph
{
    [NodeEditor(typeof(RampNode))]
    public class RampNodeEditor : ImageNodeEditorBase, INeedUpdateNodeVisual
    {
        private static readonly GUIContent FALLOFF = new GUIContent("Falloff", "Define the blend factor between the current terrain height and the spline height. The mapping function is [0,1] -> [terrainHeight, splineHeight]");

        public override void OnGUI(INode node)
        {
            RampNode n = node as RampNode;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            string splineName = EditorGUILayout.DelayedTextField(SplineModuleEditorUtilities.SPLINE_ID, n.splineId);
            List<string> splineNameSelector = SplineModuleEditorUtilities.GetAllSplinesId();
            if (splineNameSelector.Count > 0)
            {
                Rect dropDownRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.popup, GUILayout.Width(20));
                if (GUI.Button(dropDownRect, "", EditorStyles.popup))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (string name in splineNameSelector)
                    {
                        menu.AddItem(
                            new GUIContent(name),
                            false,
                            () =>
                            {
                                m_graphEditor.RegisterUndo(n);
                                n.splineId = name;
                                m_graphEditor.UpdateNodesVisual();
                            });
                    }
                    menu.DropDown(dropDownRect);
                }
            }
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.splineId = splineName;
            }

            EditorGUILayout.HelpBox(SplineModuleEditorUtilities.ADD_SPLINE_EVALUATOR_HELP, MessageType.Info);

            EditorGUI.BeginChangeCheck();
            AnimationCurve falloff = EditorGUILayout.CurveField(FALLOFF, n.falloff, Color.red, new Rect(0, 0, 1, 1));

            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.falloff = falloff;
            }
        }

        public void UpdateVisual(INode node, NodeView nv)
        {
            RampNode n = node as RampNode;
            NodeMetadataAttribute meta = NodeMetadata.Get<RampNode>();
            if (string.IsNullOrEmpty(n.splineId))
            {
                nv.title = meta.title;
            }
            else
            {
                nv.title = $"{meta.title} ({n.splineId})";
            }

            ISplineEvaluator splineEvaluator = SplineModuleUtilities.GetFirstSplineWithId(n.splineId);
            if (splineEvaluator == null)
            {
                nv.AddWarning(SplineModuleEditorUtilities.WARNING_SPLINE_NOT_FOUND);
            }
        }
    }
}
#endif
