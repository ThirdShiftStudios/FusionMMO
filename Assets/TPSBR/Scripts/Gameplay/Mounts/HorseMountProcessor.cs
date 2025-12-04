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
            _moveAmount = Mathf.Clamp(worldMoveDirection.magnitude, 0f, 1f);
            _moveDirection = _moveAmount > 0f ? worldMoveDirection.normalized : Vector3.zero;
        }

        public float GetProjectedNormalizedSpeed(float inputMagnitude, float deltaTime)
        {
            float targetSpeed = Mathf.Clamp(inputMagnitude, 0f, 1f) * _moveSpeed;
            float projectedSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * deltaTime);

            return _moveSpeed > 0f ? projectedSpeed / _moveSpeed : 0f;
        }

        public void Execute(PrepareData stage, KCC kcc, KCCData data)
        {
            float targetSpeed = _moveAmount * _moveSpeed;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * data.DeltaTime);

            data.Gravity = Physics.gravity;
            data.KinematicDirection = _moveDirection;
            data.KinematicSpeed = _currentSpeed;
            data.KinematicVelocity = _moveDirection * _currentSpeed;
        }
    }
}
