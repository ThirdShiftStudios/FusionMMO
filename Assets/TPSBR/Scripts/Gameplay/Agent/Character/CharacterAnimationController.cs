namespace TPSBR
{
    using UnityEngine;
    using Fusion.Addons.KCC;
    using Fusion.Addons.AnimationController;

    [DefaultExecutionOrder(3)]
    public sealed class CharacterAnimationController : AnimationController
    {
        // PRIVATE MEMBERS
        private KCC _kcc;
        private Agent _agent;
        private Inventory _inventory;

        private LocomotionLayer _locomotion;
        private FullBodyLayer _fullBody;
        private LowerBodyLayer _lowerBody;
        private UpperBodyLayer _upperBody;
        private AttackLayer _attack;
        private InteractionsAnimationLayer _interactionsLayer;

        private IInteraction _activeInteraction;
        private ItemBox _activeItemBox;
        private Vector3 _itemBoxStartPosition;
        private float _itemBoxCancelDistanceSqr;
        private float _itemBoxCancelInputThresholdSqr;
        private bool _itemBoxOpened;

        // PUBLIC METHODS

        public AttackLayer AttackLayer => _attack;
        public bool HasActiveInteraction => _interactionsLayer != null && _interactionsLayer.HasActiveInteraction;
        public IInteraction ActiveInteraction => _activeInteraction;

        public bool CanJump()
        {
            if (_fullBody.IsActive() == true)
            {
                if (_fullBody.Jump.IsActive(true) == true)
                    return false;
                if (_fullBody.Fall.IsActive(true) == true)
                    return false;
                if (_fullBody.Dead.IsActive(true) == true)
                    return false;
            }

            return true;
        }

        public bool CanSwitchWeapons(bool force)
        {
            if (_fullBody.IsActive() == true)
            {
                if (_fullBody.Dead.IsActive() == true)
                    return false;
            }

            if (_upperBody.IsActive() == true)
            {
                
            }

            return true;
        }

        public void SetDead(bool isDead)
        {
            if (isDead == true)
            {
                _fullBody.Dead.Activate(0.2f);

                if (_kcc.Data.IsGrounded == true)
                {
                    _kcc.SetColliderLayer(LayerMask.NameToLayer("Ignore Raycast"));
                    _kcc.SetCollisionLayerMask(_kcc.Settings.CollisionLayerMask &
                                               ~(1 << LayerMask.NameToLayer("AgentKCC")));
                }

                _upperBody.DeactivateAllStates(0.2f, true);
            }
            else
            {
                _fullBody.Dead.Deactivate(0.2f);
                _kcc.SetShape(EKCCShape.Capsule);
            }
        }

        public bool StartUseItem(Weapon weapon, in WeaponUseRequest request)
        {
            if (weapon == null)
                return false;

            if (_fullBody.Dead.IsActive() == true)
                return false;
            if (_upperBody.HasActiveState() == true)
                return false;

            if (_attack != null && request.ShouldUse == true)
            {
                if (_attack.TryHandleUse(weapon, request) == false)
                    return false;
            }

            return true;
        }

        public void Turn(float angle)
        {
            _lowerBody.Turn.Refresh(angle);
        }

        public void RefreshSnapping()
        {
        }

        public bool TryStartItemBoxInteraction(ItemBox itemBox, float openNormalizedTime, float cancelMoveDistance,
            float cancelInputThreshold)
        {
            if (itemBox == null)
                return false;

            if (_interactionsLayer == null)
                return false;

            if (_interactionsLayer.TryBeginInteraction(InteractionsAnimationLayer.InteractionType.OpenChest) == false)
                return false;

            OpenChestState openChest = _interactionsLayer.OpenChest;
            if (openChest == null)
            {
                _interactionsLayer.EndInteraction(InteractionsAnimationLayer.InteractionType.OpenChest);
                return false;
            }

            if (openChest.Play(openNormalizedTime) == false)
            {
                _interactionsLayer.EndInteraction(InteractionsAnimationLayer.InteractionType.OpenChest);
                return false;
            }

            _activeInteraction = itemBox;
            _activeItemBox = itemBox;
            _itemBoxOpened = false;
            cancelMoveDistance = Mathf.Max(0.0f, cancelMoveDistance);
            cancelInputThreshold = Mathf.Max(0.0f, cancelInputThreshold);

            _itemBoxCancelDistanceSqr = cancelMoveDistance * cancelMoveDistance;
            _itemBoxCancelInputThresholdSqr = cancelInputThreshold * cancelInputThreshold;
            _itemBoxStartPosition = _kcc != null ? _kcc.FixedData.TargetPosition : transform.position;

            return true;
        }

        // AnimationController INTERFACE

        protected override void OnSpawned()
        {
            if (HasStateAuthority == true)
            {
                Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        protected override void OnFixedUpdate()
        {
            UpdateInteractionState();
        }

        protected override void OnEvaluate()
        {
        }

        // MonoBehaviour INTERFACE

        protected override void Awake()
        {
            base.Awake();

            _kcc = this.GetComponentNoAlloc<KCC>();
            _agent = this.GetComponentNoAlloc<Agent>();
            _inventory = this.GetComponentNoAlloc<Inventory>();

            _locomotion = FindLayer<LocomotionLayer>();
            _fullBody = FindLayer<FullBodyLayer>();
            _lowerBody = FindLayer<LowerBodyLayer>();
            _upperBody = FindLayer<UpperBodyLayer>();
            _attack = FindLayer<AttackLayer>();
            _interactionsLayer = FindLayer<InteractionsAnimationLayer>();

            _kcc.MoveState = _locomotion.FindState<MoveState>();
        }

        private void UpdateInteractionState()
        {
            if (_interactionsLayer == null || _interactionsLayer.ActiveInteraction == InteractionsAnimationLayer.InteractionType.None)
                return;

            if (_interactionsLayer.ActiveInteraction != InteractionsAnimationLayer.InteractionType.OpenChest)
                return;

            OpenChestState openChest = _interactionsLayer.OpenChest;

            if (_activeItemBox == null || openChest == null)
            {
                openChest?.Cancel();
                CancelItemBoxInteraction();
                return;
            }

            if (openChest.IsPlaying == false)
            {
                FinishItemBoxInteraction();
                return;
            }

            if (ShouldCancelItemBoxInteraction() == true)
            {
                openChest.Cancel();
                CancelItemBoxInteraction();
                return;
            }

            if (_itemBoxOpened == false && openChest.TryConsumeOpenTrigger() == true)
            {
                _itemBoxOpened = true;
                _activeItemBox.Open();
            }
        }

        private bool ShouldCancelItemBoxInteraction()
        {
            if (_itemBoxOpened == true)
                return false;

            if (_kcc == null)
                return true;

            KCCData data = _kcc.FixedData;

            Vector3 inputDirection = data.InputDirection;
            inputDirection.y = 0f;

            if (inputDirection.sqrMagnitude > _itemBoxCancelInputThresholdSqr)
                return true;

            Vector3 horizontalDelta = data.TargetPosition - _itemBoxStartPosition;
            horizontalDelta.y = 0f;

            if (horizontalDelta.sqrMagnitude > _itemBoxCancelDistanceSqr)
                return true;

            return false;
        }

        private void CancelItemBoxInteraction()
        {
            _interactionsLayer?.EndInteraction(InteractionsAnimationLayer.InteractionType.OpenChest);
            _activeInteraction = null;
            _activeItemBox = null;
            _itemBoxOpened = false;
            _itemBoxCancelDistanceSqr = 0f;
            _itemBoxCancelInputThresholdSqr = 0f;
        }

        private void FinishItemBoxInteraction()
        {
            _interactionsLayer?.EndInteraction(InteractionsAnimationLayer.InteractionType.OpenChest);
            _activeInteraction = null;
            _activeItemBox = null;
            _itemBoxOpened = false;
            _itemBoxCancelDistanceSqr = 0f;
            _itemBoxCancelInputThresholdSqr = 0f;
        }
    }
}