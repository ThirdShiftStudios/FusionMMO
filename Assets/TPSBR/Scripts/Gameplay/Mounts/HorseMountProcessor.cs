namespace TPSBR
{
    using Fusion.Addons.KCC;
    using UnityEngine;

    public sealed class HorseMountProcessor : KCCProcessor, IPrepareData
    {
        [SerializeField] private MountDefinition _definition;
        [SerializeField] private MountAnimator _animator;

        private float _currentSpeed;

        public void Configure(MountDefinition definition, MountAnimator animator)
        {
            _definition = definition;
            _animator = animator;
        }

        public void ResetState()
        {
            _currentSpeed = 0f;
        }

        public override float GetPriority(KCC kcc) => 0f;

        public void Execute(PrepareData stage, KCC kcc, KCCData data)
        {
            if (_definition == null)
                return;

            Vector3 inputDirection = data.InputDirection;
            float targetSpeed = Mathf.Clamp01(inputDirection.magnitude) * _definition.MoveSpeed;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _definition.Acceleration * data.DeltaTime);

            Vector3 kinematicDirection = Vector3.zero;
            Vector3 kinematicVelocity = Vector3.zero;

            if (_currentSpeed > float.Epsilon && inputDirection.sqrMagnitude > float.Epsilon)
            {
                kinematicDirection = inputDirection.normalized;
                kinematicVelocity = kinematicDirection * _currentSpeed;
            }

            data.KinematicDirection = kinematicDirection;
            data.KinematicSpeed = _currentSpeed;
            data.KinematicVelocity = kinematicVelocity;

            if (_animator != null)
            {
                float normalizedSpeed = _definition.MoveSpeed > 0f ? _currentSpeed / _definition.MoveSpeed : 0f;
                _animator.SetMoveInput(normalizedSpeed);
            }
        }
    }
}
