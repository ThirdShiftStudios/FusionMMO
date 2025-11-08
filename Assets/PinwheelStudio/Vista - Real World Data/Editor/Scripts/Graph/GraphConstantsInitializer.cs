#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using Pinwheel.VistaEditor.Graph;
using UnityEditor;
using GraphConstants = Pinwheel.Vista.RealWorldData.Graph.GraphConstants;

namespace Pinwheel.VistaEditor.RealWorldData.Graph
{
    [InitializeOnLoad]
    public static class GraphConstantsInitializer
    {
        [InitializeOnLoadMethod]
        public static void InitOutputNameSelector()
        {
            InputNodeEditor.nameSelector.Add(new NameSelectorEntry()
            {
                name = GraphConstants.REAL_WORLD_HEIGHT_INPUT_NAME,
                slotType = typeof(MaskSlot)
            });
            InputNodeEditor.nameSelector.Add(new NameSelectorEntry()
            {
                name = GraphConstants.REAL_WORLD_COLOR_INPUT_NAME,
                slotType = typeof(ColorTextureSlot)
            });
        }
    }
}
#endif
