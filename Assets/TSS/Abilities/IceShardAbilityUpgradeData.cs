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

            return new IceShardAbilityLevelData
            {
                NumberOfShards = ApplyPerLevelIncrease(_level.NumberOfShards, _level.NumberOfShardsIncreasePercent, clampedLevel),
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
