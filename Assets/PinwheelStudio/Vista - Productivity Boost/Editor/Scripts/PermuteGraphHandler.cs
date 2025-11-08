#if VISTA
using Pinwheel.Vista.Graph;
using Pinwheel.VistaEditor.Graph;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.ProductivityBoost
{
    public static class PermuteGraphHandler
    {
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            GraphEditorGraphView.addUtilityButtonCallback += OnAddUtilityButton;
        }

        private static void OnAddUtilityButton(GraphEditorGraphView graphView, List<UtilityButton> buttons)
        {
            UtilityButton permuteGraphButton = new UtilityButton() { name = "permute-graph-button" };
            permuteGraphButton.image = Resources.Load<Texture2D>("Vista/Textures/PermuteGraph");
            permuteGraphButton.tooltip = "Permute graph. Change the seed of all nodes in this graph to generate a completely new terrain.";
            permuteGraphButton.clicked += () => { PermuteGraph(graphView); };
            buttons.Add(permuteGraphButton);
        }

        private static void PermuteGraph(GraphEditorGraphView graphView)
        {
            GraphAsset graph = graphView.m_editor.clonedGraph;
            if (graph != null)
            {
                List<INode> nodes = graph.GetNodes();
                foreach (INode n in nodes)
                {
                    if (n is IHasSeed ihs)
                    {
                        ihs.seed = Random.Range(0, 10000);
                    }
                }
            }

            graphView.m_editor.ExecuteGraph();
        }
    }
}
#endif
