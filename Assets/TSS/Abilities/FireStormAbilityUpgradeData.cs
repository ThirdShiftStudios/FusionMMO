using System;
using UnityEngine;

namespace TPSBR.Abilities
{
    [Serializable]
    public struct FireStormAbilityUpgradeLevel
    {
        [Header("Base Values")]
        public float Damage;
        public float Duration;
        public float Radius;
        public float CastingTime;

        [Header("Per Level Increase (%)")]
        public float DamageIncreasePercent;
        public float DurationIncreasePercent;
        public float RadiusIncreasePercent;
        public float CastingTimeIncreasePercent;
    }

    public struct FireStormAbilityLevelData
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
        private FireStormAbilityUpgradeLevel _level;

        public FireStormAbilityUpgradeLevel Level => _level;
        public override int LevelCount => MaxLevel;

        public FireStormAbilityLevelData GetLevelData(int level)
        {
            int clampedLevel = ClampLevel(level);

            return new FireStormAbilityLevelData
            {
                Damage = ApplyPerLevelIncrease(_level.Damage, _level.DamageIncreasePercent, clampedLevel),
                Duration = ApplyPerLevelIncrease(_level.Duration, _level.DurationIncreasePercent, clampedLevel),
                Radius = ApplyPerLevelIncrease(_level.Radius, _level.RadiusIncreasePercent, clampedLevel),
                CastingTime = ApplyPerLevelIncrease(_level.CastingTime, _level.CastingTimeIncreasePercent, clampedLevel)
            };
        }

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();
        }
#endif
    }
}
