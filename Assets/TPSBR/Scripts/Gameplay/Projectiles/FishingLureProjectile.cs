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

            FishingPoleWeapon weapon = _weapon;
            _weapon = null;

            weapon?.OnLureImpacted(this, hit);

            if (hitWater == true)
            {
                if (Runner != null && Object != null && Object.IsValid == true)
                {
                    SetDespawnCooldown(float.MaxValue);
                    transform.position = hit.Point;
                }

                return;
            }

            base.OnImpact(hit);

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
