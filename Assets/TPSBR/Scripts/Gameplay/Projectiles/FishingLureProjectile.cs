using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class FishingLureProjectile : KinematicProjectile
    {
        private FishingPoleWeapon _weapon;

        [SerializeField]
        private Transform _lineRendererEndPoint;

        public Transform LineRendererEndPoint => _lineRendererEndPoint;
        public void Initialize(FishingPoleWeapon weapon)
        {
            _weapon = weapon;
        }

        protected override void OnImpact(in LagCompensatedHit hit)
        {
            bool hitWater = hit.GameObject != null && hit.GameObject.layer == ObjectLayer.Water;

            if (hitWater == true)
            {
                // Anchor the lure in place when it lands on water so it can
                // remain for the waiting phase.
                transform.position = hit.Point;

                // Disable the despawn cooldown while the lure is
                // sitting in the water.
                DisableDespawnCooldown();

                FishingPoleWeapon waterWeapon = _weapon;
                _weapon = null;

                waterWeapon?.OnLureImpacted(this, hit);
                return;
            }

            base.OnImpact(hit);

            FishingPoleWeapon weapon = _weapon;
            _weapon = null;

            weapon?.OnLureImpacted(this, hit);

            if (Runner != null && Object != null && Object.IsValid == true)
            {
                Runner.Despawn(Object);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            _weapon = null;
        }
    }
}
