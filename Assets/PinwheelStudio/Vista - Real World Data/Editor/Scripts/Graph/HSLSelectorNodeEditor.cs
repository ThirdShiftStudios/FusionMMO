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
    [NodeEditor(typeof(HSLSelectorNode))]
    public class HSLSelectorNodeEditor : ImageNodeEditorBase
    {
        private static readonly GUIContent HUE = new GUIContent("Hue", "The hue range");
        private static readonly GUIContent SATURATION = new GUIContent("Saturation", "The saturation range");
        private static readonly GUIContent LIGHTNESS = new GUIContent("Lightness", "The lightness range");
        private static readonly GUIContent EMPTY = new GUIContent(" ");
        private static readonly GUIContent SAMPLE_2D_VIEWPORT = new GUIContent("Pick from 2D Viewport");
        private static readonly GUIContent PICK_COLOR = new GUIContent(" ");

        private static readonly string HSL_STRIPS_SHADER = "Hidden/Vista/RealWorldData/HSLStrips";
        private static readonly string KW_DRAW_H_STRIP = "DRAW_H_STRIP";
        private static readonly string KW_DRAW_S_STRIP = "DRAW_S_STRIP";
        private static readonly string KW_DRAW_L_STRIP = "DRAW_L_STRIP";
        private static readonly int HUE_PROP = Shader.PropertyToID("_Hue");

        public static bool isColorPickingModeEnabled { get; set; }

        public override void OnViewport2dGUI(INode node, Rect imguiRect, Rect imageRect)
        {
            if (!isColorPickingModeEnabled)
                return;

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
            {
                Vector2 pos = HandleUtility.GUIPointToScreenPixelCoordinate(Event.current.mousePosition);
                Color[] colors = InternalEditorUtility.ReadScreenPixelUnderCursor(pos, 1, 1);
                Vector3 hsl = Pinwheel.Vista.RealWorldData.Utilities.RGB2HSL(colors[0]);

                m_graphEditor.RegisterUndo(node);

                float tolerance = 0.025f;
                HSLSelectorNode n = node as HSLSelectorNode;
                n.minHue = Mathf.Max(0, hsl.x - tolerance);
                n.maxHue = Mathf.Min(1, hsl.x + tolerance);
                n.minSaturation = 0;
                n.maxSaturation = 1;
                n.minLightness = 0;
                n.maxLightness = 1;

                GUI.changed = true;
            }
        }

        public override void OnGUI(INode node)
        {
            HSLSelectorNode n = node as HSLSelectorNode;
            Material matH = new Material(Shader.Find(HSL_STRIPS_SHADER));
            matH.SetFloat(HUE_PROP, (n.minHue + n.maxHue) * 0.5f);
            matH.EnableKeyword(KW_DRAW_H_STRIP);
            Material matS = new Material(Shader.Find(HSL_STRIPS_SHADER));
            matS.SetFloat(HUE_PROP, (n.minHue + n.maxHue) * 0.5f);
            matS.EnableKeyword(KW_DRAW_S_STRIP);
            Material matL = new Material(Shader.Find(HSL_STRIPS_SHADER));
            matL.SetFloat(HUE_PROP, (n.minHue + n.maxHue) * 0.5f);
            matL.EnableKeyword(KW_DRAW_L_STRIP);

            Rect r;
            EditorGUI.BeginChangeCheck();
            float minH = n.minHue;
            float maxH = n.maxHue;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(HUE, GUILayout.Width(EditorGUIUtility.labelWidth));
            r = EditorGUILayout.GetControlRect();
            EditorGUI.DrawPreviewTexture(r, Texture2D.whiteTexture, matH);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.MinMaxSlider(EMPTY, ref minH, ref maxH, 0f, 1f);

            float minS = n.minSaturation;
            float maxS = n.maxSaturation;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(SATURATION, GUILayout.Width(EditorGUIUtility.labelWidth));
            r = EditorGUILayout.GetControlRect();
            EditorGUI.DrawPreviewTexture(r, Texture2D.whiteTexture, matS);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.MinMaxSlider(EMPTY, ref minS, ref maxS, 0f, 1f);

            float minL = n.minLightness;
            float maxL = n.maxLightness;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(LIGHTNESS, GUILayout.Width(EditorGUIUtility.labelWidth));
            r = EditorGUILayout.GetControlRect();
            EditorGUI.DrawPreviewTexture(r, Texture2D.whiteTexture, matL);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.MinMaxSlider(EMPTY, ref minL, ref maxL, 0f, 1f);

            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.minHue = minH;
                n.maxHue = maxH;
                n.minSaturation = minS;
                n.maxSaturation = maxS;
                n.minLightness = minL;
                n.maxLightness = maxL;
            }
            Object.DestroyImmediate(matH);
            Object.DestroyImmediate(matS);
            Object.DestroyImmediate(matL);

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(EMPTY, GUILayout.Width(EditorGUIUtility.labelWidth));
            GUI.backgroundColor = isColorPickingModeEnabled ? Color.gray : Color.white;
            if (GUILayout.Button(SAMPLE_2D_VIEWPORT))
            {
                isColorPickingModeEnabled = !isColorPickingModeEnabled;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            Color sampledColor = EditorGUILayout.ColorField(PICK_COLOR, Color.clear);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 hsl = Pinwheel.Vista.RealWorldData.Utilities.RGB2HSL(sampledColor);

                m_graphEditor.RegisterUndo(node);

                float tolerance = 0.025f;
                n.minHue = Mathf.Max(0, hsl.x - tolerance);
                n.maxHue = Mathf.Min(1, hsl.x + tolerance);
                n.minSaturation = 0;
                n.maxSaturation = 1;
                n.minLightness = 0;
                n.maxLightness = 1;
            }
        }
    }
}
#endif
