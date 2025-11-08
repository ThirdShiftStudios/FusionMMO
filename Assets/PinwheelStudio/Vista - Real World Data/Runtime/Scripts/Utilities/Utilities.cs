#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using System.IO;
using System.Text;
using Unity.Collections;
using Pinwheel.Vista.Graph;

namespace Pinwheel.Vista.RealWorldData
{
    public class Utilities
    {
        ///https://learn.microsoft.com/en-us/bingmaps/articles/bing-maps-tile-system#sample-code
        /// <summary>  
        /// Converts tile XY coordinates into a QuadKey at a specified level of detail.  
        /// </summary>  
        /// <param name="tileX">Tile X coordinate.</param>  
        /// <param name="tileY">Tile Y coordinate.</param>  
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)  
        /// to 23 (highest detail).</param>  
        /// <returns>A string containing the QuadKey.</returns>  
        public static string TileXYToQuadKey(int tileX, int tileY, int levelOfDetail)
        {
            StringBuilder quadKey = new StringBuilder();
            for (int i = levelOfDetail; i > 0; i--)
            {
                char digit = '0';
                int mask = 1 << (i - 1);
                if ((tileX & mask) != 0)
                {
                    digit++;
                }
                if ((tileY & mask) != 0)
                {
                    digit++;
                    digit++;
                }
                quadKey.Append(digit);
            }
            return quadKey.ToString();
        }

        public static Vector3 RGB2HSL(Color c)
        {
            Vector3 rgb = new Vector3(c.r, c.g, c.b);
            Vector3 hsl = _RGBtoHSL(rgb);
            return hsl;
        }

        private static Vector3 _RGBtoHSL(in Vector3 RGB)
        {
            float Epsilon = (float)1e-10;
            Vector3 HCV = _RGBtoHCV(RGB);
            float L = HCV.z - HCV.y * 0.5f;
            float S = HCV.y / (1 - Mathf.Abs(L * 2 - 1) + Epsilon);
            return new Vector3(HCV.x, S, L);
        }

        private static Vector3 _RGBtoHCV(in Vector3 RGB)
        {
            float Epsilon = (float)1e-10;
            // Based on work by Sam Hocevar and Emil Persson
            Vector4 P = (RGB.y < RGB.z) ? new Vector4(RGB.z, RGB.y, -1.0f, 2.0f / 3.0f) : new Vector4(RGB.y, RGB.z, 0.0f, -1.0f / 3.0f);
            Vector4 Q = (RGB.x < P.x) ? new Vector4(P.x, P.y, P.w, RGB.x) : new Vector4(RGB.x, P.y, P.z, P.x);
            float C = Q.x - Mathf.Min(Q.w, Q.y);
            float H = Mathf.Abs((Q.w - Q.y) / (6 * C + Epsilon) + Q.z);
            return new Vector3(H, C, Q.x);
        }

        public static IEnumerator ExtractHeightData(ProgressiveTask taskHandle, float[] sourceData, int sourceWidth, int sourceHeight, GeoRect sourceGPS, GeoRect extractGPS, float[] extractedData)
        {
            Texture2D hm = new Texture2D(sourceWidth, sourceHeight, TextureFormat.RFloat, false, true);
            hm.wrapMode = TextureWrapMode.Clamp;
            hm.SetPixelData(sourceData, 0, 0);
            hm.Apply();
            yield return null;

            RenderTexture rt = RenderTexture.GetTemporary(sourceWidth, sourceHeight, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);

            float minU = (float)Mathd.InverseLerp(sourceGPS.minX, sourceGPS.maxX, extractGPS.minX);
            float minV = (float)Mathd.InverseLerp(sourceGPS.minY, sourceGPS.maxY, extractGPS.minY);
            float maxU = (float)Mathd.InverseLerp(sourceGPS.minX, sourceGPS.maxX, extractGPS.maxX);
            float maxV = (float)Mathd.InverseLerp(sourceGPS.minY, sourceGPS.maxY, extractGPS.maxY);

            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(minU, minV);
            uvs[1] = new Vector2(minU, maxV);
            uvs[2] = new Vector2(maxU, maxV);
            uvs[3] = new Vector2(maxU, minV);

            Material mat = new Material(Shader.Find("Hidden/Vista/Blit"));
            mat.SetTexture("_MainTex", hm);

            Drawing.DrawQuad(rt, Drawing.unitQuad, uvs, mat, 0);
            RenderTexture.active = rt;
            hm.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            hm.Apply();
            RenderTexture.active = null;
            yield return null;

            NativeArray<float> nativeData = hm.GetPixelData<float>(0);
            nativeData.CopyTo(extractedData);
            nativeData.Dispose();

            UnityEngine.Object.DestroyImmediate(hm);
            UnityEngine.Object.DestroyImmediate(mat);
            RenderTexture.ReleaseTemporary(rt);
            taskHandle.Complete();
        }

        public static void ExtractColorData(Texture2D tex, GeoRect sourceGPS, GeoRect extractGPS)
        {
            RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            float minU = (float)Mathd.InverseLerp(sourceGPS.minX, sourceGPS.maxX, extractGPS.minX);
            float minV = (float)Mathd.InverseLerp(sourceGPS.minY, sourceGPS.maxY, extractGPS.minY);
            float maxU = (float)Mathd.InverseLerp(sourceGPS.minX, sourceGPS.maxX, extractGPS.maxX);
            float maxV = (float)Mathd.InverseLerp(sourceGPS.minY, sourceGPS.maxY, extractGPS.maxY);

            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(minU, minV);
            uvs[1] = new Vector2(minU, maxV);
            uvs[2] = new Vector2(maxU, maxV);
            uvs[3] = new Vector2(maxU, minV);

            Material mat = new Material(Shader.Find("Hidden/Vista/Blit"));
            mat.SetTexture("_MainTex", tex);

            Drawing.DrawQuad(rt, Drawing.unitQuad, uvs, mat, 0);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            UnityEngine.Object.DestroyImmediate(mat);
            RenderTexture.ReleaseTemporary(rt);
        }

        public static bool IsTextureDataValid(System.Array data, Vector2Int size)
        {
            if (data == null)
                return false;
            if (data.Length == 0)
                return false;
            if (size.x * size.y != data.Length)
                return false;
            return true;
        }

        public static bool HasActiveInput(GraphAsset graph, string inputName)
        {
            List<InputNode> inputNodes = graph.GetNodesOfType<InputNode>();
            return inputNodes.Exists(n => string.Equals(n.inputName, inputName) && !n.isBypassed);
        }
    }
}
#endif
