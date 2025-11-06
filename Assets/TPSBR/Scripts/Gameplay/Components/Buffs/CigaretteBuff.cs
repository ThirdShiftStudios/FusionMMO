using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "CigaretteBuff", menuName = "TSS/Data Definitions/Buffs/Cigarette")]
    public sealed class CigaretteBuff : BuffDefinition
    {
        public override void OnAdd(BuffSystem buffSystem, ref BuffData data, int previousStacks)
        {
            ToggleVisual(buffSystem, true);
        }

        public override void OnTick(BuffSystem buffSystem, ref BuffData data, float deltaTime)
        {
        }

        public override void OnRemove(BuffSystem buffSystem, ref BuffData data)
        {
            ToggleVisual(buffSystem, false);
        }

        private void ToggleVisual(BuffSystem buffSystem, bool active)
        {
            if (buffSystem == null)
            {
                return;
            }

            Agent agent = buffSystem.Agent;
            if (agent == null)
            {
                return;
            }

            BuffVisual[] visuals = agent.GetComponentsInChildren<BuffVisual>(true);
            for (int i = 0; i < visuals.Length; i++)
            {
                BuffVisual visual = visuals[i];
                if (visual != null && visual.MatchesDefinition(this) == true)
                {
                    visual.SetVisualActive(active);
                }
            }
        }
    }
}
