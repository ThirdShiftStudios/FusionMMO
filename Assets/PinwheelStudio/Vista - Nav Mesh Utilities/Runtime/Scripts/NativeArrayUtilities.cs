#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Pinwheel.Vista.NavMeshUtilities
{
    public static class NativeArrayUtilities
    {
        public static void Dispose<T>(NativeArray<T> array) where T : struct
        {
            try
            {
                if (array.IsCreated)
                {
                    array.Dispose();
                }
            }
            catch (System.InvalidOperationException)
            {
            }
        }
    }
}
#endif