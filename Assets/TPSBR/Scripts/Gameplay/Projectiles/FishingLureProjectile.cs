using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class FishingLureProjectile : KinematicProjectile
    {
        private FishingPoleWeapon _weapon;
        [SerializeField] 
        Transform _lineRendererEndPoint;
        public void Initialize(FishingPoleWeapon weapon)
        {
            _weapon = weapon;
        }

        protected override void OnImpact(in LagCompensatedHit hit)
        {
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
