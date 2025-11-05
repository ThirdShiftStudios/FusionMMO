using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "BasicExperienceBoostBuff", menuName = "TSS/Data Definitions/Buffs/Basic Experience Boost")]
    public sealed class BasicExperienceBoostBuff : BuffDefinition, IExperienceMultiplierBuff
    {
        [SerializeField, Range(0f, 100f)]
        private float _experienceBonusPercent = 10f;

        public float ExperienceMultiplier => 1f + Mathf.Max(0f, _experienceBonusPercent) / 100f;

        public override void OnAdd(BuffSystem buffSystem, ref BuffData data, int previousStacks)
        {
            data.Stacks = 1;
        }

        public override void OnTick(BuffSystem buffSystem, ref BuffData data, float deltaTime)
        {
        }

        public override void OnRemove(BuffSystem buffSystem, ref BuffData data)
        {
        }

        public float GetExperienceMultiplier(BuffSystem buffSystem, BuffData data)
        {
            int stacks = Mathf.Max(1, data.Stacks);
            return Mathf.Pow(ExperienceMultiplier, stacks);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var serializedObject = new UnityEditor.SerializedObject(this);
            serializedObject.FindProperty("_isStackable").boolValue = false;
            serializedObject.FindProperty("_maxStacks").intValue = 1;
            serializedObject.FindProperty("_duration").floatValue = 180f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
#endif
    }
}
