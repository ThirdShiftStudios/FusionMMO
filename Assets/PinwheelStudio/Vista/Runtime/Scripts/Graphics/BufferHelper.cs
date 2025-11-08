#if VISTA
using UnityEngine;

namespace Pinwheel.Vista.Graphics
{
    public static class BufferHelper
    {
        private static readonly string COPY_SHADER_NAME = "Vista/Shaders/BufferCopy";
        private static readonly int SRC_BUFFER = Shader.PropertyToID("_SrcBuffer");
        private static readonly int DEST_BUFFER = Shader.PropertyToID("_DestBuffer");
        private static readonly int COPY_KERNEL = 0;

        private static readonly int BASE_INDEX = Shader.PropertyToID("_BaseIndex");

        private static ComputeShader s_copyBufferShader;

        public static void Copy(ComputeBuffer from, ComputeBuffer to)
        {
            s_copyBufferShader = Resources.Load<ComputeShader>(COPY_SHADER_NAME);
            s_copyBufferShader.SetBuffer(COPY_KERNEL, SRC_BUFFER, from);
            s_copyBufferShader.SetBuffer(COPY_KERNEL, DEST_BUFFER, to);

            int maxElementPerStep = 64000*8;
            int remainingCount = from.count;
            int baseIndex = 0;
            while (remainingCount > 0)
            {
                int count = Mathf.Min(maxElementPerStep, remainingCount);
                s_copyBufferShader.SetInt(BASE_INDEX, baseIndex);
                s_copyBufferShader.Dispatch(COPY_KERNEL, (count + 7) / 8, 1, 1);
                remainingCount -= count;
                baseIndex += count;
            }
            Resources.UnloadAsset(s_copyBufferShader);
        }

        public static ComputeBuffer Clone(ComputeBuffer src)
        {
            ComputeBuffer cloned = new ComputeBuffer(src.count, src.stride);
            Copy(src, cloned);
            return cloned;
        }        
    }
}
#endif
