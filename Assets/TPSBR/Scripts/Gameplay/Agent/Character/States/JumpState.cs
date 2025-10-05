namespace TPSBR
{
	using Fusion.Addons.AnimationController;

	public class JumpState : MirrorBlendTreeState
	{
		// PRIVATE MEMBERS

		private Inventory _inventory;

		// AnimationState INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_inventory = Controller.GetComponentNoAlloc<Inventory>();
		}
	}
}
