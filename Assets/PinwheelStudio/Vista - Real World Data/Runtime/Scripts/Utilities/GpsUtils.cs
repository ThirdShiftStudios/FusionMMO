#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;

namespace Pinwheel.Vista.RealWorldData
{
    public static class GpsUtils
    {
        public static readonly GeoRect VIETNAM = new GeoRect(100.19733, 115.46933, 8.53067, 23.80267);
        public static readonly GeoRect VIETNAM_TAXUA = new GeoRect(104.32779735924106, 104.77854210836347, 21.218820709892697, 21.669277422465015);
        public static readonly GeoRect USA = new GeoRect(-138.935919991872, -57.9907302394041, 18.4373925563469, 54.0332330412418);
        public static readonly GeoRect USA_YELLOWSTONE = new GeoRect(-111.05128006590652, -110.1496257188974, 44.097089288742779, 44.999025135136719);
        public static readonly GeoRect USA_PART_OF_COLORADO = new GeoRect(-107.99962201830448, -107.09767135222233, 37.37803947954967, 38.289448805843165);
    }
}
#endif
