using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.Abilities
{
    [Serializable]
    public struct FireNovaAbilityUpgradeLevel
    {
        public float RadiusDelta;
        public float DamageDelta;
        public float BurnDurationDelta;
        public float BurnDamageDelta;
        public float CastingTimeDelta;
    }

    [Serializable]
    public sealed class FireNovaAbilityUpgradeData : AbilityUpgradeData
    {
        [SerializeField]
        private FireNovaAbilityUpgradeLevel[] _levels = Array.Empty<FireNovaAbilityUpgradeLevel>();

        public IReadOnlyList<FireNovaAbilityUpgradeLevel> Levels => _levels ?? Array.Empty<FireNovaAbilityUpgradeLevel>();
        public override int LevelCount => Levels.Count;

        public FireNovaAbilityUpgradeLevel GetLevelData(int level)
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
            _levels ??= Array.Empty<FireNovaAbilityUpgradeLevel>();
        }
#endif
    }
}
