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

		// AnimationLayer INTERFACE

		protected override void OnFixedUpdate()
		{
			
		}
	}
}
