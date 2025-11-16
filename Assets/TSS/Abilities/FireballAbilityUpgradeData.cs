using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.Abilities
{
    [Serializable]
    public struct FireballAbilityUpgradeLevel
    {
        public float Radius;
        public float Damage;
        public float CastingTime;
    }

    [Serializable]
    public sealed class FireballAbilityUpgradeData : AbilityUpgradeData
    {
        [SerializeField]
        private FireballAbilityUpgradeLevel[] _levels = Array.Empty<FireballAbilityUpgradeLevel>();

        public IReadOnlyList<FireballAbilityUpgradeLevel> Levels => _levels ?? Array.Empty<FireballAbilityUpgradeLevel>();
        public override int LevelCount => Levels.Count;

        public FireballAbilityUpgradeLevel GetLevelData(int level)
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
            _levels ??= Array.Empty<FireballAbilityUpgradeLevel>();
        }
#endif
    }
}
