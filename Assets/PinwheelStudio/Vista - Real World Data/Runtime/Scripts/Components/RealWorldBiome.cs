#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.Graph;
using CoreGraphConstants = Pinwheel.Vista.Graph.GraphConstants;
using RWGraphConstants = Pinwheel.Vista.RealWorldData.Graph.GraphConstants;

namespace Pinwheel.Vista.RealWorldData
{
    [ExecuteInEditMode]
    [AddComponentMenu("Vista/Real World Biome (Experimental)")]
    [HelpURL("https://docs.google.com/document/d/1zRDVjqaGY2kh4VXFut91oiyVCUex0OV5lTUzzCSwxcY/edit#heading=h.tbau0ilxe5zn")]
    public class RealWorldBiome : MonoBehaviour, IBiome, IProceduralBiome
    {
        protected static HashSet<RealWorldBiome> s_allInstances = new HashSet<RealWorldBiome>();
        public static IEnumerable<RealWorldBiome> allInstances
        {
            get
            {
                return s_allInstances;
            }
        }

        [SerializeField]
        [HideInInspector]
        protected int m_order;
        public int order
        {
            get
            {
                return m_order;
            }
            set
            {
                m_order = value;
            }
        }

        [SerializeField]
        protected TerrainGraph m_terrainGraph;
        public TerrainGraph terrainGraph
        {
            get
            {
                return m_terrainGraph;
            }
            set
            {
                m_terrainGraph = value;
            }
        }

        [SerializeField]
        protected BiomeDataMask m_dataMask;
        public BiomeDataMask dataMask
        {
            get
            {
                return m_dataMask;
            }
            set
            {
                m_dataMask = value;
            }
        }

        [SerializeField]
        [HideInInspector]
        protected int m_seed;
        public int seed
        {
            get
            {
                return m_seed;
            }
            set
            {
                m_seed = value;
            }
        }

        protected long m_updateCounter;
        public long updateCounter
        {
            get
            {
                return m_updateCounter;
            }
            set
            {
                m_updateCounter = value;
            }
        }

        public BiomeBlendOptions blendOptions
        {
            get
            {
                return BiomeBlendOptions.Default();
            }
        }
               
        [SerializeField]
        [HideInInspector]
        internal string m_guid = System.Guid.NewGuid().ToString();

        [SerializeField]
        protected GeoRect m_realWorldBoundsGPS;
        public GeoRect realWorldBoundsGPS
        {
            get
            {
                return m_realWorldBoundsGPS;
            }
            set
            {
                m_realWorldBoundsGPS = value;
            }
        }

