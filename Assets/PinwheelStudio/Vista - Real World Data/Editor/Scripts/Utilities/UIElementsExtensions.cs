#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEngine.UIElements;

namespace Pinwheel.VistaEditor.RealWorldData
{
    public static class UIElementsExtensions
    {
        /// <summary>
        /// To help the code looks cleaner when setup GUI
        /// </summary>
        /// <param name="self"></param>
        /// <param name="parent"></param>
        public static void AddTo(this VisualElement self, VisualElement parent)
        {
            parent.Add(self);
        }
    }
}
#endif
