namespace TPSBR
{
    using UnityEngine;
    using Fusion;

    [DefaultExecutionOrder(-4)]
    public sealed class MountController : ContextBehaviour
    {
        [SerializeField] private Transform _riderAnchor;

        private Agent _agent;
        private Character _character;
        private Interactions _interactions;
        private MountCollection _mountCollection;
        private HorseMount _activeMount;
        private bool _kccEnabled;
        private Transform _defaultRiderAnchor;

        public bool IsMounted => _activeMount != null;
        public HorseMount ActiveMount => _activeMount;

        public void TryMount(HorseMount mount)
        {
            if (mount == null || IsMounted == true)
                return;

            if (_mountCollection != null && _mountCollection.HasMount(mount.MountCode) == false)
                return;

            if (HasStateAuthority == false)
                return;

            _activeMount = mount;
            _activeMount.BeginRide(this);

            Transform preferredAnchor = mount.RiderAnchor != null ? mount.RiderAnchor : _defaultRiderAnchor;
            _riderAnchor = preferredAnchor != null ? preferredAnchor : mount.transform;

            _kccEnabled = _character.CharacterController.enabled;
            _character.CharacterController.enabled = false;

            if (_riderAnchor != null)
            {
                _character.transform.SetPositionAndRotation(_riderAnchor.position, _riderAnchor.rotation);
                _character.transform.SetParent(_riderAnchor, true);
            }

            if (_activeMount.MountCamera != null)
            {
                _interactions?.SetInteractionCameraAuthority(_activeMount.MountCamera);
            }

            _character.AnimationController?.SetMounted(true, mount.Definition);
        }

        public void Dismount()
        {
            if (_activeMount == null)
                return;

            Transform activeMountTransform = _activeMount.transform;
            Vector3 dismountPosition = activeMountTransform.position;

            if (_riderAnchor != null && _character.transform.parent == _riderAnchor)
            {
                _character.transform.SetParent(null, true);
            }

            _character.transform.SetPositionAndRotation(dismountPosition, activeMountTransform.rotation);

            _activeMount.EndRide();
            _activeMount = null;

            _character.CharacterController.enabled = _kccEnabled;
            _character.CharacterController.SetPosition(dismountPosition);
            _interactions?.ClearInteractionCameraAuthority();

            _character.AnimationController?.SetMounted(false, null);

            _riderAnchor = _defaultRiderAnchor;
        }

        public bool ProcessFixedInput(GameplayInput input, float deltaTime)
        {
            if (_activeMount == null)
                return false;

            if (_agent.AgentInput.WasActivated(EGameplayInputAction.Mount, input) == true ||
                _agent.AgentInput.WasActivated(EGameplayInputAction.Interact, input) == true ||
                _agent.AgentInput.WasActivated(EGameplayInputAction.Jump, input) == true)
            {
                Dismount();
                return true;
            }

            _activeMount.SimulateMovement(input.MoveDirection, input.LookRotationDelta, deltaTime);
            SyncRiderTransform();
            return true;
        }

        public void SyncRiderTransform()
        {
            if (_activeMount == null || _riderAnchor == null)
                return;

            _character.transform.SetPositionAndRotation(_riderAnchor.position, _riderAnchor.rotation);
        }

        public override void Spawned()
        {
            base.Spawned();

            _agent = GetComponent<Agent>();
            _character = GetComponent<Character>();
            _interactions = GetComponent<Interactions>();
            _mountCollection = GetComponent<MountCollection>();
            _defaultRiderAnchor = _riderAnchor != null ? _riderAnchor : transform;
        }
    }
}
