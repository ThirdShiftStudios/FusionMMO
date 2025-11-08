#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pinwheel.Vista;
using Pinwheel.Vista.RealWorldData;
using Pinwheel.Vista.Graph;
using Pinwheel.VistaEditor.Graph;

namespace Pinwheel.VistaEditor.RealWorldData
{
    [CustomEditor(typeof(RealWorldBiome))]
    public class RealWorldBiomeInspector : Editor
    {
        private RealWorldBiome m_instance;

        private static readonly string UNDO_NAME = $"Modify {typeof(RealWorldBiome).Name}";

        internal class Prefs
        {
            public static readonly string DEFERRED_UPDATE = "pinwheel.vista.rwb.deferredupdate";
            public static bool useDeferredUpdate;

            public static void Load()
            {
                useDeferredUpdate = EditorPrefs.GetBool(DEFERRED_UPDATE, false);
            }

            public static void Save()
            {
                EditorPrefs.SetBool(DEFERRED_UPDATE, useDeferredUpdate);
            }
        }

        private void OnEnable()
        {
            m_instance = target as RealWorldBiome; SceneView.duringSceneGui += DuringSceneGUI;
            Prefs.Load();
            WorldMapWindow.regionSelectedCallback += OnWorldMapWindowRegionSelected;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            Prefs.Save();
            WorldMapWindow.regionSelectedCallback -= OnWorldMapWindowRegionSelected;
        }

        public override void OnInspectorGUI()
        {
            DrawOrphanedBiomeWarningGUI();
            DrawGeneralGUI();
            DrawGenerationConfigsGUI();
            DrawBoundariesGUI();
            DrawDataProvidersGUI();
        }

        private void DrawOrphanedBiomeWarningGUI()
        {
            VistaManager vm = m_instance.GetVistaManagerInstance();
            if (vm == null)
            {
                EditorCommon.DrawWarning("This biome must be a child of a Vista Manager instance, otherwise it won't take effect.");
            }
        }

        private class GeneralGUI
        {
            public static readonly string ID = "pinwheel.vista.rwb.general";
            public static readonly GUIContent HEADER = new GUIContent("General");
            public static readonly GUIContent ORDER = new GUIContent("Order", "The order of this biome among others, used for biomes sorting");
            public static readonly GUIContent TERRAIN_GRAPH = new GUIContent("Terrain Graph", "The Terrain Graph Asset used for generating this biome");
            public static readonly GUIContent EDIT_GRAPH = new GUIContent("Edit");

            public static readonly string NULL_GRAPH_WARNING = "You need to assign a Terrain Graph asset.";
        }

        private void DrawGeneralGUI()
        {
            if (EditorCommon.BeginFoldout(GeneralGUI.ID, GeneralGUI.HEADER, null, true))
            {
                EditorGUI.BeginChangeCheck();
                int order = m_instance.order;

                EditorGUILayout.BeginHorizontal();
                TerrainGraph terrainGraph = EditorGUILayout.ObjectField(GeneralGUI.TERRAIN_GRAPH, m_instance.terrainGraph, typeof(TerrainGraph), false) as TerrainGraph;
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_instance, $"Modify {m_instance.name}");
                    EditorUtility.SetDirty(m_instance);
                    m_instance.order = order;
                    m_instance.terrainGraph = terrainGraph;
                    if (!Prefs.useDeferredUpdate)
                    {
                        MarkChangedAndGenerate();
                    }
                }

                GUI.enabled &= m_instance.terrainGraph != null;
                if (GUILayout.Button(GeneralGUI.EDIT_GRAPH, GUILayout.Width(50)))
                {
                    if (m_instance.terrainGraph != null)
                    {
                        float maxHeight = 2000;
                        VistaManager vm = m_instance.GetComponentInParent<VistaManager>();
                        if (vm != null)
                        {
                            maxHeight = vm.terrainMaxHeight;
                        }

                        Bounds worldBounds = m_instance.worldBounds;
                        TerrainGenerationConfigs configs = TerrainGenerationConfigs.Create();
                        configs.resolution = (int)m_instance.inSceneWidth;
                        configs.seed = m_instance.seed;
                        configs.terrainHeight = maxHeight;
                        configs.worldBounds = new Rect(worldBounds.min.x, worldBounds.min.z, worldBounds.size.x, worldBounds.size.z);
                        m_instance.terrainGraph.debugConfigs = configs;

                        RWBInputProvider inputProvider = new RWBInputProvider(m_instance);
                        CoroutineUtility.StartCoroutine(IDownloadSampleData(inputProvider));
                        GraphEditorBase graphEditor = GraphEditorBase.OpenGraph(m_instance.terrainGraph, inputProvider);
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
                if (terrainGraph == null)
                {
                    EditorCommon.DrawWarning(GeneralGUI.NULL_GRAPH_WARNING, true);
                }
            }
            EditorCommon.EndFoldout();
        }

