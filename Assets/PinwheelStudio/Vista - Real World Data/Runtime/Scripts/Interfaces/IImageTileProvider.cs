#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Pinwheel.Vista.RealWorldData
{
    public interface IImageTileProvider
    {
        int minZoom { get; }
        int maxZoom { get; }
        UnityWebRequest CreateTileRequest(int zoom, int x, int y);
    }
}
#endif
