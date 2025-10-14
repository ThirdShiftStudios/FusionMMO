namespace TPSBR
{
	using UnityEngine;
	using Fusion.Addons.AnimationController;

	public sealed class UpperBodyLayer : AnimationLayer
	{
		// PUBLIC MEMBERS

		// PRIVATE MEMBERS

                private Agent          _agent;
                private Inventory      _inventory;

		// AnimationLayer INTERFACE

		protected override void OnInitialize()
		{
                        _agent = Controller.GetComponentNoAlloc<Agent>();
                        _inventory = _agent != null ? _agent.Inventory : null;
                }

                protected override void OnFixedUpdate()
                {

                }
        }
}
