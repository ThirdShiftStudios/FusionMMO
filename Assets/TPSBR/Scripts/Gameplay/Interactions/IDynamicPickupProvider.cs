using UnityEngine;

namespace TPSBR
{
        public interface IDynamicPickupProvider : IPickup
        {
                Transform InterpolationTarget { get; }
                Collider  Collider            { get; }
                float     DespawnTime         { get; }
        }
}
