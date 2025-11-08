#if VISTA
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Random = Unity.Mathematics.Random;

namespace Pinwheel.Vista.NavMeshUtilities
{
    //[BurstCompile(CompileSynchronously = false)]
    public struct CreateVertexJob : IJob
    {
        [ReadOnly]
        public NativeArray<SubdivNode> nodes;
        [ReadOnly]
        public NativeArray<byte> creationState;

        public TextureNativeDataDescriptor<float> heightMap;
        public TextureNativeDataDescriptor<float> holeMap;

        [WriteOnly]
        public NativeArray<Vector3> vertices;
        [WriteOnly]
        public NativeArray<int> metadata;

        public int meshBaseResolution;
        public int meshResolution;
        public int lod;
        public int displacementSeed;
        public float displacementStrength;
        public bool smoothNormal;
        public bool mergeUV;

        public Vector3 terrainSize;
        public Rect chunkUvRect;
        public Vector3 chunkLocalPosition;
        public float texelSize;

        public void Execute()
        {
            SubdivNode n;
            Vector3 v0 = Vector3.zero;
            Vector3 v1 = Vector3.zero;
            Vector3 v2 = Vector3.zero;

            Vector2 uv0 = Vector2.zero;
            Vector2 uv1 = Vector2.zero;
            Vector2 uv2 = Vector2.zero;
            Vector2 uvc = Vector2.zero;

            Vector3 normal = Vector3.zero;
            Color32 color = new Color32();

            int i0 = 0;
            int i1 = 0;
            int i2 = 0;

            //Color hmData0 = Color.black;
            //Color hmData1 = Color.black;
            //Color hmData2 = Color.black;
            float hmData0 = 0;
            float hmData1 = 0;
            float hmData2 = 0;

            float heightSample = 0;

            meshBaseResolution = Mathf.Max(0, meshBaseResolution - lod);

            int length = nodes.Length;
            int leafIndex = 0;
            int startIndex = GeometryJobUtilities.GetStartIndex(ref meshBaseResolution);
            int removedLeafCount = 0;

            for (int i = startIndex; i < length; ++i)
            {
                if (creationState[i] != GeometryJobUtilities.STATE_LEAF)
                    continue;
                n = nodes[i];
                ProcessTriangle(
                    ref n, ref leafIndex,
                    ref uv0, ref uv1, ref uv2, ref uvc,
                    ref v0, ref v1, ref v2,
                    ref i0, ref i1, ref i2,
                    ref normal, ref color,
                    ref hmData0, ref hmData1, ref hmData2,
                    ref heightSample, ref removedLeafCount);
                leafIndex += 1;
            }

            metadata[GeometryJobUtilities.METADATA_LEAF_REMOVED] = removedLeafCount;
        }

        private void ProcessTriangle(
            ref SubdivNode n, ref int leafIndex,
            ref Vector2 uv0, ref Vector2 uv1, ref Vector2 uv2, ref Vector2 uvc,
            ref Vector3 v0, ref Vector3 v1, ref Vector3 v2,
            ref int i0, ref int i1, ref int i2,
            ref Vector3 normal, ref Color32 color,
            ref float hmData0, ref float hmData1, ref float hmData2,
            ref float heightSample, ref int removedLeafCount)
        {
            GeometryJobUtilities.NormalizeToPoint(ref uv0, ref chunkUvRect, ref n.v0);
            GeometryJobUtilities.NormalizeToPoint(ref uv1, ref chunkUvRect, ref n.v1);
            GeometryJobUtilities.NormalizeToPoint(ref uv2, ref chunkUvRect, ref n.v2);

            if (displacementStrength > 0)
            {
                DisplaceUV(ref uv0);
                DisplaceUV(ref uv1);
                DisplaceUV(ref uv2);
            }

            GetHeightMapData(ref hmData0, ref uv0);
            GetHeightMapData(ref hmData1, ref uv1);
            GetHeightMapData(ref hmData2, ref uv2);

            GetHeightSample(ref heightSample, ref hmData0);
            v0.Set(
                uv0.x * terrainSize.x - chunkLocalPosition.x,
                heightSample * terrainSize.y,
                uv0.y * terrainSize.z - chunkLocalPosition.z);

            GetHeightSample(ref heightSample, ref hmData1);
            v1.Set(
                uv1.x * terrainSize.x - chunkLocalPosition.x,
                heightSample * terrainSize.y,
                uv1.y * terrainSize.z - chunkLocalPosition.z);

            GetHeightSample(ref heightSample, ref hmData2);
            v2.Set(
                uv2.x * terrainSize.x - chunkLocalPosition.x,
                heightSample * terrainSize.y,
                uv2.y * terrainSize.z - chunkLocalPosition.z);

            i0 = leafIndex * 3 + 0;
            i1 = leafIndex * 3 + 1;
            i2 = leafIndex * 3 + 2;

            vertices[i0] = v0;
            vertices[i1] = v1;
            vertices[i2] = v2;
            
            float hole0 = GeometryJobUtilities.GetFloatBilinear(holeMap, ref uv0);
            float hole1 = GeometryJobUtilities.GetFloatBilinear(holeMap, ref uv1);
            float hole2 = GeometryJobUtilities.GetFloatBilinear(holeMap, ref uv2);
            if (hole0 <= 0.05 || hole1 <= 0.05 || hole2 <= 0.05) //actually it's an area mask where bright pixels represent the area
            {
                vertices[i0] = v0;
                vertices[i1] = v0;
                vertices[i2] = v0;
                removedLeafCount += 1;
            }
        }

