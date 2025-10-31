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

        public override bool Interact(in InteractionContext context, out string message)
        {
                Inventory inventory = context.Inventory;

                if (inventory == null && context.Interactor != null)
                {
                        inventory = context.Interactor.GetComponent<Inventory>();
                }

                if (inventory == null)
                {
                        message = "No inventory available";
                        return false;
                }

                inventory.Pickup(this);

                message = string.Empty;
                return true;
        }

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
