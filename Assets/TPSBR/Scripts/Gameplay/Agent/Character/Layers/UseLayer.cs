using Fusion.Addons.AnimationController;

namespace TPSBR
{
    using UnityEngine;

    public sealed class UseLayer : AnimationLayer
    {
        [SerializeField]
        private FishingPoleCastParentState _fishingCast;

        public bool TryHandleUse(Weapon weapon, in WeaponUseRequest request, out bool handled)
        {
            handled = false;

            FishingPoleWeapon fishingWeapon = weapon as FishingPoleWeapon;
            if (fishingWeapon == null)
            {
                return true;
            }

            if (request.ShouldUse == false || request.Animation != WeaponUseAnimation.FishingCast)
            {
                handled = false;
                return true;
            }

            handled = true;

            FishingPoleCastParentState castState = GetFishingCastState();
            if (castState == null)
            {
                fishingWeapon.OnCastRequestFailed();
                return false;
            }

            return castState.TryBeginCast(fishingWeapon);
        }

        public bool HasActiveCast()
        {
            FishingPoleCastParentState castState = GetFishingCastState();
            return castState != null && castState.IsActive(true) == true;
        }

        private FishingPoleCastParentState GetFishingCastState()
        {
            if (_fishingCast == null)
            {
                FindState(out _fishingCast, true);
            }

            return _fishingCast;
        }
    }
}
