#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEditor;
using Pinwheel.VistaEditor.Graph;
using Pinwheel.Vista.Graph;
using System;
using Pinwheel.Vista.ExposeProperty;

namespace Pinwheel.VistaEditor.ExposeProperty
{
    public static class GraphEditorUpdateNodeVisualHandler
    {
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            GraphEditorBase.updateNodeVisualCallback += OnUpdateNodeVisual;
        }

        private static void OnUpdateNodeVisual(GraphEditorBase editor, NodeView nv, INode node)
        {
            nv.SetBadgeEnable<HasExposedPropertiesBadge>(editor.clonedGraph.HasPropertyExposed(node.id));
        }
    }
}
#endif
