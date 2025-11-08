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
    public struct CountLeafNodeJob : IJob
    {
        public NativeArray<byte> creationState;
        [WriteOnly]
        public NativeArray<int> metadata;

        public int baseResolution;
        public int resolution;
        public int lod;

        public void Execute()
        {
            int startIndex = 0;
            int endIndex = 0;
            int leftNodeIndex = 0;
            int rightNodeIndex = 0;
            int count = 0;
            int length = creationState.Length;

            baseResolution = Mathf.Max(0, baseResolution - lod);

            for (int res = baseResolution; res <= resolution; ++res)
            {
                startIndex = GeometryJobUtilities.GetStartIndex(ref res);
                endIndex = startIndex + GeometryJobUtilities.GetElementCountForSubdivLevel(ref res) - 1;
                for (int i = startIndex; i <= endIndex; ++i)
                {
                    if (creationState[i] != GeometryJobUtilities.STATE_CREATED)
                        continue;
                    GeometryJobUtilities.GetChildrenNodeIndex(ref i, ref leftNodeIndex, ref rightNodeIndex);
                    if (leftNodeIndex >= length || rightNodeIndex >= length)
                    {
                        creationState[i] = GeometryJobUtilities.STATE_LEAF;
                        count += 1;
                        continue;
                    }
                    if (creationState[leftNodeIndex] == GeometryJobUtilities.STATE_NOT_CREATED ||
                        creationState[rightNodeIndex] == GeometryJobUtilities.STATE_NOT_CREATED)
                    {
                        creationState[i] = GeometryJobUtilities.STATE_LEAF;
                        count += 1;
                        continue;
                    }
                }
            }
            metadata[0] = count;
        }
    }
}

#endif