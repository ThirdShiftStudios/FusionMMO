#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEngine.UIElements;

namespace Pinwheel.VistaEditor.RealWorldData
{
    public class UtilityButton : Button
    {
        public UtilityButton() : base()
        {
            StyleSheet uss = Resources.Load<StyleSheet>("Vista/USS/UtilityButton");
            this.styleSheets.Add(uss);
            this.text = "";
        }
    }
}
#endif
