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

        private AbilityDefinition _runtimeAbility;
        private float _defaultClipSpeed = 1f;

        public AbilityDefinition Ability => _ability;
        public float AbilityTriggerNormalizedTime => Mathf.Clamp01(_abilityTriggerNormalizedTime);

        public void SetRuntimeAbilityDefinition(AbilityDefinition ability)
        {
            _runtimeAbility = ability;
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();

            ClipNode node = Node;
            if (node != null)
            {
                _defaultClipSpeed = node.Speed;
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            ApplyCastTimeScaling();
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();

            ClipNode node = Node;
            if (node != null)
            {
                node.Speed = _defaultClipSpeed;
            }

            _runtimeAbility = null;
        }

        private void ApplyCastTimeScaling()
        {
            ClipNode node = Node;

            if (node == null || node.Length <= 0f)
            {
                return;
            }

            float targetSpeed = _defaultClipSpeed;
            AbilityDefinition ability = _runtimeAbility ?? _ability;

            if (ability != null)
            {
                float baseCastTime = ability.BaseCastTime;
                float triggerNormalized = AbilityTriggerNormalizedTime;

                if (baseCastTime > Mathf.Epsilon && triggerNormalized > 0f)
                {
                    float clipTimeAtTrigger = triggerNormalized * node.Length;

                    if (clipTimeAtTrigger > 0f)
                    {
                        targetSpeed = clipTimeAtTrigger / baseCastTime;
                    }
                }
            }

            node.Speed = targetSpeed;
        }

    }
}
