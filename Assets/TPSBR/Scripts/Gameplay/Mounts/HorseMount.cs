namespace TPSBR
{
    using Fusion;
    using UnityEngine;
    using Fusion.Addons.KCC;

    [RequireComponent(typeof(KCC))]
    [DefaultExecutionOrder(-3)]
    public sealed class HorseMount : MountBase
    {
        [SerializeField] private MountDefinition _definition;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _riderAnchor;
        [SerializeField] private MountAnimator _animator;
        [SerializeField] private HorseMountProcessor _mountProcessor;

        private MountController _rider;
        private KCC _kcc;
        private bool _processorRegistered;

        public string MountCode => _definition != null ? _definition.Code : string.Empty;
        public Transform MountCamera => _cameraTransform;
        public Transform RiderAnchor => _riderAnchor;
        public MountDefinition Definition => _definition;

        private void Awake()
        {
            _kcc = GetComponent<KCC>();

            if (_animator == null)
            {
                _animator = GetComponent<MountAnimator>();
            }

            if (_mountProcessor == null)
            {
                _mountProcessor = GetComponent<HorseMountProcessor>();

                if (_mountProcessor == null)
                {
                    _mountProcessor = gameObject.AddComponent<HorseMountProcessor>();
                }
            }
        }

        public override void Spawned()
        {
            base.Spawned();

            RegisterProcessor();
            ApplyMountDefinition();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            if (_processorRegistered == true && _kcc != null && _mountProcessor != null)
            {
                _kcc.RemoveLocalProcessor(_mountProcessor);
                _processorRegistered = false;
            }
        }

        public void BeginRide(MountController rider)
        {
            _rider = rider;

            _mountProcessor?.ResetState();
            _kcc?.SetInputDirection(Vector3.zero);

            ApplyMountDefinition();

            if (_animator != null)
            {
                _animator.SetMoveInput(0f);
                _animator.SetMounted(true);
            }
        }

        public void EndRide()
        {
            _rider = null;

            _mountProcessor?.ResetState();
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

            ApplyLookDelta(lookDelta, deltaTime);

            Vector3 worldMoveDirection = CalculateMoveDirection(move);

            _kcc?.SetInputDirection(worldMoveDirection);
            _mountProcessor?.SetMoveInput(worldMoveDirection);

            float projectedSpeed = _mountProcessor != null ? _mountProcessor.GetProjectedNormalizedSpeed(move.magnitude, deltaTime) : 0f;
            _animator?.SetMoveInput(projectedSpeed);
        }

        private void ApplyLookDelta(Vector2 lookDelta, float deltaTime)
        {
            if (_kcc == null || _definition == null)
                return;

            if (lookDelta.sqrMagnitude < 0.0001f)
                return;

            Vector2 currentLookRotation = _kcc.Data.GetLookRotation(true, true);
            currentLookRotation.y += lookDelta.y * _definition.TurnSpeed * deltaTime;

            _kcc.SetLookRotation(currentLookRotation);
        }

        private Vector3 CalculateMoveDirection(Vector2 move)
        {
            if (_kcc == null || move.IsZero() == true)
                return Vector3.zero;

            return _kcc.FixedData.TransformRotation * new Vector3(move.x, 0f, move.y);
        }

        private void RegisterProcessor()
        {
            if (_processorRegistered == true || _kcc == null || _mountProcessor == null)
                return;

            if (_kcc.AddLocalProcessor(_mountProcessor) == true)
            {
                _processorRegistered = true;
            }
        }

        private void ApplyMountDefinition()
        {
            if (_definition == null)
                return;

            _mountProcessor?.ApplyDefinition(_definition);
            _animator?.ApplyDefinition(_definition);
        }
    }
}
