#if VISTA
using Pinwheel.VistaEditor.Graph;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinwheel.VistaEditor.ProductivityBoost
{
    public static class StickyImageDragDropHandler
    {
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            GraphEditorGraphView.filterDragObjectCallback += FilterDragObject;
            GraphEditorGraphView.objectDroppedCallback += DropObjects;
        }

        private static void FilterDragObject(GraphEditorGraphView graphView, UnityEngine.Object[] objects, ref bool accepted)
        {
            for (int i = 0; i < objects.Length; ++i)
            {
                if (objects[i] is Texture2D && AssetDatabase.Contains(objects[i]))
                {
                    accepted = true;
                    return;
                }
            }
        }

        private static void DropObjects(GraphEditorGraphView graphView, UnityEngine.Object[] objects, Vector2 localDropPosition)
        {
            List<Texture2D> images = new List<Texture2D>();
            foreach (Object o in objects)
            {
                if (o is Texture2D t && AssetDatabase.Contains(t))
                {
                    images.Add(t);
                }
            }
            if (images.Count == 0)
                return;

            graphView.m_editor.RegisterUndo("Add Sticky Image");
            for (int i = 0; i < images.Count; ++i)
            {
                Vector2 pos = VisualElementExtensions.ChangeCoordinatesTo(graphView, graphView.contentViewContainer, localDropPosition);
                graphView.AddStickyImage(images[i], pos - StickyImageView.defaultSize * 0.5f + Vector2.one * 50 * i);
                EditorUtility.SetDirty(graphView.m_editor.clonedGraph);
            }
        }
    }
}
#endif
