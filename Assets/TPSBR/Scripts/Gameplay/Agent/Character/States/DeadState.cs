namespace TPSBR
{
	using UnityEngine;
	using Fusion.Addons.AnimationController;

	public sealed class DeadState : MultiClipState
	{
		// PRIVATE MEMBERS

		private Inventory _inventory;

		// MultiClipState INTERFACE

		protected override int GetClipID()
		{
			if (_inventory.CurrentWeaponSlot > 2)
				return 1; // For grenades we use pistol set

			return Mathf.Max(0, _inventory.CurrentWeaponSlot);
		}

		// AnimationState INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_inventory = Controller.GetComponentNoAlloc<Inventory>();
		}
	}
}
