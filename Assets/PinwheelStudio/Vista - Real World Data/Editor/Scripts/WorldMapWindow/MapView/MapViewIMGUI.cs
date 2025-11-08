#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.RealWorldData;
using UnityEditor;
using Pinwheel.Vista.Geometric;
using Math = System.Math;

namespace Pinwheel.VistaEditor.RealWorldData
{
    public class MapViewIMGUI : System.IDisposable
    {
        public delegate void RegionSelectedHandler(GeoRect selectionRect);
        public static event RegionSelectedHandler regionSelectedCallback;

        private GeoRect m_viewport100;
        internal GeoRect viewport100
        {
            get
            {
                return m_viewport100;
            }
        }

        private GeoRect m_animatedViewport100;

        internal GeoRect viewForRendering => m_viewport100;

        private double viewDx;
        private double viewDy;
        private int viewAnimDuration = 60; //frame

        private List<MapViewTile> m_rootTiles;
        private List<MapViewTile> m_tilesToRender;
        internal RenderTexture m_canvas;
        private Vector2[] m_quadVertices;
        private Vector2[] m_quadUvs;
        private int m_frameCount;
        private Material m_drawTileMaterial;
        private Material m_gradientMaterial;

        private int m_tileQualityOffset = (int)TileQuality.Normal + 1;

        private readonly string BLIT_SHADER = "Hidden/Vista/Blit";
        private readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");

        private Rect m_oldRect;
        private const float ZOOM_OUT = 2;
        private const float ZOOM_IN = 0.5f;

        public MapInteractionMode interactionMode { get; set; }
        private GeoPoint? m_selectionStartVp100;
        private GeoPoint? m_selectionEndVp100;
        private bool isDraggingSelection;

        private static GUIStyle s_selectionApproxSizeLabelStyle;
        private static GUIStyle selectionApproxSizeLabelSize
        {
            get
            {
                if (s_selectionApproxSizeLabelStyle == null)
                {
                    s_selectionApproxSizeLabelStyle = new GUIStyle(EditorStyles.label);
                }
                s_selectionApproxSizeLabelStyle.normal.textColor = Color.red;
                s_selectionApproxSizeLabelStyle.fontStyle = FontStyle.Italic;
                return s_selectionApproxSizeLabelStyle;
            }
        }

        private IImageTileProvider m_tileProvider;
        private IMapAttributionDrawer m_attributionDrawer;

        public MapViewIMGUI(IImageTileProvider tileProvider, IMapAttributionDrawer attributionDrawer)
        {
            m_oldRect = new Rect(0, 0, 1, 1);
            m_tileProvider = tileProvider;
            m_attributionDrawer = attributionDrawer;
        }

        public void OnEnable()
        {
            m_rootTiles = new List<MapViewTile>();
            m_rootTiles.Add(MapTileUtilities.CreateRoot<MapViewTile>());
            m_frameCount = 0;
            SetViewNoAnimation(GeoRect.rect100);
            EditorApplication.update += OnEditorUpdate;
        }

        public void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            Dispose();
        }

        private void OnEditorUpdate()
        {
            m_frameCount += 1;
            if (m_frameCount % 500 == 0)
            {
                FreeTilesMemory();
            }

            m_animatedViewport100 = GeoRectExtensions.MoveTowards(m_animatedViewport100, viewport100, viewDx, viewDy);
        }

        public void Dispose()
        {
            if (m_canvas != null)
            {
                m_canvas.Release();
                Object.DestroyImmediate(m_canvas);
            }

            if (m_drawTileMaterial != null)
            {
                Object.DestroyImmediate(m_drawTileMaterial);
            }

            if (m_gradientMaterial != null)
            {
                Object.DestroyImmediate(m_gradientMaterial);
            }

            foreach (MapViewTile t in m_rootTiles)
            {
                t.ForEach((t0) => t0.Dispose());
            }
        }

        public void OnGUI(Rect r)
        {
            HandleGeometryChanged(r);
            HandleInteraction(r);
            ValidateViewRegion();
            HandleTilesLoop();
            RenderCanvas(r);
            RenderHandles(r);
            RenderAttribution(r);
        }

