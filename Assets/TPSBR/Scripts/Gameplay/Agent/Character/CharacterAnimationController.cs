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
        private OreNode _activeOreNode;
        private Vector3 _oreStartPosition;
        private float _oreCancelDistanceSqr;
        private float _oreCancelInputThresholdSqr;
        private HerbNode _activeHerbNode;
        private Vector3 _herbStartPosition;
        private float _herbCancelDistanceSqr;
        private float _herbCancelInputThresholdSqr;
        private TreeNode _activeTreeNode;
        private Vector3 _treeStartPosition;
        private float _treeCancelDistanceSqr;
        private float _treeCancelInputThresholdSqr;

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

        public bool TryStartOreInteraction(OreNode oreNode, float cancelMoveDistance, float cancelInputThreshold)
        {
            if (oreNode == null)
                return false;

            if (_agent == null)
                return false;

            if (_interactionsLayer == null)
                return false;

            if (_interactionsLayer.TryBeginInteraction(InteractionsAnimationLayer.InteractionType.MineOre) == false)
                return false;

            MineOreState mineOre = _interactionsLayer.MineOre;
            if (mineOre == null)
            {
                _interactionsLayer.EndInteraction(InteractionsAnimationLayer.InteractionType.MineOre);
                return false;
            }

            if (mineOre.Play() == false)
            {
                _interactionsLayer.EndInteraction(InteractionsAnimationLayer.InteractionType.MineOre);
                return false;
            }

            if (oreNode.TryBeginMining(_agent) == false)
            {
                mineOre.Stop();
                _interactionsLayer.EndInteraction(InteractionsAnimationLayer.InteractionType.MineOre);
                return false;
            }

            _inventory?.BeginPickaxeUse();

            _activeInteraction = oreNode;
            _activeOreNode = oreNode;

            cancelMoveDistance = Mathf.Max(0.0f, cancelMoveDistance);
            cancelInputThreshold = Mathf.Max(0.0f, cancelInputThreshold);

            _oreCancelDistanceSqr = cancelMoveDistance * cancelMoveDistance;
            _oreCancelInputThresholdSqr = cancelInputThreshold * cancelInputThreshold;
            _oreStartPosition = _kcc != null ? _kcc.FixedData.TargetPosition : transform.position;

            return true;
        }

        public bool TryStartTreeInteraction(TreeNode treeNode, float cancelMoveDistance, float cancelInputThreshold)
        {
            if (treeNode == null)
                return false;

            if (_agent == null)
                return false;

            if (_interactionsLayer == null)
                return false;

            if (_interactionsLayer.TryBeginInteraction(InteractionsAnimationLayer.InteractionType.ChopTree) == false)
                return false;

            ChopTreeState chopTree = _interactionsLayer.ChopTree;
            if (chopTree == null)
            {
                _interactionsLayer.EndInteraction(InteractionsAnimationLayer.InteractionType.ChopTree);
                return false;
            }

            if (chopTree.Play() == false)
            {
                _interactionsLayer.EndInteraction(InteractionsAnimationLayer.InteractionType.ChopTree);
                return false;
            }

            if (treeNode.TryBeginChopping(_agent) == false)
            {
                chopTree.Stop();
                _interactionsLayer.EndInteraction(InteractionsAnimationLayer.InteractionType.ChopTree);
                return false;
            }

            _inventory?.BeginWoodAxeUse();

            _activeInteraction = treeNode;
            _activeTreeNode = treeNode;

            cancelMoveDistance = Mathf.Max(0.0f, cancelMoveDistance);
            cancelInputThreshold = Mathf.Max(0.0f, cancelInputThreshold);

            _treeCancelDistanceSqr = cancelMoveDistance * cancelMoveDistance;
            _treeCancelInputThresholdSqr = cancelInputThreshold * cancelInputThreshold;
            _treeStartPosition = _kcc != null ? _kcc.FixedData.TargetPosition : transform.position;

            return true;
        }

        public bool TryStartHerbInteraction(HerbNode herbNode, float cancelMoveDistance, float cancelInputThreshold)
        {
            if (herbNode == null)
                return false;

            if (_agent == null)
                return false;

            if (_interactionsLayer == null)
                return false;

            if (_interactionsLayer.TryBeginInteraction(InteractionsAnimationLayer.InteractionType.GatherHerbs) == false)
                return false;

            GatherHerbsState gatherHerbs = _interactionsLayer.GatherHerbs;
            if (gatherHerbs == null)
            {
                _interactionsLayer.EndInteraction(InteractionsAnimationLayer.InteractionType.GatherHerbs);
                return false;
            }

            if (gatherHerbs.Play() == false)
            {
                _interactionsLayer.EndInteraction(InteractionsAnimationLayer.InteractionType.GatherHerbs);
                return false;
            }

            if (herbNode.TryBeginHarvesting(_agent) == false)
            {
                gatherHerbs.Stop();
                _interactionsLayer.EndInteraction(InteractionsAnimationLayer.InteractionType.GatherHerbs);
                return false;
            }

            _activeInteraction = herbNode;
            _activeHerbNode = herbNode;

            cancelMoveDistance = Mathf.Max(0.0f, cancelMoveDistance);
            cancelInputThreshold = Mathf.Max(0.0f, cancelInputThreshold);

            _herbCancelDistanceSqr = cancelMoveDistance * cancelMoveDistance;
            _herbCancelInputThresholdSqr = cancelInputThreshold * cancelInputThreshold;
            _herbStartPosition = _kcc != null ? _kcc.FixedData.TargetPosition : transform.position;

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

            switch (_interactionsLayer.ActiveInteraction)
            {
                case InteractionsAnimationLayer.InteractionType.OpenChest:
                    UpdateOpenChestInteraction();
                    break;
                case InteractionsAnimationLayer.InteractionType.MineOre:
                    UpdateOreInteraction();
                    break;
                case InteractionsAnimationLayer.InteractionType.GatherHerbs:
                    UpdateHerbInteraction();
                    break;
                case InteractionsAnimationLayer.InteractionType.ChopTree:
                    UpdateTreeInteraction();
                    break;
            }
        }

        private bool ShouldCancelItemBoxInteraction()
        {
            return ShouldCancelInteraction(_itemBoxStartPosition, _itemBoxCancelDistanceSqr, _itemBoxCancelInputThresholdSqr);
        }

        private void CancelItemBoxInteraction()
        {
            OpenChestState openChest = _interactionsLayer?.OpenChest;

            openChest?.Cancel();
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

        private void UpdateOpenChestInteraction()
        {
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

                if (_kcc != null)
                {
                    _itemBoxStartPosition = _kcc.FixedData.TargetPosition;
                }
                else
                {
                    _itemBoxStartPosition = transform.position;
                }

                _activeItemBox.Open();
            }
        }

        private void UpdateOreInteraction()
        {
            MineOreState mineOre = _interactionsLayer.MineOre;

            if (_activeOreNode == null || mineOre == null)
            {
                CancelOreInteraction();
                return;
            }

            if (mineOre.IsPlaying == false)
            {
                FinishOreInteraction();
                return;
            }

            if (ShouldCancelInteraction(_oreStartPosition, _oreCancelDistanceSqr, _oreCancelInputThresholdSqr) == true)
            {
                CancelOreInteraction();
                return;
            }

            if (_agent == null)
                return;

            float deltaTime = Runner != null ? Runner.DeltaTime : Time.fixedDeltaTime;

            if (_activeOreNode.TickMining(deltaTime, _agent) == true)
            {
                FinishOreInteraction();
            }
        }

        private void CancelOreInteraction()
        {
            MineOreState mineOre = _interactionsLayer?.MineOre;

            mineOre?.Stop();
            _interactionsLayer?.EndInteraction(InteractionsAnimationLayer.InteractionType.MineOre);

            _activeOreNode?.CancelMining(_agent);

            ResetOreInteraction();
        }

        private void FinishOreInteraction()
        {
            MineOreState mineOre = _interactionsLayer?.MineOre;

            mineOre?.Stop();
            _interactionsLayer?.EndInteraction(InteractionsAnimationLayer.InteractionType.MineOre);

            ResetOreInteraction();
        }

        private void ResetOreInteraction()
        {
            _inventory?.EndPickaxeUse();

            _activeInteraction = null;
            _activeOreNode = null;
            _oreStartPosition = Vector3.zero;
            _oreCancelDistanceSqr = 0f;
            _oreCancelInputThresholdSqr = 0f;
        }

        private void UpdateHerbInteraction()
        {
            GatherHerbsState gatherHerbs = _interactionsLayer.GatherHerbs;

            if (_activeHerbNode == null || gatherHerbs == null)
            {
                CancelHerbInteraction();
                return;
            }

            if (gatherHerbs.IsPlaying == false)
            {
                FinishHerbInteraction();
                return;
            }

            if (ShouldCancelInteraction(_herbStartPosition, _herbCancelDistanceSqr, _herbCancelInputThresholdSqr) == true)
            {
                CancelHerbInteraction();
                return;
            }

            if (_agent == null)
                return;

            float deltaTime = Runner != null ? Runner.DeltaTime : Time.fixedDeltaTime;

            if (_activeHerbNode.TickHarvesting(deltaTime, _agent) == true)
            {
                FinishHerbInteraction();
            }
        }

        private void CancelHerbInteraction()
        {
            GatherHerbsState gatherHerbs = _interactionsLayer?.GatherHerbs;

            gatherHerbs?.Stop();
            _interactionsLayer?.EndInteraction(InteractionsAnimationLayer.InteractionType.GatherHerbs);

            _activeHerbNode?.CancelHarvesting(_agent);

            ResetHerbInteraction();
        }

        private void FinishHerbInteraction()
        {
            GatherHerbsState gatherHerbs = _interactionsLayer?.GatherHerbs;

            gatherHerbs?.Stop();
            _interactionsLayer?.EndInteraction(InteractionsAnimationLayer.InteractionType.GatherHerbs);

            ResetHerbInteraction();
        }

        private void ResetHerbInteraction()
        {
            _activeInteraction = null;
            _activeHerbNode = null;
            _herbStartPosition = Vector3.zero;
            _herbCancelDistanceSqr = 0f;
            _herbCancelInputThresholdSqr = 0f;
        }

        private void UpdateTreeInteraction()
        {
            ChopTreeState chopTree = _interactionsLayer.ChopTree;

            if (_activeTreeNode == null || chopTree == null)
            {
                CancelTreeInteraction();
                return;
            }

            if (chopTree.IsPlaying == false)
            {
                FinishTreeInteraction();
                return;
            }

            if (ShouldCancelInteraction(_treeStartPosition, _treeCancelDistanceSqr, _treeCancelInputThresholdSqr) == true)
            {
                CancelTreeInteraction();
                return;
            }

            if (_agent == null)
                return;

            float deltaTime = Runner != null ? Runner.DeltaTime : Time.fixedDeltaTime;

            if (_activeTreeNode.TickChopping(deltaTime, _agent) == true)
            {
                FinishTreeInteraction();
            }
        }

        private void CancelTreeInteraction()
        {
            ChopTreeState chopTree = _interactionsLayer?.ChopTree;

            chopTree?.Stop();
            _interactionsLayer?.EndInteraction(InteractionsAnimationLayer.InteractionType.ChopTree);

            _activeTreeNode?.CancelChopping(_agent);

            ResetTreeInteraction();
        }

        private void FinishTreeInteraction()
        {
            ChopTreeState chopTree = _interactionsLayer?.ChopTree;

            chopTree?.Stop();
            _interactionsLayer?.EndInteraction(InteractionsAnimationLayer.InteractionType.ChopTree);

            ResetTreeInteraction();
        }

        private void ResetTreeInteraction()
        {
            _inventory?.EndWoodAxeUse();

            _activeInteraction = null;
            _activeTreeNode = null;
            _treeStartPosition = Vector3.zero;
            _treeCancelDistanceSqr = 0f;
            _treeCancelInputThresholdSqr = 0f;
        }

        private bool ShouldCancelInteraction(Vector3 startPosition, float cancelDistanceSqr, float cancelInputThresholdSqr)
        {
            if (_kcc == null)
                return true;

            KCCData data = _kcc.FixedData;

            Vector3 inputDirection = data.InputDirection;
            inputDirection.y = 0f;

            if (inputDirection.sqrMagnitude > cancelInputThresholdSqr)
                return true;

            Vector3 horizontalDelta = data.TargetPosition - startPosition;
            horizontalDelta.y = 0f;

            if (horizontalDelta.sqrMagnitude > cancelDistanceSqr)
                return true;

            return false;
        }
    }
}
