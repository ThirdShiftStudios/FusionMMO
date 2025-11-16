using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.Abilities
{
    [Serializable]
    public struct IceShardAbilityUpgradeLevel
    {
        public float NumberOfShards;
        public float Damage;
        public float CastingTime;
    }

    [Serializable]
    public sealed class IceShardAbilityUpgradeData : AbilityUpgradeData
    {
        [SerializeField]
        private IceShardAbilityUpgradeLevel[] _levels = Array.Empty<IceShardAbilityUpgradeLevel>();

        public IReadOnlyList<IceShardAbilityUpgradeLevel> Levels => _levels ?? Array.Empty<IceShardAbilityUpgradeLevel>();
        public override int LevelCount => Levels.Count;

        public IceShardAbilityUpgradeLevel GetLevelData(int level)
        {
            if (Levels.Count == 0)
            {
                return default;
            }

            int index = Mathf.Clamp(level - 1, 0, Levels.Count - 1);
            return _levels[index];
        }

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();
            _levels ??= Array.Empty<IceShardAbilityUpgradeLevel>();
        }
#endif
    }
}
