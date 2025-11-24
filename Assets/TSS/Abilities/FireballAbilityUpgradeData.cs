using System;
using UnityEngine;

namespace TPSBR.Abilities
{
    [Serializable]
    public struct FireballAbilityUpgradeLevel
    {
        [Header("Base Values")]
        public float Radius;
        public float Damage;
        public float CastingTime;

        [Header("Per Level Increase (%)")]
        public float RadiusIncreasePercent;
        public float DamageIncreasePercent;
        public float CastingTimeIncreasePercent;
    }

    public struct FireballAbilityLevelData
    {
        public float Radius;
        public float Damage;
        public float CastingTime;
    }

    [Serializable]
    public sealed class FireballAbilityUpgradeData : AbilityUpgradeData
    {
        [SerializeField]
        private FireballAbilityUpgradeLevel _level;

        public FireballAbilityUpgradeLevel Level => _level;
        public override int LevelCount => MaxLevel;

        public FireballAbilityLevelData GetLevelData(int level)
        {
            int clampedLevel = ClampLevel(level);

            FireballAbilityUpgradeLevel resolvedLevel = _level;

            return new FireballAbilityLevelData
            {
                Radius = ApplyPerLevelIncrease(resolvedLevel.Radius, resolvedLevel.RadiusIncreasePercent, clampedLevel),
                Damage = ApplyPerLevelIncrease(resolvedLevel.Damage, resolvedLevel.DamageIncreasePercent, clampedLevel),
                CastingTime = ApplyPerLevelIncrease(resolvedLevel.CastingTime, resolvedLevel.CastingTimeIncreasePercent, clampedLevel)
            };
        }
    }
}
