#if VISTA
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.HandPainting.Graph;
using Pinwheel.VistaEditor.Graph;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.VistaEditor.HandPainting.Graph
{
    [NodeEditor(typeof(PaintColorNode))]
    public class PaintColorNodeEditor : ExecutableNodeEditorBase
    {
        public static readonly GUIContent RESOLUTION = new GUIContent("Resolution", "The size of the canvas in pixels, use smallest size possible because the texture data will be embedded in the graph asset, which will affect the editor performance when loading the graph.");

        private static readonly GUIContent RADIUS = new GUIContent("Radius", "Radius of the brush");
        private static readonly GUIContent FALLOFF = new GUIContent("Falloff", "Falloff range of the brush");
        private static readonly GUIContent COLOR = new GUIContent("Color", "Color of the brush");

        private static readonly string PAINT_SHADER = "Hidden/Vista/Graph/PaintColorNode";
        private static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");
        private static readonly int FALL_OFF = Shader.PropertyToID("_Falloff");
        private static readonly int BRUSH_COLOR = Shader.PropertyToID("_Color");

        private static readonly int PASS_ADD = 0;
        private static readonly int PASS_SUB = 1;
        private static readonly int PASS_SMOOTH = 2;

        public override bool needConstantUpdate2D
        {
            get
            {
                return true;
            }
        }

        public override void OnGUI(INode node)
        {
            PaintColorNode n = node as PaintColorNode;
            EditorGUI.BeginChangeCheck();
            int resolution = EditorGUILayout.DelayedIntField(RESOLUTION, n.resolution);
            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.resolution = resolution;
            }

            EditorGUI.BeginChangeCheck();
            float radius = EditorGUILayout.Slider(RADIUS, n.brushRadius, 0f, 1f);
            float falloff = EditorGUILayout.Slider(FALLOFF, n.brushFalloff, 0f, 1f);
            Color color = EditorGUILayout.ColorField(COLOR, n.brushColor);
            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                n.brushRadius = radius;
                n.brushFalloff = falloff;
                n.brushColor = color;
            }
        }

        public override void OnViewport2dGUI(INode node, Rect imguiRect, Rect imageRect)
        {
            PaintColorNode n = node as PaintColorNode;

            Handles.BeginGUI();
            float outterRadiusPixel = n.brushRadius * imageRect.width;
            Handles.color = new Color(1, 1, 1, 0.5f);
            Handles.DrawWireDisc(Event.current.mousePosition, Vector3.forward, outterRadiusPixel);

            float innerRadiusPixel = n.brushRadius * (1 - n.brushFalloff) * imageRect.width;
            Handles.color = new Color(1, 1, 1, 1);
            Handles.DrawWireDisc(Event.current.mousePosition, Vector3.forward, innerRadiusPixel);

            Handles.EndGUI();

            if (Event.current.isMouse && Event.current.button == 0)
            {
                int pass =
                    Event.current.shift ? PASS_SMOOTH :
                    Event.current.control ? PASS_SUB :
                    PASS_ADD;
                Material paintMaterial = new Material(ShaderUtilities.Find(PAINT_SHADER));
                paintMaterial.SetTexture(MAIN_TEX, n.texture);
                paintMaterial.SetFloat(FALL_OFF, n.brushFalloff);
                paintMaterial.SetColor(BRUSH_COLOR, n.brushColor);

                Vector2[] quad = CalculateQuad(imageRect, Event.current.mousePosition, n.brushRadius);

                RenderTexture tempRT = RenderTexture.GetTemporary(n.resolution, n.resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Drawing.Blit(n.texture, tempRT);
                Drawing.DrawQuad(tempRT, quad, paintMaterial, pass);

                RenderTexture.active = tempRT;
                n.texture.ReadPixels(new Rect(0, 0, n.resolution, n.resolution), 0, 0);
                n.texture.Apply();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(tempRT);
                GUI.changed = true;
                Object.DestroyImmediate(paintMaterial);
            }
        }

        private static Vector2[] CalculateQuad(Rect imageRect, Vector2 mousePosition, float radius)
        {
            Rect selfRect = new Rect() { size = 2 * radius * imageRect.size, center = mousePosition };
            float minX = Utilities.InverseLerpUnclamped(imageRect.min.x, imageRect.max.x, selfRect.min.x);
            float maxX = Utilities.InverseLerpUnclamped(imageRect.min.x, imageRect.max.x, selfRect.max.x);
            float minY = Utilities.InverseLerpUnclamped(imageRect.min.y, imageRect.max.y, selfRect.min.y);
            float maxY = Utilities.InverseLerpUnclamped(imageRect.min.y, imageRect.max.y, selfRect.max.y);

            Vector2[] uvCorner = new Vector2[]
            {
                new Vector2(minX, 1-minY),
                new Vector2(minX, 1-maxY),
                new Vector2(maxX, 1-maxY),
                new Vector2(maxX, 1-minY)
            };

            return uvCorner;
        }
    }
}
#endif
