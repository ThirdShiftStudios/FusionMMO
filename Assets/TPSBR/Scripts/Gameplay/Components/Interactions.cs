namespace TPSBR
{
    using System;
    using UnityEngine;
    using Fusion;
    using Fusion.Addons.KCC;

    [DefaultExecutionOrder(-8)]
    public sealed class Interactions : ContextBehaviour
    {
        // PUBLIC MEMBERS

        public IInteraction InteractionTarget { get; private set; }
        public Vector3 TargetPoint { get; private set; }
        public bool IsUndesiredTargetPoint { get; private set; }

        public float ItemDropTime => _itemDropTime;
        public float ItemBoxOpenNormalizedTime => _itemBoxOpenNormalizedTime;
        public float ItemBoxCancelMoveDistance => _itemBoxCancelMoveDistance;
        public float ItemBoxCancelInputThreshold => _itemBoxCancelInputThreshold;
        public float OreCancelMoveDistance => _oreCancelMoveDistance;
        public float OreCancelInputThreshold => _oreCancelInputThreshold;
        public float RuneCancelMoveDistance => _runeCancelMoveDistance;
        public float RuneCancelInputThreshold => _runeCancelInputThreshold;
        public float HerbCancelMoveDistance => _herbCancelMoveDistance;
        public float HerbCancelInputThreshold => _herbCancelInputThreshold;
        public float TreeCancelMoveDistance => _treeCancelMoveDistance;
        public float TreeCancelInputThreshold => _treeCancelInputThreshold;

        public Inventory Inventory => _inventory;
        public Character Character => _character;
        public CharacterAnimationController AnimationController => _animationController;

        public void SetActiveResourceNode(ResourceNode resourceNode)
        {
            ActiveResourceNode = resourceNode;
            InteractionTarget = resourceNode;
        }

        public event Action<string> InteractionFailed;

        // PRIVATE MEMBERS

        [SerializeField] private LayerMask _interactionMask;
        [SerializeField] private float _interactionDistance = 2f;
        [SerializeField] private float _interactionPrecisionRadius = 0.3f;
        [SerializeField] private float _itemDropTime;
        [SerializeField] private float _itemBoxCancelMoveDistance = 0.35f;
        [SerializeField] private float _itemBoxCancelInputThreshold = 0.1f;
        [SerializeField, Range(0.0f, 1.0f)] private float _itemBoxOpenNormalizedTime = 0.8f;
        [SerializeField] private float _oreCancelMoveDistance = 0.35f;
        [SerializeField] private float _oreCancelInputThreshold = 0.1f;
        [SerializeField] private float _runeCancelMoveDistance = 0.35f;
        [SerializeField] private float _runeCancelInputThreshold = 0.1f;
        [SerializeField] private float _herbCancelMoveDistance = 0.35f;
        [SerializeField] private float _herbCancelInputThreshold = 0.1f;
        [SerializeField] private float _treeCancelMoveDistance = 0.35f;
        [SerializeField] private float _treeCancelInputThreshold = 0.1f;

        private Health _health;
        private Inventory _inventory;
        private Character _character;
        private CharacterAnimationController _animationController;

        [Networked]
        public ResourceNode ActiveResourceNode { get; set; }
        private RaycastHit[] _interactionHits = new RaycastHit[10];
        private Transform _interactionCameraTransform;

        // PUBLIC METHODS

        public void TryInteract(bool interact, bool hold)
        {
            if (_animationController != null && _animationController.HasActiveInteraction == true)
            {
                InteractionTarget = _animationController.ActiveInteraction;
                if (InteractionTarget is ResourceNode resourceNode)
                {
                    ActiveResourceNode = resourceNode;
                }
                return;
            }

            if (hold == false)
            {
                return;
            }


            if (_inventory.CurrentWeapon != null && _inventory.CurrentWeapon.IsBusy() == true)
            {
                return;
            }

            if (HasStateAuthority == false)
                return;

            UpdateInteractionTarget();

            if (InteractionTarget == null)
            {
                return;
            }

            if (interact == false)
                return;

            Agent agent = _character != null ? _character.Agent : null;
            var interactionContext = new InteractionContext(this, gameObject, agent, _character, _inventory, _animationController);

            if (InteractionTarget.Interact(interactionContext, out string message) == false && message.HasValue() == true)
            {
                RPC_InteractionFailed(message);
            }
        }

        public Vector3 GetTargetPoint(bool checkReachability, bool resolveRenderHistory)
        {
            var cameraTransform = _character.GetCameraTransform(resolveRenderHistory);
            var cameraDirection = cameraTransform.Rotation * Vector3.forward;

            var fireTransform = _character.GetFireTransform(resolveRenderHistory);
            var targetPoint = cameraTransform.Position + cameraDirection * 500f;

            if (Runner.LagCompensation.Raycast(cameraTransform.Position, cameraDirection, 500f, Object.InputAuthority,
                    out LagCompensatedHit hit, _inventory.HitMask,
                    HitOptions.IncludePhysX | HitOptions.SubtickAccuracy | HitOptions.IgnoreInputAuthority) == true)
            {
                var firingDirection = (hit.Point - fireTransform.Position).normalized;

                // Check angle
                if (Vector3.Dot(cameraDirection, firingDirection) > 0.95f)
                {
                    targetPoint = hit.Point;
                }
            }

            if (checkReachability == true)
            {
                IsUndesiredTargetPoint = _inventory.CurrentWeapon != null &&
                                         _inventory.CurrentWeapon.CanFireToPosition(fireTransform.Position,
                                             ref targetPoint, _inventory.HitMask) == false;
            }

            return targetPoint;
        }

        public void SetInteractionCameraAuthority(Transform cameraTransform)
        {
            if (_character == null || cameraTransform == null)
                return;

            if (_interactionCameraTransform != cameraTransform)
            {
                ClearInteractionCameraAuthority();
                _interactionCameraTransform = cameraTransform;
            }

            _character.SetOtherCameraAuthority(cameraTransform);
        }

        public void ClearInteractionCameraAuthority(Transform cameraTransform = null)
        {
            if (_character == null)
                return;

            if (cameraTransform != null && _interactionCameraTransform != cameraTransform)
                return;

            if (_interactionCameraTransform == null)
                return;

            _character.ClearOtherCameraAuthority(_interactionCameraTransform);
            _interactionCameraTransform = null;
        }

        // NetworkBehaviour INTERFACE

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            InteractionFailed = null;
        }

        public override void Render()
        {
            if (_character.HasInputAuthority == false)
            {
                ClearInteractionCameraAuthority();
                InteractionTarget = null;
                ActiveResourceNode = null;
                return;
            }

            if (_health.IsAlive == false)
            {
                ClearInteractionCameraAuthority();
                InteractionTarget = null;
                ActiveResourceNode = null;
                return;
            }

            if (ActiveResourceNode == null && _animationController != null)
            {
                if (_animationController.ActiveInteraction is ResourceNode activeResource)
                {
                    ActiveResourceNode = activeResource;
                }
            }

            UpdateActiveResourceNode();

            if (ActiveResourceNode != null)
            {
                InteractionTarget = ActiveResourceNode;
            }
            else
            {
                UpdateInteractionTarget();
            }

            TargetPoint = GetTargetPoint(true, false);
        }

        // MonoBehaviour INTERFACE

        private void Awake()
        {
            _health = GetComponent<Health>();
            _inventory = GetComponent<Inventory>();
            _character = GetComponent<Character>();
            _animationController = GetComponent<CharacterAnimationController>();
        }

        private void OnDisable()
        {
            ClearInteractionCameraAuthority();
        }

        // PRIVATE METHODS

        private void UpdateInteractionTarget()
        {
            InteractionTarget = null;

            var cameraTransform = _character.GetCameraTransform(false);
            var cameraDirection = cameraTransform.Rotation * Vector3.forward;

            var physicsScene = Runner.GetPhysicsScene();
            int hitCount = physicsScene.SphereCast(cameraTransform.Position, _interactionPrecisionRadius,
                cameraDirection, _interactionHits, _interactionDistance, _interactionMask,
                QueryTriggerInteraction.Ignore);

            if (hitCount == 0)
                return;

            RaycastHit validHit = default;

            // Try to pick object that is directly in the center of the crosshair
            if (physicsScene.Raycast(cameraTransform.Position, cameraDirection, out RaycastHit raycastHit,
                    _interactionDistance, _interactionMask, QueryTriggerInteraction.Ignore) == true &&
                raycastHit.collider.gameObject.layer == ObjectLayer.Interaction)
            {
                validHit = raycastHit;
            }
            else
            {
                RaycastUtility.Sort(_interactionHits, hitCount);

                for (int i = 0; i < hitCount; i++)
                {
                    var hit = _interactionHits[i];

                    if (hit.collider.gameObject.layer == ObjectLayer.Default)
                        return; // Something is blocking interaction

                    if (hit.collider.gameObject.layer == ObjectLayer.Interaction)
                    {
                        validHit = hit;
                        break;
                    }
                }
            }

            var collider = validHit.collider;

            if (collider == null)
                return;

            var interaction = collider.GetComponent<IInteraction>();
            if (interaction == null)
            {
                interaction = collider.GetComponentInParent<IInteraction>();
            }

            if (interaction != null && interaction.IsActive == true)
            {
                InteractionTarget = interaction;
            }
        }

        private void UpdateActiveResourceNode()
        {
            if (ActiveResourceNode == null)
                return;

            if (ActiveResourceNode.Object == null || ActiveResourceNode.Object.IsSpawned == false)
            {
                ActiveResourceNode = null;
                return;
            }

            Agent agent = _character != null ? _character.Agent : null;
            if (agent == null || ActiveResourceNode.IsInteracting(agent) == false)
            {
                ActiveResourceNode = null;
            }
        }

        // RPCs

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_InteractionFailed(string reason)
        {
            InteractionFailed?.Invoke(reason);
        }
    }
}