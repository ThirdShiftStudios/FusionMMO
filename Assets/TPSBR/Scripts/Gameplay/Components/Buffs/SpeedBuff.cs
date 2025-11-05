using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "SpeedBuff", menuName = "TSS/Data Definitions/Buffs/Speed")]
    public sealed class SpeedBuff : BuffDefinition, IMovementSpeedMultiplierBuff
    {
        [SerializeField, Range(0f, 100f)]
        private float _speedPercent = 30f;

        public float MovementMultiplier => 1f + Mathf.Max(0f, _speedPercent) / 100f;

        public override void OnAdd(BuffSystem buffSystem, ref BuffData data, int previousStacks)
        {
        }

        public override void OnTick(BuffSystem buffSystem, ref BuffData data, float deltaTime)
        {
        }

        public override void OnRemove(BuffSystem buffSystem, ref BuffData data)
        {
        }

        public float GetMovementSpeedMultiplier(BuffSystem buffSystem, BuffData data)
        {
            int stacks = Mathf.Max(1, data.Stacks);
            return Mathf.Pow(MovementMultiplier, stacks);
        }
    }
}