        private void RenderCanvas(Rect r)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (m_canvas != null && (m_canvas.width != (int)r.width || m_canvas.height != (int)r.height))
            {
                m_canvas.Release();
                Object.DestroyImmediate(m_canvas);
            }

            if (m_canvas == null)
            {
                m_canvas = new RenderTexture((int)r.width, (int)r.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            }

            if (m_quadVertices == null)
            {
                m_quadVertices = new Vector2[4];
            }

            if (m_quadUvs == null)
            {
                m_quadUvs = new Vector2[4];
            }

            if (m_drawTileMaterial == null)
            {
                m_drawTileMaterial = new Material(Shader.Find(BLIT_SHADER));
            }

            Vector2 normalizedMousePos = Rect.PointToNormalized(r, Event.current.mousePosition);
            normalizedMousePos.y = 1 - normalizedMousePos.y;
            //Clear canvas
            Drawing.Blit(Texture2D.blackTexture, m_canvas);

            //Cull & draw tiles
            CullTiles();
            foreach (MapViewTile tile in m_tilesToRender)
            {
                Texture2D tileTexture;
                if (tile.TryGetTexture(out tileTexture, m_tileProvider))
                {
                    CalculateTileQuad(tileTexture, viewForRendering, tile.bounds100, m_quadVertices, m_quadUvs);

                    m_drawTileMaterial.SetTexture(MAIN_TEX, tileTexture);
                    Drawing.DrawQuad(m_canvas, m_quadVertices, m_quadUvs, m_drawTileMaterial, 0);
                }
            }

            DrawVignette();
            //Output to editor window
            EditorGUI.DrawPreviewTexture(r, m_canvas);
        }

        private void CalculateTileQuad(Texture tileTexture, GeoRect canvas, GeoRect tileBounds, Vector2[] vertices, Vector2[] uvs)
        {
            GeoRect.CalculateNormalizedQuadNonAlloc(canvas, tileBounds, vertices);

            Vector2 texelSize = tileTexture.texelSize;
            uvs[0] = new Vector2(texelSize.x, texelSize.y);
            uvs[1] = new Vector2(texelSize.x, 1 - texelSize.y);
            uvs[2] = new Vector2(1 - texelSize.x, 1 - texelSize.y);
            uvs[3] = new Vector2(1 - texelSize.x, texelSize.y);
        }

        private void DrawVignette()
        {
            string SHADER = "Hidden/Vista/RealWorldData/WorldMapWindow/FooterGradient";

            if (m_gradientMaterial == null)
            {
                m_gradientMaterial = new Material(Shader.Find(SHADER));
            }

            m_quadVertices[0] = new Vector2(0, 0);
            m_quadVertices[1] = new Vector2(0, 0.05f);
            m_quadVertices[2] = new Vector2(1, 0.05f);
            m_quadVertices[3] = new Vector2(1, 0);
            Drawing.DrawQuad(m_canvas, m_quadVertices, m_gradientMaterial, 0);
        }

        private void CullTiles()
        {
            int zoom = CalculateZoomLevel();
            if (m_tilesToRender == null)
            {
                m_tilesToRender = new List<MapViewTile>();
            }
            m_tilesToRender.Clear();

            Stack<MapViewTile> stack = new Stack<MapViewTile>();
            foreach (MapViewTile root in m_rootTiles)
            {
                stack.Push(root);
            }

            while (stack.Count > 0)
            {
                MapViewTile t = stack.Pop();
                if (!GeoRect.Intersect(t.bounds100, viewport100))
                    continue;

                if (t.zoom < zoom)
                {
                    if (!t.ChildrenNotNull())
                    {
                        t.Split();
                    }

                    bool childrenNotAvailable = false;
                    if (MapViewTile.IsTileNotAvailable(t.topLeft))
                    {
                        childrenNotAvailable = true;
                    }
                    else
                    {
                        stack.Push(t.topLeft);
                    }

                    if (MapViewTile.IsTileNotAvailable(t.topRight))
                    {
                        childrenNotAvailable = true;
                    }
                    else
                    {
                        stack.Push(t.topRight);
                    }

                    if (MapViewTile.IsTileNotAvailable(t.bottomLeft))
                    {
                        childrenNotAvailable = true;
                    }
                    else
                    {
                        stack.Push(t.bottomLeft);
                    }

                    if (MapViewTile.IsTileNotAvailable(t.bottomRight))
                    {
                        childrenNotAvailable = true;
                    }
                    else
                    {
                        stack.Push(t.bottomRight);
                    }

                    if (childrenNotAvailable)
                    {
                        m_tilesToRender.Add(t);
                    }

                    continue;
                }

                if (t.zoom == zoom)
                {
                    m_tilesToRender.Add(t);
                    continue;
                }

                if (t.zoom > zoom)
                    continue;
            }
        }

