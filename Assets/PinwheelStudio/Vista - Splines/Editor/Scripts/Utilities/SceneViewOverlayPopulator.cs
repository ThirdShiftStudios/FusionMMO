#if VISTA
#if UNITY_2021_2_OR_NEWER
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEditor;
using Pinwheel.VistaEditor;
using Pinwheel.VistaEditor.UIElements;
using System;

namespace Pinwheel.VistaEditor.Splines
{
    [InitializeOnLoad]
    public class SceneViewOverlayPopulator
    {
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            SceneViewOverlay.populateItemCallback += OnPopulateItem;
        }

        private static void OnPopulateItem(Collector<string> elementIds)
        {
            elementIds.Add(DrawSplineMeshToolbarToggle.ID);
        }
    }
}
#endif
#endif