using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.Abilities
{
    public static class AbilityImpactRegistry
    {
        private static readonly Dictionary<string, GameObject> _impactGraphics = new Dictionary<string, GameObject>();

        public static void Register(GameObject impactGraphic)
        {
            if (impactGraphic == null)
            {
                return;
            }

            _impactGraphics[impactGraphic.name] = impactGraphic;
        }

        public static bool TryGet(string impactGraphicName, out GameObject impactGraphic)
        {
            if (string.IsNullOrEmpty(impactGraphicName) == false && _impactGraphics.TryGetValue(impactGraphicName, out impactGraphic) == true)
            {
                return true;
            }

            impactGraphic = null;
            return false;
        }
    }
}