        private void HandleInteraction(Rect r)
        {
            if (interactionMode == MapInteractionMode.None)
                return;

            if (interactionMode == MapInteractionMode.Movement)
                HandleMapMovement(r);

            if (interactionMode == MapInteractionMode.SelectRegion)
                HandleSelectRegion(r);
        }

        private void HandleMapMovement(Rect r)
        {
            if (!r.Contains(Event.current.mousePosition))
                return;

            //zooming
            if (Event.current.type == EventType.ScrollWheel)
            {
                Vector2 pivot = Vector2.one * 0.5f;// Rect.PointToNormalized(r, Event.current.mousePosition);
                float currentZoom = CalculateZoomLevel();
                if (Event.current.delta.y > 0)
                {
                    if (viewport100.height < 180)
                    {
                        GeoRect newView = viewport100.Scale(ZOOM_OUT, pivot.x, pivot.y);
                        CalculateAnimationFactors(newView);
                        SetViewWithAnimation(newView);
                    }
                }
                else if (Event.current.delta.y < 0)
                {
                    int maxZoom = GetMaxZoom();
                    if (currentZoom < maxZoom)
                    {
                        GeoRect newView = viewport100.Scale(ZOOM_IN, pivot.x, pivot.y);
                        CalculateAnimationFactors(newView);
                        SetViewWithAnimation(newView);
                    }
                }
            }

            //panning with left mouse
            if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
            {
                Vector3 mouseDelta = Event.current.delta;
                double fX = -mouseDelta.x / r.width;
                double fY = mouseDelta.y / r.height;
                double dLong = fX * viewport100.width;
                double dLat = fY * viewport100.height;
                GeoRect newView = viewport100.Offset(dLong, dLat);
                SetViewNoAnimation(newView);
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F)
            {
                GeoRect selectionGPS;
                if (TryCalculateSelectionRectGPS(out selectionGPS))
                {
                    LookAt(selectionGPS);
                }
            }
        }

        private void HandleSelectRegion(Rect r)
        {
            if (!r.Contains(Event.current.mousePosition))
                return;
            if (!Event.current.isMouse)
                return;
            if (Event.current.button != 0)
                return;

            Vector2 mousePos = Event.current.mousePosition;
            Vector2 mousePosNormalized = Rect.PointToNormalized(r, mousePos);
            mousePosNormalized.y = 1 - mousePosNormalized.y;

            if (Event.current.type == EventType.MouseDown)
            {
                isDraggingSelection = true;
                m_selectionStartVp100 = new GeoPoint()
                {
                    x = Mathd.Lerp(m_viewport100.minX, m_viewport100.maxX, mousePosNormalized.x),
                    y = Mathd.Lerp(m_viewport100.minY, m_viewport100.maxY, mousePosNormalized.y)
                };
                m_selectionEndVp100 = m_selectionStartVp100;
            }
            else if (Event.current.type == EventType.MouseDrag)
            {
                if (isDraggingSelection)
                {
                    m_selectionEndVp100 = new GeoPoint()
                    {
                        x = Mathd.Lerp(m_viewport100.minX, m_viewport100.maxX, mousePosNormalized.x),
                        y = Mathd.Lerp(m_viewport100.minY, m_viewport100.maxY, mousePosNormalized.y)
                    };
                    if (Event.current.shift)
                    {
                        RectifySelection();
                    }
                }
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                if (isDraggingSelection)
                {
                    isDraggingSelection = false;
                    m_selectionEndVp100 = new GeoPoint()
                    {
                        x = Mathd.Lerp(m_viewport100.minX, m_viewport100.maxX, mousePosNormalized.x),
                        y = Mathd.Lerp(m_viewport100.minY, m_viewport100.maxY, mousePosNormalized.y)
                    };
                    if (Event.current.shift)
                    {
                        RectifySelection();
                    }

                    GeoRect selectionRectGPS;
                    if (TryCalculateSelectionRectGPS(out selectionRectGPS))
                    {
                        regionSelectedCallback?.Invoke(selectionRectGPS);
                    }
                }
            }
        }

