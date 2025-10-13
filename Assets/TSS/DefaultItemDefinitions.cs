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
        private string _defaultPickaxeConfiguration;
        [SerializeField]
        private WoodAxeDefinition _defaultWoodAxe;
        [SerializeField]
        private string _defaultWoodAxeConfiguration;
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
        public string DefaultPickaxeConfiguration => _defaultPickaxeConfiguration;
        public WoodAxeDefinition DefaultWoodAxe => _defaultWoodAxe;
        public string DefaultWoodAxeConfiguration => _defaultWoodAxeConfiguration;
    }
}
