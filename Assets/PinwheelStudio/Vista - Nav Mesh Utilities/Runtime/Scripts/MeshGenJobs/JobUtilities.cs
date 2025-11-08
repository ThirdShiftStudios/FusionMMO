#if VISTA

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;

namespace Pinwheel.Vista.NavMeshUtilities
{
    public static class JobUtilities
    {
        public static void CompleteAll(JobHandle[] handles)
        {
            for (int i = 0; i < handles.Length; ++i)
            {
                handles[i].Complete();
            }
        }
    }
}

#endif