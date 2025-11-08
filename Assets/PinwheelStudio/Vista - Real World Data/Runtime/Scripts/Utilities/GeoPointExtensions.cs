#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Math = System.Math;

namespace Pinwheel.Vista.RealWorldData
{
    public static class GeoPointExtensions
    {
        //https://en.wikipedia.org/wiki/Web_Mercator_projection
        private static readonly double ADJUSTMENT = 256.0;
        private static readonly double PI = Math.PI;
        private static readonly double TWO_PI = 2 * Math.PI;
        private static readonly double DEG2RAD = TWO_PI / 360.0;
        private static readonly double RAD2DEG = 360.0 / TWO_PI;

        public static GeoPoint GpsToWebMercator(this GeoPoint p, int zoom = 0)
        {
            if (p.y < -GeoPoint.MAX_LAT_COVERAGE_GPS || p.y > GeoPoint.MAX_LAT_COVERAGE_GPS)
            {
                UnityEngine.Debug.LogWarning($"Convert {nameof(GeoPoint)} near the pole (abs(y)>{GeoPoint.MAX_LAT_COVERAGE_GPS}) will yield incorrect result");
            }

            double longRad = p.x * DEG2RAD;
            double latRad = p.y * DEG2RAD;

            double a = Math.Pow(2, zoom) * ADJUSTMENT / TWO_PI;
            double x = a * (longRad + PI);

            double t = Math.Tan(PI / 4 + latRad / 2);
            double ln = Math.Log(t);
            double y = a * (PI - ln);

            return new GeoPoint(x, y);
        }

        public static GeoPoint WebMercatorToGps(this GeoPoint p, int zoom = 0)
        {
            double a = Math.Pow(2, zoom) * ADJUSTMENT / TWO_PI;
            double lon = RAD2DEG * (p.x / a - PI);

            double c = Math.Exp(PI - p.y / a);
            double lat = RAD2DEG * 2 * Math.Atan((c - 1) / (c + 1));

            return new GeoPoint(lon, lat);
        }

        public static GeoPoint WebMercatorToViewport100(this GeoPoint p, int zoom = 0)
        {
            double tileCountAtZoom = Math.Pow(2, zoom);
            double pixelCountAtZoom = tileCountAtZoom * 256;
            double x = (p.x / pixelCountAtZoom) * 2.0 - 1.0;
            double y = -((p.y / pixelCountAtZoom) * 2.0 - 1.0);
            GeoPoint p100 = new GeoPoint(x * 100.0, y * 100.0);
            return p100;
        }

        public static GeoPoint Viewport100ToWebMercator(this GeoPoint p, int zoom = 0)
        {
            double tileCountAtZoom = Math.Pow(2, zoom);
            double pixelCountAtZoom = tileCountAtZoom * 256;

            double x01 = p.x / 100.0;
            double y01 = p.y / 100.0;

            double x = (x01 + 1) * pixelCountAtZoom / 2.0;
            double y = -(y01 - 1) * pixelCountAtZoom / 2.0;
            return new GeoPoint(x, y);
        }

        public static GeoPoint ValidateGPS(this GeoPoint p)
        {
            double dx = Mathd.InverseLerp(-180, 180, p.x);
            double dy = Mathd.InverseLerp(-90, 90, p.y);
            dx = dx - Math.Floor(dx);
            dy = dy - Math.Floor(dy);

            double x0 = Mathd.Lerp(-180, 180, dx);
            double y0 = Mathd.Lerp(-90, 90, dy);

            GeoPoint p0 = new GeoPoint(x0, y0);
            return p0;
        }
    }
}
#endif
