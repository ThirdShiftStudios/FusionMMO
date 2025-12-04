namespace TPSBR
{
    using Fusion;
    using UnityEngine;
    using Fusion.Addons.KCC;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(KCC))]
    [RequireComponent(typeof(HorseMountProcessor))]
    [DefaultExecutionOrder(-3)]
    public sealed class HorseMount : MountBase
    {
        [SerializeField] private MountDefinition _definition;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _riderAnchor;
        [SerializeField] private MountAnimator _animator;
        [SerializeField] private KCC _kcc;
        [SerializeField] private HorseMountProcessor _movementProcessor;

        private MountController _rider;
        private float _currentSpeed;

        public string MountCode => _definition != null ? _definition.Code : string.Empty;
        public Transform MountCamera => _cameraTransform;
        public Transform RiderAnchor => _riderAnchor;
        public MountDefinition Definition => _definition;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<MountAnimator>();
            }

            if (_kcc == null)
            {
                _kcc = GetComponent<KCC>();
            }

            if (_movementProcessor == null)
            {
                _movementProcessor = GetComponent<HorseMountProcessor>();
            }

            if (_animator != null)
            {
                _animator.ApplyDefinition(_definition);
            }
        }

        public void BeginRide(MountController rider)
        {
            _rider = rider;
            _currentSpeed = 0f;

            _movementProcessor?.ResetMovement();
            _kcc?.SetInputDirection(Vector3.zero);

            if (_animator != null)
            {
                _animator.ApplyDefinition(_definition);
                _animator.SetMoveInput(0f);
                _animator.SetMounted(true);
            }
        }

        public void EndRide()
        {
            _rider = null;
            _currentSpeed = 0f;

            _movementProcessor?.ResetMovement();
            _kcc?.SetInputDirection(Vector3.zero);

            if (_animator != null)
            {
                _animator.SetMoveInput(0f);
                _animator.SetMounted(false);
            }
        }

        public void SimulateMovement(Vector2 move, Vector2 lookDelta, float deltaTime)
        {
            if (_definition == null || Object == null || Object.HasStateAuthority == false)
                return;

            if (lookDelta.sqrMagnitude > 0.0001f)
            {
                float yawDelta = lookDelta.y * _definition.TurnSpeed * deltaTime;
                transform.rotation *= Quaternion.Euler(0f, yawDelta, 0f);
            }

            Vector3 desiredDirection = transform.forward * move.y + transform.right * move.x;
            float targetSpeed = move.magnitude * _definition.MoveSpeed;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _definition.Acceleration * deltaTime);

            float normalizedSpeed = _definition.MoveSpeed > 0f ? _currentSpeed / _definition.MoveSpeed : 0f;
            _animator?.SetMoveInput(normalizedSpeed);

            if (_movementProcessor != null)
            {
                _movementProcessor.SetMovement(desiredDirection, _currentSpeed);
            }

            if (_kcc != null)
            {
                _kcc.SetInputDirection(desiredDirection);
            }
        }
    }
}
