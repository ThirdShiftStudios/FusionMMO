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

        [Networked, HideInInspector] public TickTimer DropItemTimer { get; private set; }

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
        [SerializeField] private float _herbCancelMoveDistance = 0.35f;
        [SerializeField] private float _herbCancelInputThreshold = 0.1f;
        [SerializeField] private float _treeCancelMoveDistance = 0.35f;
        [SerializeField] private float _treeCancelInputThreshold = 0.1f;

        private Health _health;
        private Inventory _inventory;
        private Character _character;
        private CharacterAnimationController _animationController;
        private ResourceNode _activeResourceNode;
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
                    _activeResourceNode = resourceNode;
                }
                return;
            }

            if (hold == false)
            {
                DropItemTimer = default;
                return;
            }


            if (_inventory.CurrentWeapon != null && _inventory.CurrentWeapon.IsBusy() == true)
            {
                DropItemTimer = default;
                return;
            }

            if (HasStateAuthority == false)
                return;

            UpdateInteractionTarget();

            if (InteractionTarget == null)
            {
                if (DropItemTimer.IsRunning == false && _inventory.CurrentWeaponSlot > 0 && interact == true)
                {
                    DropItemTimer = TickTimer.CreateFromSeconds(Runner, _itemDropTime);
                }

                if (DropItemTimer.Expired(Runner) == true)
                {
                    DropItemTimer = default;
                    _inventory.DropCurrentWeapon();
                }

                return;
            }

            if (interact == false)
                return;

            if (InteractionTarget is InventoryItemPickupProvider itemProvider)
            {
                _inventory.Pickup(itemProvider);
            }
            else if (InteractionTarget is WeaponPickup weaponPickup)
            {
                _inventory.Pickup(weaponPickup);
            }
            else if (InteractionTarget is ItemBox itemBox)
            {
                TryOpenItemBox(itemBox);
            }
            else if (InteractionTarget is StaticPickup staticPickup)
            {
                bool success = staticPickup.TryConsume(gameObject, out string result);
                if (success == false && result.HasValue() == true)
                {
                    RPC_InteractionFailed(result);
                }
            }
            else if (InteractionTarget is ArcaneConduit arcaneConduit)
            {
                Agent agent = _character != null ? _character.Agent : null;
                arcaneConduit.Interact(agent);
            }
            else if (InteractionTarget is ItemVendor itemVendor)
            {
                Agent agent = _character != null ? _character.Agent : null;
                itemVendor.Interact(agent);
            }
            else if (InteractionTarget is OreNode oreNode)
            {
                TryMineOreNode(oreNode);
            }
            else if (InteractionTarget is HerbNode herbNode)
            {
                TryHarvestHerbNode(herbNode);
            }
            else if (InteractionTarget is TreeNode treeNode)
            {
                TryChopTreeNode(treeNode);
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
                _activeResourceNode = null;
                return;
            }

            if (_health.IsAlive == false)
            {
                ClearInteractionCameraAuthority();
                InteractionTarget = null;
                _activeResourceNode = null;
                return;
            }

            if (_activeResourceNode == null && _animationController != null)
            {
                if (_animationController.ActiveInteraction is ResourceNode activeResource)
                {
                    _activeResourceNode = activeResource;
                }
            }

            UpdateActiveResourceNode();

            if (_activeResourceNode != null)
            {
                InteractionTarget = _activeResourceNode;
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
            if (_activeResourceNode == null)
                return;

            Agent agent = _character != null ? _character.Agent : null;
            if (agent == null || _activeResourceNode.IsInteracting(agent) == false)
            {
                _activeResourceNode = null;
            }
        }

        private void TryOpenItemBox(ItemBox itemBox)
        {
            if (itemBox == null)
                return;

            if (_animationController == null ||
                _animationController.TryStartItemBoxInteraction(itemBox, _itemBoxOpenNormalizedTime,
                    _itemBoxCancelMoveDistance, _itemBoxCancelInputThreshold) == false)
            {
                itemBox.Open();
            }
        }

        private void TryMineOreNode(OreNode oreNode)
        {
            if (oreNode == null)
                return;

            if (_animationController != null &&
                _animationController.TryStartOreInteraction(oreNode, _oreCancelMoveDistance, _oreCancelInputThreshold) == true)
            {
                _activeResourceNode = oreNode;
                InteractionTarget = oreNode;
                return;
            }

            Agent agent = _character != null ? _character.Agent : null;

            if (agent != null && oreNode.TryBeginMining(agent) == true)
            {
                _activeResourceNode = oreNode;
                InteractionTarget = oreNode;
            }
        }

        private void TryHarvestHerbNode(HerbNode herbNode)
        {
            if (herbNode == null)
                return;

            if (_animationController != null &&
                _animationController.TryStartHerbInteraction(herbNode, _herbCancelMoveDistance, _herbCancelInputThreshold) == true)
            {
                _activeResourceNode = herbNode;
                InteractionTarget = herbNode;
                return;
            }

            Agent agent = _character != null ? _character.Agent : null;

            if (agent != null && herbNode.TryBeginHarvesting(agent) == true)
            {
                _activeResourceNode = herbNode;
                InteractionTarget = herbNode;
            }
        }

        private void TryChopTreeNode(TreeNode treeNode)
        {
            if (treeNode == null)
                return;

            if (_animationController != null &&
                _animationController.TryStartTreeInteraction(treeNode, _treeCancelMoveDistance, _treeCancelInputThreshold) == true)
            {
                _activeResourceNode = treeNode;
                InteractionTarget = treeNode;
                return;
            }

            Agent agent = _character != null ? _character.Agent : null;

            if (agent != null && treeNode.TryBeginChopping(agent) == true)
            {
                _activeResourceNode = treeNode;
                InteractionTarget = treeNode;
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