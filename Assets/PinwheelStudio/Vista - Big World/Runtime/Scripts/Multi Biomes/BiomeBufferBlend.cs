#if VISTA
using Pinwheel.Vista.Graphics;
using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Vista.BigWorld
{
    public static class BiomeBufferBlend
    {
        private static readonly string BUFFER_BLEND_SHADER = "Vista/Shaders/BiomeBufferBlend";
        private static readonly int SRC_BUFFER = Shader.PropertyToID("_SrcBuffer");
        private static readonly int MASK_MAP = Shader.PropertyToID("_MaskMap");
        private static readonly int KERNEL = 0;

        private static readonly string KW_DATA_TYPE_INSTANCE_SAMPLE = "DATA_TYPE_INSTANCE_SAMPLE";
        private static readonly string KW_DATA_TYPE_POSITION_SAMPLE = "DATA_TYPE_POSITION_SAMPLE";
        private static readonly string KW_FLIP_MASK = "FLIP_MASK";

        private static readonly int BASE_INDEX = Shader.PropertyToID("_BaseIndex");
        private static readonly int THREAD_PER_GROUP = 8;
        private static readonly int MAX_THREAD_GROUP = 64000 / THREAD_PER_GROUP;

        private static ComputeShader s_bufferBlendShader;

        public static void BlendTreeBuffer(BiomeData destData, List<BiomeData> srcDatas, List<BiomeBlendOptions> blendOptions)
        {
            for (int i = 0; i < srcDatas.Count; ++i)
            {
                BiomeBlendOptions.BufferBlendMode blendMode = blendOptions[i].instancesBlendMode;
                if (blendMode == BiomeBlendOptions.BufferBlendMode.Linear)
                {
                    List<TreeTemplate> destTemplates = new List<TreeTemplate>();
                    List<ComputeBuffer> destBuffers = new List<ComputeBuffer>();
                    destData.GetTrees(destTemplates, destBuffers);

                    for (int j = 0; j < destBuffers.Count; ++j)
                    {
                        BlendBufferByBiomeMask<InstanceSample>(destBuffers[j], srcDatas[i].biomeMaskMap, true);
                    }
                }

                List<TreeTemplate> srcTemplates = new List<TreeTemplate>();
                List<ComputeBuffer> srcBuffers = new List<ComputeBuffer>();
                srcDatas[i].GetTrees(srcTemplates, srcBuffers);
                for (int j = 0; j < srcBuffers.Count; ++j)
                {
                    ComputeBuffer clonedBuffer = BufferHelper.Clone(srcBuffers[j]);
                    BlendBufferByBiomeMask<InstanceSample>(clonedBuffer, srcDatas[i].biomeMaskMap, false);
                    destData.AddTree(srcTemplates[j], clonedBuffer);
                }
            }
        }

        public static void BlendDetailInstanceBuffer(BiomeData destData, List<BiomeData> srcDatas, List<BiomeBlendOptions> blendOptions)
        {
            for (int i = 0; i < srcDatas.Count; ++i)
            {
                BiomeBlendOptions.BufferBlendMode blendMode = blendOptions[i].instancesBlendMode;
                if (blendMode == BiomeBlendOptions.BufferBlendMode.Linear)
                {
                    List<DetailTemplate> destTemplates = new List<DetailTemplate>();
                    List<ComputeBuffer> destBuffers = new List<ComputeBuffer>();
                    destData.GetDetailInstances(destTemplates, destBuffers);

                    for (int j = 0; j < destBuffers.Count; ++j)
                    {
                        BlendBufferByBiomeMask<InstanceSample>(destBuffers[j], srcDatas[i].biomeMaskMap, true);
                    }
                }

                List<DetailTemplate> srcTemplates = new List<DetailTemplate>();
                List<ComputeBuffer> srcBuffers = new List<ComputeBuffer>();
                srcDatas[i].GetDetailInstances(srcTemplates, srcBuffers);
                for (int j = 0; j < srcBuffers.Count; ++j)
                {
                    ComputeBuffer clonedBuffer = BufferHelper.Clone(srcBuffers[j]);
                    BlendBufferByBiomeMask<InstanceSample>(clonedBuffer, srcDatas[i].biomeMaskMap, false);
                    destData.AddDetailInstance(srcTemplates[j], clonedBuffer);
                }
            }
        }

        public static void BlendObjectBuffer(BiomeData destData, List<BiomeData> srcDatas, List<BiomeBlendOptions> blendOptions)
        {
            for (int i = 0; i < srcDatas.Count; ++i)
            {
                BiomeBlendOptions.BufferBlendMode blendMode = blendOptions[i].instancesBlendMode;
                if (blendMode == BiomeBlendOptions.BufferBlendMode.Linear)
                {
                    List<ObjectTemplate> destTemplates = new List<ObjectTemplate>();
                    List<ComputeBuffer> destBuffers = new List<ComputeBuffer>();
                    destData.GetObjects(destTemplates, destBuffers);

                    for (int j = 0; j < destBuffers.Count; ++j)
                    {
                        BlendBufferByBiomeMask<InstanceSample>(destBuffers[j], srcDatas[i].biomeMaskMap, true);
                    }
                }

                List<ObjectTemplate> srcTemplates = new List<ObjectTemplate>();
                List<ComputeBuffer> srcBuffers = new List<ComputeBuffer>();
                srcDatas[i].GetObjects(srcTemplates, srcBuffers);
                for (int j = 0; j < srcBuffers.Count; ++j)
                {
                    ComputeBuffer clonedBuffer = BufferHelper.Clone(srcBuffers[j]);
                    BlendBufferByBiomeMask<InstanceSample>(clonedBuffer, srcDatas[i].biomeMaskMap, false);
                    destData.AddObject(srcTemplates[j], clonedBuffer);
                }
            }
        }

        public static void BlendGenericBuffer(BiomeData destData, List<BiomeData> srcDatas, List<BiomeBlendOptions> blendOptions)
        {
            for (int i = 0; i < srcDatas.Count; ++i)
            {
                BiomeBlendOptions.BufferBlendMode blendMode = blendOptions[i].instancesBlendMode;
                if (blendMode == BiomeBlendOptions.BufferBlendMode.Linear)
                {
                    List<string> destLabels = new List<string>();
                    List<ComputeBuffer> destBuffers = new List<ComputeBuffer>();
                    destData.GetGenericBuffers(destLabels, destBuffers);

                    for (int j = 0; j < destBuffers.Count; ++j)
                    {
                        BlendBufferByBiomeMask<InstanceSample>(destBuffers[j], srcDatas[i].biomeMaskMap, true);
                    }
                }

                List<string> srcLabels = new List<string>();
                List<ComputeBuffer> srcBuffers = new List<ComputeBuffer>();
                srcDatas[i].GetGenericBuffers(srcLabels, srcBuffers);
                for (int j = 0; j < srcBuffers.Count; ++j)
                {
                    ComputeBuffer clonedBuffer = BufferHelper.Clone(srcBuffers[j]);
                    BlendBufferByBiomeMask<InstanceSample>(clonedBuffer, srcDatas[i].biomeMaskMap, false);
                    destData.AddGenericBuffer(srcLabels[j], clonedBuffer);
                }
            }
        }

        private static void BlendBufferByBiomeMask<T>(ComputeBuffer buffer, Texture biomeMask, bool flipMask)
        {
            int structSize;
            string dataTypeKw;
            if (typeof(T).Equals(typeof(PositionSample)))
            {
                structSize = PositionSample.SIZE;
                dataTypeKw = KW_DATA_TYPE_POSITION_SAMPLE;
            }
            else if (typeof(T).Equals(typeof(InstanceSample)))
            {
                structSize = InstanceSample.SIZE;
                dataTypeKw = KW_DATA_TYPE_INSTANCE_SAMPLE;
            }
            else
            {
                throw new System.ArgumentException($"Buffer data type must be {typeof(InstanceSample).Name} or {typeof(PositionSample).Name}");
            }

            s_bufferBlendShader = Resources.Load<ComputeShader>(BUFFER_BLEND_SHADER);
            s_bufferBlendShader.SetBuffer(KERNEL, SRC_BUFFER, buffer);
            s_bufferBlendShader.SetTexture(KERNEL, MASK_MAP, biomeMask != null ? biomeMask : Texture2D.blackTexture);
            s_bufferBlendShader.shaderKeywords = null;
            s_bufferBlendShader.EnableKeyword(dataTypeKw);
            if (flipMask)
            {
                s_bufferBlendShader.EnableKeyword(KW_FLIP_MASK);
            }

            int instanceCount = buffer.count / structSize;
            int totalThreadGroupX = (instanceCount + THREAD_PER_GROUP - 1) / THREAD_PER_GROUP;
            int iteration = (totalThreadGroupX + MAX_THREAD_GROUP - 1) / MAX_THREAD_GROUP;
            for (int i = 0; i < iteration; ++i)
            {
                int threadGroupX = Mathf.Min(MAX_THREAD_GROUP, totalThreadGroupX);
                totalThreadGroupX -= MAX_THREAD_GROUP;
                int baseIndex = i * MAX_THREAD_GROUP * THREAD_PER_GROUP;
                s_bufferBlendShader.SetInt(BASE_INDEX, baseIndex);
                s_bufferBlendShader.Dispatch(KERNEL, threadGroupX, 1, 1);
            }
            //int threadGroupX = (instanceCount + 7) / 8;
            //int threadGroupY = 1;
            //int threadGroupZ = 1;
            //s_bufferBlendShader.Dispatch(KERNEL, threadGroupX, threadGroupY, threadGroupZ);

            Resources.UnloadAsset(s_bufferBlendShader);
        }
    }
}
#endif
