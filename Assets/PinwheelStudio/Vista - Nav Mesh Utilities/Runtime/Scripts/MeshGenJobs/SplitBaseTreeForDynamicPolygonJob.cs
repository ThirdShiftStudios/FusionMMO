#if VISTA

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;


namespace Pinwheel.Vista.NavMeshUtilities
{
    //[BurstCompile(CompileSynchronously = false)]
    public struct SplitBaseTreeForDynamicPolygonJob : IJob
    {
        [ReadOnly]
        public TextureNativeDataDescriptor<Color32> subdivMap;

        public NativeArray<SubdivNode> baseTree;
        [WriteOnly]
        public NativeArray<bool> vertexMarker;
        public NativeArray<byte> creationState;

        public int baseResolution;
        public int resolution;
        public int lod;
        public Rect uvRect;

        public void Execute()
        {
            int startIndex = 0;
            int endIndex = 0;
            int leftNodeIndex = 0;
            int rightNodeIndex = 0;

            SubdivNode currentNode;
            SubdivNode leftNode = new SubdivNode();
            SubdivNode rightNode = new SubdivNode();

            Vector2 uv0 = Vector2.zero;
            Vector2 uv1 = Vector2.zero;
            Vector2 uv2 = Vector2.zero;
            Vector2 uvc = Vector2.zero;

            float r0 = 0;
            float r1 = 0;
            float r2 = 0;
            float rc = 0;
            float rMax = 0;
            int subDivLevel = 0;

            baseResolution = Mathf.Max(0, baseResolution - lod);
            resolution = Mathf.Max(0, resolution - lod);

            int maxLevel = baseResolution + Mathf.Min(Mathf.FloorToInt(1f / GeometryJobUtilities.SUB_DIV_STEP), resolution - baseResolution);

            for (int res = baseResolution; res < maxLevel; ++res)
            {
                startIndex = GeometryJobUtilities.GetStartIndex(ref res);
                endIndex = startIndex + GeometryJobUtilities.GetElementCountForSubdivLevel(ref res) - 1;
                for (int i = startIndex; i <= endIndex; ++i)
                {
                    if (creationState[i] != GeometryJobUtilities.STATE_CREATED)
                        continue;
                    currentNode = baseTree[i];
                    GeometryJobUtilities.NormalizeToPoint(ref uv0, ref uvRect, ref currentNode.v0);
                    GeometryJobUtilities.NormalizeToPoint(ref uv1, ref uvRect, ref currentNode.v1);
                    GeometryJobUtilities.NormalizeToPoint(ref uv2, ref uvRect, ref currentNode.v2);
                    uvc = (uv0 + uv1 + uv2) / 3;

                    r0 = GeometryJobUtilities.GetColorBilinear(subdivMap, ref uv0).r;
                    r1 = GeometryJobUtilities.GetColorBilinear(subdivMap, ref uv1).r;
                    r2 = GeometryJobUtilities.GetColorBilinear(subdivMap, ref uv2).r;
                    rc = GeometryJobUtilities.GetColorBilinear(subdivMap, ref uvc).r;

                    rMax = Mathf.Max(Mathf.Max(r0, r1), Mathf.Max(r2, rc));

                    subDivLevel = baseResolution + (int)(rMax / GeometryJobUtilities.SUB_DIV_STEP);
                    if (subDivLevel <= res)
                        continue;

                    GeometryJobUtilities.GetChildrenNodeIndex(ref i, ref leftNodeIndex, ref rightNodeIndex);
                    currentNode.Split(ref leftNode, ref rightNode);

                    baseTree[leftNodeIndex] = leftNode;
                    creationState[leftNodeIndex] = GeometryJobUtilities.STATE_CREATED;
                    GeometryJobUtilities.MarkVertices(ref vertexMarker, ref leftNode);

                    baseTree[rightNodeIndex] = rightNode;
                    creationState[rightNodeIndex] = GeometryJobUtilities.STATE_CREATED;
                    GeometryJobUtilities.MarkVertices(ref vertexMarker, ref rightNode);
                }
            }
        }
    }
}

#endif