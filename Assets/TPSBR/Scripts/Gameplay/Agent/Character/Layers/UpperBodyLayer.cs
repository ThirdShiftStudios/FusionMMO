namespace TPSBR
{
	using UnityEngine;
	using Fusion.Addons.AnimationController;

	public sealed class UpperBodyLayer : AnimationLayer
	{
		// PUBLIC MEMBERS

		// PRIVATE MEMBERS

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
