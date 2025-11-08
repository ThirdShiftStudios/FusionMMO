#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using System;
using UnityEngine.UIElements;
using Pinwheel.Vista.RealWorldData;

namespace Pinwheel.VistaEditor.RealWorldData
{
    public class MapView : IMGUIContainer, IDisposable
    {
        public delegate void RegionSelectedHandler(GeoRect selectionRectGPS);
        public static event RegionSelectedHandler regionSelectedCallback;

        internal MapViewIMGUI m_mapViewIMGUI;
        public GeoRect viewRegion100
        {
            get
            {
                return m_mapViewIMGUI.viewport100;
            }
        }

        internal int sessionId { get; set; }

        public MapInteractionMode interactionMode
        {
            get
            {
                return m_mapViewIMGUI.interactionMode;
            }
            set
            {
                m_mapViewIMGUI.interactionMode = value;
            }
        }

        public MapView(IImageTileProvider tileProvider, IMapAttributionDrawer attributionDrawer)
        {
            m_mapViewIMGUI = new MapViewIMGUI(tileProvider, attributionDrawer);
            this.onGUIHandler = OnGUI;

            MapViewIMGUI.regionSelectedCallback += OnRegionSelectedGPS;
        }

        public void OnEnable()
        {
            m_mapViewIMGUI.OnEnable();
        }

        public void OnDisable()
        {
            m_mapViewIMGUI.OnDisable();
        }

        private void OnGUI()
        {
            if (resolvedStyle == null)
                return;
            Rect r = this.localBound;
            m_mapViewIMGUI.OnGUI(r);
        }

        public void LookAt(GeoRect gps)
        {
            m_mapViewIMGUI.LookAt(gps);
        }

        private void OnRegionSelectedGPS(GeoRect selectionRect)
        {
            regionSelectedCallback?.Invoke(selectionRect);
        }

        public void SetRegionSelectionGPS(GeoRect selectionGPS)
        {
            m_mapViewIMGUI.SetRegionSelectionGPS(selectionGPS);
        }

        public int CalculateZoomLevel()
        {
            return m_mapViewIMGUI.CalculateZoomLevel();
        }
    }
}
#endif
