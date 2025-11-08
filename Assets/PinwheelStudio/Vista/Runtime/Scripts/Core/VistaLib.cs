#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;

namespace Pinwheel.Vista
{
    public static class VistaLib
    {
        private class ExtractNormalMapUtils
        {
            public static readonly int HEIGHT_MAP = Shader.PropertyToID("_HeightMap");
            public static readonly int SIZE = Shader.PropertyToID("_Size");
            public static readonly int RESOLUTION = Shader.PropertyToID("_Resolution");
            public static readonly int TARGET_RT = Shader.PropertyToID("_TargetRT");
            public static readonly string SHADER_NAME = "Vista/Shaders/ExtractNormalMap";
        }

        /// <summary>
        /// Extract an object space normal map, range [0, 1]. Should be remapped to [-1,-1] for computation.
        /// </summary>
        /// <param name="targetRT"></param>
        /// <param name="heightMap"></param>
        /// <param name="sizeOS"></param>
        public static void ExtractNormalMapOS(RenderTexture targetRT, Texture heightMap, Vector3 sizeOS)
        {
            Debug.Assert(targetRT.width == heightMap.width && targetRT.height == heightMap.height, "Extractring normal map: targetRT & heightMap must have the same resolution");

            ComputeShader shader = Resources.Load<ComputeShader>(ExtractNormalMapUtils.SHADER_NAME);
            shader.SetVector(ExtractNormalMapUtils.SIZE, sizeOS);
            shader.SetVector(ExtractNormalMapUtils.RESOLUTION, new Vector4(targetRT.width, targetRT.height));
            shader.SetTexture(0, ExtractNormalMapUtils.HEIGHT_MAP, heightMap);
            shader.SetTexture(0, ExtractNormalMapUtils.TARGET_RT, targetRT);

            int threadGroupX = (targetRT.width + 7) / 8;
            int threadGroupY = 1;
            int threadGroupZ = (targetRT.height + 7) / 8;
            shader.Dispatch(0, threadGroupX, threadGroupY, threadGroupZ);
        }

        private class FillColorUtils
        {
            public static readonly int COLOR = Shader.PropertyToID("_Color");
            public static readonly int TARGET_RT = Shader.PropertyToID("_TargetRT");
            public static readonly string SHADER_NAME = "Vista/Shaders/FillColor";
        }

        public static void FillColor(RenderTexture targetRT, Color color)
        {
            ComputeShader shader = Resources.Load<ComputeShader>(FillColorUtils.SHADER_NAME);
            shader.SetVector(FillColorUtils.COLOR, color);
            shader.SetTexture(0, FillColorUtils.TARGET_RT, targetRT);

            int threadGroupX = (targetRT.width + 7) / 8;
            int threadGroupY = 1;
            int threadGroupZ = (targetRT.height + 7) / 8;
            shader.Dispatch(0, threadGroupX, threadGroupY, threadGroupZ);
        }

        private class RemapUtils
        {
            public static readonly string COMPUTE_SHADER_NAME = "Vista/Shaders/Graph/Remap";
            public static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");
            public static readonly int MIN_MAX_BUFFER = Shader.PropertyToID("_MinMaxBuffer");
            public static readonly int INT_LIMIT = Shader.PropertyToID("_IntLimit");
            public static readonly int KERNEL_INDEX = 0;

            public static readonly string SHADER_NAME = "Hidden/Vista/Graph/Remap";
            public static readonly int IN_MIN = Shader.PropertyToID("_InMin");
            public static readonly int IN_MAX = Shader.PropertyToID("_InMax");
            public static readonly int OUT_MIN = Shader.PropertyToID("_OutMin");
            public static readonly int OUT_MAX = Shader.PropertyToID("_OutMax");
            public static readonly int PASS = 0;
        }

        public static void Remap(RenderTexture targetRT, Texture inputTexture, float outMin, float outMax)
        {
            int[] minMaxData = new int[2];
            if (inputTexture != Texture2D.blackTexture)
            {
                minMaxData[0] = int.MaxValue;
                minMaxData[1] = int.MinValue;
                ComputeShader cs = Resources.Load<ComputeShader>(RemapUtils.COMPUTE_SHADER_NAME);
                cs.SetTexture(RemapUtils.KERNEL_INDEX, RemapUtils.MAIN_TEX, inputTexture);

                ComputeBuffer minMaxBuffer = new ComputeBuffer(2, sizeof(int));
                minMaxBuffer.SetData(minMaxData);
                cs.SetBuffer(RemapUtils.KERNEL_INDEX, RemapUtils.MIN_MAX_BUFFER, minMaxBuffer);
                cs.SetInt(RemapUtils.INT_LIMIT, int.MaxValue);
                int threadGroupX = (inputTexture.width + 7) / 8;
                int threadGroupY = (inputTexture.height + 7) / 8;
                int threadGroupZ = 1;
                cs.Dispatch(RemapUtils.KERNEL_INDEX, threadGroupX, threadGroupY, threadGroupZ);

                minMaxBuffer.GetData(minMaxData);
                minMaxBuffer.Dispose();
                Resources.UnloadAsset(cs);
            }
            else
            {
                minMaxData[0] = 0;
                minMaxData[1] = 0;
            }

            Material mat = new Material(ShaderUtilities.Find(RemapUtils.SHADER_NAME));
            mat.SetTexture(RemapUtils.MAIN_TEX, inputTexture);
            mat.SetFloat(RemapUtils.IN_MIN, minMaxData[0] * 1.0f / int.MaxValue);
            mat.SetFloat(RemapUtils.IN_MAX, minMaxData[1] * 1.0f / int.MaxValue);
            mat.SetFloat(RemapUtils.OUT_MIN, outMin);
            mat.SetFloat(RemapUtils.OUT_MAX, outMax);
            Drawing.DrawQuad(targetRT, mat, RemapUtils.PASS);
            Object.DestroyImmediate(mat);
        }
    }
}
#endif
