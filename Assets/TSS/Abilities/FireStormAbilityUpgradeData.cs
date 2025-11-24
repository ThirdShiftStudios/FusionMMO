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
            FireStormAbilityUpgradeLevel resolvedLevel = _level;

            return new FireStormAbilityLevelData
            {
                Damage = ApplyPerLevelIncrease(resolvedLevel.Damage, resolvedLevel.DamageIncreasePercent, clampedLevel),
                Duration = ApplyPerLevelIncrease(resolvedLevel.Duration, resolvedLevel.DurationIncreasePercent, clampedLevel),
                Radius = ApplyPerLevelIncrease(resolvedLevel.Radius, resolvedLevel.RadiusIncreasePercent, clampedLevel),
                CastingTime = ApplyPerLevelIncrease(resolvedLevel.CastingTime, resolvedLevel.CastingTimeIncreasePercent, clampedLevel)
            };
        }
    }
}
