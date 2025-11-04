using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "DrunkBuffDefinition", menuName = "TSS/Data Definitions/Buffs/Drunk")]
    public class DrunkBuffDefinition : BuffDefinition
    {
        [SerializeField, Range(0f, 1f)] private float _intensityPerStack = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _maxIntensity = 1f;

        public float IntensityPerStack => Mathf.Clamp01(_intensityPerStack);
        public float MaxIntensity => Mathf.Clamp01(_maxIntensity);

        public override void OnAdd(BuffSystem buffSystem, ref BuffData data, int previousStacks)
        {
            if (buffSystem == null)
            {
                return;
            }

            AgentSenses senses = buffSystem.Senses;
            if (senses == null)
            {
                return;
            }

            int stacksApplied = data.Stacks > previousStacks ? data.Stacks - previousStacks : 1;
            stacksApplied = Mathf.Max(1, stacksApplied);

            senses.ApplyDrunkStacks(IntensityPerStack, Duration, MaxIntensity, stacksApplied);
        }

        public override void OnTick(BuffSystem buffSystem, ref BuffData data, float deltaTime)
        {
        }

        public override void OnRemove(BuffSystem buffSystem, ref BuffData data)
        {
        }
    }
}
