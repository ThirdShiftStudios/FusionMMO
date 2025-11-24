using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField, FormerlySerializedAs("_levels")]
        private FireNovaAbilityUpgradeLevel[] _legacyLevels = Array.Empty<FireNovaAbilityUpgradeLevel>();

        public FireNovaAbilityUpgradeLevel Level => _level;
        public IReadOnlyList<FireNovaAbilityUpgradeLevel> LegacyLevels => _legacyLevels ?? Array.Empty<FireNovaAbilityUpgradeLevel>();
        public override int LevelCount => MaxLevel;

        public FireNovaAbilityLevelData GetLevelData(int level)
        {
            int clampedLevel = ClampLevel(level);
            FireNovaAbilityUpgradeLevel resolvedLevel = ResolveLevel();

            return new FireNovaAbilityLevelData
            {
                Radius = ApplyPerLevelIncrease(resolvedLevel.Radius, resolvedLevel.RadiusIncreasePercent, clampedLevel),
                Damage = ApplyPerLevelIncrease(resolvedLevel.Damage, resolvedLevel.DamageIncreasePercent, clampedLevel),
                BurnDuration = ApplyPerLevelIncrease(resolvedLevel.BurnDuration, resolvedLevel.BurnDurationIncreasePercent, clampedLevel),
                BurnDamage = ApplyPerLevelIncrease(resolvedLevel.BurnDamage, resolvedLevel.BurnDamageIncreasePercent, clampedLevel),
                CastingTime = ApplyPerLevelIncrease(resolvedLevel.CastingTime, resolvedLevel.CastingTimeIncreasePercent, clampedLevel)
            };
        }

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();
            _legacyLevels ??= Array.Empty<FireNovaAbilityUpgradeLevel>();

            if (IsLevelConfigured(_level) == false && TryPopulateLevelFromLegacy(LegacyLevels) == true)
            {
                _level = PopulateLevelFromLegacy(LegacyLevels);
            }
        }
#endif

        private FireNovaAbilityUpgradeLevel ResolveLevel()
        {
            if (IsLevelConfigured(_level) == true)
            {
                return _level;
            }

            if (TryPopulateLevelFromLegacy(LegacyLevels) == true)
            {
                return PopulateLevelFromLegacy(LegacyLevels);
            }

            return _level;
        }

        private static bool IsLevelConfigured(FireNovaAbilityUpgradeLevel level)
        {
            return level.Radius != 0f || level.Damage != 0f || level.BurnDuration != 0f || level.BurnDamage != 0f || level.CastingTime != 0f ||
                   level.RadiusIncreasePercent != 0f || level.DamageIncreasePercent != 0f || level.BurnDurationIncreasePercent != 0f ||
                   level.BurnDamageIncreasePercent != 0f || level.CastingTimeIncreasePercent != 0f;
        }

        private static bool TryPopulateLevelFromLegacy(IReadOnlyList<FireNovaAbilityUpgradeLevel> levels)
        {
            return levels != null && levels.Count > 0;
        }

        private static FireNovaAbilityUpgradeLevel PopulateLevelFromLegacy(IReadOnlyList<FireNovaAbilityUpgradeLevel> levels)
        {
            if (levels == null || levels.Count == 0)
            {
                return default;
            }

            FireNovaAbilityUpgradeLevel baseLevel = levels[0];

            return new FireNovaAbilityUpgradeLevel
            {
                Radius = baseLevel.Radius,
                Damage = baseLevel.Damage,
                BurnDuration = baseLevel.BurnDuration,
                BurnDamage = baseLevel.BurnDamage,
                CastingTime = baseLevel.CastingTime,
                RadiusIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.Radius),
                DamageIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.Damage),
                BurnDurationIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.BurnDuration),
                BurnDamageIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.BurnDamage),
                CastingTimeIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.CastingTime),
            };
        }
    }
}
