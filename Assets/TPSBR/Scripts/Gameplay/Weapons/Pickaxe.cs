using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public class Pickaxe : Tool
    {
        [SerializeField]
        private PickaxeDefinition _pickaxeDefinition;

        public PickaxeDefinition Definition => _pickaxeDefinition;
    }
}
