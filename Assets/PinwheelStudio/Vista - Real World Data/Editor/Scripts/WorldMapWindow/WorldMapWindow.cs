#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using Pinwheel.Vista.RealWorldData;
using Pinwheel.Vista.RealWorldData.USGS;

namespace Pinwheel.VistaEditor.RealWorldData
{
    public partial class WorldMapWindow : EditorWindow
    {
        public delegate void RegionSelectedHandler(int sessionId, GeoRect selectionRectGPS);
        public static event RegionSelectedHandler regionSelectedCallback;

        private static readonly string USS_PATH = "Vista/USS/WorldMapWindow";

        public VisualElement taskbar { get; private set; }
        public MapView mapView { get; private set; }
        private UtilityButton m_helpButton;
        private UtilityButton m_bookmarkButton;
        private SelectRegionButton m_selectRegionButton;
        private Label m_mouseLocationLabel;

        private IMGUIContainer m_debugIMGUI;

        public int sessionId { get; set; }

        public static WorldMapWindow ShowWindow(int sessionId = int.MinValue, GeoRect currentSelection = default)
        {
            WorldMapWindow window = EditorWindow.CreateInstance<WorldMapWindow>();
            window.titleContent = new GUIContent("World Map");
            window.sessionId = sessionId;
            window.Show();
            window.SetRegionSelectionGPS(currentSelection);
            return window;
        }

        public static void ShowWindow()
        {
            ShowWindow(0);
        }

        public void OnEnable()
        {
            SetupGUI();
            EditorApplication.update += OnEditorUpdate;
        }

        public void OnDisable()
        {
            TearDownGUI();
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            rootVisualElement.MarkDirtyRepaint();
        }

        private void SetupGUI()
        {
            StyleSheet uss = Resources.Load<StyleSheet>(USS_PATH);
            rootVisualElement.styleSheets.Add(uss);

            mapView = new MapView(new USGSImageryTileProvider(), new USGSAttributionDrawer()) { name = "map-view" };
            mapView.AddTo(rootVisualElement);
            mapView.RegisterCallback<MouseMoveEvent>(OnMouseMoveOnMapView);
            mapView.OnEnable();

            MapView.regionSelectedCallback += OnRegionSelectedGPS;

            taskbar = new VisualElement() { name = "taskbar" };
            taskbar.pickingMode = PickingMode.Ignore;
            taskbar.AddTo(rootVisualElement);

            m_debugIMGUI = new IMGUIContainer(OnDebugIMGUI) { name = "debug-imgui" };
            m_debugIMGUI.AddTo(rootVisualElement);

            m_helpButton = new UtilityButton() { name = "help-button", text = "?" };
            TextAsset helpTextAsset = Resources.Load<TextAsset>("Vista/Texts/WorldMapHelpText");
            if (helpTextAsset != null)
            {
                m_helpButton.tooltip = helpTextAsset.text;
            }
            m_helpButton.AddTo(taskbar);

            m_bookmarkButton = new UtilityButton() { name = "bookmark-button", text = "@", tooltip = "Bookmark this area" };
            m_bookmarkButton.AddTo(taskbar);
            m_bookmarkButton.clicked += OnBookmarkButtonClicked;

            m_selectRegionButton = new SelectRegionButton() { name = "select-region-button" };
            m_selectRegionButton.clicked += OnSelectRegionButtonClicked;
            m_selectRegionButton.AddTo(taskbar);

            m_mouseLocationLabel = new Label() { name = "mouse-location-label", text = "Long Lat" };
            m_mouseLocationLabel.AddTo(rootVisualElement);

            rootVisualElement.MarkDirtyRepaint();
        }

        private void OnMouseMoveOnMapView(MouseMoveEvent evt)
        {
            if (mapView.resolvedStyle == null)
                return;
            if (m_mouseLocationLabel == null)
                return;

            Vector2 mousePos = evt.localMousePosition;
            float dx = Mathf.InverseLerp(0, mapView.resolvedStyle.width, mousePos.x);
            float dy = 1 - Mathf.InverseLerp(0, mapView.resolvedStyle.height, mousePos.y);
            int zoom = mapView.CalculateZoomLevel();
            GeoRect vp100 = mapView.viewRegion100;
            GeoPoint mouseLocationVp100 = new GeoPoint(
                Mathd.Lerp(vp100.minX, vp100.maxX, dx),
                Mathd.Lerp(vp100.minY, vp100.maxY, dy));
            GeoPoint mouseLocationGPS = mouseLocationVp100.Viewport100ToWebMercator(zoom).WebMercatorToGps(zoom).ValidateGPS();
            m_mouseLocationLabel.text = $"Longitude: {mouseLocationGPS.x}, Latitude: {mouseLocationGPS.y}";
        }

        private void OnRegionSelectedGPS(GeoRect selectionRect)
        {
            regionSelectedCallback?.Invoke(sessionId, selectionRect.ValidateGPS());
        }

        private void OnDebugIMGUI()
        {
            //int zoom = mapView.m_mapViewIMGUI.CalculateZoomLevel();
            //EditorGUILayout.SelectableLabel("Viewport 100: " + mapView.viewRegion100);
            //EditorGUILayout.SelectableLabel("GPS: " + mapView.viewRegion100.Viewport100ToWebMercator(zoom).WebMercatorToGps(zoom));
            //GeoRect selectionRegion;
            //if (mapView.m_mapViewIMGUI.TryCalculateSelectionRectGPS(out selectionRegion))
            //{
            //    EditorGUILayout.SelectableLabel("Selection: " + selectionRegion);
            //    GeoRect wb = selectionRegion.GpsToWebMercator(31);
            //    EditorGUILayout.SelectableLabel("Selection WM: " + wb.ToString());
            //}

            //EditorGUILayout.LabelField("Zoom: " + zoom);
            //EditorGUILayout.LabelField("Interaction mode: " + mapView.interactionMode.ToString());
        }

        private void TearDownGUI()
        {
            if (mapView != null)
            {
                mapView.OnDisable();
            }
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Clear();
        }

        private void OnSelectRegionButtonClicked()
        {
            if (mapView != null)
            {
                MapInteractionMode currentMode = mapView.interactionMode;
                if (currentMode != MapInteractionMode.SelectRegion)
                {
                    currentMode = MapInteractionMode.SelectRegion;
                    mapView.interactionMode = currentMode;
                    m_selectRegionButton.text = "Done";
                }
                else
                {
                    currentMode = MapInteractionMode.Movement;
                    mapView.interactionMode = currentMode;
                    m_selectRegionButton.text = "Select a region";
                }
            }
        }

        public void SetRegionSelectionGPS(GeoRect selection)
        {
            mapView.SetRegionSelectionGPS(selection);
        }

        private void OnBookmarkButtonClicked()
        {
            GeoRect selectionGPS;
            if (mapView.m_mapViewIMGUI.TryCalculateSelectionRectGPS(out selectionGPS))
            {
                Rect rect = m_bookmarkButton.worldBound;
                rect.position += this.position.position + Vector2.up * 8;
                AddBookmarkWindow.Show(selectionGPS, rect);
            }
        }
    }
}
#endif
