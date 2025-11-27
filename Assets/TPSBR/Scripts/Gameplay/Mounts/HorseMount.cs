namespace TPSBR
{
    using Fusion;
    using UnityEngine;

    [DefaultExecutionOrder(-3)]
    public sealed class HorseMount : NetworkBehaviour, IInteraction
    {
        [SerializeField] private MountDefinition _definition;
        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _riderAnchor;
        [SerializeField] private LayerMask _blockedLayers;

        private MountController _rider;
        private float _currentSpeed;

        public string Name => _definition != null ? _definition.DisplayName : "Mount";
        public string Description => _definition != null ? _definition.Description : string.Empty;
        public Vector3 HUDPosition => _interactionPoint != null ? _interactionPoint.position : transform.position + Vector3.up;
        public bool IsActive => _rider == null;
        public string MountCode => _definition != null ? _definition.Code : string.Empty;
        public Transform MountCamera => _cameraTransform;
        public Transform RiderAnchor => _riderAnchor;

        public bool Interact(in InteractionContext context, out string message)
        {
            message = default;

            if (_definition == null)
            {
                message = "Missing mount definition.";
                return false;
            }

            if (IsActive == false)
            {
                message = "Mount already occupied.";
                return false;
            }

            var riderController = context.Agent != null ? context.Agent.GetComponent<MountController>() : null;
            var mountInventory = context.Agent != null ? context.Agent.GetComponent<MountCollection>() : null;

            if (riderController == null)
            {
                message = "No rider controller found.";
                return false;
            }

            if (mountInventory != null && mountInventory.HasMount(MountCode) == false)
            {
                message = "You have not unlocked this mount.";
                return false;
            }

            riderController.TryMount(this);
            return true;
        }

        public void BeginRide(MountController rider)
        {
            _rider = rider;
            _currentSpeed = 0f;
        }

        public void EndRide()
        {
            _rider = null;
            _currentSpeed = 0f;
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
            if (displacement.sqrMagnitude < float.Epsilon)
                return;

            Vector3 nextPosition = transform.position + displacement;
            if (Physics.Linecast(transform.position, nextPosition, out RaycastHit hit, _blockedLayers, QueryTriggerInteraction.Ignore) == true)
            {
                nextPosition = hit.point;
            }

            transform.position = nextPosition;
        }
    }
}
