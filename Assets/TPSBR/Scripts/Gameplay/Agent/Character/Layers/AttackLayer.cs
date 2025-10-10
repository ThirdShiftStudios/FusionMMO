using UnityEngine.Serialization;

namespace TPSBR
{
	using UnityEngine;
	using Fusion.Addons.AnimationController;

	public sealed class AttackLayer : AnimationLayer
	{
		// PUBLIC MEMBERS

                public StaffAttackState StaffAttack => staffAttack;

		// PRIVATE MEMBERS

		[FormerlySerializedAs("_shoot")] [SerializeField]
                private StaffAttackState staffAttack;

                public bool TryHandleUse(Weapon weapon, in WeaponUseRequest request)
                {
                        if (request.ShouldUse == false)
                                return true;

                        if (weapon == null)
                                return false;

                        return weapon.HandleAnimationRequest(this, request);
                }

                // AnimationLayer INTERFACE

                protected override void OnFixedUpdate()
                {
			
		}
	}
}
