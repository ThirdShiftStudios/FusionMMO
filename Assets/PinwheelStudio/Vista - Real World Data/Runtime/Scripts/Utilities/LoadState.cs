#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Vista.RealWorldData
{
    public enum LoadState
    {
        NotLoaded,
        Loading,
        Loaded,
        FailToLoad,
        Unknown
    }
}
#endif
