using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TPSBR.Abilities
{
    [Serializable]
    public struct IceShardAbilityUpgradeLevel
    {
        [Header("Base Values")]
        public float NumberOfShards;
        public float Damage;
        public float CastingTime;

        [Header("Per Level Increase (%)")]
        public float NumberOfShardsIncreasePercent;
        public float DamageIncreasePercent;
        public float CastingTimeIncreasePercent;
    }

    public struct IceShardAbilityLevelData
    {
        public float NumberOfShards;
        public float Damage;
        public float CastingTime;
    }

    [Serializable]
    public sealed class IceShardAbilityUpgradeData : AbilityUpgradeData
    {
        [SerializeField]
        private IceShardAbilityUpgradeLevel _level;
        [SerializeField, FormerlySerializedAs("_levels")]
        private IceShardAbilityUpgradeLevel[] _legacyLevels = Array.Empty<IceShardAbilityUpgradeLevel>();

        public IceShardAbilityUpgradeLevel Level => _level;
        public IReadOnlyList<IceShardAbilityUpgradeLevel> LegacyLevels => _legacyLevels ?? Array.Empty<IceShardAbilityUpgradeLevel>();
        public override int LevelCount => MaxLevel;

        public IceShardAbilityLevelData GetLevelData(int level)
        {
            int clampedLevel = ClampLevel(level);
            IceShardAbilityUpgradeLevel resolvedLevel = ResolveLevel();

            return new IceShardAbilityLevelData
            {
                NumberOfShards = ApplyPerLevelIncrease(resolvedLevel.NumberOfShards, resolvedLevel.NumberOfShardsIncreasePercent, clampedLevel),
                Damage = ApplyPerLevelIncrease(resolvedLevel.Damage, resolvedLevel.DamageIncreasePercent, clampedLevel),
                CastingTime = ApplyPerLevelIncrease(resolvedLevel.CastingTime, resolvedLevel.CastingTimeIncreasePercent, clampedLevel)
            };
        }

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();
            _legacyLevels ??= Array.Empty<IceShardAbilityUpgradeLevel>();

            if (IsLevelConfigured(_level) == false && TryPopulateLevelFromLegacy(LegacyLevels) == true)
            {
                _level = PopulateLevelFromLegacy(LegacyLevels);
            }
        }
#endif

        private IceShardAbilityUpgradeLevel ResolveLevel()
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

        private static bool IsLevelConfigured(IceShardAbilityUpgradeLevel level)
        {
            return level.NumberOfShards != 0f || level.Damage != 0f || level.CastingTime != 0f ||
                   level.NumberOfShardsIncreasePercent != 0f || level.DamageIncreasePercent != 0f || level.CastingTimeIncreasePercent != 0f;
        }

        private static bool TryPopulateLevelFromLegacy(IReadOnlyList<IceShardAbilityUpgradeLevel> levels)
        {
            return levels != null && levels.Count > 0;
        }

        private static IceShardAbilityUpgradeLevel PopulateLevelFromLegacy(IReadOnlyList<IceShardAbilityUpgradeLevel> levels)
        {
            if (levels == null || levels.Count == 0)
            {
                return default;
            }

            IceShardAbilityUpgradeLevel baseLevel = levels[0];

            return new IceShardAbilityUpgradeLevel
            {
                NumberOfShards = baseLevel.NumberOfShards,
                Damage = baseLevel.Damage,
                CastingTime = baseLevel.CastingTime,
                NumberOfShardsIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.NumberOfShards),
                DamageIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.Damage),
                CastingTimeIncreasePercent = CalculateAverageIncreasePercent(levels, level => level.CastingTime),
            };
        }
    }
}
