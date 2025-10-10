namespace TPSBR
{
	using UnityEngine;
	using Fusion.Addons.KCC;
	using Fusion.Addons.AnimationController;

	[DefaultExecutionOrder(3)]
	public sealed class CharacterAnimationController : AnimationController
	{
		// PRIVATE MEMBERS
		private KCC             _kcc;
		private Agent           _agent;
		private Inventory         _inventory;

		private LocomotionLayer _locomotion;
		private FullBodyLayer   _fullBody;
		private LowerBodyLayer  _lowerBody;
		private UpperBodyLayer  _upperBody;
                private AttackLayer      _attack;
		private LookLayer       _look;

		// PUBLIC METHODS

                public AttackLayer AttackLayer => _attack;

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
				if (_upperBody.Grenade.IsActive() == true && _upperBody.Grenade.CanSwitchWeapon() == false)
					return false;
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
					_kcc.SetCollisionLayerMask(_kcc.Settings.CollisionLayerMask & ~(1 << LayerMask.NameToLayer("AgentKCC")));
				}

				_upperBody.DeactivateAllStates(0.2f, true);
				_look.DeactivateAllStates(0.2f, true);
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

		public void ProcessThrow(bool start, bool hold)
		{
			_upperBody.Grenade.ProcessThrow(start, hold);
		}

		public void Turn(float angle)
		{
			_lowerBody.Turn.Refresh(angle);
		}

		public void RefreshSnapping()
		{

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
			
		}

		protected override void OnEvaluate()
		{

		}

		// MonoBehaviour INTERFACE

		protected override void Awake()
		{
			base.Awake();

			_kcc        = this.GetComponentNoAlloc<KCC>();
			_agent      = this.GetComponentNoAlloc<Agent>();
			_inventory    = this.GetComponentNoAlloc<Inventory>();

			_locomotion = FindLayer<LocomotionLayer>();
			_fullBody   = FindLayer<FullBodyLayer>();
			_lowerBody  = FindLayer<LowerBodyLayer>();
			_upperBody  = FindLayer<UpperBodyLayer>();
			_attack      = FindLayer<AttackLayer>();
			_look       = FindLayer<LookLayer>();

			_kcc.MoveState = _locomotion.FindState<MoveState>();
		}
	}
}
