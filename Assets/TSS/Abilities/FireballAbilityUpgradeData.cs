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

            return new FireballAbilityLevelData
            {
                Radius = ApplyPerLevelIncrease(_level.Radius, _level.RadiusIncreasePercent, clampedLevel),
                Damage = ApplyPerLevelIncrease(_level.Damage, _level.DamageIncreasePercent, clampedLevel),
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