        private void DisplaceUV(ref Vector2 uv)
        {
            if (uv.x == 0 || uv.y == 0 || uv.x == 1 || uv.y == 1)
                return;

            Random rnd = Random.CreateFromIndex((uint)(displacementSeed ^ (uint)(uv.x * 1000) ^ (uint)(uv.y * 1000)));
            float noise0 = rnd.NextFloat() - 0.5f;
            float noise1 = rnd.NextFloat() - 0.5f;

            Vector2 v = new Vector2(noise0 * displacementStrength / terrainSize.x, noise1 * displacementStrength / terrainSize.z);
            uv.Set(
                Mathf.Clamp01(uv.x + v.x),
                Mathf.Clamp01(uv.y + v.y));
        }

        //private void GetHeightMapData(ref Color data, ref Vector2 uv)
        //{
        //    data = GGeometryJobUtilities.GetColorBilinear(hmC, ref uv);
        //}
        // data  > float, 

        private void GetHeightMapData(ref float data, ref Vector2 uv)
        {
            data = GeometryJobUtilities.GetFloatBilinear(heightMap, ref uv);
        }
        private float DecodeFloatRG(ref Vector2 enc)
        {
            Vector2 kDecodeDot = new Vector2(1.0f, 1f / 255.0f);
            return Vector2.Dot(enc, kDecodeDot);
        }

        //private void GetHeightSample(ref float sample, ref Color data)
        //{
        //    Vector2 enc = new Vector2(data.r, data.g);
        //    sample = DecodeFloatRG(ref enc);
        //}
        private void GetHeightSample(ref float sample, ref float data)
        {
            sample = data;
        }
        private Vector3 GetSmoothNormal(ref Vector2 uv, ref Vector3 v)
        {
            //Color hmData = Color.black;
            float hmData = 0;
            Vector2 sampleUV = Vector2.zero;

            //bl
            sampleUV.Set(uv.x - texelSize, uv.y - texelSize);
            GetHeightMapData(ref hmData, ref sampleUV);
            float h0 = 0;
            GetHeightSample(ref h0, ref hmData);
            Vector3 v0 = new Vector3(sampleUV.x * terrainSize.x, h0 * terrainSize.y, sampleUV.y * terrainSize.z);

            //l
            sampleUV.Set(uv.x - texelSize, uv.y);
            GetHeightMapData(ref hmData, ref sampleUV);
            float h1 = 0;
            GetHeightSample(ref h1, ref hmData);
            Vector3 v1 = new Vector3(sampleUV.x * terrainSize.x, h1 * terrainSize.y, sampleUV.y * terrainSize.z);

            //tl
            sampleUV.Set(uv.x - texelSize, uv.y + texelSize);
            GetHeightMapData(ref hmData, ref sampleUV);
            float h2 = 0;
            GetHeightSample(ref h2, ref hmData);
            Vector3 v2 = new Vector3(sampleUV.x * terrainSize.x, h2 * terrainSize.y, sampleUV.y * terrainSize.z);

            //t
            sampleUV.Set(uv.x, uv.y + texelSize);
            GetHeightMapData(ref hmData, ref sampleUV);
            float h3 = 0;
            GetHeightSample(ref h3, ref hmData);
            Vector3 v3 = new Vector3(sampleUV.x * terrainSize.x, h3 * terrainSize.y, sampleUV.y * terrainSize.z);

            //tr
            sampleUV.Set(uv.x + texelSize, uv.y + texelSize);
            GetHeightMapData(ref hmData, ref sampleUV);
            float h4 = 0;
            GetHeightSample(ref h4, ref hmData);
            Vector3 v4 = new Vector3(sampleUV.x * terrainSize.x, h4 * terrainSize.y, sampleUV.y * terrainSize.z);

            //r
            sampleUV.Set(uv.x + texelSize, uv.y);
            GetHeightMapData(ref hmData, ref sampleUV);
            float h5 = 0;
            GetHeightSample(ref h5, ref hmData);
            Vector3 v5 = new Vector3(sampleUV.x * terrainSize.x, h5 * terrainSize.y, sampleUV.y * terrainSize.z);

            //br
            sampleUV.Set(uv.x + texelSize, uv.y - texelSize);
            GetHeightMapData(ref hmData, ref sampleUV);
            float h6 = 0;
            GetHeightSample(ref h6, ref hmData);
            Vector3 v6 = new Vector3(sampleUV.x * terrainSize.x, h6 * terrainSize.y, sampleUV.y * terrainSize.z);

            //b
            sampleUV.Set(uv.x, uv.y - texelSize);
            GetHeightMapData(ref hmData, ref sampleUV);
            float h7 = 0;
            GetHeightSample(ref h7, ref hmData);
            Vector3 v7 = new Vector3(sampleUV.x * terrainSize.x, h7 * terrainSize.y, sampleUV.y * terrainSize.z);

            Vector3 n0 = Vector3.Cross(v0 - v, v1 - v);
            Vector3 n1 = Vector3.Cross(v1 - v, v2 - v);
            Vector3 n2 = Vector3.Cross(v2 - v, v3 - v);
            Vector3 n3 = Vector3.Cross(v3 - v, v4 - v);
            Vector3 n4 = Vector3.Cross(v4 - v, v5 - v);
            Vector3 n5 = Vector3.Cross(v5 - v, v6 - v);
            Vector3 n6 = Vector3.Cross(v6 - v, v7 - v);
            Vector3 n7 = Vector3.Cross(v7 - v, v0 - v);

            Vector3 n = (n0 + n1 + n2 + n3 + n4 + n5 + n6 + n7) / 8.0f;

            return n;
        }

        private float GetSmoothNormalMask(ref Vector2 uv)
        {
            return 1;
        }
    }
}

#endif