#if VISTA
using Pinwheel.VistaEditor.Graph;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinwheel.VistaEditor.ProductivityBoost
{
    public static class AddAnchorNodeHandler
    {
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            GraphEditorGraphView.edgeDoubleClickedCallback += OnEdgeDoubleClicked;
        }

        private static void OnEdgeDoubleClicked(GraphEditorGraphView graphView, EdgeView edgeView, Vector2 mousePosInGraph)
        {
            PortView outputPort = edgeView.output as PortView;
            Rect nodeRect = new Rect(mousePosInGraph.x - NodeView.ANCHOR_NODE_SIZE.x * 0.5f, mousePosInGraph.y - NodeView.ANCHOR_NODE_SIZE.y * 0.5f, 0, 0);
            GraphEditorGraphView.AddNodeResult addAnchorResult = graphView.AddAnchorNode(outputPort.adapter.slotType, nodeRect);

            GraphViewChange change = new GraphViewChange();
            change.elementsToRemove = new List<GraphElement>();
            change.elementsToRemove.Add(edgeView);
            change.edgesToCreate = new List<UnityEditor.Experimental.GraphView.Edge>();

            NodeView nodeView = addAnchorResult.nodeView;
            PortView nvInputPort = nodeView.inputContainer.Q<PortView>();
            PortView nvOutputPort = nodeView.outputContainer.Q<PortView>();

            if (nvInputPort != null)
            {
                EdgeView edge0 = edgeView.output.ConnectTo<EdgeView>(nvInputPort);
                change.edgesToCreate.Add(edge0);
            }
            if (nvOutputPort != null)
            {
                EdgeView edge1 = nvOutputPort.ConnectTo<EdgeView>(edgeView.input);
                change.edgesToCreate.Add(edge1);
            }

            graphView.graphViewChanged.Invoke(change);
            foreach (EdgeView item in change.edgesToCreate)
            {
                graphView.AddElement(item);
            }
            graphView.DeleteElements(new GraphElement[] { edgeView });
        }
    }
}
#endif
