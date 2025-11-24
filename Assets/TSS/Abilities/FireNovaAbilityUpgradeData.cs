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

            return new FireNovaAbilityLevelData
            {
                Radius = ApplyPerLevelIncrease(_level.Radius, _level.RadiusIncreasePercent, clampedLevel),
                Damage = ApplyPerLevelIncrease(_level.Damage, _level.DamageIncreasePercent, clampedLevel),
                BurnDuration = ApplyPerLevelIncrease(_level.BurnDuration, _level.BurnDurationIncreasePercent, clampedLevel),
                BurnDamage = ApplyPerLevelIncrease(_level.BurnDamage, _level.BurnDamageIncreasePercent, clampedLevel),
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
