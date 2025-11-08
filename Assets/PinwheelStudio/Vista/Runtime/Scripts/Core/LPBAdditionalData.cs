#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.Geometric;
using System.Linq;

namespace Pinwheel.Vista
{
    [AddComponentMenu("")]
    public class LPBAdditionalData : MonoBehaviour
    {
        [SerializeField]
        private List<int> m_hexagonTrace = new List<int>();

        [SerializeField]
        private float m_hexagonRadius;
        public float hexagonRadius
        {
            get
            {
                return m_hexagonRadius;
            }
            set
            {
                m_hexagonRadius = value;
            }
        }

        [SerializeField]
        private Hexagon2D.Orientation m_hexagonOrientation;
        public Hexagon2D.Orientation hexagonOrientation
        {
            get
            {
                return m_hexagonOrientation;
            }
            set
            {
                m_hexagonOrientation = value;
            }
        }

        private void Reset()
        {
            m_hexagonRadius = 100;
        }

        public void AddHexTrace(int direction)
        {
            Debug.Assert(direction >= 0 && direction < 6);
            m_hexagonTrace.Add(direction);
        }

        public void RemoveLastHexagon()
        {
            if (m_hexagonTrace.Count > 0)
            {
                m_hexagonTrace.RemoveAt(m_hexagonTrace.Count - 1);
            }
        }

        public int GetHexagonTraceCount()
        {
            return m_hexagonTrace.Count;
        }

        public void ClearHexagons()
        {
            m_hexagonTrace.Clear();
        }

        public List<Hexagon2D> GetHexagons(bool makeUnique = false)
        {
            List<Hexagon2D> hexagons = new List<Hexagon2D>();
            Hexagon2D hex = new Hexagon2D(Vector2.zero, m_hexagonRadius, m_hexagonOrientation);
            hexagons.Add(hex);

            CustomHexComparer comp = new CustomHexComparer();

            foreach (int t in m_hexagonTrace)
            {
                Line2D segment = hex.GetSegment(t);
                Vector2 segmentCenter = segment.startPoint * 0.5f + segment.endPoint * 0.5f;
                Vector2 nextHexCenter = 2 * segmentCenter - hex.center;
                hex = new Hexagon2D(nextHexCenter, m_hexagonRadius, m_hexagonOrientation);
                if (makeUnique)
                {
                    if (hexagons.Exists(h => comp.Equals(hex, h)))
                    {
                        continue;
                    }
                }
                hexagons.Add(hex);
            }

            return hexagons;
        }

        private struct CustomHexComparer : IEqualityComparer<Hexagon2D>
        {
            public bool Equals(Hexagon2D x, Hexagon2D y)
            {
                return Vector2.Distance(x.center, y.center) < x.radius;
            }

            public int GetHashCode(Hexagon2D obj)
            {
                return obj.GetHashCode();
            }
        }

