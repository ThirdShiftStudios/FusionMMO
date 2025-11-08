#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;

namespace Pinwheel.Vista.RealWorldData
{
    public class MapTile : IMapTile<MapTile>
    {
        public int x { get; set; }
        public int y { get; set; }
        public int zoom { get; set; }
        public GeoRect bounds100 { get; set; }

        public MapTile topLeft { get; set; }
        public MapTile topRight { get; set; }
        public MapTile bottomLeft { get; set; }
        public MapTile bottomRight { get; set; }
    }
}
#endif
