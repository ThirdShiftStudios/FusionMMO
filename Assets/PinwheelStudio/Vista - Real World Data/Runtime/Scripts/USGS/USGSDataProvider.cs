#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.RealWorldData;
using System;
using UnityEngine.Networking;
using Pinwheel.Vista.RealWorldData.ArcGIS;
using Unity.Collections;

namespace Pinwheel.Vista.RealWorldData.USGS
{
    [System.Serializable]
    public class USGSDataProvider : IRwdProvider
    {
        public static event Delegates.PostProcessDataHandler<float> postProcessHeightDataCallback;
        public static event Delegates.PostProcessDataHandler<Color32> postProcessColorDataCallback;

        public const float MIN_HEIGHT = -58.3733f;
        public const float MAX_HEIGHT = 3922.4734f;
        public const double BOUNDS_EXPANSION = 500;

        [SerializeField]
        private bool m_downloadHeightMap;
        public bool downloadHeightMap
        {
            get
            {
                return m_downloadHeightMap;
            }
            set
            {
                m_downloadHeightMap = value;
            }
        }

        [SerializeField]
        private int m_heightMapResolution;
        public int heightMapResolution
        {
            get
            {
                return m_heightMapResolution;
            }
            set
            {
                m_heightMapResolution = Mathf.Clamp(value, 1, USGS3DEPImageExporter.MAX_IMAGE_SIZE);
            }
        }

        [SerializeField]
        private bool m_downloadColorMap;
        public bool downloadColorMap
        {
            get
            {
                return m_downloadColorMap;
            }
            set
            {
                m_downloadColorMap = value;
            }
        }

        [SerializeField]
        private int m_colorMapResolution;
        public int colorMapResolution
        {
            get
            {
                return m_colorMapResolution;
            }
            set
            {
                m_colorMapResolution = Mathf.Clamp(value, 1, USGSNAIPImageryExporter.MAX_IMAGE_SIZE);
            }
        }

        public DataAvailability availability => DataAvailability.HeightMap | DataAvailability.ColorMap;

        public USGSDataProvider()
        {
            heightMapResolution = 1024;
            colorMapResolution = 1024;
            downloadHeightMap = true;
            downloadColorMap = true;
        }

        public DataRequest RequestHeightMap(GeoRect gps)
        {
            ProgressHandler progress = new ProgressHandler();
            DataRequest dataRequest = new DataRequest(progress);
            CoroutineUtility.StartCoroutine(IRequestHeightMap(dataRequest, progress, gps));
            return dataRequest;
        }

