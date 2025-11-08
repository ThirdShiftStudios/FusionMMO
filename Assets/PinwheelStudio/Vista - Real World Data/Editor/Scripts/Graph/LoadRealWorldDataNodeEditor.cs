#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.VistaEditor.Graph;
using Pinwheel.Vista.RealWorldData;
using Pinwheel.Vista.RealWorldData.Graph;
using Pinwheel.Vista.Graph;
using UnityEditor;

namespace Pinwheel.VistaEditor.RealWorldData.Graph
{
    [NodeEditor(typeof(LoadRealWorldDataNode))]
    public class LoadRealWorldDataNodeEditor : ImageNodeEditorBase, INeedUpdateNodeVisual
    {
        private static readonly GUIContent DATA_PROVIDER_ASSET = new GUIContent("Data Provider Asset", "The provider asset containing world data. Right click on Project window, then Create>Vista>Data Provider to create one.");
        private static readonly GUIContent SRC_MIN_HEIGHT = new GUIContent("Source Min Height", "The minimum height level that the height dataset can contain, this value usually comes from the provider");
        private static readonly GUIContent SRC_MAX_HEIGHT = new GUIContent("Source Max Height", "The maximum height level that the height dataset can contain, this value usually comes from the provider");

        public override void OnGUI(INode node)
        {
            LoadRealWorldDataNode n = node as LoadRealWorldDataNode;
            EditorGUI.BeginChangeCheck();
            DataProviderAsset asset = (DataProviderAsset)EditorGUILayout.ObjectField(DATA_PROVIDER_ASSET, n.dataProviderAsset, typeof(DataProviderAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.dataProviderAsset = asset;
            }

            if (asset != null)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.LabelField(SRC_MIN_HEIGHT, new GUIContent(asset.minHeight.ToString()));
                EditorGUILayout.LabelField(SRC_MAX_HEIGHT, new GUIContent(asset.maxHeight.ToString()));
                EditorGUI.indentLevel -= 1;                
            }

        }

        public void UpdateVisual(INode node, NodeView nv)
        {
            LoadRealWorldDataNode n = node as LoadRealWorldDataNode;
            NodeMetadataAttribute meta = NodeMetadata.Get<LoadRealWorldDataNode>();
            if (n.dataProviderAsset == null)
            {
                nv.title = meta.title;
            }
            else
            {
                nv.title = $"{meta.title} ({n.dataProviderAsset.name})";
            }
        }
    }
}
#endif
