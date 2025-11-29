namespace TPSBR
{
    using Fusion.Addons.AnimationController;
    using UnityEngine;

    [DefaultExecutionOrder(-5)]
    public sealed class MountAnimator : AnimationController
    {
        [SerializeField] private MountLocomotionLayer _locomotion;

        public void ApplyDefinition(MountDefinition definition)
        {
            _locomotion?.ApplyDefinition(definition);
        }

        public void SetMounted(bool mounted)
        {
            _locomotion?.SetMounted(mounted);
        }

        public void SetMoveInput(float normalizedSpeed)
        {
            _locomotion?.SetMoveInput(normalizedSpeed);
        }
    }
}
