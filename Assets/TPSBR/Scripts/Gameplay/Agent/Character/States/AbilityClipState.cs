using Fusion.Addons.AnimationController;
using TPSBR.Abilities;
using UnityEngine;

namespace TPSBR
{
    public class AbilityClipState : ClipState
    {
        [SerializeField]
        private AbilityDefinition _ability;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Normalized time within the clip when the ability should trigger.")]
        private float _abilityTriggerNormalizedTime = 0.5f;

        public AbilityDefinition Ability => _ability;
        public float AbilityTriggerNormalizedTime => Mathf.Clamp01(_abilityTriggerNormalizedTime);

        protected override void OnActivate()
        {
            base.OnActivate();
            UpdateClipSpeed();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateClipSpeed();
        }
#endif

        private void UpdateClipSpeed()
        {
            AbilityDefinition ability = _ability;
            ClipNode node = Node;

            if (ability == null || node == null || node.Clip == null)
            {
                return;
            }

            float triggerTimeNormalized = Mathf.Clamp01(_abilityTriggerNormalizedTime);
            float baseCastTime = Mathf.Max(ability.BaseCastTime, Mathf.Epsilon);

            if (triggerTimeNormalized <= 0f)
            {
                node.Speed = 1f;
                return;
            }

            float clipLength = node.Length;

            if (clipLength <= 0f)
            {
                node.Speed = 1f;
                return;
            }

            node.Speed = triggerTimeNormalized * clipLength / baseCastTime;
        }
    }
}
