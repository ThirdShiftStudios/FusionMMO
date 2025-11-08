#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Vista.RealWorldData
{
    [System.Flags]
    public enum DataAvailability
    {
        HeightMap = 1,
        ColorMap = 2
    }
}
#endif
