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
    [NodeEditor(typeof(ThinOutAlongNode))]
    public class ThinOutAlongNodeEditor : ExecutableNodeEditorBase, INeedUpdateNodeVisual
    {
        private static readonly GUIContent FALLOFF = new GUIContent("Falloff", "Define the shape of the path mask where the curve will be mirrored using the spline center curve");
        private static readonly GUIContent INVERT_MASK = new GUIContent("Invert", "Perform invert on the spline mask. If on, the instances inside the spline path will be remove. If off, the instances outside the spline path will be removed.");
        private static readonly GUIContent INPUT_COUNT = new GUIContent("Input Count", "The number of input position buffer to perform the thin out action");

        public override void OnGUI(INode node)
        {
            ThinOutAlongNode n = node as ThinOutAlongNode;
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
            bool invert = EditorGUILayout.Toggle(INVERT_MASK, n.invertMask);
            int inputCount = EditorGUILayout.DelayedIntField(INPUT_COUNT, n.inputCount);
            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.falloff = falloff;
                n.invertMask = invert;
                n.inputCount = inputCount;
            }
        }

        public void UpdateVisual(INode node, NodeView nv)
        {
            ThinOutAlongNode n = node as ThinOutAlongNode;
            NodeMetadataAttribute meta = NodeMetadata.Get<ThinOutAlongNode>();
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
