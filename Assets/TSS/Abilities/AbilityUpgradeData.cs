using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

        protected static float CalculateAverageIncreasePercent<TLevel>(IReadOnlyList<TLevel> levels, Func<TLevel, float> valueSelector)
        {
            if (levels == null || levels.Count < 2)
            {
                return 0f;
            }

            float firstValue = valueSelector(levels[0]);
            float lastValue = valueSelector(levels[levels.Count - 1]);

            if (Mathf.Approximately(firstValue, 0f) == true)
            {
                return 0f;
            }

            int steps = Mathf.Max(1, levels.Count - 1);
            return ((lastValue - firstValue) / firstValue) / steps * 100f;
        }

#if UNITY_EDITOR
        public virtual void OnValidate()
        {
        }
#endif
    }
}
