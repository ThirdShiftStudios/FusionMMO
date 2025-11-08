#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.RealWorldData;
using System;
using UnityEngine.Networking;

namespace Pinwheel.Vista.RealWorldData
{
    [System.Serializable]
    public class CustomTiledDataProvider : IRwdProvider
    {
        public static event Delegates.PostProcessDataHandler<Color32> postProcessColorDataCallback;

        private static readonly string BLIT_SHADER = "Hidden/Vista/Blit";
        private static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");

        public DataAvailability availability => DataAvailability.ColorMap;

        [SerializeField]
        [Multiline(9)]
#pragma warning disable CS0414 
        private string m_instruction;
#pragma warning restore CS0414 
        private readonly string INSTRUCTION =
            $"This is mostly used for getting colored images from custom sources.\n" +
            $"Provide an URL with HTTPS to the box below, where:\n" +
            $"    {CustomImageTileProvider.ZOOM} will be replaced with zoom level,\n" +
            $"    {CustomImageTileProvider.X} will be replaced with column index,\n" +
            $"    {CustomImageTileProvider.Y} will be replaced with row index,\n" +
            $"    {CustomImageTileProvider.QUAD_KEY} will be replaced with quad key.\n" +
            $"    {CustomImageTileProvider.API_KEY} will be replaced with the service API Key.\n" +
            $"The server must be able to respond with PNG/JPG images, otherwise the operation will fail.";

        [SerializeField]
        protected string m_urlTemplate;
        public string urlTemplate
        {
            get
            {
                return m_urlTemplate;
            }
            set
            {
                m_urlTemplate = value;
            }
        }

        public string apiKey { get; set; }

        [SerializeField]
#pragma warning disable IDE0052 // Remove unread private members
        private string m_testUrl;
#pragma warning restore IDE0052 // Remove unread private members
        internal const int TEST_Z = 4;
        internal const int TEST_X = 3;
        internal const int TEST_Y = 5;

        [SerializeField]
        protected TileQuality m_quality;
        public TileQuality quality
        {
            get
            {
                return m_quality;
            }
            set
            {
                m_quality = value;
            }
        }

        [SerializeField]
        [Tooltip("Maximum zoom level allowed by the tile service, please check the service documentation for correct value. Free services usually allow about 10 zooms, while paid services can provide upto 20+ zooms.")]
        protected int m_maxZoom;
        public int maxZoom
        {
            get
            {
                return m_maxZoom;
            }
            set
            {
                m_maxZoom = value;
            }
        }

        public CustomTiledDataProvider()
        {
            m_urlTemplate = "https://basemap.nationalmap.gov/arcgis/rest/services/USGSImageryOnly/MapServer/tile/{z}/{y}/{x}";
            m_quality = TileQuality.Normal;
            m_maxZoom = 10;
            OnValidate();
        }

        internal void OnValidate()
        {
            m_instruction = INSTRUCTION;
            m_testUrl = CustomImageTileProvider.CreateUrl(m_urlTemplate, apiKey, TEST_Z, TEST_X, TEST_Y);
        }

        public DataRequest RequestColorMap(GeoRect gps)
        {
            ProgressHandler progress = new ProgressHandler();
            DataRequest dataRequest = new DataRequest(progress);
            CoroutineUtility.StartCoroutine(IRequestColorMap(dataRequest, progress, gps));
            return dataRequest;
        }

        private IEnumerator IRequestColorMap(DataRequest dataRequest, ProgressHandler progress, GeoRect gps)
        {
            GeoRect viewport100 = gps.GpsToWebMercator().WebMercatorToViewport100();
            List<MapTile> rootTiles = MapTileUtilities.CreateRootTilesForLoopMap<MapTile>(viewport100);

            ProgressiveTask task = new ProgressiveTask();
            CoroutineUtility.StartCoroutine(IDownloadImage(task, dataRequest, gps, viewport100, rootTiles));
            while (!task.isCompleted)
            {
                progress.value = Mathf.MoveTowards(progress.value, 0.5f, UnityEngine.Random.value * 0.02f);
                yield return null;
            }

            dataRequest.Complete();
        }

        private IEnumerator IDownloadImage(ProgressiveTask taskHandle, DataRequest dataRequest, GeoRect gps, GeoRect viewport100, List<MapTile> rootTiles)
        {
            CustomImageTileProvider provider = new CustomImageTileProvider();
            provider.urlTemplate = this.urlTemplate;
            provider.apiKey = this.apiKey;
            provider.maxZoom = this.maxZoom;

            int zoom = MapTileUtilities.CalculateZoom(viewport100) + (int)m_quality;
            zoom = Mathf.Clamp(zoom, provider.minZoom, provider.maxZoom);

            GeoRect rectWM = viewport100.Viewport100ToWebMercator(zoom);
            int width = (int)Math.Ceiling(Math.Abs(rectWM.width));
            int height = (int)Math.Ceiling(Math.Abs(rectWM.height));

            List<MapTile> tilesToRender = MapTileUtilities.GetTilesForRendering(rootTiles, zoom, viewport100);
            List<UnityWebRequestAsyncOperation> downloads = new List<UnityWebRequestAsyncOperation>();

            foreach (MapTile t in tilesToRender)
            {
                UnityWebRequest r = provider.CreateTileRequest(t.zoom, t.x, t.y);
                r.downloadHandler = new DownloadHandlerTexture();
                downloads.Add(r.SendWebRequest());
            }

            yield return new WaitAllWebRequestsCompleted(downloads);

            RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Vector2[] vertices = new Vector2[4];
            Vector2[] uvs = new Vector2[4];
            Material material = new Material(Shader.Find(BLIT_SHADER));

            for (int i = 0; i < tilesToRender.Count; ++i)
            {
                MapTile t = tilesToRender[i];
                UnityWebRequestAsyncOperation d = downloads[i];
                Texture2D tex;
                if (d.webRequest.result == UnityWebRequest.Result.Success)
                {
                    tex = (d.webRequest.downloadHandler as DownloadHandlerTexture).texture;
                    GeoRect.CalculateNormalizedQuadNonAlloc(viewport100, t.bounds100, vertices);

                    Vector2 texelSize = tex.texelSize;
                    uvs[0] = new Vector2(texelSize.x, texelSize.y);
                    uvs[1] = new Vector2(texelSize.x, 1 - texelSize.y);
                    uvs[2] = new Vector2(1 - texelSize.x, 1 - texelSize.y);
                    uvs[3] = new Vector2(1 - texelSize.x, texelSize.y);

                    material.SetTexture(MAIN_TEX, tex);
                    Drawing.DrawQuad(tempRT, vertices, uvs, material, 0);
                    UnityEngine.Object.DestroyImmediate(tex);
                }
                else
                {
                    Debug.Log(d.webRequest.error);
                }

                d.webRequest.Dispose();
            }

            Texture2D colorMap = new Texture2D(width, height, TextureFormat.RGBA32, true, false);
            RenderTexture.active = tempRT;
            colorMap.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            colorMap.Apply();
            RenderTexture.active = null;

            Vector2Int dataSize = new Vector2Int(width, height);
            Color32[] data = colorMap.GetPixels32();

            RenderTexture.ReleaseTemporary(tempRT);
            UnityEngine.Object.DestroyImmediate(colorMap);
            UnityEngine.Object.DestroyImmediate(material);

            postProcessColorDataCallback?.Invoke(gps, ref data, ref dataSize);
            dataRequest.colorMapData = data;
            dataRequest.colorMapSize = dataSize;

            taskHandle.Complete();
        }

        public DataRequest RequestHeightMap(GeoRect gps)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
