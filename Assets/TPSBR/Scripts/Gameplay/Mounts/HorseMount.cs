namespace TPSBR
{
    using Fusion;
    using Fusion.Addons.KCC;
    using UnityEngine;

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
        [SerializeField] private HorseMountProcessor _processor;

        private MountController _rider;

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

            if (_processor == null)
            {
                _processor = GetComponent<HorseMountProcessor>();
            }

            RegisterProcessor();
        }

        public void BeginRide(MountController rider)
        {
            _rider = rider;
            _processor?.ResetState();

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
            _processor?.ResetState();

            if (_animator != null)
            {
                _animator.SetMoveInput(0f);
                _animator.SetMounted(false);
            }

            if (_kcc != null)
            {
                _kcc.SetDynamicVelocity(Vector3.zero);
                _kcc.SetKinematicVelocity(Vector3.zero);
            }
        }

        public void SimulateMovement(Vector2 move, Vector2 lookDelta, float deltaTime)
        {
            if (_definition == null || Object == null || Object.HasStateAuthority == false)
                return;

            if (_kcc == null || _processor == null)
                return;

            Vector3 worldMoveDirection = Vector3.zero;

            if (lookDelta.sqrMagnitude > 0.0001f)
            {
                float yawDelta = lookDelta.y * _definition.TurnSpeed * deltaTime;
                _kcc.AddLookRotation(0f, yawDelta);
            }

            if (move.sqrMagnitude > 0.0001f)
            {
                Vector3 inputDirection = new Vector3(move.x, 0f, move.y);
                worldMoveDirection = (_kcc.FixedData.TransformRotation * inputDirection).normalized;
            }

            _kcc.SetInputDirection(worldMoveDirection);
            _processor.SetMoveInput(worldMoveDirection, move.magnitude, _definition.MoveSpeed, _definition.Acceleration, deltaTime);
            _animator?.SetMoveInput(_processor.NormalizedSpeed);
        }

        private void RegisterProcessor()
        {
            if (_kcc == null || _processor == null)
                return;

            if (_kcc.IsSpawned == true)
            {
                _kcc.AddLocalProcessor(_processor);
            }
            else
            {
                _kcc.InvokeOnSpawn(kcc => kcc.AddLocalProcessor(_processor));
            }
        }
    }
}
