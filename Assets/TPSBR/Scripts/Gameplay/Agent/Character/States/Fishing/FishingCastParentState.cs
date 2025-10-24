using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class FishingCastParentState : MixerState
    {
        [SerializeField] private FishingCastBeginState _begin;
        [SerializeField] private FishingCastThrowState _throw;

        public bool IsBeginActive => _begin != null && _begin.IsActive(true);
        public bool IsThrowActive => _throw != null && _throw.IsActive(true);
        public FishingPoleWeapon ActiveWeapon => _activeWeapon;

        private FishingPoleWeapon _activeWeapon;

        internal void SetActiveWeapon(FishingPoleWeapon weapon)
        {
            _activeWeapon = weapon;
            _begin?.SetActiveWeapon(weapon);
            _throw?.SetActiveWeapon(weapon);
        }

        internal void ClearActiveWeapon(FishingPoleWeapon weapon)
        {
            if (_activeWeapon == weapon)
            {
                _activeWeapon = null;
            }

            _begin?.ClearActiveWeapon(weapon);
            _throw?.ClearActiveWeapon(weapon);
        }

        public void PlayBegin(float blendDuration)
        {
            if (_begin == null)
                return;

            if (_throw != null && _throw.IsActive(true) == true)
            {
                _throw.Deactivate(blendDuration, true);
            }

            _begin.SetAnimationTime(0f);
            _begin.Activate(blendDuration);
            Activate(blendDuration);
        }

        public void PlayThrow(float blendDuration)
        {
            if (_throw == null)
                return;

            if (_activeWeapon != null)
            {
                _throw.SetActiveWeapon(_activeWeapon);
            }

            if (_begin != null && _begin.IsActive(true) == true)
            {
                _begin.Deactivate(blendDuration, true);
            }

            _throw.SetAnimationTime(0f);
            _throw.Activate(blendDuration);
            Activate(blendDuration);
        }

        public void Stop(float blendDuration)
        {
            if (_begin != null && _begin.IsActive(true) == true)
            {
                _begin.Deactivate(blendDuration, true);
            }

            if (_throw != null && _throw.IsActive(true) == true)
            {
                _throw.Deactivate(blendDuration, true);
            }

            if (_activeWeapon != null)
            {
                ClearActiveWeapon(_activeWeapon);
            }

            if (IsActive(true) == true)
            {
                Deactivate(blendDuration);
            }
        }
    }
}
