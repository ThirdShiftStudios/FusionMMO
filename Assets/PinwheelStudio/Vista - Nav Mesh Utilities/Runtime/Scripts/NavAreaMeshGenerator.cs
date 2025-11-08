#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pinwheel.Vista.NavMeshUtilities
{
    [ExecuteInEditMode]
    public class NavAreaMeshGenerator : MonoBehaviour
    {
        public delegate void SpawnNavAreaHandler(NavAreaMeshGenerator sender, GameObject navAreaObject);
        public static event SpawnNavAreaHandler spawnNavAreaCallback;

        [SerializeField]
        private string[] m_outputNames;
        public string[] outputNames
        {
            get
            {
                return m_outputNames;
            }
            set
            {
                m_outputNames = value;
            }
        }

        [SerializeField]
        private int m_navAreaIndex;
        public int navAreaIndex
        {
            get
            {
                return m_navAreaIndex;
            }
            set
            {
                m_navAreaIndex = Mathf.Clamp(value, 0, 31);
            }
        }

        [SerializeField]
        private bool m_useAutoMeshOptions;
        public bool useAutoMeshOptions
        {
            get
            {
                return m_useAutoMeshOptions;
            }
            set
            {
                m_useAutoMeshOptions = value;
            }
        }

        [SerializeField]
        private int m_meshMinSubdiv;
        public int meshMinSubdiv
        {
            get
            {
                return m_meshMinSubdiv;
            }
            set
            {
                m_meshMinSubdiv = Mathf.Clamp(value, 1, 10);
                m_meshMaxSubdiv = Mathf.Clamp(m_meshMaxSubdiv, m_meshMinSubdiv, 13);
            }
        }

        [SerializeField]
        private int m_meshMaxSubdiv;
        public int meshMaxSubdiv
        {
            get
            {
                return m_meshMaxSubdiv;
            }
            set
            {
                m_meshMaxSubdiv = Mathf.Clamp(value, m_meshMinSubdiv, 13);
            }
        }

        [SerializeField]
        private int m_meshChunkGridSize;
        public int meshChunkGridSize
        {
            get
            {
                return m_meshChunkGridSize;
            }
            set
            {
                m_meshChunkGridSize = Mathf.Max(1, value);
            }
        }

        private Dictionary<ITile, Texture2D> m_heightMapByTile;
        private const int SUBDIV_MAP_RESOLUTION = 512;

        private void Reset()
        {
            m_outputNames = new string[] { };
            m_navAreaIndex = 0;
            m_useAutoMeshOptions = true;
            m_meshMinSubdiv = 6;
            m_meshMaxSubdiv = 12;
            m_meshChunkGridSize = 5;
        }

        private void OnEnable()
        {
            VistaManager.beforeGenerating += OnBeforeGenerating;
            VistaManager.heightMapPopulated += OnPopulateHeightMap;
            VistaManager.genericTexturesPopulated += OnPopulateGenericTexture;
            VistaManager.afterGenerating += OnAfterGenerating;
        }

        private void OnDisable()
        {
            VistaManager.beforeGenerating -= OnBeforeGenerating;
            VistaManager.heightMapPopulated -= OnPopulateHeightMap;
            VistaManager.genericTexturesPopulated -= OnPopulateGenericTexture;
            VistaManager.afterGenerating -= OnAfterGenerating;
        }

        private void OnBeforeGenerating(VistaManager sender)
        {
            m_heightMapByTile = new Dictionary<ITile, Texture2D>();
            foreach (Transform child in transform)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private void OnPopulateHeightMap(VistaManager sender, ITile tile, RenderTexture texture)
        {
            Texture2D heightMap2D = new Texture2D(texture.width - 1, texture.height - 1, TextureFormat.RFloat, false, true);
            RenderTexture.active = texture;
            heightMap2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            heightMap2D.Apply();
            RenderTexture.active = null;
            m_heightMapByTile.Add(tile, heightMap2D);
        }

        private void OnPopulateGenericTexture(VistaManager sender, ITile tile, List<string> labels, List<RenderTexture> textures)
        {
            foreach (string n in m_outputNames)
            {
                int index = labels.FindIndex(l => { return string.Equals(l, n); });
                if (index >= 0)
                {
                    RenderTexture tex = textures[index];
                    GenerateMesh(tile, n, tex);
                }
            }
        }

        private void OnAfterGenerating(VistaManager sender)
        {
            foreach (Texture2D t in m_heightMapByTile.Values)
            {
                if (t != null)
                {
                    Object.DestroyImmediate(t);
                }
            }
            m_heightMapByTile = null;
        }

        private void GenerateMesh(ITile tile, string outputName, RenderTexture texture)
        {
            Texture2D heightMap = GetHeightMap(tile, texture.width);
            int resolution = heightMap.width;
            Texture2D holeMap = CopyHoleMap(texture, resolution);
            Texture2D subdivMap = CreateSubdivMap(heightMap, holeMap);

            MeshGenOptions options = MeshGenOptions.Create();
            options.width = tile.worldBounds.size.x;
            options.height = tile.worldBounds.size.y;
            options.length = tile.worldBounds.size.z;

            if (m_useAutoMeshOptions)
            {
                options.meshResolution = 13;
                options.meshBaseResolution = 5;
                options.chunkGridSize = Mathf.RoundToInt(Mathf.Max(options.width, options.length) / 100f);
            }
            else
            {
                options.meshBaseResolution = m_meshMinSubdiv;
                options.meshResolution = m_meshMaxSubdiv;
                options.chunkGridSize = m_meshChunkGridSize;
            }
            MeshGenResult result = new MeshGenResult();

            GenerateMeshes(options, result, heightMap, holeMap, subdivMap);
            GameObject navAreaObject = CreateGameObject(tile, outputName, result, options);
            spawnNavAreaCallback?.Invoke(this, navAreaObject);

            Object.DestroyImmediate(holeMap);
            Object.DestroyImmediate(subdivMap);
        }

        private Texture2D GetHeightMap(ITile tile, int defaultResolutionIfNull)
        {
            Texture2D heightMap;
            if (!m_heightMapByTile.TryGetValue(tile, out heightMap))
            {
                heightMap = new Texture2D(defaultResolutionIfNull, defaultResolutionIfNull, TextureFormat.RFloat, false, true);
                m_heightMapByTile.Add(tile, heightMap);
            }
            return heightMap;
        }

        private Texture2D CopyHoleMap(RenderTexture texture, int resolution)
        {
            Texture2D tex2D = new Texture2D(resolution, resolution, TextureFormat.RFloat, false, true);
            RenderTexture.active = texture;
            tex2D.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            tex2D.Apply();
            RenderTexture.active = null;
            return tex2D;
        }

        private Texture2D CreateSubdivMap(Texture2D heightMap, Texture2D holeMap)
        {
            Texture2D subDivisionMap = new Texture2D(SUBDIV_MAP_RESOLUTION, SUBDIV_MAP_RESOLUTION, TextureFormat.RGBA32, false);

            RenderTexture rt = new RenderTexture(SUBDIV_MAP_RESOLUTION, SUBDIV_MAP_RESOLUTION, 0, RenderTextureFormat.RFloat);
            Material mat = new Material(Shader.Find("Hidden/Vista/NavMeshUtilities/NavMeshGenSubdivMap"));
            mat.SetTexture("_HeightMap", heightMap);
            mat.SetTexture("_HoleMap", holeMap);

            Drawing.DrawQuad(rt, mat, 0);

            RenderTexture.active = rt;
            subDivisionMap.ReadPixels(new Rect(0, 0, SUBDIV_MAP_RESOLUTION, SUBDIV_MAP_RESOLUTION), 0, 0);
            subDivisionMap.Apply();
            RenderTexture.active = null;

            rt.Release();
            Object.DestroyImmediate(rt);

            return subDivisionMap;
        }

        public Chunk[] CreateChunks(MeshGenOptions options)
        {
            int gridSize = options.chunkGridSize;
            List<Chunk> chunks = new List<Chunk>();
            for (int z = 0; z < gridSize; ++z)
            {
                for (int x = 0; x < gridSize; ++x)
                {
                    Chunk chunk = new Chunk();
                    chunk.index = new Vector2(x, z);
                    chunk.terrainSize = options.size;
                    chunk.chunkGridSize = gridSize;
                    chunks.Add(chunk);
                }
            }

            for (int i = 0; i < chunks.Count; ++i)
            {
                Chunk currentChunk = chunks[i];

                Utilities.Fill(currentChunk.neighborChunks, null);
                for (int j = 0; j < chunks.Count; ++j)
                {
                    Chunk otherChunk = chunks[j];
                    if (otherChunk.index == currentChunk.index + Vector2.left)
                    {
                        currentChunk.neighborChunks[0] = otherChunk;
                    }
                    if (otherChunk.index == currentChunk.index + Vector2.up)
                    {
                        currentChunk.neighborChunks[1] = otherChunk;
                    }
                    if (otherChunk.index == currentChunk.index + Vector2.right)
                    {
                        currentChunk.neighborChunks[2] = otherChunk;
                    }
                    if (otherChunk.index == currentChunk.index + Vector2.down)
                    {
                        currentChunk.neighborChunks[3] = otherChunk;
                    }
                }
            }


            return chunks.ToArray();
        }

        public void GenerateMeshes(MeshGenOptions options, MeshGenResult result, Texture2D heightMap, Texture2D holeMap, Texture2D subdivMap)
        {
            Chunk[] chunks = CreateChunks(options);
            InitSubdivArrays(chunks, options);
            CreateBaseSubdivTree(chunks, options);
            SplitBaseTreeForDynamicPolygon(chunks, options, subdivMap);

            if (options.meshBaseResolution != options.meshResolution)
            {
                StitchSeam(chunks, options);
            }

            CountLeafNode(chunks, options);
            CreateVertex(chunks, options, heightMap, holeMap);
            UpdateMesh(chunks, result);
            CleanupSubdivArrays(chunks);

            result.Validate();
        }

        private void InitSubdivArrays(Chunk[] chunks, MeshGenOptions options)
        {
            foreach (Chunk c in chunks)
            {
                c.InitSubdivArrays(options.meshResolution);
            }
        }

        private void CreateBaseSubdivTree(
            Chunk[] chunks,
            MeshGenOptions options)
        {
            JobHandle[] jobHandles = new JobHandle[chunks.Length];
            for (int i = 0; i < chunks.Length; ++i)
            {
                CreateBaseTreeJob j = chunks[i].GetCreateBaseSubdivTreeJob(
                    options.meshBaseResolution,
                    options.meshResolution,
                    0);
                jobHandles[i] = j.Schedule();
            }
            JobUtilities.CompleteAll(jobHandles);
        }

        private void SplitBaseTreeForDynamicPolygon(
            Chunk[] chunks,
            MeshGenOptions options,
            Texture2D subdivMap)
        {
            JobHandle[] jobHandles = new JobHandle[chunks.Length];
            TextureNativeDataDescriptor<Color32> subdivMapDescriptor = new TextureNativeDataDescriptor<Color32>(subdivMap);
            for (int i = 0; i < chunks.Length; ++i)
            {
                SplitBaseTreeForDynamicPolygonJob j = chunks[i].GetSplitBaseTreeForDynamicPolygonJob(
                    options.meshBaseResolution,
                    options.meshResolution,
                    0,
                    subdivMapDescriptor);
                jobHandles[i] = j.Schedule();
            }
            JobUtilities.CompleteAll(jobHandles);
        }

        private void StitchSeam(
            Chunk[] chunks,
            MeshGenOptions options)
        {
            JobHandle[] jobHandles = new JobHandle[chunks.Length];
            int stitchSeamIteration = 0;
            int stitchSeamMaxIteration = 10;
            bool newVertexCreated = true;
            List<NativeArray<bool>> markers = new List<NativeArray<bool>>();

            while (newVertexCreated && stitchSeamIteration <= stitchSeamMaxIteration)
            {
                StitchSeamJob[] stitchJobs = new StitchSeamJob[chunks.Length];
                for (int i = 0; i < chunks.Length; ++i)
                {
                    Chunk c = chunks[i];

                    Chunk leftChunk = GetLeftNeighborChunk(c, options.chunkGridSize);
                    bool hasLeftMarkers = leftChunk != null;
                    NativeArray<bool> leftMarkers = hasLeftMarkers ? leftChunk.GetVertexMarker() : new NativeArray<bool>(1, Allocator.TempJob);
                    markers.Add(leftMarkers);

                    Chunk topChunk = GetTopNeighborChunk(c, options.chunkGridSize);
                    bool hasTopMarkers = topChunk != null;
                    NativeArray<bool> topMarkers = hasTopMarkers ? topChunk.GetVertexMarker() : new NativeArray<bool>(1, Allocator.TempJob);
                    markers.Add(topMarkers);

                    Chunk rightChunk = GetRightNeighborChunk(c, options.chunkGridSize);
                    bool hasRightMarkers = rightChunk != null;
                    NativeArray<bool> rightMarkers = hasRightMarkers ? rightChunk.GetVertexMarker() : new NativeArray<bool>(1, Allocator.TempJob);
                    markers.Add(rightMarkers);

                    Chunk bottomChunk = GetBottomNeighborChunk(c, options.chunkGridSize);
                    bool hasBottomMarkers = bottomChunk != null;
                    NativeArray<bool> bottomMarkers = hasBottomMarkers ? bottomChunk.GetVertexMarker() : new NativeArray<bool>(1, Allocator.TempJob);
                    markers.Add(bottomMarkers);

                    StitchSeamJob j = c.GetStitchSeamJob(
                        options.meshBaseResolution,
                        options.meshResolution,
                        hasLeftMarkers, leftMarkers,
                        hasTopMarkers, topMarkers,
                        hasRightMarkers, rightMarkers,
                        hasBottomMarkers, bottomMarkers
                        );
                    stitchJobs[i] = j;
                }
                for (int i = 0; i < stitchJobs.Length; ++i)
                {
                    jobHandles[i] = stitchJobs[i].Schedule();
                }

                JobUtilities.CompleteAll(jobHandles);

                stitchSeamIteration += 1;
                int tmp = 0;
                for (int i = 0; i < chunks.Length; ++i)
                {
                    tmp += chunks[i].GetGenerationMetadata(GeometryJobUtilities.METADATA_NEW_VERTEX_CREATED);
                }
                newVertexCreated = tmp > 0;
            }

            for (int i = 0; i < markers.Count; ++i)
            {
                NativeArrayUtilities.Dispose(markers[i]);
            }

            foreach (Chunk c in chunks)
            {
                c.CacheMarker0();
            }
        }

        private Chunk GetLeftNeighborChunk(Chunk c, int chunkGridSize)
        {
            int maxIndex = chunkGridSize - 1;
            Vector2 index = c.index;
            if (index.x > 0)
            {
                return c.neighborChunks[0];
            }
            else
            {
                return null;
            }
        }

        private Chunk GetTopNeighborChunk(Chunk c, int chunkGridSize)
        {
            int maxIndex = chunkGridSize - 1;
            Vector2 index = c.index;
            if (index.y < maxIndex)
            {
                return c.neighborChunks[1];
            }
            else
            {
                return null;
            }
        }

        private Chunk GetRightNeighborChunk(Chunk c, int chunkGridSize)
        {
            int maxIndex = chunkGridSize - 1;
            Vector2 index = c.index;
            if (index.x < maxIndex)
            {
                return c.neighborChunks[2];
            }
            else
            {
                return null;
            }
        }

        private Chunk GetBottomNeighborChunk(Chunk c, int chunkGridSize)
        {
            int maxIndex = chunkGridSize - 1;
            Vector2 index = c.index;
            if (index.y > 0)
            {
                return c.neighborChunks[3];
            }
            else
            {
                return null;
            }
        }

        private void CountLeafNode(
            Chunk[] chunks,
            MeshGenOptions options)
        {
            JobHandle[] jobHandles = new JobHandle[chunks.Length];
            for (int i = 0; i < chunks.Length; ++i)
            {
                CountLeafNodeJob j = chunks[i].GetCountLeafNodeJob(options.meshBaseResolution, options.meshResolution, 0);
                jobHandles[i] = j.Schedule();
            }
            JobUtilities.CompleteAll(jobHandles);
        }

        private void CreateVertex(
            Chunk[] chunks,
            MeshGenOptions options,
            Texture2D heightMap,
            Texture2D holeMap)
        {
            JobHandle[] jobHandles = new JobHandle[chunks.Length];
            TextureNativeDataDescriptor<float> heightMapDesc = new TextureNativeDataDescriptor<float>(heightMap);
            TextureNativeDataDescriptor<float> holeMapDesc = new TextureNativeDataDescriptor<float>(holeMap);
            for (int i = 0; i < chunks.Length; ++i)
            {
                CreateVertexJob j = chunks[i].GetCreateVertexJob(
                    options.meshBaseResolution,
                    options.meshResolution,
                    0,
                    0,
                    0,
                    false,
                    false,
                    heightMapDesc,
                    holeMapDesc,
                    heightMap.texelSize.x);

                jobHandles[i] = j.Schedule();
            }
            JobUtilities.CompleteAll(jobHandles);
        }

        private void UpdateMesh(
            Chunk[] chunks,
            MeshGenResult result)
        {
            for (int i = 0; i < chunks.Length; ++i)
            {
                chunks[i].UpdateMesh(result);
            }
        }

        private void CleanupSubdivArrays(Chunk[] chunks)
        {
            foreach (Chunk c in chunks)
            {
                c.CleanUpSubdivArrays();
            }
        }

        public GameObject CreateGameObject(ITile tile, string outputName, MeshGenResult meshes, MeshGenOptions options)
        {
            GameObject go = new GameObject($"~NavArea_{outputName}");
            go.transform.parent = this.transform;
            go.transform.position = tile.gameObject.transform.position;
            go.transform.rotation = tile.gameObject.transform.rotation;
            go.transform.localScale = tile.gameObject.transform.localScale;

            Material material = new Material(Shader.Find("UI/Default"));
            material.name = "Material";
            material.color = GetAreaColor(m_navAreaIndex);

            for (int x = 0; x < options.chunkGridSize; ++x)
            {
                for (int z = 0; z < options.chunkGridSize; ++z)
                {
                    Mesh meshLOD0 = meshes.GetMesh(x, z, 0);
                    if (meshLOD0 == null)
                        continue;

                    GameObject chunkLOD0 = new GameObject();
                    chunkLOD0.name = $"C({x},{z})";
                    chunkLOD0.transform.parent = go.transform;
                    chunkLOD0.transform.localPosition = new Vector3(0, 0, 0);
                    chunkLOD0.transform.localRotation = Quaternion.identity;
                    chunkLOD0.transform.localScale = Vector3.one;

                    MeshFilter mf0 = chunkLOD0.AddComponent<MeshFilter>();
                    mf0.sharedMesh = meshLOD0;

                    MeshRenderer mr0 = chunkLOD0.AddComponent<MeshRenderer>();
                    mr0.sharedMaterial = material;

#if UNITY_EDITOR
                    GameObjectUtility.SetStaticEditorFlags(chunkLOD0, StaticEditorFlags.NavigationStatic);
                    GameObjectUtility.SetNavMeshArea(chunkLOD0, m_navAreaIndex);
#endif
                }
            }

            return go;
        }

        #region Taken from UnityEditor.NavMeshEditorWindow
        private int Bit(int a, int b)
        {
            return (a & (1 << b)) >> b;
        }

        private Color GetAreaColor(int i)
        {
            if (i == 0)
            {
                return new Color(0f, 0.75f, 1f, 0.5f);
            }
            int num = (Bit(i, 4) + Bit(i, 1) * 2 + 1) * 63;
            int num2 = (Bit(i, 3) + Bit(i, 2) * 2 + 1) * 63;
            int num3 = (Bit(i, 5) + Bit(i, 0) * 2 + 1) * 63;
            return new Color((float)num / 255f, (float)num2 / 255f, (float)num3 / 255f, 0.5f);
        }
        #endregion
    }
}
#endif
