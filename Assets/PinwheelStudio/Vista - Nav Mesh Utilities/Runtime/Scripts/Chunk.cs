#if VISTA

using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;

namespace Pinwheel.Vista.NavMeshUtilities
{
    public class Chunk
    {
        public Vector2 index;
        public int chunkGridSize;
        public Vector3 terrainSize;

        private NativeArray<SubdivNode> subdivNodeNativeArray;
        private NativeArray<byte> subdivNodeCreationState;

        private NativeArray<Vector3> vertexNativeArray;

        private NativeArray<bool> vertexMarkerNativeArray;

        private NativeArray<int> generationMetadata;

        private Chunk[] m_neighborChunks;
        internal Chunk[] neighborChunks
        {
            get
            {
                if (m_neighborChunks == null || m_neighborChunks.Length != 4)
                {
                    m_neighborChunks = new Chunk[4];
                }
                return m_neighborChunks;
            }
            set
            {
                m_neighborChunks = value;
            }
        }

        public Rect GetUvRange()
        {
            int gridSize = chunkGridSize;
            Vector2 position = index / gridSize;
            Vector2 size = Vector2.one / gridSize;
            return new Rect(position, size);
        }

        private void RecalculateTangentIfNeeded(Mesh m)
        {
            if (m == null)
                return;
            m.RecalculateTangents();
        }

        internal int GetSubdivTreeNodeCount(int meshResolution)
        {
            int count = 0;
            for (int i = 0; i <= meshResolution; ++i)
            {
                count += GetSubdivTreeNodeCountForLevel(i);
            }
            return count;
        }

        internal int GetSubdivTreeNodeCountForLevel(int level)
        {
            return 2 * Mathf.FloorToInt(Mathf.Pow(2, level));
        }

