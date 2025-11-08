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
    internal struct CreateBaseTreeJob : IJob
    {
        public NativeArray<SubdivNode> nodes;
        [WriteOnly]
        public NativeArray<byte> creationState;
        [WriteOnly]
        public NativeArray<bool> vertexMarker;

        [WriteOnly]
        public NativeArray<int> metadata;

        public int baseResolution;
        public int resolution;
        public int lod;

        public void Execute()
        {
            ResetMetadata();
            ResetStates();
            ResetMarker();

            SubdivNode nodes0 = new SubdivNode()
            {
                v0 = Vector2.up,
                v1 = Vector2.one,
                v2 = Vector2.zero
            };
            nodes[0] = nodes0;
            creationState[0] = GeometryJobUtilities.STATE_CREATED;
            GeometryJobUtilities.MarkVertices(ref vertexMarker, ref nodes0);

            SubdivNode nodes1 = new SubdivNode()
            {
                v0 = Vector2.right,
                v1 = Vector2.zero,
                v2 = Vector2.one
            };
            nodes[1] = nodes1;
            creationState[1] = GeometryJobUtilities.STATE_CREATED;
            GeometryJobUtilities.MarkVertices(ref vertexMarker, ref nodes1);

            int startIndex = 0;
            int endIndex = 0;
            int leftNodeIndex = 0;
            int rightNodeIndex = 0;
            SubdivNode currentNode;
            SubdivNode leftNode = new SubdivNode();
            SubdivNode rightNode = new SubdivNode();

            for (int res = 0; res < resolution; ++res)
            {
                startIndex = GeometryJobUtilities.GetStartIndex(ref res);
                endIndex = startIndex + GeometryJobUtilities.GetElementCountForSubdivLevel(ref res) - 1;
                for (int i = startIndex; i <= endIndex; ++i)
                {
                    currentNode = nodes[i];
                    currentNode.Split(ref leftNode, ref rightNode);
                    GeometryJobUtilities.GetChildrenNodeIndex(ref i, ref leftNodeIndex, ref rightNodeIndex);

                    nodes[leftNodeIndex] = leftNode;
                    nodes[rightNodeIndex] = rightNode;
                }
            }

            baseResolution = Mathf.Max(0, baseResolution - lod);
            for (int res = 0; res <= baseResolution; ++res)
            {
                startIndex = GeometryJobUtilities.GetStartIndex(ref res);
                endIndex = startIndex + GeometryJobUtilities.GetElementCountForSubdivLevel(ref res) - 1;
                for (int i = startIndex; i <= endIndex; ++i)
                {
                    currentNode = nodes[i];
                    creationState[i] = GeometryJobUtilities.STATE_CREATED;
                    GeometryJobUtilities.MarkVertices(ref vertexMarker, ref currentNode);
                }
            }
        }

        private void ResetMetadata()
        {
            metadata[GeometryJobUtilities.METADATA_LEAF_COUNT] = 0;
            metadata[GeometryJobUtilities.METADATA_NEW_VERTEX_CREATED] = 0;
        }

        private void ResetStates()
        {
            for (int i = 0; i < creationState.Length; ++i)
            {
                creationState[i] = GeometryJobUtilities.STATE_NOT_CREATED;
            }
        }

        private void ResetMarker()
        {
            for (int i = 0; i < vertexMarker.Length; ++i)
            {
                vertexMarker[i] = false;
            }
        }
    }
}

#endif