#if VISTA

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Pinwheel.Vista.NavMeshUtilities
{
    public struct TextureNativeDataDescriptor<T> where T : struct
    {
        [ReadOnly]
        public NativeArray<T> data;
        public int width;
        public int height;

        private bool isValid;
        public bool IsValid
        {
            get
            {
                return isValid;
            }
        }

        public TextureNativeDataDescriptor(Texture2D tex)
        {
            if (tex != null)
            {
                data = tex.GetRawTextureData<T>();
                width = tex.width;
                height = tex.height;
                isValid = true;
            }
            else
            {
                data = new NativeArray<T>(1, Allocator.Persistent);
                width = 0;
                height = 0;
                isValid = false;
            }
        }

        public void Dispose()
        {
            if (!isValid)
            {
                NativeArrayUtilities.Dispose(data);
            }
        }
    }
}

#endif