        private IEnumerator IDownloadSampleData(RWBInputProvider inputProvider)
        {
            int progressId = Progress.Start("Downloading Real World Biome Sample Data");
            if (m_instance.heightMapProviderAsset != null)
            {
                DataRequest r = m_instance.heightMapProviderAsset.RequestHeightMap(m_instance.realWorldBoundsGPS);
                while (!r.isCompleted)
                {
                    Progress.Report(progressId, r.progress, "Downloading height map");
                    yield return null;
                }

                if (Vista.RealWorldData.Utilities.IsTextureDataValid(r.heightMapData, r.heightMapSize))
                {
                    inputProvider.heightMapData = r.heightMapData;
                    inputProvider.heightMapSize = r.heightMapSize;
                }
            }

            if (m_instance.colorMapProviderAsset != null)
            {
                DataRequest r = m_instance.colorMapProviderAsset.RequestColorMap(m_instance.realWorldBoundsGPS);
                while (!r.isCompleted)
                {
                    Progress.Report(progressId, r.progress, "Downloading color map");
                    yield return null;
                }

                if (Vista.RealWorldData.Utilities.IsTextureDataValid(r.colorMapData, r.colorMapSize))
                {
                    inputProvider.colorMapData = r.colorMapData;
                    inputProvider.colorMapSize = r.colorMapSize;
                }
            }

            Progress.Remove(progressId);
            yield return null;
        }

        private class GenerationConfigsGUI
        {
            public static readonly string ID = "pinwheel.vista.rwb.generationconfigs";
            public static readonly GUIContent HEADER = new GUIContent("Generation Configs");
            public static readonly GUIContent SPACE = new GUIContent("Space", "The coordinate for the generation. World space will affect some nodes (noise, etc) depends on the biome position, while Local space will not.");
            public static readonly GUIContent DATA_MASK = new GUIContent("Data Mask", $"Filter out the biome output where unnecessary data will be ignored. For example, if you uncheck {BiomeDataMask.HeightMap} flag, the graph won't output height data even when you have added a {ObjectNames.NicifyVariableName(typeof(HeightOutputNode).Name)}");
            public static readonly GUIContent BASE_RESOLUTION = new GUIContent("Base Resolution", "Base resolution for generated textures to inherit from. Final result will depends on the graph.");
            public static readonly GUIContent PPM = new GUIContent("Pixel Per Meter", "The number of pixels to cover 1 meter in world space, calculated based on the Base Resolution and the biome anchors. Higher value means higher quality but uses more VRAM.");
            public static readonly GUIContent SEED = new GUIContent("Seed", "An integer to randomize the result");
            public static readonly GUIContent COLLECT_SCENE_HEIGHT = new GUIContent("Collect Scene Height", $"Should it collect height data from the scene and feed to the graph as input? The input name is {GraphConstants.SCENE_HEIGHT_INPUT_NAME}");

            public static readonly string DATA_MASK_WARNING = "Nothing? Are you sure?";
            public static readonly string SCENE_HEIGHT_WARNING = $"There is no Input Node with the variable name of \"{GraphConstants.SCENE_HEIGHT_INPUT_NAME}\", consider to turn this checkbox off to improve its performance.";


        }

