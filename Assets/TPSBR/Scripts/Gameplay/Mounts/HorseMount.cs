namespace TPSBR
{
    using Fusion;
    using Fusion.Addons.KCC;
    using UnityEngine;
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

        public string MountCode => _definition != null ? _definition.Identifier : string.Empty;
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

            _movementProcessor?.Configure(_definition, _animator);
        }

        public void BeginRide(MountController rider)
        {
            _rider = rider;

            _movementProcessor?.ResetState();

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

            _movementProcessor?.ResetState();
            _kcc?.SetInputDirection(Vector3.zero);

            if (_animator != null)
            {
                _animator.SetMoveInput(0f);
                _animator.SetMounted(false);
            }
        }

        public void ApplyFixedInput(GameplayInput input, float deltaTime)
        {
            if (_definition == null || Object == null || Object.HasStateAuthority == false)
                return;

            if (_kcc == null)
                return;

            SetLookRotation(_kcc.FixedData, input.LookRotationDelta, deltaTime);

            Vector3 inputDirection = input.MoveDirection.IsZero() == true
                ? Vector3.zero
                : _kcc.FixedData.TransformRotation * input.MoveDirection.X0Y();

            _kcc.SetInputDirection(inputDirection);
        }

        public void ApplyRenderInput(GameplayInput input, float deltaTime)
        {
            if (Object == null || Object.HasInputAuthority == false)
                return;

            if (_kcc == null)
                return;

            SetLookRotation(_kcc.RenderData, input.LookRotationDelta, deltaTime);

            Vector3 inputDirection = input.MoveDirection.IsZero() == true
                ? Vector3.zero
                : _kcc.RenderData.TransformRotation * input.MoveDirection.X0Y();

            _kcc.SetInputDirection(inputDirection);
        }

        private void SetLookRotation(KCCData kccData, Vector2 lookRotationDelta, float deltaTime)
        {
            if (_definition == null)
                return;

            Vector2 lookRotation = kccData.GetLookRotation(true, true);
            lookRotation.y += lookRotationDelta.y * _definition.TurnSpeed * deltaTime;

            _kcc.SetLookRotation(lookRotation);
        }
    }
}
