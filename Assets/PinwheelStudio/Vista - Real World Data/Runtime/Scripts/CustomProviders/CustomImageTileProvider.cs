#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.RealWorldData;
using System;
using UnityEngine.Networking;
using System.Text;

namespace Pinwheel.Vista.RealWorldData
{
    public class CustomImageTileProvider : IImageTileProvider
    {
        public const string ZOOM = "{z}";
        public const string X = "{x}";
        public const string Y = "{y}";
        public const string QUAD_KEY = "{qk}";
        public const string API_KEY = "{ak}";

        public string urlTemplate { get; set; }
        public string apiKey { get; set; }
        public int minZoom => 1;
        public int maxZoom { get; set; }

        public UnityWebRequest CreateTileRequest(int zoom, int x, int y)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(CreateUrl(urlTemplate, apiKey, zoom, x, y));
            return webRequest;
        }

        public static string CreateUrl(string urlTemplate, string apiKey, int zoom, int x, int y)
        {
            StringBuilder sb = new StringBuilder(urlTemplate)
                .Replace(ZOOM, zoom.ToString())
                .Replace(X, x.ToString())
                .Replace(Y, y.ToString())
                .Replace(QUAD_KEY, Utilities.TileXYToQuadKey(x, y, zoom))
                .Replace(API_KEY, apiKey);
            return sb.ToString();
        }
    }
}
#endif
