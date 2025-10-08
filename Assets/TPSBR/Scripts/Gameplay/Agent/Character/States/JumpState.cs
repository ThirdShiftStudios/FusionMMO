using Fusion.Addons.KCC;
using UnityEngine;

namespace TPSBR
{
	using Fusion.Addons.AnimationController;

	public class JumpState : ClipState
	{
		// PRIVATE MEMBERS

		private Inventory _inventory;
		private KCC     _kcc;
		private Agent   _agent;
		
		// AnimationState INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_inventory = Controller.GetComponentNoAlloc<Inventory>();
		}
	}
}
