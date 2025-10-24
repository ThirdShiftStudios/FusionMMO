using UnityEngine;

namespace TPSBR
{
    public class FishingPoleWeapon : Weapon
    {
        [SerializeField]
        private float _holdToCastDuration = 1f;

        private float _primaryHoldTime;
        private bool _isPrimaryHeld;
        private bool _castRequested;
        private bool _castActive;
        private bool _waitingForPrimaryRelease;

        public override bool CanFire(bool keyDown)
        {
            return false;
        }

        public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
        {
            // Fishing pole currently has no firing behaviour. Override when casting logic is implemented.
        }

        public override WeaponUseRequest EvaluateUse(bool attackActivated, bool attackHeld, bool attackReleased)
        {
            if (Character == null || Character.Agent == null)
            {
                ResetHoldTracking();
                _castRequested = false;
                _castActive = false;
                _waitingForPrimaryRelease = false;
                return WeaponUseRequest.None;
            }

            if (attackReleased == true)
            {
                _isPrimaryHeld = false;
                _primaryHoldTime = 0f;
                _waitingForPrimaryRelease = false;
            }
            else if (attackHeld == true)
            {
                if (_waitingForPrimaryRelease == true)
                {
                    return WeaponUseRequest.None;
                }

                if (_isPrimaryHeld == false)
                {
                    _isPrimaryHeld = true;
                    _primaryHoldTime = 0f;
                }

                if (_castActive == false && _castRequested == false)
                {
                    _primaryHoldTime += GetDeltaTime();

                    if (_primaryHoldTime >= _holdToCastDuration)
                    {
                        _castRequested = true;
                        return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.FishingCast);
                    }
                }
            }
            else
            {
                ResetHoldTracking();
            }

            return WeaponUseRequest.None;
        }

        public override bool HandleAnimationRequest(UseLayer attackLayer, in WeaponUseRequest request)
        {
            if (request.Animation == WeaponUseAnimation.FishingCast)
            {
                FishingPoleUseState fishingUse = attackLayer?.FishingPoleUseState;

                if (fishingUse == null)
                {
                    _castRequested = false;
                    return false;
                }

                if (fishingUse.BeginCast(this) == false)
                {
                    _castRequested = false;
                    return false;
                }

                return true;
            }

            return base.HandleAnimationRequest(attackLayer, request);
        }

        internal void NotifyCastStarted()
        {
            _castRequested = false;
            _castActive = true;
        }

        internal void NotifyCastThrown()
        {
            // The cast remains active until the animation finishes.
        }

        internal void NotifyCastCompleted()
        {
            _castActive = false;
            _castRequested = false;
            ResetHoldTracking();
        }

        internal void NotifyCastCancelled()
        {
            _castActive = false;
            _castRequested = false;
            ResetHoldTracking();
            _waitingForPrimaryRelease = true;
        }

        private void ResetHoldTracking()
        {
            _isPrimaryHeld = false;
            _primaryHoldTime = 0f;
        }

        private float GetDeltaTime()
        {
            if (Runner != null)
            {
                return Runner.DeltaTime;
            }

            return Time.deltaTime;
        }
    }
}
