#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Math = System.Math;

namespace Pinwheel.Vista.RealWorldData
{
    public static class MapTileUtilities
    {
        public const int COMMON_TILE_SIZE = 256;

        public static void ForEach<T>(this T tile, System.Action<T> action) where T : class, IMapTile<T>, new()
        {
            Stack<T> stack = new Stack<T>();
            stack.Push(tile);
            while (stack.Count != 0)
            {
                T t = stack.Pop();
                if (t.topLeft != null)
                {
                    stack.Push(t.topLeft);
                }
                if (t.topRight != null)
                {
                    stack.Push(t.topRight);
                }
                if (t.bottomLeft != null)
                {
                    stack.Push(t.bottomLeft);
                }
                if (t.bottomRight != null)
                {
                    stack.Push(t.bottomRight);
                }
                action.Invoke(t);
            }
        }

        public static bool ChildrenNotNull<T>(this T tile) where T : class, IMapTile<T>, new()
        {
            return tile.topLeft != null && tile.topRight != null && tile.bottomLeft != null && tile.bottomRight != null;
        }
        
        public static void Split<T>(this T tile) where T : class, IMapTile<T>, new()
        {
            int nextZoom = tile.zoom + 1;

            tile.topLeft = new T();
            tile.topLeft.x = tile.x * 2;
            tile.topLeft.y = tile.y * 2 + 1;
            tile.topLeft.zoom = nextZoom;
            tile.topLeft.bounds100 = tile.bounds100.GetTopLeft();

            tile.topRight = new T();
            tile.topRight.x = tile.x * 2 + 1;
            tile.topRight.y = tile.y * 2 + 1;
            tile.topRight.zoom = nextZoom;
            tile.topRight.bounds100 = tile.bounds100.GetTopRight();

            tile.bottomLeft = new T();
            tile.bottomLeft.x = tile.x * 2;
            tile.bottomLeft.y = tile.y * 2;
            tile.bottomLeft.zoom = nextZoom;
            tile.bottomLeft.bounds100 = tile.bounds100.GetBottomLeft();

            tile.bottomRight = new T();
            tile.bottomRight.x = tile.x * 2 + 1;
            tile.bottomRight.y = tile.y * 2;
            tile.bottomRight.zoom = nextZoom;
            tile.bottomRight.bounds100 = tile.bounds100.GetBottomRight();
        }

        public static int CalculateZoom(GeoRect viewport100)
        {
            float f = 200.0f / (float)viewport100.height;
            int z = Mathf.CeilToInt(Mathf.Log(f, 2));
            return z;
        }

        public static T CreateRoot<T>() where T : class, IMapTile<T>, new()
        {
            T root = new T();
            root.x = 0;
            root.y = 0;
            root.zoom = 0;
            root.bounds100 = GeoRect.rect100;

            return root;
        }

        public static List<T> CreateRootTilesForLoopMap<T>(GeoRect viewport100) where T : class, IMapTile<T>, new()
        {
            GeoRect fullRect = GeoRect.rect100;
            double centerX = viewport100.centerX;
            double f = Mathd.InverseLerp(fullRect.minX, fullRect.maxX, centerX);
            f = Math.Floor(f);
            double offsetX = f * fullRect.width;
            T firstTile = CreateRoot<T>();
            firstTile.bounds100 = fullRect.Offset(offsetX, 0);

            List<T> rootTiles = new List<T>();
            rootTiles.Add(firstTile);

            while (rootTiles[0].bounds100.minX > viewport100.minX)
            {
                T root = CreateRoot<T>();
                root.bounds100 = rootTiles[0].bounds100.Offset(-fullRect.width, 0);
                rootTiles.Insert(0, root);
            }

            while (rootTiles[rootTiles.Count - 1].bounds100.maxX < viewport100.maxX)
            {
                T root = CreateRoot<T>();
                root.bounds100 = rootTiles[rootTiles.Count - 1].bounds100.Offset(fullRect.width, 0);
                rootTiles.Add(root);
            }

            return rootTiles;
        }

        public static List<T> GetTilesForRendering<T>(List<T> rootTiles, int zoom, GeoRect viewport100) where T : class, IMapTile<T>, new()
        {
            List<T> tiles = new List<T>();
            Stack<T> stack = new Stack<T>();
            foreach (T root in rootTiles)
            {
                stack.Push(root);
            }

            while (stack.Count > 0)
            {
                T t = stack.Pop();
                if (!GeoRect.Intersect(t.bounds100, viewport100))
                    continue;

                if (t.zoom < zoom)
                {
                    if (!t.ChildrenNotNull())
                    {
                        t.Split<T>();
                    }
                    stack.Push(t.topLeft);
                    stack.Push(t.topRight);
                    stack.Push(t.bottomLeft);
                    stack.Push(t.bottomRight);
                    continue;
                }

                if (t.zoom == zoom)
                {
                    tiles.Add(t);
                    continue;
                }

                if (t.zoom > zoom)
                    continue;
            }

            return tiles;
        }


    }
}
#endif
