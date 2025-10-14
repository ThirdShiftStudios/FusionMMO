namespace TPSBR
{
	using UnityEngine;
	using Fusion.Addons.AnimationController;

	public sealed class DeadState : MultiClipState
	{
		// PRIVATE MEMBERS

                private Agent _agent;
                private Inventory _inventory;

		// MultiClipState INTERFACE

                protected override int GetClipID()
                {
                        WeaponSize currentSize = GetCurrentWeaponSize();
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

                        _agent = Controller.GetComponentNoAlloc<Agent>();
                        _inventory = _agent != null ? _agent.Inventory : null;
                }

                private Inventory GetInventory()
                {
                        if (_agent != null)
                        {
                                Inventory agentInventory = _agent.Inventory;

                                if (ReferenceEquals(_inventory, agentInventory) == false)
                                {
                                        _inventory = agentInventory;
                                }
                        }
                        else
                        {
                                _inventory = null;
                        }

                        return _inventory;
                }

                private WeaponSize GetCurrentWeaponSize()
                {
                        var inventory = GetInventory();

                        return inventory != null ? inventory.CurrentWeaponSize : WeaponSize.Unarmed;
                }
        }
}
