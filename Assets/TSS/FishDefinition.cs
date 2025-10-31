using System;
using TSS.Data;
using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "FishDefinition", menuName = "TSS/Data Definitions/Fish")]
    public sealed class FishDefinition : ItemDefinition, IGrantsProfessionExperience
    {
        [SerializeField]
        private FishItem _fishPrefab;

        [SerializeField]
        private Professions.ProfessionIndex _profession = Professions.ProfessionIndex.Fishing;

        [SerializeField, Min(0)]
        private int _experienceAmount = 100;

        public FishItem FishPrefab => _fishPrefab;
        public Professions.ProfessionIndex Profession => _profession;
        public int ExperienceAmount => _experienceAmount;
    }
}
