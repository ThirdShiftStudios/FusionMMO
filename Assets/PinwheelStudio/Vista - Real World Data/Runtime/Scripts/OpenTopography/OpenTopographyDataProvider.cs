#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEngine.Networking;
using Unity.Collections;
using System;
using System.Text;

namespace Pinwheel.Vista.RealWorldData
{
    [System.Serializable]
    public class OpenTopographyDataProvider : IRwdProvider
    {
        public static event Delegates.PostProcessDataHandler<float> postProcessHeightDataCallback;
        public enum DEMTypes
        {
            SRTMGL3,
            SRTMGL1,
            SRTMGL1_E,
            AW3D30,
            AW3D30_E,
            SRTM15Plus,
            NASADEM,
            COP30,
            COP90,
            EU_DTM,
            GEDI_L3,
        }

        public const int MIN_HEIGHT = 0;
        public const int MAX_HEIGHT = 10000;
        public const double BOUNDS_EXPANSION = 500;

        public DataAvailability availability => DataAvailability.HeightMap;

        [SerializeField]
        [Tooltip("The dataset to query from")]
        private DEMTypes m_demType;
        public DEMTypes demType
        {
            get
            {
                return m_demType;
            }
            set
            {
                m_demType = value;
            }
        }

        private const string DEM_INSTRUCTION =
            "From OpenTopography document:\n" +
            "Access global topographic datasets including\n" +
            "SRTM GL3 (Global 90m), \n" +
            "GL1 (Global 30m), \n" +
            "ALOS World 3D and SRTM15+ V2.1 (Global Bathymetry 500m). \n" +
            "Note: Requests are limited to \n" +
            "500,000,000 km2 for GEDI L3, \n" +
            "125,000,000 km2 for SRTM15+ V2.1, \n" +
            "4,050,000 km2 for SRTM GL3, COP90 and \n" +
            "450,000 km2 for all other data.";
#pragma warning disable CS0414 
        [SerializeField]
        [Multiline(10)]
        private string m_demTypeInstruction = DEM_INSTRUCTION;
#pragma warning restore CS0414 

        public string apiKey { get; set; }

        internal void OnValidate()
        {
            m_demTypeInstruction = DEM_INSTRUCTION;
        }

        public DataRequest RequestHeightMap(GeoRect gps)
        {
            ProgressHandler progress = new ProgressHandler();
            DataRequest request = new DataRequest(progress);
            CoroutineUtility.StartCoroutine(IRequestHeightMap(request, progress, gps));
            return request;
        }

        private IEnumerator IRequestHeightMap(DataRequest dataRequest, ProgressHandler progress, GeoRect gps)
        {
            GeoRect expandedGPS = gps.ExpandMeters(BOUNDS_EXPANSION);
            UnityWebRequest webRequest = CreateWebRequest(expandedGPS);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            webRequest.SendWebRequest();
            while (!webRequest.isDone)
            {
                progress.value = webRequest.downloadProgress;
                yield return null;
            }

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string contentType = webRequest.GetResponseHeader("Content-Type");
                if (!string.Equals(contentType.ToLower(), "application/octet-stream"))
                {
                    Debug.Log("The server respond no data for the selected region & dataset");
                }
                else
                {
                    string content = (webRequest.downloadHandler as DownloadHandlerBuffer).text;
                    AAIGridReader.Result readResult = AAIGridReader.Read(content);
                    GeoRect m_actualRect = readResult.header.CalculateRectGPS();
                    if (readResult.success)
                    {
                        float[] data = readResult.data;
                        int width = readResult.width;
                        int height = readResult.height;
                        RemapHeightData(readResult.data);
                        ProgressiveTask extractTask = new ProgressiveTask();
                        CoroutineUtility.StartCoroutine(Utilities.ExtractHeightData(extractTask, data, width, height, m_actualRect, gps, data));
                        yield return extractTask;

                        Vector2Int dataSize = new Vector2Int(width, height);
                        postProcessHeightDataCallback?.Invoke(gps, ref data, ref dataSize);

                        dataRequest.heightMapData = data;
                        dataRequest.heightMapSize = dataSize;
                    }
                }
            }
            else
            {
                Debug.LogError(webRequest.error);
            }

            webRequest.Dispose();
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

        private UnityWebRequest CreateWebRequest(GeoRect gps)
        {
            //Doc
            //https://portal.opentopography.org/apidocs/#/Public/getGlobalDem

            string uri = $"https://portal.opentopography.org/API/globaldem?demtype={m_demType.ToString()}&south={gps.minY}&north={gps.maxY}&west={gps.minX}&east={gps.maxX}&outputFormat=AAIGrid&API_Key={apiKey}";
            UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            return webRequest;
        }

        public DataRequest RequestColorMap(GeoRect gps)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
