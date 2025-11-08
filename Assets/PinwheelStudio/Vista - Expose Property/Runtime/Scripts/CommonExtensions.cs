#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using System.Linq;

namespace Pinwheel.Vista.ExposeProperty
{
    public static class CommonExtensions
    {
        public static PropertyOverride GetPropertyOverride(this LocalProceduralBiome biome, PropertyId id)
        {
            PropertyOverride po = biome.m_propertyOverrides.FirstOrDefault(po => id.Equals(po.id));
            if (po == null)
            {
                po = new PropertyOverride(id.nodeId, id.propertyName);
                if (biome.terrainGraph != null)
                {
                    po.SyncWithGraph(biome.terrainGraph);
                }
                List<PropertyOverride> newOverrideList = new List<PropertyOverride>(biome.m_propertyOverrides);
                newOverrideList.Add(po);
                biome.m_propertyOverrides = newOverrideList.ToArray();
            }
            return po;
        }

    }
}
#endif