        private IEnumerator IRequestHeightMap(DataRequest dataRequest, ProgressHandler progress, GeoRect gps)
        {
            GeoRect expandedGPS = gps.ExpandMeters(BOUNDS_EXPANSION);

            UnityWebRequest infoRequest = USGS3DEPImageExporter.CreateWebRequest(expandedGPS, heightMapResolution, heightMapResolution);
            yield return infoRequest.SendWebRequest();

            if (infoRequest.result == UnityWebRequest.Result.Success)
            {
                ArcGISImageExporter.Response r = new ArcGISImageExporter.Response();
                JsonUtility.FromJsonOverwrite(infoRequest.downloadHandler.text, r);
                GeoRect actualRect = new GeoRect(r.extent.xmin, r.extent.xmax, r.extent.ymin, r.extent.ymax);

                UnityWebRequest downloadRequest = UnityWebRequest.Get(r.href);
                downloadRequest.downloadHandler = new DownloadHandlerBuffer();
                UnityWebRequestAsyncOperation download = downloadRequest.SendWebRequest();
                while (!download.isDone)
                {
                    progress.value = download.progress;
                    yield return null;
                }

                if (downloadRequest.result == UnityWebRequest.Result.Success)
                {
                    byte[] tiffByte = downloadRequest.downloadHandler.data;
                    TiffReader.Result decodedTiff = null;
                    try
                    {
                        decodedTiff = TiffReader.Read(tiffByte, TiffReader.BitDepth.Bit32, TiffReader.Pivot.TopLeft);
                    }
                    catch (TiffReader.ReadTileFailedException)
                    {
                        Debug.Log("Cannot decode Tiff file, this can be caused by the selected region is outside of the US, or an internal server error.");
                        decodedTiff = null;
                    }

                    if (decodedTiff != null)
                    {
                        float[] textureData = new float[decodedTiff.data.Length / 4];
                        Buffer.BlockCopy(decodedTiff.data, 0, textureData, 0, decodedTiff.data.Length);
                        RemapHeightData(textureData);

                        ProgressiveTask extractTask = new ProgressiveTask();
                        float[] extractedData = new float[textureData.Length];
                        CoroutineUtility.StartCoroutine(Utilities.ExtractHeightData(extractTask, textureData, decodedTiff.width, decodedTiff.height, actualRect, gps, extractedData));
                        yield return extractTask;

                        Vector2Int dataSize = new Vector2Int(decodedTiff.width, decodedTiff.height);
                        postProcessHeightDataCallback?.Invoke(gps, ref extractedData, ref dataSize);

                        dataRequest.heightMapData = extractedData;
                        dataRequest.heightMapSize = dataSize;
                    }
                }
                else
                {
                    Debug.Log(downloadRequest.error);
                }
                downloadRequest.Dispose();
            }
            else
            {
                Debug.Log(infoRequest.error);
            }

            infoRequest.Dispose();
            dataRequest.Complete();
        }

        private void RemapHeightData(float[] data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                float hMeters = data[i];
                float h01 = Mathf.InverseLerp(MIN_HEIGHT, MAX_HEIGHT, hMeters);
                data[i] = h01;
            }
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
            GeoRect expandedGPS = gps.ExpandMeters(BOUNDS_EXPANSION);

            UnityWebRequest infoRequest = USGSNAIPImageryExporter.CreateWebRequest(expandedGPS, colorMapResolution, colorMapResolution, true);
            yield return infoRequest.SendWebRequest();

            if (infoRequest.result == UnityWebRequest.Result.Success)
            {
                ArcGISImageExporter.Response r = new ArcGISImageExporter.Response();
                JsonUtility.FromJsonOverwrite(infoRequest.downloadHandler.text, r);
                GeoRect actualRect = new GeoRect(r.extent.xmin, r.extent.xmax, r.extent.ymin, r.extent.ymax);

                //using r.href usually return a Bad Request, so we make another direct image request with the same parameters
                UnityWebRequest downloadRequest = USGSNAIPImageryExporter.CreateWebRequest(expandedGPS, colorMapResolution, colorMapResolution, false);
                downloadRequest.downloadHandler = new DownloadHandlerTexture();
                UnityWebRequestAsyncOperation download = downloadRequest.SendWebRequest();
                while (!download.isDone)
                {
                    progress.value = download.progress;
                    yield return null;
                }

                if (downloadRequest.result == UnityWebRequest.Result.Success)
                {
                    Texture2D tex = (downloadRequest.downloadHandler as DownloadHandlerTexture).texture;
                    Utilities.ExtractColorData(tex, actualRect, gps);
                    Color32[] extractData = tex.GetPixels32();
                    Vector2Int dataSize = new Vector2Int(tex.width, tex.height);
                    postProcessColorDataCallback?.Invoke(gps, ref extractData, ref dataSize);

                    dataRequest.colorMapData = extractData;
                    dataRequest.colorMapSize = dataSize;

                    UnityEngine.Object.DestroyImmediate(tex);
                }
                else
                {
                    Debug.Log(downloadRequest.error);
                }

                downloadRequest.Dispose();
            }
            else
            {
                Debug.Log(infoRequest.error);
            }

            infoRequest.Dispose();
            dataRequest.Complete();
        }
    }
}
#endif
