using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField, FormerlySerializedAs("_levels")]
        private FireballAbilityUpgradeLevel[] _legacyLevels = Array.Empty<FireballAbilityUpgradeLevel>();

        public FireballAbilityUpgradeLevel Level => _level;
        public IReadOnlyList<FireballAbilityUpgradeLevel> LegacyLevels => _legacyLevels ?? Array.Empty<FireballAbilityUpgradeLevel>();
        public override int LevelCount => MaxLevel;

        public FireballAbilityLevelData GetLevelData(int level)
        {
            int clampedLevel = ClampLevel(level);
            FireballAbilityUpgradeLevel resolvedLevel = ResolveLevel();

            return new FireballAbilityLevelData
            {
                Radius = ApplyPerLevelIncrease(resolvedLevel.Radius, resolvedLevel.RadiusIncreasePercent, clampedLevel),
                Damage = ApplyPerLevelIncrease(resolvedLevel.Damage, resolvedLevel.DamageIncreasePercent, clampedLevel),
                CastingTime = ApplyPerLevelIncrease(resolvedLevel.CastingTime, resolvedLevel.CastingTimeIncreasePercent, clampedLevel)
            };
        }

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();
            _legacyLevels ??= Array.Empty<FireballAbilityUpgradeLevel>();

            if (IsLevelConfigured(_level) == false && TryPopulateLevelFromLegacy(LegacyLevels) == true)
            {
                _level = PopulateLevelFromLegacy(LegacyLevels);
            }
        }
#endif

        private FireballAbilityUpgradeLevel ResolveLevel()
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

        private static bool IsLevelConfigured(FireballAbilityUpgradeLevel level)
        {
            return level.Radius != 0f || level.Damage != 0f || level.CastingTime != 0f ||
                   level.RadiusIncreasePercent != 0f || level.DamageIncreasePercent != 0f || level.CastingTimeIncreasePercent != 0f;
        }

        private static bool TryPopulateLevelFromLegacy(IReadOnlyList<FireballAbilityUpgradeLevel> levels)
        {
            return levels != null && levels.Count > 0;
        }

        private static FireballAbilityUpgradeLevel PopulateLevelFromLegacy(IReadOnlyList<FireballAbilityUpgradeLevel> levels)
        {
            if (levels == null || levels.Count == 0)
            {
                return default;
            }

            FireballAbilityUpgradeLevel baseLevel = levels[0];

            return new FireballAbilityUpgradeLevel
            {
                Radius = baseLevel.Radius,
                Damage = baseLevel.Damage,
                CastingTime = baseLevel.CastingTime,
                RadiusIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.Radius),
                DamageIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.Damage),
                CastingTimeIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.CastingTime),
            };
        }
    }
}
