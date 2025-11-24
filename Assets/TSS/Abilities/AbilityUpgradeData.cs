using System;
using UnityEngine;

namespace TPSBR.Abilities
{
    [Serializable]
    public abstract class AbilityUpgradeData
    {
        public const int MaxLevel = 99;

        public virtual int LevelCount => MaxLevel;

        protected static int ClampLevel(int level)
        {
            return Mathf.Clamp(level, 1, MaxLevel);
        }

        protected static float ApplyPerLevelIncrease(float baseValue, float increasePercent, int level)
        {
            int levelOffset = Mathf.Max(0, level - 1);
            return baseValue * (1f + (increasePercent * 0.01f * levelOffset));
        }

#if UNITY_EDITOR
        public virtual void OnValidate()
        {
        }
#endif
    }
}
