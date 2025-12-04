namespace TPSBR
{
    using Fusion.Addons.KCC;
    using UnityEngine;

    public sealed class HorseMountProcessor : KCCProcessor, IPrepareData, ISetDynamicVelocity, ISetKinematicDirection, ISetKinematicSpeed, ISetKinematicVelocity
    {
        [Header("Grounding")]
        [SerializeField, Range(0f, 90f)] private float _maxGroundAngle = 60f;
        [SerializeField, Range(0f, 90f)] private float _maxWallAngle = 5f;
        [SerializeField, Range(0f, 90f)] private float _maxHangAngle = 30f;
        [SerializeField, Range(0f, 50f)] private float _groundFriction = 20f;

        private Vector3 _inputDirection;
        private float _currentSpeed;
        private float _normalizedSpeed;

        public float NormalizedSpeed => _normalizedSpeed;

        public override float GetPriority(KCC kcc) => EnvironmentProcessor.DefaultPriority;

        public void ResetState()
        {
            _inputDirection = default;
            _currentSpeed = 0f;
            _normalizedSpeed = 0f;
        }

        public void SetMoveInput(Vector2 moveInput, float moveSpeed, float acceleration, float deltaTime)
        {
            float targetSpeed = Mathf.Clamp01(moveInput.magnitude) * moveSpeed;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * deltaTime);
            _normalizedSpeed = moveSpeed > 0f ? _currentSpeed / moveSpeed : 0f;
            _inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        }

        public void Execute(PrepareData stage, KCC kcc, KCCData data)
        {
            data.Gravity = Physics.gravity;
            data.MaxGroundAngle = _maxGroundAngle;
            data.MaxWallAngle = _maxWallAngle;
            data.MaxHangAngle = _maxHangAngle;
        }

        public void Execute(ISetDynamicVelocity stage, KCC kcc, KCCData data)
        {
            KCCData fixedData = kcc.FixedData;
            float fixedDeltaTime = fixedData.DeltaTime;
            Vector3 dynamicVelocity = fixedData.DynamicVelocity;

            dynamicVelocity += data.Gravity * fixedDeltaTime;

            if (dynamicVelocity.sqrMagnitude > float.Epsilon && fixedData.IsGrounded == true)
            {
                dynamicVelocity = Vector3.MoveTowards(dynamicVelocity, Vector3.zero, _groundFriction * fixedDeltaTime);
            }

            data.DynamicVelocity = dynamicVelocity;

            if (kcc.IsInFixedUpdate == true)
            {
                data.ExternalVelocity = default;
                data.ExternalImpulse = default;
                data.JumpImpulse = default;
            }

            data.ExternalAcceleration = default;
            data.ExternalForce = default;
        }

        public void Execute(ISetKinematicDirection stage, KCC kcc, KCCData data)
        {
            data.KinematicDirection = _inputDirection.sqrMagnitude > float.Epsilon ? (data.TransformRotation * _inputDirection).normalized : Vector3.zero;
        }

        public void Execute(ISetKinematicSpeed stage, KCC kcc, KCCData data)
        {
            data.KinematicSpeed = _currentSpeed;
        }

        public void Execute(ISetKinematicVelocity stage, KCC kcc, KCCData data)
        {
            data.KinematicVelocity = data.KinematicDirection * data.KinematicSpeed;
        }
    }
}