        private void DrawGenerationConfigsGUI()
        {
            if (EditorCommon.BeginFoldout(GenerationConfigsGUI.ID, GenerationConfigsGUI.HEADER, null, true))
            {
                EditorGUI.BeginChangeCheck();
                GUI.enabled = false;
                EditorGUILayout.EnumPopup(GenerationConfigsGUI.SPACE, Space.World);
                GUI.enabled = true;
                BiomeDataMask dataMask = (BiomeDataMask)EditorGUILayout.EnumFlagsField(GenerationConfigsGUI.DATA_MASK, m_instance.dataMask);
                if (dataMask == 0)
                {
                    EditorCommon.DrawWarning(GenerationConfigsGUI.DATA_MASK_WARNING, true);
                }
                int seed = EditorGUILayout.DelayedIntField(GenerationConfigsGUI.SEED, m_instance.seed);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_instance, $"Modify {m_instance.name}");
                    m_instance.dataMask = dataMask;
                    m_instance.seed = seed;
                    if (!Prefs.useDeferredUpdate)
                    {
                        MarkChangedAndGenerate();
                    }
                }
            }
            EditorCommon.EndFoldout();
        }


        private class BoundariesGUI
        {
            public static readonly string ID = "pinwheel.vista.rwb.boundaries";
            public static readonly GUIContent HEADER = new GUIContent("Boundaries");
            public static readonly GUIContent REAL_WORLD_GPS = new GUIContent("Real World GPS");
            public static readonly GUIContent W = new GUIContent("W", "The west gps coordinate");
            public static readonly GUIContent E = new GUIContent("E", "The east gps coordinate");
            public static readonly GUIContent S = new GUIContent("S", "The south gps coordinate");
            public static readonly GUIContent N = new GUIContent("N", "The north gps coordinate");
            public static readonly GUIContent PICK_FROM_MAP = new GUIContent("Select region from Map", Resources.Load<Texture2D>("Vista/Textures/MapIcon"));
            public static readonly GUIContent PICK_FROM_BOOKMARKS = new GUIContent("Select region from Bookmarks");

            public static readonly GUIContent IN_SCENE_WIDTH = new GUIContent("In Scene Width");
            public static readonly GUIContent IN_SCENE_LENGTH = new GUIContent("In Scene Length");

            public static int worldMapWindowSessionId { get; set; }
        }

        private void DrawBoundariesGUI()
        {
            if (EditorCommon.BeginFoldout(BoundariesGUI.ID, BoundariesGUI.HEADER, null, true))
            {
                EditorGUI.BeginChangeCheck();
                GeoRect gps = m_instance.realWorldBoundsGPS;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(BoundariesGUI.REAL_WORLD_GPS);
                EditorGUILayout.BeginVertical();
                using (IndentScope iScope = new IndentScope(0))
                {
                    using (LabelWidthScope lwScope = new LabelWidthScope(18))
                    {
                        gps.minX = EditorGUILayout.DoubleField(BoundariesGUI.W, gps.minX);
                        gps.maxX = EditorGUILayout.DoubleField(BoundariesGUI.E, gps.maxX);
                        gps.minY = EditorGUILayout.DoubleField(BoundariesGUI.S, gps.minY);
                        gps.maxY = EditorGUILayout.DoubleField(BoundariesGUI.N, gps.maxY);
                    }
                    Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    if (GUI.Button(r, BoundariesGUI.PICK_FROM_MAP))
                    {
                        BoundariesGUI.worldMapWindowSessionId = Random.Range(0, int.MaxValue);
                        WorldMapWindow worldMapWindow = WorldMapWindow.ShowWindow(BoundariesGUI.worldMapWindowSessionId, m_instance.realWorldBoundsGPS);
                        CoroutineUtility.StartCoroutine(LookAtDelay(worldMapWindow, m_instance.realWorldBoundsGPS.Scale(2.5)));
                    }
                    if (MapBookmarks.HasBookmarks())
                    {
                        r = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                        if (GUI.Button(r, BoundariesGUI.PICK_FROM_BOOKMARKS))
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
                                        m_instance.realWorldBoundsGPS = b.coordinates;
                                    });
                            }
                            menu.DropDown(r);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                float inSceneWidth = EditorGUILayout.FloatField(BoundariesGUI.IN_SCENE_WIDTH, m_instance.inSceneWidth);
                float inSceneLength = EditorGUILayout.FloatField(BoundariesGUI.IN_SCENE_LENGTH, m_instance.inSceneLength);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_instance, UNDO_NAME);
                    m_instance.realWorldBoundsGPS = gps;
                    m_instance.inSceneWidth = inSceneWidth;
                    m_instance.inSceneLength = inSceneLength;
                }
            }
            EditorCommon.EndFoldout();
        }

        private IEnumerator LookAtDelay(WorldMapWindow window, GeoRect longlat)
        {
            yield return null;
            window.mapView.LookAt(longlat);
        }

        private void OnWorldMapWindowRegionSelected(int sessionId, GeoRect selectionRect)
        {
            if (sessionId != BoundariesGUI.worldMapWindowSessionId)
                return;

            Undo.RecordObject(m_instance, "Region Selection Changed");
            EditorUtility.SetDirty(m_instance);
            m_instance.realWorldBoundsGPS = selectionRect;
        }

        private class DataProvidersGUI
        {
            public static readonly string ID = "pinwheel.vista.rwb.dataproviders";
            public static readonly GUIContent HEADER = new GUIContent("Data Providers");
            public static readonly GUIContent HEIGHT_MAP_PROVIDER = new GUIContent("Height Map Provider", $"The data provider asset that will be used as a template to download height map. Downloaded height map will be fed into the graph using {Pinwheel.Vista.RealWorldData.Graph.GraphConstants.REAL_WORLD_HEIGHT_INPUT_NAME} input");
            public static readonly GUIContent COLOR_MAP_PROVIDER = new GUIContent("Color Map Provider", $"The data provider asset that will be used as a template to download height map. Downloaded color map will be fed into the graph using {Pinwheel.Vista.RealWorldData.Graph.GraphConstants.REAL_WORLD_COLOR_INPUT_NAME} input");

            public static string GetHeightMapProviderWarning(DataProviderAsset providerAsset)
            {
                if (!providerAsset.provider.availability.HasFlag(DataAvailability.HeightMap))
                {
                    string s = $"Looks like {providerAsset.GetType().Name} doesn't support height map";
                    return s;
                }
                return null;
            }

            public static string GetColorMapProviderWarning(DataProviderAsset providerAsset)
            {
                if (!providerAsset.provider.availability.HasFlag(DataAvailability.ColorMap))
                {
                    string s = $"Looks like {providerAsset.GetType().Name} doesn't support color map";
                    return s;
                }
                return null;
            }
        }

        private void DrawDataProvidersGUI()
        {
            if (EditorCommon.BeginFoldout(DataProvidersGUI.ID, DataProvidersGUI.HEADER))
            {
                EditorGUI.BeginChangeCheck();
                DataProviderAsset heightMapProvider = EditorGUILayout.ObjectField(DataProvidersGUI.HEIGHT_MAP_PROVIDER, m_instance.heightMapProviderAsset, typeof(DataProviderAsset), false) as DataProviderAsset;
                if (heightMapProvider != null)
                {
                    string warning = DataProvidersGUI.GetHeightMapProviderWarning(heightMapProvider);
                    if (!string.IsNullOrEmpty(warning))
                    {
                        EditorCommon.DrawWarning(warning, true);
                    }
                }

                DataProviderAsset colorMapProvider = EditorGUILayout.ObjectField(DataProvidersGUI.COLOR_MAP_PROVIDER, m_instance.colorMapProviderAsset, typeof(DataProviderAsset), false) as DataProviderAsset;
                if (colorMapProvider != null)
                {
                    string warning = DataProvidersGUI.GetColorMapProviderWarning(colorMapProvider);
                    if (!string.IsNullOrEmpty(warning))
                    {
                        EditorCommon.DrawWarning(warning, true);
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_instance, UNDO_NAME);
                    EditorUtility.SetDirty(m_instance);
                    m_instance.heightMapProviderAsset = heightMapProvider;
                    m_instance.colorMapProviderAsset = colorMapProvider;
                }
            }
            EditorCommon.EndFoldout();
        }

        public void MarkChangedAndGenerate()
        {
            EditorUtility.SetDirty(m_instance);
            if (m_instance.terrainGraph != null)
            {
                m_instance.CleanUp();
                m_instance.MarkChanged();
                m_instance.GenerateBiomesInGroup();
            }
        }

        private class SceneGUI
        {
            public static readonly Color LINE_COLOR = Color.red;
            public static readonly float LINE_WIDTH = 5;
        }

        private void DuringSceneGUI(SceneView sv)
        {
            DrawWorldBounds();
            DrawOverlappedTiles();
        }

        private void DrawWorldBounds()
        {
            Bounds b = m_instance.worldBounds;
            Vector3 v0 = new Vector3(b.min.x, b.min.y, b.min.z);
            Vector3 v1 = new Vector3(b.min.x, b.min.y, b.max.z);
            Vector3 v2 = new Vector3(b.max.x, b.min.y, b.max.z);
            Vector3 v3 = new Vector3(b.max.x, b.min.y, b.min.z);

            Handles.color = SceneGUI.LINE_COLOR;
            Handles.DrawAAPolyLine(SceneGUI.LINE_WIDTH, v0, v1, v2, v3, v0);
        }

        private void DrawOverlappedTiles()
        {
            VistaManager manager = m_instance.GetComponentInParent<VistaManager>();
            if (manager == null)
                return;
            List<ITile> tiles = manager.GetTiles();
            using (new HandleScope(new Color(0, 1, 1, 0.5f), UnityEngine.Rendering.CompareFunction.LessEqual))
            {
                foreach (ITile t in tiles)
                {
                    Bounds bounds = t.worldBounds;
                    if (m_instance.IsOverlap(bounds))
                    {
                        Handles.DrawWireCube(bounds.center, bounds.size);
                    }
                }
            }
        }
    }
}
#endif
