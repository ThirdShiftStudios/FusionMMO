#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEngine.UIElements;

namespace Pinwheel.VistaEditor.RealWorldData
{
    public class SelectRegionButton : Button
    {
        public SelectRegionButton() : base()
        {
            StyleSheet uss = Resources.Load<StyleSheet>("Vista/USS/SelectRegionButton");
            this.styleSheets.Add(uss);

            text = "Select a region";
        }
    }
}
#endif
