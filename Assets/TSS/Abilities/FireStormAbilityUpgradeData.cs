using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.Abilities
{
    [Serializable]
    public struct FireStormAbilityUpgradeLevel
    {
        public float Damage;
        public float Duration;
        public float Radius;
        public float CastingTime;
    }

    [Serializable]
    public sealed class FireStormAbilityUpgradeData : AbilityUpgradeData
    {
        [SerializeField]
        private FireStormAbilityUpgradeLevel[] _levels = Array.Empty<FireStormAbilityUpgradeLevel>();

        public IReadOnlyList<FireStormAbilityUpgradeLevel> Levels => _levels ?? Array.Empty<FireStormAbilityUpgradeLevel>();
        public override int LevelCount => Levels.Count;

        public FireStormAbilityUpgradeLevel GetLevelData(int level)
        {
            if (Levels.Count == 0)
            {
                return default;
            }

            int index = Mathf.Clamp(level - 1, 0, Levels.Count - 1);
            return _levels[index];
        }

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();
            _levels ??= Array.Empty<FireStormAbilityUpgradeLevel>();
        }
#endif
    }
}
