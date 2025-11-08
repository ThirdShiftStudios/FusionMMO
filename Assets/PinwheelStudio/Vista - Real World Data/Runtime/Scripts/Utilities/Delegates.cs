#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;

namespace Pinwheel.Vista.RealWorldData
{
    public static class Delegates
    {
        public delegate void PostProcessDataHandler<T>(GeoRect extents, ref T[] data, ref Vector2Int size);
    }
}
#endif
