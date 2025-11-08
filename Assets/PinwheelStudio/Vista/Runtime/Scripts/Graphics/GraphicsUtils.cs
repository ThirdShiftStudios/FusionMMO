#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;

namespace Pinwheel.Vista
{
    public static class GraphicsUtils
    {
        public static void ClearWithZeros(RenderTexture rt)
        {
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = null;
        }

        private static readonly string CLEAR_BUFFER_SHADER_NAME = "Vista/Shaders/BufferClear";
        private static readonly int BUFFER = Shader.PropertyToID("_Buffer");
        private static readonly int BASE_INDEX = Shader.PropertyToID("_BaseIndex");
        private static readonly int KERNEL = 0;

        private static ComputeShader s_clearBufferShader;

        public static void ClearWithZeros(ComputeBuffer buffer)
        {
#if UNITY_EDITOR
            if (buffer.count % 8 != 0)
            {
                Debug.LogWarning("Attempting to use shader to clear a buffer with non-multiple-of-8 count. This may failed.");
            }
#endif  

            if (s_clearBufferShader == null)
            {
                s_clearBufferShader = Resources.Load<ComputeShader>(CLEAR_BUFFER_SHADER_NAME);
            }
            s_clearBufferShader.SetBuffer(KERNEL, BUFFER, buffer);

            int maxElementPerStep = 64000 * 8;
            int remainingCount = buffer.count;
            int baseIndex = 0;
            while (remainingCount > 0)
            {
                int count = Mathf.Min(maxElementPerStep, remainingCount);
                s_clearBufferShader.SetInt(BASE_INDEX, baseIndex);
                s_clearBufferShader.Dispatch(KERNEL, (count + 7) / 8, 1, 1);
                remainingCount -= count;
                baseIndex += count;
            }
        }
    }
}
#endif