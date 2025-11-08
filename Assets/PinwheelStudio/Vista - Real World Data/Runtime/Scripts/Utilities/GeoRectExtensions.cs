#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;

namespace Pinwheel.Vista.RealWorldData
{
    public static class GeoRectExtensions
    {
        public static GeoRect GetTopLeft(this GeoRect l)
        {
            double avgLong = (l.minX + l.maxX) * 0.5;
            double avgLat = (l.minY + l.maxY) * 0.5;
            GeoRect topLeft = new GeoRect(l.minX, avgLong, l.minY, avgLat);
            return topLeft;
        }

        public static GeoRect GetTopRight(this GeoRect l)
        {
            double avgLong = (l.minX + l.maxX) * 0.5;
            double avgLat = (l.minY + l.maxY) * 0.5;
            GeoRect topLeft = new GeoRect(avgLong, l.maxX, l.minY, avgLat);
            return topLeft;
        }

        public static GeoRect GetBottomLeft(this GeoRect l)
        {
            double avgLong = (l.minX + l.maxX) * 0.5;
            double avgLat = (l.minY + l.maxY) * 0.5;
            GeoRect topLeft = new GeoRect(l.minX, avgLong, avgLat, l.maxY);
            return topLeft;
        }

        public static GeoRect GetBottomRight(this GeoRect l)
        {
            double avgLong = (l.minX + l.maxX) * 0.5;
            double avgLat = (l.minY + l.maxY) * 0.5;
            GeoRect topLeft = new GeoRect(avgLong, l.maxX, avgLat, l.maxY);
            return topLeft;
        }

        public static GeoRect Scale(this GeoRect l, double f, double pivotLong = 0.5, double pivotLat = 0.5)
        {
            double avgLong = Mathd.Lerp(l.minX, l.maxX, pivotLong);
            double avgLat = Mathd.Lerp(l.minY, l.maxY, pivotLat);
            GeoRect l0 = new GeoRect();
            l0.minX = avgLong - (avgLong - l.minX) * f;
            l0.maxX = avgLong + (l.maxX - avgLong) * f;
            l0.minY = avgLat - (avgLat - l.minY) * f;
            l0.maxY = avgLat + (l.maxY - avgLat) * f;
            return l0;
        }

        public static GeoRect Offset(this GeoRect l, double dLong, double dLat)
        {
            GeoRect l0 = l;
            l0.minX += dLong;
            l0.maxX += dLong;
            l0.minY += dLat;
            l0.maxY += dLat;
            return l0;
        }

        public static GeoRect MoveTowards(GeoRect a, GeoRect b, double dx = 1, double dy = 1)
        {
            GeoRect v = new GeoRect();
            v.minX = Mathd.MoveTowards(a.minX, b.minX, dx);
            v.maxX = Mathd.MoveTowards(a.maxX, b.maxX, dx);
            v.minY = Mathd.MoveTowards(a.minY, b.minY, dy);
            v.maxY = Mathd.MoveTowards(a.maxY, b.maxY, dy);
            return v;
        }

        public static GeoRect GpsToWebMercator(this GeoRect r, int zoom = 0)
        {
            GeoPoint min = new GeoPoint(r.minX, r.minY).GpsToWebMercator(zoom);
            GeoPoint max = new GeoPoint(r.maxX, r.maxY).GpsToWebMercator(zoom);
            GeoRect r0 = new GeoRect(min.x, max.x, min.y, max.y);
            return r0;
        }

        public static GeoRect WebMercatorToGps(this GeoRect r, int zoom = 0)
        {
            GeoPoint min = new GeoPoint(r.minX, r.minY).WebMercatorToGps(zoom);
            GeoPoint max = new GeoPoint(r.maxX, r.maxY).WebMercatorToGps(zoom);
            GeoRect r0 = new GeoRect(min.x, max.x, min.y, max.y);
            return r0;
        }

        public static GeoRect WebMercatorToViewport100(this GeoRect r, int zoom = 0)
        {
            GeoPoint min = new GeoPoint(r.minX, r.minY).WebMercatorToViewport100(zoom);
            GeoPoint max = new GeoPoint(r.maxX, r.maxY).WebMercatorToViewport100(zoom);
            GeoRect r0 = new GeoRect(min.x, max.x, min.y, max.y);
            return r0;
        }

        public static GeoRect Viewport100ToWebMercator(this GeoRect r, int zoom = 0)
        {
            GeoPoint min = new GeoPoint(r.minX, r.minY).Viewport100ToWebMercator(zoom);
            GeoPoint max = new GeoPoint(r.maxX, r.maxY).Viewport100ToWebMercator(zoom);
            GeoRect r0 = new GeoRect(min.x, max.x, min.y, max.y);
            return r0;
        }

        public static void CalculateSizeApproxInKMs(this GeoRect r, out double width, out double height)
        {
            width = r.width * Mathd.DEGREE_TO_KM;
            height = r.height * Mathd.DEGREE_TO_KM;
        }

        public static GeoRect ValidateGPS(this GeoRect r)
        {
            GeoPoint min = new GeoPoint(r.minX, r.minY).ValidateGPS();
            GeoPoint max = new GeoPoint(r.maxX, r.maxY).ValidateGPS();
            GeoRect r0 = new GeoRect(min.x, max.x, min.y, max.y);
            return r0;
        }

        /// <summary>
        /// Expand each side of the rect by meters
        /// </summary>
        /// <param name="r"></param>
        /// <param name="meters"></param>
        /// <returns></returns>
        public static GeoRect ExpandMeters(this GeoRect r, double meters)
        {
            double d = meters * Mathd.M_TO_DEGREE;
            r.minX -= d;
            r.maxX += d;
            r.minY -= d;
            r.maxY += d;
            return r;
        }
    }
}
#endif
