using System;
using Fusion.Addons.KCC;

namespace TPSBR
{
	using UnityEngine;
	using Fusion.Addons.AnimationController;

	[Serializable]
	public sealed class MoveStateCategories
	{
		[SerializeField] 
		private WeaponSize _weaponSize;
		[SerializeField]
		private MoveState _move;

		public WeaponSize WeaponSize => _weaponSize;
		public MoveState Move => _move;
	}

	public sealed class LocomotionLayer : AnimationLayer
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private MoveState _move;
		[SerializeField] 
		private MoveStateCategories[] _moveStateCategories;
		
		private KCC _kcc;
		private Agent _agent;
		private WeaponSize _lastWeaponSize = WeaponSize.Unknown;

		// AnimationState INTERFACE

		protected override void OnInitialize()
		{
			_kcc = Controller.GetComponentNoAlloc<KCC>();
			_agent = Controller.GetComponentNoAlloc<Agent>();

			for (int i = 0; i < _moveStateCategories.Length; i++)
			{
				_moveStateCategories[i].Move.SetAnimationCategory(AnimationCategory.UseFirstSet);
			}
		}

		protected override void OnFixedUpdate()
		{
			KCCData kccData = _kcc.FixedData;
			WeaponSize currentWeaponSize = _agent.Inventory.CurrentWeaponSize;
			if(_lastWeaponSize == currentWeaponSize)
				return;
			_lastWeaponSize = currentWeaponSize;

			// Activate the current weapon
			bool found = false;
			for (int i = 0; i < _moveStateCategories.Length; i++)
			{
				if (_moveStateCategories[i].WeaponSize == currentWeaponSize)
				{
					_moveStateCategories[i].Move.Activate(0.0f);
					found = true;
					continue;
				}
				_moveStateCategories[i].Move.Deactivate(0.0f);
			}

			if (found == false)
			{
				Debug.LogError($"[LocomotionLayer]: Could not find MoveState for WeaponSize == {currentWeaponSize}");
			}
		}
	}
}
