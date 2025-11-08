#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Vista.RealWorldData
{
    public interface IMapTile<T>
    {
        int x { get; set; }
        int y { get; set; }
        int zoom { get; set; }
        GeoRect bounds100 { get; set; }

        T topLeft { get; set; }
        T topRight { get; set; }
        T bottomLeft { get; set; }
        T bottomRight { get; set; }
    }
}
#endif
