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

        private Health _health;
        private Inventory _inventory;
        private Character _character;
        private RaycastHit[] _interactionHits = new RaycastHit[10];
        private InteractionsAnimationLayer _interactionsAnimationLayer;
        private OpenChestState _openChestState;
        private ItemBox _activeItemBox;
        private Vector3 _itemBoxStartPosition;
        private bool _isOpeningItemBox;
        private bool _itemBoxOpened;

        // PUBLIC METHODS

        public void TryInteract(bool interact, bool hold)
        {
            if (_isOpeningItemBox == true)
            {
                InteractionTarget = _activeItemBox;

                if (ShouldCancelItemBoxInteraction() == true)
                {
                    CancelItemBoxInteraction();
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

        // NetworkBehaviour INTERFACE

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            InteractionFailed = null;
        }

        public override void Render()
        {
            if (_character.HasInputAuthority == false)
            {
                InteractionTarget = null;
                return;
            }

            if (_health.IsAlive == false)
            {
                InteractionTarget = null;
                return;
            }

            UpdateInteractionTarget();

            TargetPoint = GetTargetPoint(true, false);
        }

        // MonoBehaviour INTERFACE

        private void Awake()
        {
            _health = GetComponent<Health>();
            _inventory = GetComponent<Inventory>();
            _character = GetComponent<Character>();

            InitializeAnimationLayer();
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

        private void InitializeAnimationLayer()
        {
            if (_character == null)
                return;

            CharacterAnimationController animationController = _character.AnimationController;
            if (animationController == null)
                return;

            if (animationController.FindLayer<InteractionsAnimationLayer>(out var interactionsLayer) == true)
            {
                _interactionsAnimationLayer = interactionsLayer;
                _openChestState = _interactionsAnimationLayer.OpenChest;
            }
        }

        private bool EnsureOpenChestState()
        {
            if (_openChestState != null)
                return true;

            if (_interactionsAnimationLayer == null)
            {
                if (_character == null)
                    return false;

                CharacterAnimationController animationController = _character.AnimationController;
                if (animationController == null)
                    return false;

                _interactionsAnimationLayer = animationController.FindLayer<InteractionsAnimationLayer>();
            }

            if (_interactionsAnimationLayer == null)
                return false;

            _openChestState = _interactionsAnimationLayer.OpenChest;
            return _openChestState != null;
        }

        private void TryOpenItemBox(ItemBox itemBox)
        {
            if (itemBox == null)
                return;

            if (EnsureOpenChestState() == false)
            {
                itemBox.Open();
                return;
            }

            if (_openChestState.IsPlaying == true)
                return;

            _activeItemBox = itemBox;
            _itemBoxOpened = false;
            _isOpeningItemBox = true;

            KCC kcc = _character.CharacterController;
            if (kcc != null)
            {
                _itemBoxStartPosition = kcc.FixedData.TargetPosition;
            }
            else
            {
                _itemBoxStartPosition = transform.position;
            }

            if (_openChestState.Play(OnItemBoxOpened, OnItemBoxAnimationFinished, _itemBoxOpenNormalizedTime) == false)
            {
                _isOpeningItemBox = false;
                _activeItemBox = null;
                itemBox.Open();
            }
        }

        private bool ShouldCancelItemBoxInteraction()
        {
            if (_itemBoxOpened == true)
                return false;

            if (_character == null)
                return true;

            KCC kcc = _character.CharacterController;
            if (kcc == null)
                return true;

            KCCData data = kcc.FixedData;

            Vector3 inputDirection = data.InputDirection;
            inputDirection.y = 0f;

            float inputThreshold = _itemBoxCancelInputThreshold * _itemBoxCancelInputThreshold;
            if (inputDirection.sqrMagnitude > inputThreshold)
                return true;

            Vector3 currentPosition = data.TargetPosition;
            Vector3 horizontalDelta = currentPosition - _itemBoxStartPosition;
            horizontalDelta.y = 0f;

            float cancelDistance = _itemBoxCancelMoveDistance * _itemBoxCancelMoveDistance;
            if (horizontalDelta.sqrMagnitude > cancelDistance)
                return true;

            return false;
        }

        private void CancelItemBoxInteraction()
        {
            if (_isOpeningItemBox == false)
                return;

            _isOpeningItemBox = false;
            _itemBoxOpened = false;
            _activeItemBox = null;

            _openChestState?.Cancel();
        }

        private void OnItemBoxOpened()
        {
            if (_activeItemBox == null)
                return;

            _itemBoxOpened = true;
            _activeItemBox.Open();
        }

        private void OnItemBoxAnimationFinished()
        {
            _isOpeningItemBox = false;
            _itemBoxOpened = false;
            _activeItemBox = null;
        }

        // RPCs

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_InteractionFailed(string reason)
        {
            InteractionFailed?.Invoke(reason);
        }
    }
}