using System;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR
{
    [Serializable]
    public class ProfessionResource
    {
        public Professions.ProfessionIndex Profession => _profession;
        public Sprite Icon => _icon;

        [SerializeField]
        private Professions.ProfessionIndex _profession;
        [SerializeField]
        private Sprite _icon;
    }
    public class ProfessionResourceDefinitions : GlobalDataDefinition
    {
        private const string ResourcePath = "ProfessionResourceDefinitions";

        private static ProfessionResourceDefinitions _instance;
        [SerializeField]
        ProfessionResource[] _resources;
        public override string Name => "Profession Resources";
        public override Sprite Icon { get; }

        public static ProfessionResourceDefinitions Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ProfessionResourceDefinitions>(ResourcePath);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (_instance == null)
                    {
                        Debug.LogWarning($"Unable to locate {nameof(ProfessionResourceDefinitions)} asset at Resources/{ResourcePath}.");
                    }
#endif
                }

                return _instance;
            }
        }

        public ProfessionResource GetResource(Professions.ProfessionIndex profession)
        {
            for (int i = 0; i < _resources.Length; i++)
            {
                if (_resources[i].Profession == profession)
                {
                    return _resources[i];
                }
            }

            return null;
        }

    }
}
