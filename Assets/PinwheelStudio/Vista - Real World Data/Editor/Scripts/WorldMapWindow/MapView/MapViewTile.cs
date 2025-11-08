#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.RealWorldData;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Pinwheel.Vista.RealWorldData.USGS;

namespace Pinwheel.VistaEditor.RealWorldData
{
    public class MapViewTile : IMapTile<MapViewTile>, System.IDisposable
    {
        public int x { get; set; }
        public int y { get; set; }
        public int zoom { get; set; }

        /// <summary>
        /// Bounding box of the tile for culling & rendering, value in [-100, 100] 
        /// </summary>
        public GeoRect bounds100 { get; set; }

        public GeoRect boundsWebMercator
        {
            get
            {
                double minX = x * MapTileUtilities.COMMON_TILE_SIZE;
                double maxX = minX + MapTileUtilities.COMMON_TILE_SIZE;

                double minY = y * MapTileUtilities.COMMON_TILE_SIZE;
                double maxY = minY + MapTileUtilities.COMMON_TILE_SIZE;

                return new GeoRect(minX, maxX, minY, maxY);
            }
        }

        public GeoRect boundsGps
        {
            get
            {
                return boundsWebMercator.WebMercatorToGps(zoom);
            }
        }

        public MapViewTile topLeft { get; set; }
        public MapViewTile topRight { get; set; }
        public MapViewTile bottomLeft { get; set; }
        public MapViewTile bottomRight { get; set; }

        private static Dictionary<Vector3Int, Texture2D> s_textures = new Dictionary<Vector3Int, Texture2D>();
        private static Dictionary<Vector3Int, LoadState> s_loadStates = new Dictionary<Vector3Int, LoadState>();
        private static Dictionary<Vector3Int, long> s_httpCodes = new Dictionary<Vector3Int, long>();

        public bool TryGetTexture(out Texture2D tex, IImageTileProvider provider)
        {
            Vector3Int key = new Vector3Int(x, y, zoom);
            LoadState state = GetLoadState(key);
            if (state == LoadState.NotLoaded ||
                state == LoadState.Unknown)
            {
                SetLoadState(key, LoadState.Loading); //Should mark a Loading here to prevent other tiles to send duplicated request
                CoroutineUtility.StartCoroutine(LoadTileProgressive(key, provider)); //Looks like coroutine is late for 1 frame
                tex = null;
                return false;
            }
            else if (state == LoadState.Loading)
            {
                tex = null;
                return false;
            }
            else
            {
                tex = GetTexture(key);
                return true;
            }
        }

        public void Dispose()
        {
            Vector3Int key = new Vector3Int(x, y, zoom);
            MapViewTile.Dispose(key);
        }

        public static Texture2D GetTexture(Vector3Int key)
        {
            Texture2D tex;
            if (s_textures.TryGetValue(key, out tex))
            {
                return tex;
            }
            else
            {
                return Texture2D.redTexture;
            }
        }

        public static void SetTexture(Vector3Int key, Texture2D tex)
        {
            Texture2D existingTexture;
            if (s_textures.TryGetValue(key, out existingTexture))
            {
                if (existingTexture != null)
                {
                    throw new System.InvalidOperationException($"Map texture at key {key} already exist, remove the old texture first.");
                }
            }
            s_textures[key] = tex;
        }

        public static void RemoveAndDestroyTexture(Vector3Int key)
        {
            if (s_textures.ContainsKey(key))
            {
                Texture2D tex = s_textures[key];
                Object.DestroyImmediate(tex);
                s_textures.Remove(key);
            }
        }

        public static LoadState GetLoadState(Vector3Int key)
        {
            LoadState state;
            if (s_loadStates.TryGetValue(key, out state))
            {
                return state;
            }
            else
            {
                return LoadState.NotLoaded;
            }
        }

        public static long GetErrorCode(Vector3Int key)
        {
            long code;
            if (s_httpCodes.TryGetValue(key, out code))
            {
                return code;
            }
            else
            {
                return 0;
            }
        }

        public static void SetLoadState(Vector3Int key, LoadState state)
        {
            s_loadStates[key] = state;
        }

        public static void SetErrorCode(Vector3Int key, long code)
        {
            s_httpCodes[key] = code;
        }

        private static IEnumerator LoadTileProgressive(Vector3Int key, IImageTileProvider provider)
        {
            SetLoadState(key, LoadState.Loading);
            UnityWebRequest baseMapRequest = provider.CreateTileRequest(key.z, key.x, key.y);
            baseMapRequest.downloadHandler = new DownloadHandlerTexture();

            yield return baseMapRequest.SendWebRequest();

            RemoveAndDestroyTexture(key);
            if (baseMapRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    DownloadHandlerTexture downloadHandler = baseMapRequest.downloadHandler as DownloadHandlerTexture;
                    if (downloadHandler.texture != null)
                    {
                        SetTexture(key, downloadHandler.texture);
                        SetLoadState(key, LoadState.Loaded);
                    }
                    else
                    {
                        SetLoadState(key, LoadState.FailToLoad);
                        SetErrorCode(key, baseMapRequest.responseCode);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    SetLoadState(key, LoadState.FailToLoad);
                    SetErrorCode(key, baseMapRequest.responseCode);
                }
            }
            else
            {
                SetLoadState(key, LoadState.FailToLoad);
                SetErrorCode(key, baseMapRequest.responseCode);
            }

            baseMapRequest.disposeDownloadHandlerOnDispose = true;
            baseMapRequest.Dispose();
        }

        internal static int GetMemoryStats()
        {
            int byteCount = 0;
            foreach (Texture2D tex in s_textures.Values)
            {
                if (tex != null)
                {
                    byteCount = tex.width * tex.height * 4;
                }
            }

            return byteCount;
        }

        public static void Dispose(Vector3Int key)
        {
            RemoveAndDestroyTexture(key);
            SetLoadState(key, LoadState.NotLoaded);
        }

        internal static bool IsTileNotAvailable(MapViewTile t)
        {
            Vector3Int key = new Vector3Int(t.x, t.y, t.zoom);
            LoadState loadState = GetLoadState(key);
            long errorCode = GetErrorCode(key);
            return loadState == LoadState.FailToLoad;// && errorCode == 404;
        }

        public static List<Vector3Int> GetAllKeys()
        {
            return new List<Vector3Int>(s_textures.Keys);
        }
    }
}
#endif
