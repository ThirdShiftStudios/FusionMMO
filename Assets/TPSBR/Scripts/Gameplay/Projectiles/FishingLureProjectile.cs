using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class FishingLureProjectile : KinematicProjectile
    {
        private FishingPoleWeapon _weapon;
        private bool _isAnchoredInWater;
        private bool _applyAnchorOnNextTick;
        private Vector3 _anchorPosition;

        [SerializeField]
        private Transform _lineRendererEndPoint;

        public Transform LineRendererEndPoint => _lineRendererEndPoint;
        public void Initialize(FishingPoleWeapon weapon)
        {
            _weapon = weapon;
            _isAnchoredInWater = false;
            _applyAnchorOnNextTick = false;
            _anchorPosition = default;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (_applyAnchorOnNextTick == true)
            {
                _applyAnchorOnNextTick = false;
                _isAnchoredInWater = true;
            }

            if (_isAnchoredInWater == true && Object != null && Object.IsValid == true)
            {
                ProjectileData data = _data;
                data.HasStopped = true;
                data.FinishedPosition = _anchorPosition;
                data.DespawnCooldown = TickTimer.None;
                _data = data;

                transform.position = _anchorPosition;
            }
        }

        protected override void OnImpact(in LagCompensatedHit hit)
        {
            base.OnImpact(hit);

            bool hitWater = hit.GameObject != null && hit.GameObject.layer == ObjectLayer.Water;

            if (hitWater == true)
            {
                _anchorPosition = hit.Point;
                _applyAnchorOnNextTick = true;
                transform.position = hit.Point;
            }

            FishingPoleWeapon weapon = _weapon;
            _weapon = null;

            weapon?.OnLureImpacted(this, hit);

            if (hitWater == false && Runner != null && Object != null && Object.IsValid == true)
            {
                Runner.Despawn(Object);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            _weapon = null;
            _isAnchoredInWater = false;
            _applyAnchorOnNextTick = false;
            _anchorPosition = default;
        }
    }
}
