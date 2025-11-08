#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Pinwheel.Vista.RealWorldData
{
    [System.Serializable]
    public struct GeoPoint : IEquatable<GeoPoint>
    {
        public const double MAX_LAT_COVERAGE_GPS = 85.051129;

        public double x;
        public double y;

        public GeoPoint(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return (x, y).GetHashCode();
        }

        public bool Equals(GeoPoint other)
        {
            return this == other;
        }

        public static bool operator ==(GeoPoint a, GeoPoint b)
        {
            return a.x == b.x &&
                a.y == b.y;
        }

        public static bool operator !=(GeoPoint a, GeoPoint b)
        {
            return a.x != b.x ||
                a.y != b.y;
        }
    }
}
#endif