        public List<Vector2> GenerateHexContour()
        {
            List<Hexagon2D> uniqueHex = GetHexagons(true);

            List<Line2D> allSegments = new List<Line2D>();
            Line2D[] tmpSegments = new Line2D[6];
            foreach (Hexagon2D h in uniqueHex)
            {
                h.GetSegments(tmpSegments);
                allSegments.AddRange(tmpSegments);
            }

            int[] counts = new int[allSegments.Count];
            for (int i = 0; i < allSegments.Count; ++i)
            {
                Line2D s0 = allSegments[i];
                Vector2 s0c = s0.Center;
                int count = 0;
                for (int j = 0; j < allSegments.Count; ++j)
                {
                    Line2D s1 = allSegments[j];
                    Vector2 s1c = s1.Center;
                    if (Vector2.Distance(s0c, s1c) < m_hexagonRadius * 0.1f)
                        count += 1;
                }
                counts[i] = count;
            }

            List<Line2D> nonOverlappedSegment = new List<Line2D>();
            for (int i = 0; i < allSegments.Count; ++i)
            {
                if (counts[i] == 1)
                    nonOverlappedSegment.Add(allSegments[i]);
            }

            for (int i = 0; i < nonOverlappedSegment.Count - 1; ++i)
            {
                Line2D s0 = nonOverlappedSegment[i];
                for (int j = i + 1; j < nonOverlappedSegment.Count; ++j)
                {
                    Line2D s1 = nonOverlappedSegment[j];
                    if (Vector2.Distance(s0.endPoint, s1.startPoint) < m_hexagonRadius * 0.1f)
                    {
                        Line2D tmp = nonOverlappedSegment[i + 1];
                        nonOverlappedSegment[i + 1] = nonOverlappedSegment[j];
                        nonOverlappedSegment[j] = tmp;
                    }
                }
            }

            List<Vector2> contour = new List<Vector2>();
            for (int i = nonOverlappedSegment.Count - 1; i >= 0; --i)
            {
                Line2D s = nonOverlappedSegment[i];
                contour.Add(s.startPoint);
            }

            return contour;
        }

        public static Vector2 FindNearestPointOnHexGrid(Vector2 p, float hexRadius, Hexagon2D.Orientation hexOrientation)
        {
            Vector2[] hx = new Vector2[4];
            hx[0] = new Vector2(p.x - hexRadius * 0.5f, p.y);
            hx[0] = LPBAdditionalData.FindNearestPointOnHexGridInternal(hx[0], hexRadius, hexOrientation);
            hx[1] = new Vector2(p.x + hexRadius * 0.5f, p.y);
            hx[1] = LPBAdditionalData.FindNearestPointOnHexGridInternal(hx[1], hexRadius, hexOrientation);
            hx[2] = new Vector2(p.x, p.y - hexRadius * 0.5f);
            hx[2] = LPBAdditionalData.FindNearestPointOnHexGridInternal(hx[2], hexRadius, hexOrientation);
            hx[3] = new Vector2(p.x, p.y + hexRadius * 0.5f);
            hx[3] = LPBAdditionalData.FindNearestPointOnHexGridInternal(hx[3], hexRadius, hexOrientation);

            float dMin = float.MaxValue;
            Vector2 hexPoint = new Vector2();
            for (int i = 0; i < 4; ++i)
            {
                float sqrMag = Vector2.SqrMagnitude(p - hx[i]);
                if (sqrMag < dMin)
                {
                    dMin = sqrMag;
                    hexPoint = hx[i];
                }
            }
            return hexPoint;
        }

        private static Vector2 FindNearestPointOnHexGridInternal(Vector2 p, float hexRadius, Hexagon2D.Orientation hexOrientation)
        {
            if (hexOrientation == Hexagon2D.Orientation.Right)
            {
                float s = Mathf.Sqrt(3.0f) * 0.5f;
                float fy = Mathf.Round(p.y / (hexRadius * s));

                float px = p.x + (fy % 2 != 0 ? hexRadius * 1.5f : 0);
                float fx = Mathf.Round(px / (hexRadius * 3.0f));
                float offsetX = fy % 2 != 0 ? -hexRadius * 1.5f : 0;

                float y = fy * hexRadius * s;
                float x = fx * hexRadius * 3.0f + offsetX;

                return new Vector2(x, y);
            }
            else
            {
                float s = Mathf.Sqrt(3.0f) * 0.5f;
                float fx = Mathf.Round(p.x / (hexRadius * s));

                float py = p.y + (fx % 2 != 0 ? hexRadius * 1.5f : 0);
                float fy = Mathf.Round(py / (hexRadius * 3.0f));
                float offsetY = fx % 2 != 0 ? -hexRadius * 1.5f : 0;

                float x = fx * hexRadius * s;
                float y = fy * hexRadius * 3.0f + offsetY;

                return new Vector2(x, y);
            }

        }
    }
}
#endif
