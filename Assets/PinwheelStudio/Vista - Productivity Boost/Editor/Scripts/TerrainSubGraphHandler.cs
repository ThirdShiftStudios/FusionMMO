#if VISTA
using Pinwheel.Vista.Graph;
using Pinwheel.VistaEditor.Graph;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Pinwheel.VistaEditor.ProductivityBoost
{
    public static class TerrainSubGraphHandler
    {
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            TerrainGraphAdapter.configSubGraphCallback += OnConfigSubGraph;
            GraphEditorGraphView.filterDragObjectCallback += FilterDragObject;
            GraphEditorGraphView.objectDroppedCallback += DropObjects;
        }

        private static void OnConfigSubGraph(TerrainGraphAdapter adapter, SearcherProvider searcherProvider)
        {
            searcherProvider.SetSubGraphTypes(typeof(TerrainGraph), typeof(TerrainSubGraphNode));
        }

        private static void FilterDragObject(GraphEditorGraphView graphView, UnityEngine.Object[] objects, ref bool accepted)
        {
            GraphAsset srcGraph = graphView.m_editor.sourceGraph;
            for (int i = 0; i < objects.Length; ++i)
            {
                if (objects[i] is TerrainGraph t &&
                    t != srcGraph &&
                    AssetDatabase.Contains(objects[i]))
                {
                    accepted = true;
                    return;
                }
            }
        }

        private static void DropObjects(GraphEditorGraphView graphView, UnityEngine.Object[] objects, Vector2 localDropPosition)
        {
            GraphAsset srcGraph = graphView.m_editor.sourceGraph;
            List<TerrainGraph> subgraph = new List<TerrainGraph>();
            foreach (Object o in objects)
            {
                if (o is TerrainGraph t &&
                    t != srcGraph &&
                    AssetDatabase.Contains(t))
                {
                    subgraph.Add(t);
                }
            }
            if (subgraph.Count == 0)
                return;

            graphView.m_editor.RegisterUndo("Add Sub Graph");
            for (int i = 0; i < subgraph.Count; ++i)
            {
                Vector2 pos = VisualElementExtensions.ChangeCoordinatesTo(graphView, graphView.contentViewContainer, localDropPosition);
                Rect r = new Rect(pos + Vector2.one * 50 * i, Vector2.one);
                GraphEditorGraphView.AddNodeResult result = graphView.AddNodeOfType(typeof(TerrainSubGraphNode), r);
                TerrainSubGraphNode node = result.node as TerrainSubGraphNode;
                node.graph = subgraph[i];

                EditorUtility.SetDirty(graphView.m_editor.clonedGraph);
            }
        }
    }
}
#endif
