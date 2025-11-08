#if VISTA
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using LPB = Pinwheel.Vista.LocalProceduralBiome;
using LPBI = Pinwheel.VistaEditor.LocalProceduralBiomeInspector;

namespace Pinwheel.VistaEditor.HandPainting
{
    public class PaintBiomeMaskGUIHandler
    {
        private static RenderTexture s_baseBiomeMask;
        private static RenderTexture s_biomeMaskAdjustments;
        private static RenderTexture s_canvas;

        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            LPBI.onDisableCallback += OnDisable;
            LPBI.injectBiomeMaskGUICallback += OnInjectBiomeMaskGUI;
            LPBI.injectSceneGUICallback += OnInjectSceneGUI;
        }

        public class Prefs
        {
            public static bool isPaintingBiomeMask;
        }

        private static void OnDisable(LPBI inspector, LPB biome)
        {
            bool willRegenerate = false;
            if (Prefs.isPaintingBiomeMask)
            {
                if (EditorUtility.DisplayDialog(
                    "Save Painting",
                    "You're painting on the biome mask, do you want to apply changes before exiting?",
                    "Save", "Don't Save"))
                {
                    CopyAdjustmentDataToBiome(biome);
                    willRegenerate = true;
                }
            }

            CleanUpRT();
            Prefs.isPaintingBiomeMask = false;

            if (willRegenerate)
            {
                inspector.MarkChangedAndGenerate();
            }
        }

        private static void CleanUpRT()
        {
            if (s_baseBiomeMask != null)
            {
                s_baseBiomeMask.Release();
                Object.DestroyImmediate(s_baseBiomeMask);
            }
            if (s_biomeMaskAdjustments != null)
            {
                s_biomeMaskAdjustments.Release();
                Object.DestroyImmediate(s_biomeMaskAdjustments);
            }
            if (s_canvas != null)
            {
                s_canvas.Release();
                Object.DestroyImmediate(s_canvas);
            }
        }

        private class BiomeMaskGUI
        {
            public static readonly GUIContent PAINT_MASK = new GUIContent("Paint Mask");
            public static readonly GUIContent END_PAINT_MASK = new GUIContent("End Painting Mask");
            public static readonly GUIContent RESET_PAINTING = new GUIContent("Reset Painting");

            public static readonly GUIContent PAINT_INSTRUCTION = new GUIContent(
                "- Left Mouse to paint.\n" +
                "- Ctrl + Left Mouse to erase.\n" +
                "- Shift + Left Mouse to smooth.");

            public static readonly GUIContent BRUSH_SETTINGS = new GUIContent("Brush Settings");
            public static readonly GUIContent RADIUS = new GUIContent("Radius", "Radius of the brush");
            public static readonly GUIContent FALLOFF = new GUIContent("Falloff", "Falloff percentage of the brush");
            public static readonly GUIContent OPACITY = new GUIContent("Opacity", "Opacity of the brush");
        }

