namespace TPSBR
{
    using UnityEngine;
    using Fusion;
    using Fusion.Addons.KCC;

    [DefaultExecutionOrder(-4)]
    public sealed class MountController : ContextBehaviour
    {
        [SerializeField] private Transform _riderAnchor;

        private Agent _agent;
        private Character _character;
        private Interactions _interactions;
        private MountCollection _mountCollection;
        private Inventory _inventory;
        private HorseMount _activeMount;
        private bool _kccEnabled;
        private Transform _defaultRiderAnchor;
        [SerializeField]
        [Tooltip("How long to ignore dismount inputs right after mounting.")]
        private float _dismountInputBlockDuration = 0.25f;
        private float _dismountInputBlockedUntil;
        [SerializeField]
        [Tooltip("How long the mount input needs to be held to spawn the equipped mount.")]
        private float _mountSpawnHoldDuration = 1f;
        private float _mountHoldTimer;
        private float _mountHoldStartTime = -1f;
        private bool _mountSpawnRequested;

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

            _dismountInputBlockedUntil = Time.time + _dismountInputBlockDuration;

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

            HorseMount dismountedMount = _activeMount;

            _activeMount.EndRide();
            _activeMount = null;

            _character.CharacterController.enabled = _kccEnabled;
            _character.CharacterController.SetPosition(dismountPosition);
            _interactions?.ClearInteractionCameraAuthority();

            _character.AnimationController?.SetMounted(false, null);

            _riderAnchor = _defaultRiderAnchor;

            if (dismountedMount != null && Runner != null && HasStateAuthority == true)
            {
                NetworkObject mountObject = dismountedMount.Object;
                if (mountObject != null)
                {
                    Runner.Despawn(mountObject);
                }
            }
        }

        public bool ProcessFixedInput(GameplayInput input, float deltaTime)
        {
            bool mountHeld = EGameplayInputAction.Mount.IsActive(input);

            if (_activeMount == null)
            {
                if (mountHeld == true)
                {
                    if (_mountHoldStartTime < 0f)
                    {
                        _mountHoldStartTime = Time.time;
                        _mountHoldTimer = 0f;
                    }

                    _mountHoldTimer = Time.time - _mountHoldStartTime;

                    if (_mountSpawnRequested == false && _mountHoldTimer >= _mountSpawnHoldDuration)
                    {
                        _mountSpawnRequested = true;

                        if (HasStateAuthority == true)
                        {
                            SpawnEquippedMount();
                        }
                        else
                        {
                            RPC_RequestSpawnEquippedMount();
                        }
                    }
                }
                else
                {
                    ResetMountHold();
                }

                return false;
            }

            ResetMountHold();

            if (HasStateAuthority == false)
                return true;

            bool canProcessDismountInput = Time.time >= _dismountInputBlockedUntil;
            bool dismountRequested = canProcessDismountInput && (
                _agent.AgentInput.WasActivated(EGameplayInputAction.Mount, input) == true ||
                _agent.AgentInput.WasActivated(EGameplayInputAction.Jump, input) == true);

            if (dismountRequested == true)
            {
                Dismount();
                return true;
            }

            _activeMount.ApplyFixedInput(input, deltaTime);
            SyncRiderTransform();
            return true;
        }

        public void ProcessRenderInput(GameplayInput input, float deltaTime)
        {
            if (Object == null || Object.HasInputAuthority == false)
                return;

            bool mountHeld = EGameplayInputAction.Mount.IsActive(input);

            if (_activeMount == null)
            {
                if (mountHeld == true)
                {
                    if (_mountHoldStartTime < 0f)
                    {
                        _mountHoldStartTime = Time.time;
                        _mountHoldTimer = 0f;
                    }

                    _mountHoldTimer = Time.time - _mountHoldStartTime;

                    if (_mountSpawnRequested == false && _mountHoldTimer >= _mountSpawnHoldDuration)
                    {
                        _mountSpawnRequested = true;

                        if (HasStateAuthority == true)
                        {
                            SpawnEquippedMount();
                        }
                        else
                        {
                            RPC_RequestSpawnEquippedMount();
                        }
                    }

                    return;
                }

                ResetMountHold();

                return;
            }

            ResetMountHold();

            _activeMount.ApplyRenderInput(input, deltaTime);
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
            _inventory = GetComponent<Inventory>();
            _defaultRiderAnchor = _riderAnchor != null ? _riderAnchor : transform;
        }

        private void ResetMountHold()
        {
            _mountHoldTimer = 0f;
            _mountHoldStartTime = -1f;
            _mountSpawnRequested = false;
        }

        private MountDefinition GetEquippedMountDefinition()
        {
            if (_inventory == null)
                return null;

            InventorySlot mountSlot = _inventory.GetItemSlot(Inventory.MOUNT_SLOT_INDEX);
            if (mountSlot.IsEmpty == true)
                return null;

            return mountSlot.GetDefinition() as MountDefinition;
        }

        private void SpawnEquippedMount()
        {
            if (_activeMount != null)
                return;

            if (Runner == null || HasStateAuthority == false)
                return;

            MountDefinition mountDefinition = GetEquippedMountDefinition();
            if (mountDefinition == null)
                return;

            if (_mountCollection != null && mountDefinition.Code.HasValue() == true && _mountCollection.HasMount(mountDefinition.Code) == false)
                return;

            MountBase mountPrefab = mountDefinition.MountBase;
            if (mountPrefab == null)
                return;

            Vector3 spawnPosition = transform.position;
            Quaternion spawnRotation = transform.rotation;

            if (_character != null)
            {
                KCC kcc = _character.CharacterController;
                KCCData kccData = kcc != null ? kcc.FixedData : default;

                spawnPosition = kcc != null ? kccData.GroundPosition : _character.transform.position;
                spawnRotation = kcc != null ? kccData.TransformRotation : _character.transform.rotation;
            }

            MountBase spawnedMount = Runner.Spawn(mountPrefab, spawnPosition, spawnRotation, Object.InputAuthority, (runner, obj) =>
            {
                if (obj is IContextBehaviour contextBehaviour)
                {
                    contextBehaviour.Context = Context;
                }
            });

            HorseMount horseMount = spawnedMount as HorseMount;
            if (horseMount == null)
            {
                horseMount = spawnedMount != null ? spawnedMount.GetComponent<HorseMount>() : null;
            }

            if (horseMount != null)
            {
                TryMount(horseMount);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestSpawnEquippedMount(RpcInfo rpcInfo = default)
        {
            SpawnEquippedMount();
        }
    }
}