        [SerializeField]
        protected float m_inSceneWidth;
        public float inSceneWidth
        {
            get
            {
                return m_inSceneWidth;
            }
            set
            {
                m_inSceneWidth = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        protected float m_inSceneLength;
        public float inSceneLength
        {
            get
            {
                return m_inSceneLength;
            }
            set
            {
                m_inSceneLength = Mathf.Max(0, value);
            }
        }

        public Bounds worldBounds
        {
            get
            {
                return CalculateWorldBounds();
            }
        }

        [SerializeField]
        protected DataProviderAsset m_heighMapProviderAsset;
        public DataProviderAsset heightMapProviderAsset
        {
            get
            {
                return m_heighMapProviderAsset;
            }
            set
            {
                m_heighMapProviderAsset = value;
            }
        }

        [SerializeField]
        protected DataProviderAsset m_colorMapProviderAsset;
        public DataProviderAsset colorMapProviderAsset
        {
            get
            {
                return m_colorMapProviderAsset;
            }
            set
            {
                m_colorMapProviderAsset = value;
            }
        }

        internal Bounds? currentlyProcessingTileBounds { get; set; }

        public void Reset()
        {
            m_order = 0;
            m_terrainGraph = null;
            m_dataMask = (BiomeDataMask)(~0);
            m_seed = 0;

            m_realWorldBoundsGPS = GpsUtils.USA_PART_OF_COLORADO;
            m_inSceneWidth = 5000;
            m_inSceneLength = 5000;
        }

        protected void OnEnable()
        {
            s_allInstances.Add(this);
            GraphAsset.graphChanged += OnGraphChanged;
        }

        protected void OnDisable()
        {
            s_allInstances.Remove(this);
            GraphAsset.graphChanged -= OnGraphChanged;
            CleanUp();
        }

        protected void OnGraphChanged(GraphAsset graph)
        {
            if (graph != m_terrainGraph)
                return;
            CleanUp();
            this.MarkChanged();
            this.GenerateBiomesInGroup();
        }

        public static RealWorldBiome CreateInstanceInScene(VistaManager manager)
        {
            GameObject biomeGO = new GameObject("Real World Biome");
            RealWorldBiome biome = biomeGO.AddComponent<RealWorldBiome>();

            if (manager != null)
            {
                biome.transform.parent = manager.transform;
                biome.transform.localPosition = Vector3.zero;
                biome.transform.localRotation = Quaternion.identity;
                biome.transform.localScale = Vector3.one;
            }

            return biome;
        }

        public BiomeDataRequest RequestData(Bounds tileWorldBounds, int heightMapResolution, int textureResolution)
        {
            BiomeDataRequest request = new BiomeDataRequest();
            BiomeData data = new BiomeData();
            request.data = data;
            if (m_terrainGraph != null)
            {
                CoroutineUtility.StartCoroutine(RequestDataProgressive(request, tileWorldBounds, heightMapResolution, textureResolution));
                return request;
            }
            else
            {
                request.Complete();
                return request;
            }
        }

        private IEnumerator RequestDataProgressive(BiomeDataRequest request, Bounds tileWorldBounds, int heightMapResolution, int textureResolution)
        {
            currentlyProcessingTileBounds = tileWorldBounds;

            GeoRect tileGPS = CalculateTileGPS(tileWorldBounds);

            DataRequest heightMapRequest = null;
            if (m_heighMapProviderAsset != null && Utilities.HasActiveInput(m_terrainGraph, RWGraphConstants.REAL_WORLD_HEIGHT_INPUT_NAME))
            {
                heightMapRequest = m_heighMapProviderAsset.RequestHeightMap(tileGPS);
                yield return heightMapRequest;
            }

            DataRequest colorMapRequest = null;
            if (m_colorMapProviderAsset != null && Utilities.HasActiveInput(m_terrainGraph, RWGraphConstants.REAL_WORLD_COLOR_INPUT_NAME))
            {
                colorMapRequest = m_colorMapProviderAsset.RequestColorMap(tileGPS);
                yield return colorMapRequest;
            }

            RWBInputProvider inputProvider = new RWBInputProvider(this);
            if (heightMapRequest != null && Utilities.IsTextureDataValid(heightMapRequest.heightMapData, heightMapRequest.heightMapSize))
            {
                inputProvider.heightMapData = heightMapRequest.heightMapData;
                inputProvider.heightMapSize = heightMapRequest.heightMapSize;
            }
            if (colorMapRequest != null && Utilities.IsTextureDataValid(colorMapRequest.colorMapData, colorMapRequest.colorMapSize))
            {
                inputProvider.colorMapData = colorMapRequest.colorMapData;
                inputProvider.colorMapSize = colorMapRequest.colorMapSize;
            }
            GraphInputContainer inputContainer = new GraphInputContainer();
            inputProvider.SetInput(inputContainer);

            int baseRes = Pinwheel.Vista.Utilities.MultipleOf8(Mathf.Max(heightMapResolution, textureResolution));
            CoroutineUtility.StartCoroutine(TerrainGraphUtilities.RequestBiomeData(this, request, m_terrainGraph, tileWorldBounds, Space.World, baseRes, m_seed, inputContainer, m_dataMask, inputProvider.FillTerrainGraphArguments));
            yield return request;

            RenderTexture biomeMask = inputProvider.RemoveTexture(CoreGraphConstants.BIOME_MASK_INPUT_NAME);
            request.data.biomeMaskMap = biomeMask;

            inputProvider.CleanUp();
            request.Complete();
            currentlyProcessingTileBounds = null;
        }

        private GeoRect CalculateTileGPS(Bounds tileWorldBounds)
        {
            Bounds selfWorldBounds = worldBounds;
            double dxMin = Mathd.InverseLerp(selfWorldBounds.min.x, selfWorldBounds.max.x, tileWorldBounds.min.x);
            double dxMax = Mathd.InverseLerp(selfWorldBounds.min.x, selfWorldBounds.max.x, tileWorldBounds.max.x);
            double dzMin = Mathd.InverseLerp(selfWorldBounds.min.z, selfWorldBounds.max.z, tileWorldBounds.min.z);
            double dzMax = Mathd.InverseLerp(selfWorldBounds.min.z, selfWorldBounds.max.z, tileWorldBounds.max.z);

            double minLong = Mathd.Lerp(m_realWorldBoundsGPS.minX, m_realWorldBoundsGPS.maxX, dxMin);
            double maxLong = Mathd.Lerp(m_realWorldBoundsGPS.minX, m_realWorldBoundsGPS.maxX, dxMax);
            double minLat = Mathd.Lerp(m_realWorldBoundsGPS.minY, m_realWorldBoundsGPS.maxY, dzMin);
            double maxLat = Mathd.Lerp(m_realWorldBoundsGPS.minY, m_realWorldBoundsGPS.maxY, dzMax);

            return new GeoRect(minLong, maxLong, minLat, maxLat);
        }

        public bool IsOverlap(Bounds area)
        {
            Bounds biomeBounds = worldBounds;
            Rect biomeRect = new Rect(biomeBounds.min.x, biomeBounds.min.z, biomeBounds.size.x, biomeBounds.size.z);
            Rect areaRect = new Rect(area.min.x, area.min.z, area.size.x, area.size.z);
            return biomeRect.Overlaps(areaRect);
        }

        public void OnBeforeVMGenerate()
        {
        }

        public void OnAfterVMGenerate()
        {
        }

        public void CleanUp()
        {

        }

        protected Bounds CalculateWorldBounds()
        {
            Bounds worldBounds;

            float minX = transform.position.x;
            float minY = transform.position.y;
            float minZ = transform.position.z;
            float maxX = minX + inSceneWidth;
            float maxY = minY;
            float maxZ = minZ + inSceneLength;

            Vector3 center = new Vector3(minX + maxX, minY + maxY, minZ + maxZ) * 0.5f;
            Vector3 size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);

            worldBounds = new Bounds(center, size);

            return worldBounds;
        }
    }
}
#endif
