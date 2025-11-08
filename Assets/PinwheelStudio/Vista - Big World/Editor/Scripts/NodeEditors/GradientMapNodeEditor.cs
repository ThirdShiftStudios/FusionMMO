#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.VistaEditor;
using Pinwheel.Vista.Graph;
using Pinwheel.VistaEditor.Graph;

namespace Pinwheel.VistaEditor.Graph
{
    [NodeEditor(typeof(GradientMapNode))]
    public class GradientMapNodeEditor : ImageNodeEditorBase
    {
        private static readonly GUIContent GRADIENT = new GUIContent("Gradient", "The gradient to map to");
        private static readonly GUIContent WRAP_MODE = new GUIContent("Wrap Mode", "How to sample the gradient texture when loop > 1");
        private static readonly GUIContent LOOP = new GUIContent("Loop", "The number of time to repeat the gradient pattern");

        public override void OnGUI(INode node)
        {
            GradientMapNode n = node as GradientMapNode;
            EditorGUI.BeginChangeCheck();
            Gradient gradient = EditorGUILayout.GradientField(GRADIENT, n.gradient);
            TextureWrapMode wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup(WRAP_MODE, n.wrapMode);
            float loop = EditorGUILayout.FloatField(LOOP, n.loop);
            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.gradient = gradient;
                n.wrapMode = wrapMode;
                n.loop = loop;
            }
        }
    }
}
#endif
