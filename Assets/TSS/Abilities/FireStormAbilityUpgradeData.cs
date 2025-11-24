using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField, FormerlySerializedAs("_levels")]
        private FireStormAbilityUpgradeLevel[] _legacyLevels = Array.Empty<FireStormAbilityUpgradeLevel>();

        public FireStormAbilityUpgradeLevel Level => _level;
        public IReadOnlyList<FireStormAbilityUpgradeLevel> LegacyLevels => _legacyLevels ?? Array.Empty<FireStormAbilityUpgradeLevel>();
        public override int LevelCount => MaxLevel;

        public FireStormAbilityLevelData GetLevelData(int level)
        {
            int clampedLevel = ClampLevel(level);
            FireStormAbilityUpgradeLevel resolvedLevel = ResolveLevel();

            return new FireStormAbilityLevelData
            {
                Damage = ApplyPerLevelIncrease(resolvedLevel.Damage, resolvedLevel.DamageIncreasePercent, clampedLevel),
                Duration = ApplyPerLevelIncrease(resolvedLevel.Duration, resolvedLevel.DurationIncreasePercent, clampedLevel),
                Radius = ApplyPerLevelIncrease(resolvedLevel.Radius, resolvedLevel.RadiusIncreasePercent, clampedLevel),
                CastingTime = ApplyPerLevelIncrease(resolvedLevel.CastingTime, resolvedLevel.CastingTimeIncreasePercent, clampedLevel)
            };
        }

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();
            _legacyLevels ??= Array.Empty<FireStormAbilityUpgradeLevel>();

            if (IsLevelConfigured(_level) == false && TryPopulateLevelFromLegacy(LegacyLevels) == true)
            {
                _level = PopulateLevelFromLegacy(LegacyLevels);
            }
        }
#endif

        private FireStormAbilityUpgradeLevel ResolveLevel()
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

        private static bool IsLevelConfigured(FireStormAbilityUpgradeLevel level)
        {
            return level.Damage != 0f || level.Duration != 0f || level.Radius != 0f || level.CastingTime != 0f ||
                   level.DamageIncreasePercent != 0f || level.DurationIncreasePercent != 0f || level.RadiusIncreasePercent != 0f ||
                   level.CastingTimeIncreasePercent != 0f;
        }

        private static bool TryPopulateLevelFromLegacy(IReadOnlyList<FireStormAbilityUpgradeLevel> levels)
        {
            return levels != null && levels.Count > 0;
        }

        private static FireStormAbilityUpgradeLevel PopulateLevelFromLegacy(IReadOnlyList<FireStormAbilityUpgradeLevel> levels)
        {
            if (levels == null || levels.Count == 0)
            {
                return default;
            }

            FireStormAbilityUpgradeLevel baseLevel = levels[0];

            return new FireStormAbilityUpgradeLevel
            {
                Damage = baseLevel.Damage,
                Duration = baseLevel.Duration,
                Radius = baseLevel.Radius,
                CastingTime = baseLevel.CastingTime,
                DamageIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.Damage),
                DurationIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.Duration),
                RadiusIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.Radius),
                CastingTimeIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.CastingTime),
            };
        }
    }
}
