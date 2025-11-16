using System;
namespace TPSBR.Abilities
{
    [Serializable]
    public abstract class AbilityUpgradeData
    {
        public abstract int LevelCount { get; }

#if UNITY_EDITOR
        public virtual void OnValidate()
        {
        }
#endif
    }
}