        internal void InitMeshArrays()
        {
            int vertexCount = generationMetadata[0] * 3;
            vertexNativeArray = new NativeArray<Vector3>(vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        }

        internal void CleanUpMeshArrays()
        {
            NativeArrayUtilities.Dispose(vertexNativeArray);
        }

        internal void InitSubdivArrays(int meshResolution)
        {
            int treeNodeCount = GetSubdivTreeNodeCount(meshResolution);
            subdivNodeNativeArray = new NativeArray<SubdivNode>(treeNodeCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            subdivNodeCreationState = new NativeArray<byte>(treeNodeCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            generationMetadata = new NativeArray<int>(GeometryJobUtilities.METADATA_LENGTH, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int dimension = GeometryJobUtilities.VERTEX_MARKER_DIMENSION;
            vertexMarkerNativeArray = new NativeArray<bool>(dimension * dimension, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        }

        internal void CleanUpSubdivArrays()
        {
            NativeArrayUtilities.Dispose(subdivNodeNativeArray);
            NativeArrayUtilities.Dispose(subdivNodeCreationState);
            NativeArrayUtilities.Dispose(generationMetadata);
            NativeArrayUtilities.Dispose(vertexMarkerNativeArray);
        }

        internal void InitMarkers()
        {
            int dimension = GeometryJobUtilities.VERTEX_MARKER_DIMENSION;
            vertexMarkerNativeArray = new NativeArray<bool>(dimension * dimension, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        }

        internal void CleanupMarkers()
        {

            NativeArrayUtilities.Dispose(vertexMarkerNativeArray);
        }

        internal CreateBaseTreeJob GetCreateBaseSubdivTreeJob(
            int meshBaseResolution,
            int meshResolution,
            int lod)
        {
            CreateBaseTreeJob job = new CreateBaseTreeJob()
            {
                nodes = subdivNodeNativeArray,
                creationState = subdivNodeCreationState,
                vertexMarker = vertexMarkerNativeArray,
                metadata = generationMetadata,
                baseResolution = meshBaseResolution,
                resolution = meshResolution,
                lod = lod
            };

            return job;
        }

        internal SplitBaseTreeForDynamicPolygonJob GetSplitBaseTreeForDynamicPolygonJob(
            int meshBaseResolution, int meshResolution, int lod,
            TextureNativeDataDescriptor<Color32> subdivMap)
        {
            Rect uvRect = GetUvRange();
            SplitBaseTreeForDynamicPolygonJob job = new SplitBaseTreeForDynamicPolygonJob()
            {
                baseTree = subdivNodeNativeArray,
                creationState = subdivNodeCreationState,
                vertexMarker = vertexMarkerNativeArray,
                subdivMap = subdivMap,
                baseResolution = meshBaseResolution,
                resolution = meshResolution,
                lod = lod,
                uvRect = uvRect
            };

            return job;
        }

        internal StitchSeamJob GetStitchSeamJob(
            int meshBaseResolution,
            int meshResolution,
            bool hasLeftMarkers, NativeArray<bool> leftMarkers,
            bool hasTopMarkers, NativeArray<bool> topMarkers,
            bool hasRightMarkers, NativeArray<bool> rightMarkers,
            bool hasBottomMarkers, NativeArray<bool> bottomMarkers)
        {
            StitchSeamJob job = new StitchSeamJob()
            {
                nodes = subdivNodeNativeArray,
                creationState = subdivNodeCreationState,
                vertexMarker = vertexMarkerNativeArray,
                metadata = generationMetadata,
                meshBaseResolution = meshBaseResolution,
                meshResolution = meshResolution,

                hasLeftMarker = hasLeftMarkers,
                vertexMarkerLeft = leftMarkers,

                hasTopMarker = hasTopMarkers,
                vertexMarkerTop = topMarkers,

                hasRightMarker = hasRightMarkers,
                vertexMarkerRight = rightMarkers,

                hasBottomMarker = hasBottomMarkers,
                vertexMarkerBottom = bottomMarkers
            };

            return job;
        }

        internal void CopyVertexMarker(NativeArray<bool> markers)
        {
            if (vertexMarkerNativeArray.IsCreated)
            {
                markers.CopyTo(vertexMarkerNativeArray);
            }
        }

        internal CountLeafNodeJob GetCountLeafNodeJob(
            int meshBaseResolution,
            int meshResolution,
            int lod)
        {
            CountLeafNodeJob job = new CountLeafNodeJob()
            {
                creationState = subdivNodeCreationState,
                metadata = generationMetadata,
                baseResolution = meshBaseResolution,
                resolution = meshResolution,
                lod = lod
            };

            return job;
        }

        internal CreateVertexJob GetCreateVertexJob(
            int meshBaseResolution,
            int meshResolution,
            int lod,
            int displacementSeed,
            float displacementStrength,
            bool smoothNormal,
            bool mergeUv,
            TextureNativeDataDescriptor<float> heightMap,
            TextureNativeDataDescriptor<float> holeMap,
            float texelSize)
        {
            InitMeshArrays();

            Rect uvRect = GetUvRange();

            CreateVertexJob job = new CreateVertexJob()
            {
                nodes = subdivNodeNativeArray,
                creationState = subdivNodeCreationState,

                heightMap = heightMap,
                holeMap = holeMap,

                vertices = vertexNativeArray,
                metadata = generationMetadata,

                meshBaseResolution = meshBaseResolution,
                meshResolution = meshResolution,
                lod = lod,
                displacementSeed = displacementSeed,
                displacementStrength = displacementStrength,
                smoothNormal = smoothNormal,
                mergeUV = mergeUv,

                terrainSize = terrainSize,
                chunkUvRect = uvRect,
                texelSize = texelSize
            };
            return job;
        }

        internal void UpdateMesh(MeshGenResult result)
        {
            Mesh m = new Mesh();
            m.name = "~NavMeshArea";
            m.MarkDynamic();

            int leafCount = generationMetadata[GeometryJobUtilities.METADATA_LEAF_COUNT];
            int removedLeafCount = generationMetadata[GeometryJobUtilities.METADATA_LEAF_REMOVED];

            if (leafCount != removedLeafCount)
            {
                List<Vector3> verticesList = new List<Vector3>(vertexNativeArray.Length);
                List<int> indicesList = new List<int>(vertexNativeArray.Length);
                int currentIndex = 0;
                int triangleCount = vertexNativeArray.Length / 3;
                for (int i = 0; i < triangleCount; ++i)
                {
                    Vector3 v0 = vertexNativeArray[i * 3 + 0];
                    Vector3 v1 = vertexNativeArray[i * 3 + 1];
                    Vector3 v2 = vertexNativeArray[i * 3 + 2];
                    if (v0 == v1 && v1 == v2)
                    {
                        continue;
                    }
                    verticesList.Add(v0);
                    indicesList.Add(currentIndex++);
                    verticesList.Add(v1);
                    indicesList.Add(currentIndex++);
                    verticesList.Add(v2);
                    indicesList.Add(currentIndex++);
                }

                m.SetVertices(verticesList);
                m.SetIndices(indicesList, MeshTopology.Triangles, 0);
                m.RecalculateBounds();
            }

            result.AddMesh(m, (int)index.x, (int)index.y, 0);
            CleanUpMeshArrays();
        }

        internal NativeArray<bool> GetVertexMarker()
        {
            NativeArray<bool> markers = new NativeArray<bool>(vertexMarkerNativeArray, Allocator.TempJob);
            return markers;
        }

        internal bool[] cachedMarker0;
        internal void CacheMarker0()
        {
            cachedMarker0 = vertexMarkerNativeArray.ToArray();
        }

        internal NativeArray<bool> GetMarker0()
        {
            return new NativeArray<bool>(cachedMarker0, Allocator.TempJob);
        }

        internal int GetGenerationMetadata(int channel)
        {
            return generationMetadata[channel];
        }
    }
}

#endif