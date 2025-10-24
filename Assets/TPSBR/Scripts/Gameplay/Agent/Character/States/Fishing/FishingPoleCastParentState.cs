using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public sealed class FishingPoleCastParentState : MixerState
    {
        public enum CastResult
        {
            Cancelled,
            Thrown,
        }

        [SerializeField]
        private float _blendInDuration = 0.1f;

        [SerializeField]
        private float _blendOutDuration = 0.1f;

        [SerializeField]
        private FishingCastBeginState _begin;

        [SerializeField]
        private FishingCastThrowState _throw;

        private FishingPoleWeapon _activeWeapon;
        private bool _throwRequested;
        private bool _isThrowing;
        private bool _isFinishing;

        public bool TryBeginCast(FishingPoleWeapon weapon)
        {
            if (weapon == null)
                return false;

            if (_begin == null || _throw == null)
            {
                weapon.OnCastRequestFailed();
                return false;
            }

            if (_activeWeapon != null)
            {
                if (_activeWeapon == weapon)
                    return true;

                weapon.OnCastRequestFailed();
                return false;
            }

            _activeWeapon = weapon;
            _throwRequested = false;
            _isThrowing = false;
            _isFinishing = false;

            Activate(_blendInDuration);

            _throw.Stop();
            _begin.Play();

            _activeWeapon.OnCastStateEntered(this);

            return true;
        }

        public void RequestThrow(FishingPoleWeapon weapon)
        {
            if (weapon == null || _activeWeapon != weapon)
                return;

            _throwRequested = true;
        }

        public void RequestCancel(FishingPoleWeapon weapon)
        {
            if (weapon == null || _activeWeapon != weapon)
                return;

            Finish(CastResult.Cancelled);
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (_activeWeapon == null)
                return;

            if (_throwRequested == true && _isThrowing == false)
            {
                StartThrow();
            }

            if (_isThrowing == true)
            {
                if (_throw.IsPlaying == false || _throw.IsFinished())
                {
                    Finish(CastResult.Thrown);
                }

                return;
            }
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();

            if (_activeWeapon != null && _isFinishing == false)
            {
                var weapon = _activeWeapon;
                _activeWeapon = null;
                _throwRequested = false;
                _isThrowing = false;
                weapon.OnCastFinished(this, CastResult.Cancelled);
            }
        }

        private void StartThrow()
        {
            _throwRequested = false;
            _isThrowing = true;

            _begin.Stop();
            _throw.Play();
        }

        private void Finish(CastResult result)
        {
            if (_activeWeapon == null)
                return;

            if (_isFinishing == true)
                return;

            _isFinishing = true;

            var weapon = _activeWeapon;

            _begin.Stop();
            _throw.Stop();

            _activeWeapon = null;
            _throwRequested = false;
            _isThrowing = false;

            Deactivate(_blendOutDuration);

            weapon.OnCastFinished(this, result);

            _isFinishing = false;
        }
    }
}
