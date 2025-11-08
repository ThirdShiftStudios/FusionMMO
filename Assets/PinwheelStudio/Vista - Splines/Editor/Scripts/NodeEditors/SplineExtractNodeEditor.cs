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
    [NodeEditor(typeof(SplineExtractNode))]
    public class SplineExtractNodeEditor : ImageNodeEditorBase, INeedUpdateNodeVisual
    {
        public override void OnGUI(INode node)
        {
            SplineExtractNode n = node as SplineExtractNode;
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
        }

        public void UpdateVisual(INode node, NodeView nv)
        {
            SplineExtractNode n = node as SplineExtractNode;
            NodeMetadataAttribute meta = NodeMetadata.Get<SplineExtractNode>();
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
