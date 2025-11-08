#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.VistaEditor.Graph;
using Pinwheel.Vista.Graph;
using Pinwheel.Vista.RealWorldData.Graph;
using UnityEditor;
using UnityEditorInternal;

namespace Pinwheel.VistaEditor.RealWorldData.Graph
{
    [NodeEditor(typeof(DrawMaskOverlayNode))]
    public class DrawMaskOverlayNodeEditor : ImageNodeEditorBase
    {
        public override void OnGUI(INode node)
        {
        }
    }
}
#endif