        private static void OnInjectBiomeMaskGUI(LPBI inspector, LPB biome)
        {
            GUI.enabled = !LPBI.Prefs.isEditingAnchor;
            if (!Prefs.isPaintingBiomeMask)
            {
                if (EditorCommon.Button(BiomeMaskGUI.PAINT_MASK))
                {
                    LPBI.Prefs.isEditingAnchor = false;
                    Prefs.isPaintingBiomeMask = true;
                    InitPainting(biome);

                    Bounds biomeBounds = biome.worldBounds;
                    ArrayList sceneViews = SceneView.sceneViews;
                    foreach (SceneView sv in sceneViews)
                    {
                        sv.LookAt(biomeBounds.center, Quaternion.Euler(90, 0, 0), 0, true);
                        sv.orthographic = true;
                        sv.Frame(biomeBounds, false);
                    }
                }
            }
            else
            {
                Rect r = EditorGUILayout.GetControlRect();
                if (EditorCommon.ConfirmButton(r, BiomeMaskGUI.END_PAINT_MASK))
                {
                    Prefs.isPaintingBiomeMask = false;
                    CopyAdjustmentDataToBiome(biome);
                    CleanUpRT();
                    inspector.MarkChangedAndGenerate();

                    ArrayList sceneViews = SceneView.sceneViews;
                    foreach (SceneView sv in sceneViews)
                    {
                        sv.orthographic = false;
                    }
                }
                r = EditorGUILayout.GetControlRect();
                if (EditorCommon.RejectButton(r, BiomeMaskGUI.RESET_PAINTING))
                {
                    ResetPainting();
                }

                EditorGUILayout.LabelField(BiomeMaskGUI.PAINT_INSTRUCTION, EditorCommon.Styles.infoLabel);

                EditorCommon.Header(BiomeMaskGUI.BRUSH_SETTINGS);
                EditorSettings settings = EditorSettings.Get();
                EditorGUI.BeginChangeCheck();
                float radius = EditorGUILayout.Slider(BiomeMaskGUI.RADIUS, settings.biomeMaskBrushSettings.radius, 0f, 500f);
                float falloff = EditorGUILayout.Slider(BiomeMaskGUI.FALLOFF, settings.biomeMaskBrushSettings.falloff, 0f, 1f);
                float opacity = EditorGUILayout.Slider(BiomeMaskGUI.OPACITY, settings.biomeMaskBrushSettings.opacity, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(settings, $"Modify brush settings");
                    settings.biomeMaskBrushSettings.radius = radius;
                    settings.biomeMaskBrushSettings.falloff = falloff;
                    settings.biomeMaskBrushSettings.opacity = opacity;
                }
            }
            GUI.enabled = true;
        }

