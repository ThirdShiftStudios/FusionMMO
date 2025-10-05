namespace TPSBR
{
	using UnityEngine;
	using Fusion.Addons.AnimationController;

	public sealed class UpperBodyLayer : AnimationLayer
	{
		// PUBLIC MEMBERS
		public GrenadeState Grenade => _grenade;

		// PRIVATE MEMBERS
		[SerializeField]
		private GrenadeState _grenade;

		private Weapons      _weapons;

		// AnimationLayer INTERFACE

		protected override void OnInitialize()
		{
			_weapons = Controller.GetComponentNoAlloc<Weapons>();
		}

		protected override void OnFixedUpdate()
		{
			
		}
	}
}
