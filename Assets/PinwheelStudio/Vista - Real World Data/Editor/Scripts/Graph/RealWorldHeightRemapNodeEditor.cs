#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.VistaEditor.Graph;
using Pinwheel.Vista.RealWorldData.Graph;
using UnityEditor;
using Pinwheel.Vista.Graph;
using Pinwheel.Vista.RealWorldData;

namespace Pinwheel.VistaEditor.RealWorldData.Graph
{
    [NodeEditor(typeof(RealWorldHeightRemapNode))]
    public class RealWorldHeightRemapNodeEditor : ImageNodeEditorBase
    {
        private static readonly GUIContent DATA_PROVIDER_ASSET = new GUIContent("Pick from Provider", "Drop a data provider asset here and it will read the min and max possible height value for you.");
        private static readonly GUIContent DEST_MIN_HEIGHT = new GUIContent("Dest Min Height", "The minimum height level to remap to");
        private static readonly GUIContent DEST_MAX_HEIGHT = new GUIContent("Dest Max Height", "The maximum height level to remap to");

        public override void OnGUI(INode node)
        {
            RealWorldHeightRemapNode n = node as RealWorldHeightRemapNode;
            EditorGUI.BeginChangeCheck();
            DataProviderAsset provider = EditorGUILayout.ObjectField(DATA_PROVIDER_ASSET, null, typeof(DataProviderAsset), false) as DataProviderAsset;
            if (EditorGUI.EndChangeCheck())
            {
                if (provider != null)
                {
                    m_graphEditor.RegisterUndo(n);
                    n.destMinHeight = provider.minHeight;
                    n.destMaxHeight = provider.maxHeight;
                }
            }

            EditorGUI.BeginChangeCheck();
            float destMinHeight = EditorGUILayout.FloatField(DEST_MIN_HEIGHT, n.destMinHeight);
            float destMaxHeight = EditorGUILayout.FloatField(DEST_MAX_HEIGHT, n.destMaxHeight);
            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.destMinHeight = destMinHeight;
                n.destMaxHeight = destMaxHeight;
            }

        }
    }
}
#endif
