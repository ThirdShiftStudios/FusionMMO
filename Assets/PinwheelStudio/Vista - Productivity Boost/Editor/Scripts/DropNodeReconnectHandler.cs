#if VISTA
using Pinwheel.VistaEditor.Graph;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Pinwheel.VistaEditor.ProductivityBoost
{
    public static class DropNodeReconnectHandler
    {
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            GraphEditorGraphView.nodeOverlapEdgeCallback += OnNodeOverlapEdge;
            GraphEditorGraphView.nodeDroppedOnEdgeCallback += OnNodeDroppedOnEdge;
        }
        private static void OnNodeOverlapEdge(GraphEditorGraphView graphView, NodeView nodeView, EdgeView edgeView)
        {
            if (CanReconnect(nodeView, edgeView))
            {
                edgeView.edgeControl.edgeWidth = 6;
            }
        }

        private static void OnNodeDroppedOnEdge(GraphEditorGraphView graphView, NodeView nodeView, EdgeView edgeView)
        {
            if (!CanReconnect(nodeView, edgeView))
                return;
            PortView outputPort = edgeView.output as PortView;
            PortView firstCompatibleInputPort = null;
            if (nodeView.inputContainer.resolvedStyle.display == DisplayStyle.Flex)
            {
                nodeView.inputContainer.Query<PortView>().ForEach(pv =>
                {
                    if (firstCompatibleInputPort != null)
                        return;
                    if (outputPort.adapter.CanConnectTo(pv.adapter))
                    {
                        firstCompatibleInputPort = pv;
                    }
                });
            }

            PortView inputPort = edgeView.input as PortView;
            PortView firstCompatibleOutputPort = null;
            if (nodeView.outputContainer.resolvedStyle.display == DisplayStyle.Flex)
            {
                nodeView.outputContainer.Query<PortView>().ForEach(pv =>
                {
                    if (firstCompatibleOutputPort != null)
                        return;
                    if (inputPort.adapter.CanConnectTo(pv.adapter))
                    {
                        firstCompatibleOutputPort = pv;
                    }
                });
            }

            GraphViewChange change = new GraphViewChange();
            change.elementsToRemove = new List<GraphElement>();
            change.elementsToRemove.Add(edgeView);
            change.edgesToCreate = new List<UnityEditor.Experimental.GraphView.Edge>();

            if (firstCompatibleInputPort != null)
            {
                EdgeView edge0 = edgeView.output.ConnectTo<EdgeView>(firstCompatibleInputPort);
                change.edgesToCreate.Add(edge0);
            }
            if (firstCompatibleOutputPort != null)
            {
                EdgeView edge1 = firstCompatibleOutputPort.ConnectTo<EdgeView>(edgeView.input);
                change.edgesToCreate.Add(edge1);
            }

            graphView.graphViewChanged.Invoke(change);
            foreach (EdgeView item in change.edgesToCreate)
            {
                graphView.AddElement(item);
            }
            graphView.DeleteElements(new GraphElement[] { edgeView });
        }

        private static bool CanReconnect(NodeView nv, EdgeView ev)
        {
            bool hasInputAndOutput = true;
            if (nv.inputContainer.Q<PortView>() == null)
            {
                hasInputAndOutput = false;
            }
            if (nv.inputContainer.Q<PortView>() == null)
            {
                hasInputAndOutput = false;
            }

            if (!hasInputAndOutput)
            {
                return false;
            }

            bool isConnected = false;
            nv.Query<PortView>().ForEach(pv =>
            {
                if (pv.connected)
                {
                    isConnected = true;
                }
            });
            if (isConnected)
            {
                return false;
            }

            bool canReconnectLeft = false;
            if (nv.inputContainer.resolvedStyle.display == DisplayStyle.Flex)
            {
                PortView outputPort = ev.output as PortView;
                nv.inputContainer.Query<PortView>().ForEach(pv =>
                {
                    if (outputPort.adapter.CanConnectTo(pv.adapter))
                    {
                        canReconnectLeft = true;
                    }
                });
            }
            if (!canReconnectLeft)
            {
                return false;
            }

            bool canReconnectRight = false;
            if (nv.outputContainer.resolvedStyle.display == DisplayStyle.Flex)
            {
                PortView inputPort = ev.input as PortView;
                nv.outputContainer.Query<PortView>().ForEach(pv =>
                {
                    if (inputPort.adapter.CanConnectTo(pv.adapter))
                    {
                        canReconnectRight = true;
                    }
                });
            }
            if (!canReconnectRight)
            {
                return false;
            }

            return true;
        }
    }
}
#endif
