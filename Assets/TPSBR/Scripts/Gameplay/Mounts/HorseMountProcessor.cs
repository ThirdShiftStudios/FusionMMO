namespace TPSBR
{
    using Fusion.Addons.KCC;
    using UnityEngine;

    public sealed class HorseMountProcessor : KCCProcessor, IPrepareData
    {
        private float _moveSpeed;
        private float _acceleration;
        private Vector3 _moveDirection;
        private float _moveAmount;
        private float _currentSpeed;

        private const float k_MoveDeadzone = 0.05f;

        public float NormalizedSpeed => _moveSpeed > 0f ? _currentSpeed / _moveSpeed : 0f;

        public override float GetPriority(KCC kcc) => BREnvironmentProcessor.DefaultPriority - 1;

        public void ApplyDefinition(MountDefinition definition)
        {
            _moveSpeed = definition != null ? definition.MoveSpeed : 0f;
            _acceleration = definition != null ? definition.Acceleration : 0f;
        }

        public void ResetState()
        {
            _moveDirection = Vector3.zero;
            _moveAmount = 0f;
            _currentSpeed = 0f;
        }

        public void SetMoveInput(Vector3 worldMoveDirection)
        {
            float magnitude = Mathf.Clamp(worldMoveDirection.magnitude, 0f, 1f);

            if (magnitude < k_MoveDeadzone)
            {
                _moveAmount = 0f;
                _moveDirection = Vector3.zero;
                return;
            }

            _moveAmount = magnitude;
            _moveDirection = worldMoveDirection.normalized;
        }

        public float GetProjectedNormalizedSpeed(float inputMagnitude, float deltaTime)
        {
            float targetInput = Mathf.Clamp(inputMagnitude, 0f, 1f);

            if (targetInput < k_MoveDeadzone)
            {
                return 0f;
            }

            float targetSpeed = targetInput * _moveSpeed;
            float projectedSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * deltaTime);

            return _moveSpeed > 0f ? projectedSpeed / _moveSpeed : 0f;
        }

        public void Execute(PrepareData stage, KCC kcc, KCCData data)
        {
            KCCData fixedData = kcc.FixedData;
            float deltaTime = fixedData.DeltaTime > 0f ? fixedData.DeltaTime : data.DeltaTime;

            float targetSpeed = _moveAmount * _moveSpeed;

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * deltaTime);

            if (_currentSpeed < 0.001f && targetSpeed <= 0f)
            {
                _currentSpeed = 0f;
            }

            data.Gravity = Physics.gravity;
            data.KinematicDirection = _moveDirection;
            data.KinematicSpeed = _currentSpeed;
            data.KinematicVelocity = _moveDirection * _currentSpeed;
        }
    }
}