        private void RectifySelection()
        {
            int sign = Math.Sign(m_selectionEndVp100.Value.x - m_selectionStartVp100.Value.x);
            double height = Math.Abs(m_selectionEndVp100.Value.y - m_selectionStartVp100.Value.y);
            GeoPoint newValue = new GeoPoint()
            {
                x = m_selectionStartVp100.Value.x + sign * height,
                y = m_selectionEndVp100.Value.y
            };
            m_selectionEndVp100 = newValue;
        }

        private void CalculateAnimationFactors(GeoRect newView)
        {
            GeoRect oldView = m_viewport100;

            double dWidth = System.Math.Abs(newView.width - oldView.width);
            double dHeight = System.Math.Abs(newView.height - oldView.height);
            if (dWidth > 0)
            {
                viewDx = dWidth / viewAnimDuration;
            }
            if (dHeight > 0)
            {
                viewDy = dHeight / viewAnimDuration;
            }
        }

        private void HandleTilesLoop()
        {
            MapViewTile firstTile = m_rootTiles[0];
            if (firstTile != null)
            {
                if (viewport100.minX < firstTile.bounds100.minX)
                {
                    MapViewTile newLeftTile = MapTileUtilities.CreateRoot<MapViewTile>();
                    newLeftTile.bounds100 = firstTile.bounds100.Offset(-200, 0);
                    m_rootTiles.Insert(0, newLeftTile);
                }
            }

            MapViewTile lastTile = m_rootTiles[m_rootTiles.Count - 1];
            if (lastTile != null)
            {
                if (viewport100.maxX > lastTile.bounds100.maxX)
                {
                    MapViewTile newRightTile = MapTileUtilities.CreateRoot<MapViewTile>();
                    newRightTile.bounds100 = lastTile.bounds100.Offset(200, 0);
                    m_rootTiles.Add(newRightTile);
                }
            }
        }

        public int CalculateZoomLevel()
        {
            float f = 200.0f / (float)viewport100.height;
            int z = Mathf.CeilToInt(Mathf.Log(f, 2)) + (int)m_tileQualityOffset;
            int minZoom = GetMinZoom(m_oldRect);
            int maxZoom = GetMaxZoom();
            z = Mathf.Clamp(z, minZoom, maxZoom);
            return z;
        }

        private void FreeTilesMemory()
        {
            List<Vector3Int> keys = MapViewTile.GetAllKeys();
            foreach(MapViewTile t in m_tilesToRender)
            {
                Vector3Int k = new Vector3Int(t.x, t.y, t.zoom);
                keys.Remove(k);
            }

            foreach(Vector3Int k in keys)
            {
                MapViewTile.Dispose(k);
            }
        }

        internal int GetMapMemStats()
        {
            int byteCount = MapViewTile.GetMemoryStats();
            return byteCount;
        }

