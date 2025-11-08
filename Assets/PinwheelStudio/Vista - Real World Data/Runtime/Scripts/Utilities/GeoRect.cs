#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Pinwheel.Vista.RealWorldData
{
    [System.Serializable]
    public struct GeoRect : IEquatable<GeoRect>
    {
        [Tooltip("Usually minimum longitude, or west coordinate [-180,180]")]
        public double minX;
        [Tooltip("Usually maximum longitude, or east coordinate [-180,180]")]
        public double maxX;
        [Tooltip("Usually minimum latitude, or south coordinate [-90,90]")]
        public double minY;
        [Tooltip("Usually maximum latitude, or north coordinate [-90,90]")]
        public double maxY;

        public double width => maxX - minX;
        public double height => maxY - minY;
        public double centerX => (minX + maxX) * 0.5;
        public double centerY => (minY + maxY) * 0.5;
        public GeoPoint center => new GeoPoint(centerX, centerY);
        public double aspect => width / height;
        public float aspectF => (float)aspect;

        public static readonly GeoRect rect100 = new GeoRect(-100.0, 100.0, -100.0, 100.0);
        internal static readonly GeoRect rect100Half = new GeoRect(-50.0, 50.0, -50.0, 50.0);

        public GeoRect(double minX, double maxX, double minY, double maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
        }

        public override string ToString()
        {
            return $"({minX}, {minY}, {maxX}, {maxY})";
        }

        public static void CalculateNormalizedQuadNonAlloc(GeoRect canvas, GeoRect tile, Vector2[] vertices)
        {
            float xMin = (float)Mathd.InverseLerp(canvas.minX, canvas.maxX, tile.minX);
            float xMax = (float)Mathd.InverseLerp(canvas.minX, canvas.maxX, tile.maxX);
            float yMin = (float)Mathd.InverseLerp(canvas.minY, canvas.maxY, tile.minY);
            float yMax = (float)Mathd.InverseLerp(canvas.minY, canvas.maxY, tile.maxY);

            vertices[0] = new Vector2(xMin, yMin);
            vertices[1] = new Vector2(xMin, yMax);
            vertices[2] = new Vector2(xMax, yMax);
            vertices[3] = new Vector2(xMax, yMin);
        }

        public static bool Intersect(GeoRect a, GeoRect b)
        {
            // if rectangle has area 0, no overlap
            if (a.minX == a.maxX || a.minY == a.maxY || b.minX == b.maxX || b.minY == b.maxY)
                return false;

            // If one rectangle is on left side of other
            if (a.minX > b.maxX || b.minX > a.maxX)
                return false;

            // If one rectangle is above other
            if (a.minY > b.maxY || b.minY > a.maxY)
                return false;

            return true;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return (minX, maxX, minY, maxY).GetHashCode();
        }

        public bool Equals(GeoRect other)
        {
            return this == other;
        }

        public static bool operator ==(GeoRect a, GeoRect b)
        {
            return a.minX == b.minX &&
                a.maxX == b.maxX &&
                a.minY == b.maxY &&
                a.maxY == b.maxY;
        }

        public static bool operator !=(GeoRect a, GeoRect b)
        {
            return a.minX != b.minX ||
                   a.maxX != b.maxX ||
                   a.minY != b.maxY ||
                   a.maxY != b.maxY;
        }
    }
}
#endif
