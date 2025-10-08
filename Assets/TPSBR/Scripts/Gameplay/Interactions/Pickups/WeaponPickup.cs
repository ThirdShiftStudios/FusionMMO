using UnityEngine;

namespace TPSBR
{
	public class WeaponPickup : StaticPickup
	{
		// PUBLIC MEMBERS

		public Weapon WeaponPrefab => _weaponPrefab;

		// PRIVATE MEMBERS

		[SerializeField]
		private Weapon _weaponPrefab;

		// StaticPickup INTERFACE

		protected override bool Consume(GameObject instigator, out string result)
		{
			if (instigator.TryGetComponent(out Inventory weapons) == false)
			{
				result = "Not applicable";
				return false;
			}

			result = string.Empty;
			return true;
		}

                protected override string InteractionName        => _weaponPrefab != null ? _weaponPrefab.DisplayName : string.Empty;
                protected override string InteractionDescription => string.Empty;
        }
}
