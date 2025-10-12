using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public class DefaultItemDefinitions : GlobalDataDefinition
    {
        public override string Name => "Default Items";
        public override Texture2D Icon { get; }

        [SerializeField] 
        private PickaxeDefinition _defaultPickaxe;
    }
}