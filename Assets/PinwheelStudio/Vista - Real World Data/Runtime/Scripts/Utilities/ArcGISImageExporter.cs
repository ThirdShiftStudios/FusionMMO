#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Pinwheel.Vista.RealWorldData.ArcGIS
{
    public class ArcGISImageExporter
    {
        public enum PixelType
        {
            C128, C64, F32, F64, S16, S32, S8, U1, U16, U2, U32, U4, U8, UNKNOWN
        }

        public enum Format
        {
            jpgpng, png, png8, png24, jpg, bmp, gif, tiff, png32, bip, bsq, lerc
        }

        public struct Settings
        {
            public string serverUrl;
            public GeoRect gps;
            public int inWkid => 4326; //long lat
            public int outWkid => 4326;
            public int width;
            public int height;
            public Format format;
            public PixelType pixelType;
            public bool asJson;
        }

        [System.Serializable]
        public class Response
        {
            [System.Serializable]
            public class Envelope
            {
                public double xmin, ymin, xmax, ymax;
            }

            public string href;
            public int width;
            public int height;
            public Envelope extent;
        }

        public static UnityWebRequest CreateWebRequest(Settings s)
        {
            string url = $"{s.serverUrl}?{(s.asJson?"f=json":"f=image")}&bbox={s.gps.minX},{s.gps.minY},{s.gps.maxX},{s.gps.maxY}&bboxSR={s.inWkid}&imageSR={s.outWkid}&size={s.width},{s.height}&format={s.format.ToString()}&pixelType={s.pixelType.ToString()}";
            UnityWebRequest request = UnityWebRequest.Get(url);
            
            return request;
        }
    }
}
#endif
