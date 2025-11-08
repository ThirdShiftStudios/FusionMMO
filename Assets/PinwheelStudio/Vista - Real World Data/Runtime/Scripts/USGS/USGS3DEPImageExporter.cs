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
    public class USGS3DEPImageExporter
    {
        public const string SERVER_URL = "https://elevation.nationalmap.gov/arcgis/rest/services/3DEPElevation/ImageServer/exportImage";
        public const ArcGISImageExporter.Format FORMAT = ArcGISImageExporter.Format.tiff;
        public const ArcGISImageExporter.PixelType PIXEL_TYPE = ArcGISImageExporter.PixelType.F32;

        public const int MAX_IMAGE_SIZE = 8000;

        public static UnityWebRequest CreateWebRequest(GeoRect gps, int width, int height)
        {
            ArcGISImageExporter.Settings s = new ArcGISImageExporter.Settings();
            s.serverUrl = SERVER_URL;
            s.gps = gps;
            s.width = width;
            s.height = height;
            s.format = FORMAT;
            s.pixelType = PIXEL_TYPE;
            s.asJson = true;

            UnityWebRequest webRequest = ArcGISImageExporter.CreateWebRequest(s);
            return webRequest;
        }
    }
}
#endif