        private void HandleGeometryChanged(Rect r)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (r != m_oldRect)
            {
                OnGeometryChanged(m_oldRect, r);
            }
            m_oldRect = r;
        }

        private void OnGeometryChanged(Rect oldRect, Rect newRect)
        {
            if (oldRect.width != 0 && oldRect.height != 0)
            {
                float heightChangeRatio = newRect.height / oldRect.height;
                float widthChangeRatio = newRect.width / oldRect.width;

                GeoRect view = viewport100;
                double avgLong = (view.minX + view.maxX) * 0.5;
                double avgLat = (view.minY + view.maxY) * 0.5;

                double dLong = view.width * widthChangeRatio * 0.5;
                double dLat = view.height * heightChangeRatio * 0.5;

                view.minX = avgLong - dLong;
                view.maxX = avgLong + dLong;
                view.minY = avgLat - dLat;
                view.maxY = avgLat + dLat;
                SetViewNoAnimation(view);
            }
            else
            {
                float aspect = newRect.width / newRect.height;
                GeoRect view = GeoRect.rect100;
                view.minX -= 180 * aspect;
                view.maxX += 180 * aspect;
                SetViewNoAnimation(view);
            }
        }

        private int GetMinZoom(Rect r)
        {
            float f = r.height / 256.0f;
            int z = Mathf.CeilToInt(Mathf.Log(f, 2));
            return z;
        }

        private int GetMaxZoom()
        {
            return m_tileProvider.maxZoom;
        }

        private void ValidateViewRegion()
        {
            GeoRect view = viewport100;
            double maxViewY = 100;

            double avgLong = (view.minX + view.maxX) * 0.5;
            double oldHeight = view.height;

            double dLatTop = view.maxY - maxViewY;
            if (dLatTop > 0)
            {
                view.maxY -= dLatTop;
                view.minY -= dLatTop;
            }

            double dLatBottom = -maxViewY - view.minY;
            if (dLatBottom > 0)
            {
                view.maxY += dLatBottom;
                view.minY += dLatBottom;
            }

            view.maxY = Mathd.Clamp(view.maxY, -maxViewY, maxViewY);
            view.minY = Mathd.Clamp(view.minY, -maxViewY, maxViewY);

            double heightChangeRatio = view.height / oldHeight;
            double newWidth = view.width * heightChangeRatio;
            view.minX = avgLong - newWidth * 0.5;
            view.maxX = avgLong + newWidth * 0.5;

            SetViewWithAnimation(view);
        }

        private void RenderAttribution(Rect r)
        {
            m_attributionDrawer.Draw(r);
        }

        public void LookAt(GeoRect gps)
        {
            float aspect = m_oldRect.width / m_oldRect.height;
            int zoom = GetMaxZoom();
            GeoRect wm = gps.GpsToWebMercator(zoom);
            GeoRect view100 = wm.WebMercatorToViewport100(zoom);
            double width = view100.height * aspect;
            view100.minX = view100.centerX - width * 0.5;
            view100.maxX = view100.centerX + width * 0.5;
            SetViewWithAnimation(view100);
        }

        private void SetViewWithAnimation(GeoRect newView)
        {
            CalculateAnimationFactors(newView);
            m_viewport100 = newView;
        }

        private void SetViewNoAnimation(GeoRect newView)
        {
            m_viewport100 = newView;
            m_animatedViewport100 = newView;
        }

        private void RenderHandles(Rect r)
        {
            if (interactionMode == MapInteractionMode.SelectRegion)
            {
                Vector2 mousePos = Event.current.mousePosition;
                Vector2 start, end;
                Handles.color = Color.white;
                GUI.BeginClip(r);
                Handles.BeginGUI();
                start = new Vector2(mousePos.x, r.min.y);
                end = new Vector2(mousePos.x, r.max.y);
                Handles.DrawDottedLine(start, end, 1);
                start = new Vector2(r.min.x, mousePos.y);
                end = new Vector2(r.max.x, mousePos.y);
                Handles.DrawDottedLine(start, end, 1);
                Handles.EndGUI();
                GUI.EndClip();
            }

            GeoRect selectionRect;
            if (TryCalculateSelectionRectVp100(out selectionRect))
            {
                if (m_quadVertices == null)
                {
                    m_quadVertices = new Vector2[4];
                }

                GeoRect.CalculateNormalizedQuadNonAlloc(viewport100, selectionRect, m_quadVertices);
                m_quadVertices[0].y = 1 - m_quadVertices[0].y;
                m_quadVertices[1].y = 1 - m_quadVertices[1].y;
                m_quadVertices[2].y = 1 - m_quadVertices[2].y;
                m_quadVertices[3].y = 1 - m_quadVertices[3].y;

                m_quadVertices[0] = Pinwheel.Vista.Utilities.NormalizedToPoint(r, m_quadVertices[0]);
                m_quadVertices[1] = Pinwheel.Vista.Utilities.NormalizedToPoint(r, m_quadVertices[1]);
                m_quadVertices[2] = Pinwheel.Vista.Utilities.NormalizedToPoint(r, m_quadVertices[2]);
                m_quadVertices[3] = Pinwheel.Vista.Utilities.NormalizedToPoint(r, m_quadVertices[3]);

                Handles.color = Color.red;
                GUI.BeginClip(r);
                Handles.BeginGUI();
                Handles.DrawAAPolyLine(3, m_quadVertices[0], m_quadVertices[1], m_quadVertices[2], m_quadVertices[3], m_quadVertices[0]);
                Handles.EndGUI();
                GUI.EndClip();

                if (TryCalculateSelectionRectGPS(out selectionRect))
                {
                    double widthKM, heightKM;
                    selectionRect.CalculateSizeApproxInKMs(out widthKM, out heightKM);
                    Vector2 labelPos;

                    if (widthKM > 0 && heightKM > 0)
                    {
                        Handles.color = Color.red;
                        GUI.BeginClip(r);
                        Handles.BeginGUI();
                        labelPos = (m_quadVertices[0] + m_quadVertices[3]) * 0.5f + Vector2.up * 10;
                        Handles.Label(labelPos, $"Approx {widthKM.ToString("0.0")} km", selectionApproxSizeLabelSize);
                        labelPos = (m_quadVertices[2] + m_quadVertices[3]) * 0.5f + Vector2.right * 10;
                        Handles.Label(labelPos, $"Approx {heightKM.ToString("0.0")} km", selectionApproxSizeLabelSize);
                        Handles.EndGUI();
                        GUI.EndClip();
                    }
                }
            }
        }

        public bool TryCalculateSelectionRectVp100(out GeoRect selectionRectVp100)
        {
            if (m_selectionStartVp100 != null && m_selectionEndVp100 != null)
            {
                double minX = Math.Min(m_selectionStartVp100.Value.x, m_selectionEndVp100.Value.x);
                double maxX = Math.Max(m_selectionStartVp100.Value.x, m_selectionEndVp100.Value.x);
                double minY = Math.Min(m_selectionStartVp100.Value.y, m_selectionEndVp100.Value.y);
                double maxY = Math.Max(m_selectionStartVp100.Value.y, m_selectionEndVp100.Value.y);
                selectionRectVp100 = new GeoRect(minX, maxX, minY, maxY);
                return true;
            }
            else
            {
                selectionRectVp100 = default;
                return false;
            }
        }

        public bool TryCalculateSelectionRectGPS(out GeoRect selectionRectGPS)
        {
            GeoRect vp100;
            if (TryCalculateSelectionRectVp100(out vp100))
            {
                int zoom = CalculateZoomLevel();
                selectionRectGPS = vp100.Viewport100ToWebMercator(zoom).WebMercatorToGps(zoom);
                return true;
            }
            else
            {
                selectionRectGPS = default;
                return false;
            }
        }

        public void SetRegionSelectionGPS(GeoRect selectionGPS)
        {
            int zoom = CalculateZoomLevel();
            m_selectionStartVp100 = new GeoPoint(selectionGPS.minX, selectionGPS.minY).GpsToWebMercator(zoom).WebMercatorToViewport100(zoom);
            m_selectionEndVp100 = new GeoPoint(selectionGPS.maxX, selectionGPS.maxY).GpsToWebMercator(zoom).WebMercatorToViewport100(zoom);
        }
    }
}
#endif
