using System;
using UnityEngine;

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

        public IceShardAbilityUpgradeLevel Level => _level;
        public override int LevelCount => MaxLevel;

        public IceShardAbilityLevelData GetLevelData(int level)
        {
            int clampedLevel = ClampLevel(level);
            IceShardAbilityUpgradeLevel resolvedLevel = _level;

            return new IceShardAbilityLevelData
            {
                NumberOfShards = ApplyPerLevelIncrease(resolvedLevel.NumberOfShards, resolvedLevel.NumberOfShardsIncreasePercent, clampedLevel),
                Damage = ApplyPerLevelIncrease(resolvedLevel.Damage, resolvedLevel.DamageIncreasePercent, clampedLevel),
                CastingTime = ApplyPerLevelIncrease(resolvedLevel.CastingTime, resolvedLevel.CastingTimeIncreasePercent, clampedLevel)
            };
        }
    }
}
