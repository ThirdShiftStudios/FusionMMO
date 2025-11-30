namespace TPSBR
{
    using Fusion;
    using UnityEngine;
    [DefaultExecutionOrder(-3)]
    public sealed class HorseMount : MountBase
    {
        [SerializeField] private MountDefinition _definition;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _riderAnchor;
        [SerializeField] private LayerMask _blockedLayers;
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField, Range(0.25f, 5f)] private float _groundCheckHeight = 1.5f;
        [SerializeField, Range(0.25f, 10f)] private float _groundCheckDistance = 4f;
        [SerializeField] private MountAnimator _animator;

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
        }

        public void BeginRide(MountController rider)
        {
            _rider = rider;
            _currentSpeed = 0f;

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

            Vector3 displacement = desiredDirection.normalized * _currentSpeed * deltaTime;
            float normalizedSpeed = _definition.MoveSpeed > 0f ? _currentSpeed / _definition.MoveSpeed : 0f;
            _animator?.SetMoveInput(normalizedSpeed);

            if (displacement.sqrMagnitude < float.Epsilon)
                return;

            Vector3 nextPosition = transform.position + displacement;
            if (Physics.Linecast(transform.position, nextPosition, out RaycastHit hit, _blockedLayers, QueryTriggerInteraction.Ignore) == true)
            {
                nextPosition = hit.point;
            }

            Vector3 groundOrigin = nextPosition + Vector3.up * _groundCheckHeight;
            float groundRayLength = _groundCheckHeight + _groundCheckDistance;

            if (Physics.Raycast(groundOrigin, Vector3.down, out RaycastHit groundHit, groundRayLength, _groundLayers, QueryTriggerInteraction.Ignore) == true)
            {
                nextPosition = groundHit.point;
            }

            transform.position = nextPosition;
        }
    }
}
