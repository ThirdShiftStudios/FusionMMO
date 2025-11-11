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

    }
}