        private static void InitPainting(LPB biome)
        {
            CleanUpRT();

            s_baseBiomeMask = biome.RenderBaseBiomeMask();
            s_biomeMaskAdjustments = new RenderTexture(biome.biomeMaskResolution, biome.biomeMaskResolution, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            s_canvas = new RenderTexture(biome.biomeMaskResolution, biome.biomeMaskResolution, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);

            float[] texData = biome.biomeMaskAdjustments;
            if (texData.Length == 0)
            {
                Drawing.Blit(Texture2D.blackTexture, s_biomeMaskAdjustments);
                Drawing.Blit(Texture2D.blackTexture, s_canvas);
            }
            else
            {
                Texture2D tex2D = Utilities.TextureFromFloats(texData, biome.biomeMaskResolution, biome.biomeMaskResolution);
                Drawing.Blit(tex2D, s_biomeMaskAdjustments);
                Drawing.Blit(tex2D, s_canvas);
                Object.DestroyImmediate(tex2D);
            }
        }

        private static void ResetPainting()
        {
            Drawing.Blit(Texture2D.blackTexture, s_canvas);
            Drawing.Blit(Texture2D.blackTexture, s_biomeMaskAdjustments);
        }

        private static void CopyAdjustmentDataToBiome(LPB biome)
        {
            Texture2D tex2D = new Texture2D(s_biomeMaskAdjustments.width, s_biomeMaskAdjustments.height, TextureFormat.RFloat, false, true);
            RenderTexture.active = s_biomeMaskAdjustments;
            tex2D.ReadPixels(new Rect(0, 0, s_biomeMaskAdjustments.width, s_biomeMaskAdjustments.height), 0, 0);
            tex2D.Apply();
            RenderTexture.active = null;

            float[] adjustmentData = Utilities.FloatsFromTexture(tex2D);
            Undo.RecordObject(biome, $"Modify {biome.name}");
            biome.biomeMaskAdjustments = adjustmentData;
            Object.DestroyImmediate(tex2D);
        }

        private class SceneGUI
        {
            public static readonly Color BOUNDS_COLOR = Color.yellow;
            public static readonly Color BRUSH_COLOR = Color.yellow;


        }

        private static void OnInjectSceneGUI(LPBI inspector, LPB biome, SceneView sceneView)
        {
            if (!Prefs.isPaintingBiomeMask)
                return;
            if (!Prefs.isPaintingBiomeMask || Event.current.alt)
            {
                Tools.hidden = false;
            }
            else
            {
                Tools.hidden = true;

                EditorSettings editorSettings = EditorSettings.Get();
                EditorSettings.SimpleBrushSettings brushSettings = editorSettings.biomeMaskBrushSettings;

                Bounds worldBounds = biome.worldBounds;
                Rect worldRect = new Rect(worldBounds.min.x, worldBounds.min.z, worldBounds.size.x, worldBounds.size.z);
                Rect cursorRect = new Rect() { size = worldRect.size + 4f * Vector2.one * brushSettings.radius, center = worldRect.center };

                if (Event.current.type == EventType.Repaint)
                {
                    DrawBounds(worldBounds);
                    DrawCanvas(sceneView, biome, worldBounds);
                }

                if (Event.current.type != EventType.Repaint &&
                    Event.current.type != EventType.MouseDown &&
                    Event.current.type != EventType.MouseDrag &&
                    Event.current.type != EventType.MouseUp &&
                    Event.current.type != EventType.KeyDown)
                    return;

                int controlId = GUIUtility.GetControlID(inspector.GetHashCode(), FocusType.Passive);
                if (Event.current.type == EventType.MouseDown)
                {
                    if (Event.current.button == 0)
                    {
                        //Set the hot control to this tool, to disable marquee selection tool on mouse dragging
                        GUIUtility.hotControl = controlId;
                    }
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    if (GUIUtility.hotControl == controlId)
                    {
                        //Return the hot control back to Unity, use the default
                        GUIUtility.hotControl = 0;
                    }
                }


                Ray r = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                Plane p = new Plane(Vector3.up, biome.transform.position);
                float d;
                if (p.Raycast(r, out d))
                {
                    Vector3 hitPoint = r.origin + r.direction * d;

                    if (cursorRect.Contains(hitPoint.XZ()))
                    {
                        using (HandleScope s = new HandleScope(SceneGUI.BRUSH_COLOR, CompareFunction.Always))
                        {
                            Handles.DrawWireDisc(hitPoint, Vector3.up, brushSettings.radius * (1 - brushSettings.falloff));
                            Handles.DrawWireDisc(hitPoint, Vector3.up, brushSettings.radius);
                        }
                        if (Event.current.type != EventType.MouseUp)
                        {
                            HandlePaint(worldRect, hitPoint, brushSettings.radius, brushSettings.falloff, brushSettings.opacity);
                        }
                    }
                }

                if (Event.current.isMouse && Event.current.button == 0)
                {
                    Event.current.Use();
                }

                inspector.ExitSceneGUI();
            }
        }

        private class PaintSetup
        {
            public static readonly string PAINT_SHADER = "Hidden/Vista/BiomeMaskPainting";
            public static readonly int BASE_BIOME_MASK = Shader.PropertyToID("_BaseBiomeMask");
            public static readonly int ADJUSTMENT_MAP = Shader.PropertyToID("_AdjustmentMap");
            public static readonly int FALLOFF = Shader.PropertyToID("_Falloff");
            public static readonly int OPACITY = Shader.PropertyToID("_Opacity");
            public static readonly int MULTIPLIER = Shader.PropertyToID("_Multiplier");
            public static readonly int TEXEL_SIZE = Shader.PropertyToID("_TexelSize");
            public static Material s_biomeMaskPaintingMaterial;

            public static readonly int PASS_ADD_SUB = 0;
            public static readonly int PASS_SMOOTH = 1;
        }

        private static void HandlePaint(Rect worldRect, Vector3 worldPoint, float radius, float falloff, float opacity)
        {
            if (Event.current.isMouse && Event.current.button == 0)
            {
                if (PaintSetup.s_biomeMaskPaintingMaterial == null)
                {
                    PaintSetup.s_biomeMaskPaintingMaterial = new Material(Shader.Find(PaintSetup.PAINT_SHADER));
                }
                Material mat = PaintSetup.s_biomeMaskPaintingMaterial;
                mat.SetTexture(PaintSetup.BASE_BIOME_MASK, s_baseBiomeMask);
                mat.SetTexture(PaintSetup.ADJUSTMENT_MAP, s_biomeMaskAdjustments);
                mat.SetFloat(PaintSetup.FALLOFF, falloff);
                mat.SetFloat(PaintSetup.OPACITY, opacity);
                mat.SetVector(PaintSetup.TEXEL_SIZE, new Vector4(s_baseBiomeMask.texelSize.x, s_baseBiomeMask.texelSize.y, s_baseBiomeMask.width, s_baseBiomeMask.height));

                if (Event.current.control)
                {
                    mat.SetInt(PaintSetup.MULTIPLIER, -1);
                }
                else
                {
                    mat.SetInt(PaintSetup.MULTIPLIER, 1);
                }

                int pass = 0;
                if (Event.current.shift)
                {
                    pass = PaintSetup.PASS_SMOOTH;
                }
                else
                {
                    pass = PaintSetup.PASS_ADD_SUB;
                }

                Vector2[] quad = CalculateQuad(worldRect, worldPoint, radius);
                Drawing.DrawQuad(s_canvas, quad, mat, pass);
                Graphics.CopyTexture(s_canvas, s_biomeMaskAdjustments);
            }
        }

        private static Vector2[] CalculateQuad(Rect worldRect, Vector3 worldPoint, float radius)
        {
            Rect selfRect = new Rect() { size = 2 * radius * Vector2.one, center = worldPoint.XZ() };
            float minX = Utilities.InverseLerpUnclamped(worldRect.min.x, worldRect.max.x, selfRect.min.x);
            float maxX = Utilities.InverseLerpUnclamped(worldRect.min.x, worldRect.max.x, selfRect.max.x);
            float minY = Utilities.InverseLerpUnclamped(worldRect.min.y, worldRect.max.y, selfRect.min.y);
            float maxY = Utilities.InverseLerpUnclamped(worldRect.min.y, worldRect.max.y, selfRect.max.y);

            Vector2[] uvCorner = new Vector2[]
            {
                new Vector2(minX, minY),
                new Vector2(minX, maxY),
                new Vector2(maxX, maxY),
                new Vector2(maxX, minY)
            };

            return uvCorner;
        }

        private static void DrawBounds(Bounds bounds)
        {
            using (HandleScope s = new HandleScope(SceneGUI.BOUNDS_COLOR, CompareFunction.Always))
            {
                Handles.DrawWireCube(bounds.center, bounds.size);
            }
        }

        private class CanvasSetup
        {
            public static readonly string BIOME_MASK_COMBINE_SHADER = "Hidden/Vista/BiomeMaskCombine";
            public static readonly int BASE_BIOME_MASK = Shader.PropertyToID("_BaseBiomeMask");
            public static readonly int BIOME_MASK_ADJUSTMENTS = Shader.PropertyToID("_BiomeMaskAdjustments");
            public static readonly int PASS = 0;

            public static Material s_biomeMaskVisMaterial;

            public static readonly string PATCH_MESH = "Vista/Meshes/Patch";
            public static Mesh s_patchMesh;
        }

        private static void DrawCanvas(SceneView sv, LPB biome, Bounds bounds)
        {
            if (CanvasSetup.s_biomeMaskVisMaterial == null)
            {
                CanvasSetup.s_biomeMaskVisMaterial = new Material(Shader.Find(CanvasSetup.BIOME_MASK_COMBINE_SHADER));
            }
            Material mat = CanvasSetup.s_biomeMaskVisMaterial;
            mat.SetTexture(CanvasSetup.BASE_BIOME_MASK, s_baseBiomeMask != null ? (Texture)s_baseBiomeMask : Texture2D.blackTexture);
            mat.SetTexture(CanvasSetup.BIOME_MASK_ADJUSTMENTS, s_canvas != null ? (Texture)s_canvas : Texture2D.whiteTexture);

            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_BlendOp", (int)BlendOp.Add);

            if (CanvasSetup.s_patchMesh == null)
            {
                CanvasSetup.s_patchMesh = Resources.Load<Mesh>(CanvasSetup.PATCH_MESH);
            }
            Mesh m = CanvasSetup.s_patchMesh;
            Matrix4x4 trs = Matrix4x4.TRS(new Vector3(bounds.center.x, biome.transform.position.y, bounds.center.z), Quaternion.identity, bounds.size);
            Graphics.DrawMesh(m, trs, mat, 0, sv.camera, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off, null);
        }
    }
}
#endif
