using System;
using UnityEngine;

namespace TPSBR.Abilities
{
    [Serializable]
    public struct FireNovaAbilityUpgradeLevel
    {
        [Header("Base Values")]
        public float Radius;
        public float Damage;
        public float BurnDuration;
        public float BurnDamage;
        public float CastingTime;

        [Header("Per Level Increase (%)")]
        public float RadiusIncreasePercent;
        public float DamageIncreasePercent;
        public float BurnDurationIncreasePercent;
        public float BurnDamageIncreasePercent;
        public float CastingTimeIncreasePercent;
    }

    public struct FireNovaAbilityLevelData
    {
        public float Radius;
        public float Damage;
        public float BurnDuration;
        public float BurnDamage;
        public float CastingTime;
    }

    [Serializable]
    public sealed class FireNovaAbilityUpgradeData : AbilityUpgradeData
    {
        [SerializeField]
        private FireNovaAbilityUpgradeLevel _level;

        public FireNovaAbilityUpgradeLevel Level => _level;
        public override int LevelCount => MaxLevel;

        public FireNovaAbilityLevelData GetLevelData(int level)
        {
            int clampedLevel = ClampLevel(level);
            FireNovaAbilityUpgradeLevel resolvedLevel = _level;

            return new FireNovaAbilityLevelData
            {
                Radius = ApplyPerLevelIncrease(resolvedLevel.Radius, resolvedLevel.RadiusIncreasePercent, clampedLevel),
                Damage = ApplyPerLevelIncrease(resolvedLevel.Damage, resolvedLevel.DamageIncreasePercent, clampedLevel),
                BurnDuration = ApplyPerLevelIncrease(resolvedLevel.BurnDuration, resolvedLevel.BurnDurationIncreasePercent, clampedLevel),
                BurnDamage = ApplyPerLevelIncrease(resolvedLevel.BurnDamage, resolvedLevel.BurnDamageIncreasePercent, clampedLevel),
                CastingTime = ApplyPerLevelIncrease(resolvedLevel.CastingTime, resolvedLevel.CastingTimeIncreasePercent, clampedLevel)
            };
        }
    }
}
