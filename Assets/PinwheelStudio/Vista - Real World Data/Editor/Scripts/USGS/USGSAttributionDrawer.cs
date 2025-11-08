#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.VistaEditor.RealWorldData;
using UnityEditor;

namespace Pinwheel.VistaEditor.RealWorldData
{
    public class USGSAttributionDrawer : IMapAttributionDrawer
    {
        private static GUIStyle s_creditStyle;
        private static GUIStyle creditStyle
        {
            get
            {
                if (s_creditStyle == null)
                {
                    s_creditStyle = new GUIStyle(EditorStyles.whiteLabel);
                }
                s_creditStyle.alignment = TextAnchor.LowerRight;
                return s_creditStyle;
            }
        }

        public void Draw(Rect r)
        {
            float height = 24;
            Rect footerRect = new Rect(r.x, r.height - height, r.width, height);
            RectOffset offset = new RectOffset(8, 8, 0, 0);
            Rect footerContentRect = offset.Remove(footerRect);
            GUILayout.BeginArea(footerContentRect);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Credit: U.S. Geological Survey", creditStyle);
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

        }
    }
}
#endif
