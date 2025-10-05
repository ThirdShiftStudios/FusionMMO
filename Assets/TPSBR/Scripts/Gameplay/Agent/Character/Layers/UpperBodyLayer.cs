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

		private Inventory      _inventory;

		// AnimationLayer INTERFACE

		protected override void OnInitialize()
		{
			_inventory = Controller.GetComponentNoAlloc<Inventory>();
		}

		protected override void OnFixedUpdate()
		{
			
		}
	}
}
