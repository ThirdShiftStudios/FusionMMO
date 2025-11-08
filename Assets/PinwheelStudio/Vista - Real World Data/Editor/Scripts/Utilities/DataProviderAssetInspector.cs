#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.Vista.RealWorldData;
using Pinwheel.Vista;
using Pinwheel.Vista.RealWorldData.USGS;

namespace Pinwheel.VistaEditor.RealWorldData
{
    [CustomEditor(typeof(DataProviderAsset), true, isFallback = true)]
    public class DataProviderAssetInspector : Editor
    {
        private class GUIContents
        {
            public readonly GUIContent AVAILABLE_DATA_HEADER = new GUIContent("Available Data", "Types of data that can be downloaded from this provider");
            public readonly GUIContent USGS_REGION_NOTE = new GUIContent("Make sure to select a region within the USA");
            public readonly GUIContent UNKNOWN = new GUIContent("Unknown");
            public readonly GUIContent ACTION_HEADER = new GUIContent("Action");
            public readonly string DOWNLOADING = "Downloading...";
            public readonly GUIContent DOWNLOAD = new GUIContent("Download");
            public readonly GUIContent OPEN_MAP = new GUIContent("Select region from Map", Resources.Load<Texture2D>("Vista/Textures/MapIcon"));
            public readonly GUIContent PICK_FROM_BOOKMARKS = new GUIContent("Select region from Bookmarks");
            public readonly GUIContent RESET_ON_ERROR_MSG = new GUIContent("Having errors? Click here to clear the progress!");
            public readonly GUIContent PREVIEW_HEADER = new GUIContent("Preview");
            public readonly GUIContent NOTE_HEADER = new GUIContent("Note");
            public readonly GUIContent NOTE = new GUIContent("Each data provider has its own Term of Service and Policies, it's your responsibility to carefully read and respect that if you decide to use the data.");
        }

        private DataProviderAsset m_instance;
        private int m_worldMapWindowSessionId;
        private GUIContents m_gui;

        private void OnEnable()
        {
            m_instance = target as DataProviderAsset;
            WorldMapWindow.regionSelectedCallback += OnWorldMapWindowRegionSelected;
            m_gui = new GUIContents();
        }

        private void OnDisable()
        {
            WorldMapWindow.regionSelectedCallback -= OnWorldMapWindowRegionSelected;
        }

        public override bool RequiresConstantRepaint()
        {
            bool isRequestingData = m_instance.dataRequest != null && !m_instance.dataRequest.isCompleted;
            return isRequestingData;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorCommon.Header(m_gui.AVAILABLE_DATA_HEADER);
            EditorGUI.indentLevel += 1;
            try
            {
                DataAvailability availability = m_instance.provider.availability;
                foreach (var v in System.Enum.GetValues(typeof(DataAvailability)))
                {
                    if (((int)availability & (int)v) == (int)v)
                    {
                        string s = ObjectNames.NicifyVariableName(System.Enum.GetName(typeof(DataAvailability), v));
                        EditorGUILayout.LabelField(s);
                    }
                }
                if (m_instance is USGSDataProviderAsset)
                {
                    EditorGUILayout.LabelField(m_gui.USGS_REGION_NOTE, EditorCommon.Styles.grayMiniLabel);
                }
            }
            catch (System.NotImplementedException)
            {
                EditorGUILayout.LabelField(m_gui.UNKNOWN);
            }
            EditorGUI.indentLevel -= 1;

            EditorCommon.Header(m_gui.ACTION_HEADER);
            Rect r;
            bool isRequestingData = m_instance.dataRequest != null && !m_instance.dataRequest.isCompleted;
            r = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            if (isRequestingData)
            {
                EditorGUI.ProgressBar(r, m_instance.dataRequest.progress, m_gui.DOWNLOADING);
            }
            else if (GUI.Button(r, m_gui.DOWNLOAD))
            {
                EditorUtility.SetDirty(m_instance);
                m_instance.RequestAndSaveAll();
            }
            r = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            if (GUI.Button(r, m_gui.OPEN_MAP))
            {
                m_worldMapWindowSessionId = Random.Range(0, int.MaxValue);
                WorldMapWindow worldMapWindow = WorldMapWindow.ShowWindow(m_worldMapWindowSessionId, m_instance.longLat);
                CoroutineUtility.StartCoroutine(LookAtDelay(worldMapWindow, m_instance.longLat.Scale(2.5)));
            }
            if (MapBookmarks.HasBookmarks())
            {
                r = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                if (GUI.Button(r, m_gui.PICK_FROM_BOOKMARKS))
                {
                    List<MapBookmarks.Bookmark> bookmarks = MapBookmarks.GetAll();
                    GenericMenu menu = new GenericMenu();
                    foreach (MapBookmarks.Bookmark b in bookmarks)
                    {
                        menu.AddItem(
                            new GUIContent(b.name),
                            false,
                            () =>
                            {
                                Undo.RecordObject(m_instance, $"Modify {m_instance.name}");
                                EditorUtility.SetDirty(m_instance);
                                m_instance.longLat = b.coordinates;
                            });
                    }
                    menu.DropDown(r);
                }
            }
            if (isRequestingData)
            {
                r = EditorGUILayout.GetControlRect(GUILayout.Height(20));                
                if (GUI.Button(r, m_gui.RESET_ON_ERROR_MSG, EditorCommon.Styles.centerGrayMiniLabel))
                {
                    isRequestingData = false;
                    m_instance.dataRequest.Complete();
                }
            }

            EditorCommon.Header(m_gui.PREVIEW_HEADER);
            EditorGUILayout.BeginHorizontal();
            if (m_instance.heightMap != null)
            {
                Rect r0 = EditorGUILayout.GetControlRect(GUILayout.Width(256), GUILayout.Height(256));
                GUIContent heightMapContent = new GUIContent(m_instance.heightMap, $"{m_instance.heightMap.name} {m_instance.heightMap.width}x{m_instance.heightMap.height}");
                EditorGUI.LabelField(r0, heightMapContent);
            }

            if (m_instance.colorMap != null)
            {
                Rect r0 = EditorGUILayout.GetControlRect(GUILayout.Width(256), GUILayout.Height(256));
                GUIContent colorMapContent = new GUIContent(m_instance.colorMap, $"{m_instance.colorMap.name} {m_instance.colorMap.width}x{m_instance.colorMap.height}");
                EditorGUI.LabelField(r0, colorMapContent);
            }
            EditorGUILayout.EndHorizontal();

            EditorCommon.Header(m_gui.NOTE_HEADER);
            EditorGUILayout.LabelField(m_gui.NOTE, EditorStyles.wordWrappedLabel);
        }

        private IEnumerator LookAtDelay(WorldMapWindow window, GeoRect longlat)
        {
            yield return null;
            window.mapView.LookAt(longlat);
        }

        private void OnWorldMapWindowRegionSelected(int sessionId, GeoRect selectionRect)
        {
            if (sessionId != m_worldMapWindowSessionId)
                return;

            Undo.RecordObject(m_instance, "Region Selection Changed");
            EditorUtility.SetDirty(m_instance);
            m_instance.longLat = selectionRect;
        }
    }
}
#endif
