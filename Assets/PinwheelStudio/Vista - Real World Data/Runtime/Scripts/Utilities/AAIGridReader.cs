#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using System.IO;
using System.Text;

namespace Pinwheel.Vista.RealWorldData
{
    public static class AAIGridReader
    {
        private static readonly string[] SPACE = new string[] { " " };
        private static readonly System.StringSplitOptions SPLIT_OPTION = System.StringSplitOptions.RemoveEmptyEntries;

        [System.Serializable]
        public class Header
        {
            public const string NCOLS = "ncols";
            public const string NROWS = "nrows";
            public const string XLL = "xll";
            public const string YLL = "yll";
            public const string CELL_SIZE = "cellsize";
            public const string NO_DATA = "nodata";

            public int nCols;
            public int nRows;
            public double xLowerLeft;
            public double yLowerLeft;
            public double cellSize;
            public double noDataValue;
            public int lineCount;
            public bool isValid;

            public static bool IsHeaderLine(string s)
            {
                s = s.ToLower();
                return s.StartsWith(NCOLS) || s.StartsWith(NROWS) || s.StartsWith(XLL) || s.StartsWith(YLL) || s.StartsWith(CELL_SIZE) || s.StartsWith(NO_DATA);
            }

            public void Parse(string s)
            {
                s = s.ToLower();
                if (s.StartsWith(NCOLS))
                {
                    nCols = int.Parse(s.Split(SPACE, SPLIT_OPTION)[1]);
                }
                if (s.StartsWith(NROWS))
                {
                    nRows = int.Parse(s.Split(SPACE, SPLIT_OPTION)[1]);
                }
                if (s.StartsWith(XLL))
                {
                    xLowerLeft = double.Parse(s.Split(SPACE, SPLIT_OPTION)[1]);
                }
                if (s.StartsWith(YLL))
                {
                    yLowerLeft = double.Parse(s.Split(SPACE, SPLIT_OPTION)[1]);
                }
                if (s.StartsWith(CELL_SIZE))
                {
                    cellSize = double.Parse(s.Split(SPACE, SPLIT_OPTION)[1]);
                }
                if (s.StartsWith(NO_DATA))
                {
                    double.TryParse(s.Split(SPACE, SPLIT_OPTION)[1], out noDataValue);
                }
            }

            public GeoRect CalculateRectGPS()
            {
                double minX = xLowerLeft;
                double minY = yLowerLeft;
                double maxX = xLowerLeft + (nCols - 1) * cellSize;
                double maxY = yLowerLeft + (nRows - 1) * cellSize;
                return new GeoRect(minX, maxX, minY, maxY);
            }
        }

        public class Result
        {
            public Header header;
            public bool success;
            public int width;
            public int height;
            public float[] data;
        }

        public static Result Read(string content)
        {
            Header h = ReadHeader(content);
            Result result = new Result();
            result.header = h;
            if (!h.isValid)
            {
                result.success = false;
                return result;
            }

            StringReader reader = new StringReader(content);
            try
            {
                for (int i = 0; i < h.lineCount; ++i)
                {
                    reader.ReadLine();
                }

                result.width = h.nCols;
                result.height = h.nRows;

                float[] data = new float[h.nCols * h.nRows];
                for (int y = 0; y < h.nRows; ++y)
                {
                    string[] words = reader.ReadLine().Split(SPACE, SPLIT_OPTION);
                    for (int x = 0; x < h.nCols; ++x)
                    {
                        float value = float.Parse(words[x]);
                        int i = h.nCols * (h.nRows - 1 - y) + x;
                        data[i] = value;
                    }
                }

                result.data = data;
                result.success = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Fail to read AAIGrid file");
                Debug.LogError(e);
                result.success = false;
            }
            finally
            {
                reader.Dispose();
            }

            return result;
        }

        private static Header ReadHeader(string content)
        {
            Header h = new Header();
            StringReader reader = new StringReader(content);
            List<string> lines = new List<string>();

            try
            {
                while (true)
                {
                    string s = reader.ReadLine();
                    if (Header.IsHeaderLine(s))
                    {
                        lines.Add(s);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Fail to read AAIGrid header");
                Debug.LogError(e);
                h.isValid = false;
            }
            finally
            {
                reader.Dispose();
            }

            foreach (string s in lines)
            {
                h.Parse(s);
            }
            h.lineCount = lines.Count;
            h.isValid = true;
            return h;
        }
    }
}
#endif
