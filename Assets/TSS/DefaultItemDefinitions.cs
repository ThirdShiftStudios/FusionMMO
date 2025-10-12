using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public class DefaultItemDefinitions : GlobalDataDefinition
    {
        private const string ResourcePath = "DefaultItemDefinitions";

        private static DefaultItemDefinitions _instance;

        public override string Name => "Default Items";
        public override Texture2D Icon { get; }

        [SerializeField]
        private PickaxeDefinition _defaultPickaxe;
        [SerializeField]
        private WoodAxeDefinition _defaultWoodAxe;
        public static DefaultItemDefinitions Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<DefaultItemDefinitions>(ResourcePath);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (_instance == null)
                    {
                        Debug.LogWarning($"Unable to locate {nameof(DefaultItemDefinitions)} asset at Resources/{ResourcePath}.");
                    }
#endif
                }

                return _instance;
            }
        }

        public PickaxeDefinition DefaultPickaxe => _defaultPickaxe;
        public WoodAxeDefinition DefaultWoodAxe => _defaultWoodAxe;
    }
}
