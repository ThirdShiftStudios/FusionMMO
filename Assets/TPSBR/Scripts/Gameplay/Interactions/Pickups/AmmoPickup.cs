using UnityEngine;

namespace TPSBR
{
	public class AmmoPickup : StaticPickup
	{
		// PRIVATE MEMBERS

                [SerializeField]
                private WeaponSize _weaponSize = WeaponSize.Staff;
		[SerializeField]
		private int _amount = 50;

		// StaticPickup INTERFACE

		protected override bool Consume(GameObject instigator, out string result)
		{
			if (instigator.TryGetComponent(out Inventory weapons) == false)
			{
				result = "Not applicable";
				return false;
			}

                        return weapons.AddAmmo(_weaponSize, _amount, out result);
                }
        }
}
