namespace TPSBR
{
    using Fusion.Addons.KCC;
    using UnityEngine;

    public sealed class HorseMountProcessor : KCCProcessor, IPrepareData
    {
        [SerializeField] private MountDefinition _definition;
        [SerializeField] private MountAnimator _animator;
        [SerializeField] private Vector3 _gravity = default;
        [SerializeField] private float _upGravityMultiplier = 1f;
        [SerializeField] private float _downGravityMultiplier = 1f;

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

            Vector3 gravity = _gravity != default ? _gravity : Physics.gravity;
            KCCData fixedData = kcc.FixedData;
            Vector3 inputDirection = kcc.IsInFixedUpdate == true ? data.InputDirection : fixedData.InputDirection;

            if (kcc.IsInFixedUpdate == true)
            {
                float fixedDeltaTime = fixedData.DeltaTime;
                float targetSpeed = Mathf.Clamp01(inputDirection.magnitude) * _definition.MoveSpeed;
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _definition.Acceleration * fixedDeltaTime);
            }

            Vector3 kinematicDirection = Vector3.zero;
            Vector3 kinematicVelocity = Vector3.zero;

            if (_currentSpeed > float.Epsilon && inputDirection.sqrMagnitude > float.Epsilon)
            {
                kinematicDirection = inputDirection.normalized;
                kinematicVelocity = kinematicDirection * _currentSpeed;
            }

            Vector3 appliedGravity = gravity * (fixedData.RealVelocity.y > 0.0f ? _upGravityMultiplier : _downGravityMultiplier);

            if (fixedData.IsGrounded == false)
            {
                Vector3 dynamicVelocity = fixedData.DynamicVelocity + appliedGravity * fixedData.DeltaTime;
                data.DynamicVelocity = dynamicVelocity;
            }

            data.Gravity = appliedGravity;
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
