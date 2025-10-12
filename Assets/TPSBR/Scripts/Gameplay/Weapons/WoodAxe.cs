using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public class WoodAxe : Tool
    {
        [SerializeField]
        private WoodAxeDefinition _woodAxeDefinition;

        public WoodAxeDefinition Definition => _woodAxeDefinition;
    }
}