#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

namespace Pinwheel.Vista.RealWorldData
{
    public interface IRwdProvider
    {
        DataAvailability availability { get; }
        DataRequest RequestHeightMap(GeoRect gps);
        DataRequest RequestColorMap(GeoRect gps);
    }
}
#endif
