using System;
using System.Collections.Generic;

namespace TPSBR.Abilities
{
    [Serializable]
    public abstract class AbilityUpgradeData
    {
        public abstract IEnumerable<AbilityUpgradeTrack> EnumerateTracks();

#if UNITY_EDITOR
        public virtual void OnValidate()
        {
        }
#endif
    }

    public readonly struct AbilityUpgradeTrack
    {
        public AbilityUpgradeTrack(string id, string displayName, IReadOnlyList<float> perLevelValues)
        {
            Id = string.IsNullOrWhiteSpace(id) ? string.Empty : id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            PerLevelValues = perLevelValues ?? Array.Empty<float>();
        }

        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<float> PerLevelValues { get; }
        public int MaxRank => PerLevelValues.Count;

        public float GetValueForRank(int rank)
        {
            if (rank <= 0 || rank > PerLevelValues.Count)
            {
                return 0f;
            }

            return PerLevelValues[rank - 1];
        }
    }
}
