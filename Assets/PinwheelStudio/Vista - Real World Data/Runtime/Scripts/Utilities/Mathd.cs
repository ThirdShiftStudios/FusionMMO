#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Vista.RealWorldData
{
    public struct Mathd
    {
        public static double DEGREE_TO_KM = 111.139;
        public static double KM_TO_DEGRESS = 1.0 / DEGREE_TO_KM;
        public static double DEGREE_TO_M = 111139.0;
        public static double M_TO_DEGREE = 1.0 / DEGREE_TO_M;

        public static double Clamp(double value, double a, double b)
        {
            if (value < a)
                value = a;
            if (value > b)
                value = b;
            return value;
        }

        public static double MoveTowards(double current, double target, double maxDelta)
        {
            if (System.Math.Abs(target - current) <= maxDelta)
            {
                return target;
            }
            return current + System.Math.Sign(target - current) * maxDelta;
        }

        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        public static double InverseLerp(double min, double max, double value)
        {
            if (min == max)
                return 0;
            else
                return (value - min) / (max - min);
        }
    }
}
#endif
