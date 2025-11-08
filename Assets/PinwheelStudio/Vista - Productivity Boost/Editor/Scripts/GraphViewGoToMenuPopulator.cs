#if VISTA
using Pinwheel.VistaEditor.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Pinwheel.VistaEditor.ProductivityBoost
{
    public static class GraphViewGoToMenuPopulator
    {
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            GraphEditorGraphView.buildContextualCallback += Populate;
        }

        private static void Populate(GraphEditorGraphView graphView, ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphView)
            {
                graphView.graphElements.ForEach(e =>
                {
                    if (e is GroupView gv)
                    {
                        string groupTitle = gv.title.Replace('/', ' ');
                        evt.menu.AppendAction($"Go To/{groupTitle}", (a) =>
                        {
                            graphView.ClearSelection();
                            graphView.AddToSelection(gv);
                            graphView.FrameSelection();
                        },
                        DropdownMenuAction.Status.Normal);
                    }
                });
            }
        }
    }
}
#endif
