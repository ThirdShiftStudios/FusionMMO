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
                        WeaponSize currentSize = _inventory.CurrentWeaponSize;
                        int stanceIndex = currentSize.ToStanceIndex();

                        int clipCount = Nodes != null ? Nodes.Length : 0;
                        if (clipCount > 0)
                        {
                                stanceIndex = Mathf.Clamp(stanceIndex, 0, clipCount - 1);
                        }

                        return Mathf.Max(0, stanceIndex);
                }

		// AnimationState INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_inventory = Controller.GetComponentNoAlloc<Inventory>();
		}
	}
}
