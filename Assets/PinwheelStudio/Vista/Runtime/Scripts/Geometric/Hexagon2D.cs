#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Pinwheel.Vista.Geometric
{
    [System.Serializable]
    public struct Hexagon2D 
    {
        [SerializeField]
        private Vector2 m_center;
        public Vector2 center
        {
            get
            {
                return m_center;
            }
            set
            {
                m_center = value;
            }
        }

        [SerializeField]
        private float m_radius;
        public float radius
        {
            get
            {
                return m_radius;
            }
            set
            {
                Debug.Assert(value >= 0);
                m_radius = value;
            }
        }

        public enum Orientation { Right, Top }

        [SerializeField]
        private Orientation m_orientation;
        public Orientation orientation
        {
            get
            {
                return m_orientation;
            }
            set
            {
                m_orientation = value;
            }
        }

        public Hexagon2D(Vector2 center, float radius, Orientation orientation)
        {
            Debug.Assert(radius >= 0);
            m_center = center;
            m_radius = radius;
            m_orientation = orientation;
        }

        public Vector2 GetPoint(int index)
        {
            Debug.Assert(index >= 0 && index < 6);
            float startAngle = m_orientation == Orientation.Right ? 0 : -30;
            float angle = startAngle + index * 60;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 p = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * m_radius + center;
            return p;
        }

        public void GetPoints(Vector2[] points, int writeStartIndex = 0)
        {
            Debug.Assert(points != null && points.Length >= 6);
            for (int i = 0; i < 6; ++i)
            {
                points[i + writeStartIndex] = GetPoint(i);
            }
        }

        public Line2D GetSegment(int index)
        {
            Debug.Assert(index >= 0 && index < 6);
            if (index < 5)
            {
                Vector2 start = GetPoint(index);
                Vector2 end = GetPoint(index + 1);
                return new Line2D(start, end);
            }
            else
            {
                Vector2 start = GetPoint(5);
                Vector2 end = GetPoint(0);
                return new Line2D(start, end);
            }
        }

        public void GetSegments(Line2D[] segments, int writeStartIndex = 0)
        {
            Debug.Assert(segments != null && segments.Length >= 6);
            for (int i = 0; i < 6; ++i)
            {
                segments[i + writeStartIndex] = GetSegment(i);
            }
        }

        public override int GetHashCode()
        {
            return center.GetHashCode() ^ radius.GetHashCode() ^ orientation.GetHashCode();
        }
    }
}
#endif
