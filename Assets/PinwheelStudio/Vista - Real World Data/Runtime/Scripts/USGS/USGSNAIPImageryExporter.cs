#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEngine.Networking;
using Pinwheel.Vista.RealWorldData.ArcGIS;

namespace Pinwheel.Vista.RealWorldData.USGS
{
    public class USGSNAIPImageryExporter
    {
        public const string SERVER_URL = "https://imagery.nationalmap.gov/arcgis/rest/services/USGSNAIPImagery/ImageServer/exportImage";
        public const ArcGISImageExporter.Format FORMAT = ArcGISImageExporter.Format.jpg;
        public const ArcGISImageExporter.PixelType PIXEL_TYPE = ArcGISImageExporter.PixelType.U8;

        public const int MAX_IMAGE_SIZE = 4000;

        public static UnityWebRequest CreateWebRequest(GeoRect gps, int width, int height, bool asJson)
        {
            ArcGISImageExporter.Settings s = new ArcGISImageExporter.Settings();
            s.serverUrl = SERVER_URL;
            s.gps = gps;
            s.width = width;
            s.height = height;
            s.format = FORMAT;
            s.pixelType = PIXEL_TYPE;
            s.asJson = asJson;

            UnityWebRequest webRequest = ArcGISImageExporter.CreateWebRequest(s);
            return webRequest;
        }
    }
}
#endif
