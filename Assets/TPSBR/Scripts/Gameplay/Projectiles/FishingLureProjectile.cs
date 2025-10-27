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
        private Vector3 _visualOffset;

        [SerializeField]
        private Transform _lineRendererEndPoint;

        public Transform LineRendererEndPoint => _lineRendererEndPoint;
        public void Initialize(FishingPoleWeapon weapon)
        {
            _weapon = weapon;
            _isAnchoredInWater = false;
            _applyAnchorOnNextTick = false;
            _anchorPosition = default;
            _visualOffset = Vector3.zero;
        }

        public override void FixedUpdateNetwork()
        {
            if (_isAnchoredInWater == true)
            {
                if (Object != null && Object.IsValid == true)
                {
                    transform.position = _anchorPosition + _visualOffset;
                }

                return;
            }

            base.FixedUpdateNetwork();

            if (_applyAnchorOnNextTick == true)
            {
                _applyAnchorOnNextTick = false;
                _isAnchoredInWater = true;

                if (Object != null && Object.IsValid == true)
                {
                    transform.position = _anchorPosition + _visualOffset;
                }
            }
        }

        public override void Render()
        {
            if (_isAnchoredInWater == true)
            {
                transform.position = _anchorPosition + _visualOffset;
                return;
            }

            base.Render();
        }

        protected override void OnImpact(in LagCompensatedHit hit)
        {
            base.OnImpact(hit);

            bool hitWater = hit.GameObject != null && hit.GameObject.layer == ObjectLayer.Water;

            if (hitWater == true)
            {
                _anchorPosition = hit.Point;
                _applyAnchorOnNextTick = true;
                transform.position = hit.Point + _visualOffset;
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
            _visualOffset = Vector3.zero;
        }

        public void SetVisualOffset(Vector3 offset)
        {
            _visualOffset = offset;

            if (_isAnchoredInWater == true)
            {
                transform.position = _anchorPosition + _visualOffset;
            }
        }
    }
}
