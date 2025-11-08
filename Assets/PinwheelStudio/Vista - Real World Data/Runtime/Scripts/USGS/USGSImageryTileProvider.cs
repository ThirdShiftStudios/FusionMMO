#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.RealWorldData;
using System;
using UnityEngine.Networking;

namespace Pinwheel.Vista.RealWorldData.USGS
{
    public class USGSImageryTileProvider : IImageTileProvider
    {
        public int minZoom => 0;
        public int maxZoom => 18;

        public UnityWebRequest CreateTileRequest(int zoom, int x, int y)
        {
            //Doc
            //https://basemap.nationalmap.gov/arcgis/rest/services/USGSImageryOnly/MapServer
            //Request syntax
            //https://basemap.nationalmap.gov/arcgis/rest/services/USGSImageryOnly/MapServer/tile/{zoom}/{y}/{x}

            string uri = $"https://basemap.nationalmap.gov/arcgis/rest/services/USGSImageryOnly/MapServer/tile/{zoom}/{y}/{x}";
            UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            return webRequest;
        }
    }
}
#endif
    