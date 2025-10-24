using UnityEngine;

namespace TPSBR
{
    public class FishingPoleWeapon : Weapon
    {
        [SerializeField]
        private float _castHoldDuration = 1.0f;

        private bool _isCasting;
        private bool _pendingCast;
        private bool _trackingCastHold;
        private bool _primaryReleased;
        private bool _cancelRequested;
        private double _castHoldStartTime;

        public override bool CanFire(bool keyDown)
        {
            return false;
        }

        public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
        {
            // Fishing pole currently has no firing behaviour. Override when casting logic is implemented.
        }

        public void ApplyExtendedInput(bool cancelActivated)
        {
            if (cancelActivated == true && (_isCasting == true || _pendingCast == true))
            {
                _cancelRequested = true;
            }
        }

        public override WeaponUseRequest EvaluateUse(bool attackActivated, bool attackHeld, bool attackReleased)
        {
            if (attackActivated == true)
            {
                _trackingCastHold = true;
                _primaryReleased = false;
                _castHoldStartTime = GetCurrentTime();
            }

            bool shouldTriggerCast = false;

            if (_trackingCastHold == true && attackHeld == true)
            {
                if (_isCasting == false && _pendingCast == false)
                {
                    double currentTime = GetCurrentTime();

                    if (currentTime - _castHoldStartTime >= _castHoldDuration)
                    {
                        _pendingCast = true;
                        shouldTriggerCast = true;
                    }
                }

                if (shouldTriggerCast == true)
                {
                    _trackingCastHold = false;
                }
            }
            else if (attackHeld == false)
            {
                _trackingCastHold = false;
            }

            if (attackReleased == true)
            {
                _primaryReleased = true;
                _trackingCastHold = false;
            }

            if (shouldTriggerCast == true)
            {
                return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.FishingCast);
            }

            return WeaponUseRequest.None;
        }

        public override void OnUseStarted(in WeaponUseRequest request)
        {
            base.OnUseStarted(request);

            if (request.Animation == WeaponUseAnimation.FishingCast)
            {
                _pendingCast = false;
                _isCasting = true;
            }
        }

        public override bool HandleAnimationRequest(UseLayer attackLayer, in WeaponUseRequest request)
        {
            if (request.Animation != WeaponUseAnimation.FishingCast)
            {
                return base.HandleAnimationRequest(attackLayer, request);
            }

            if (attackLayer == null)
            {
                _pendingCast = false;
                return false;
            }

            FishingPoleUseState fishingUse = attackLayer.FishingPoleUseState;

            if (fishingUse == null)
            {
                _pendingCast = false;
                return false;
            }

            if (fishingUse.StartCast(this) == false)
            {
                _pendingCast = false;
                return false;
            }

            return true;
        }

        internal bool TryConsumePrimaryRelease()
        {
            if (_primaryReleased == false)
                return false;

            _primaryReleased = false;
            return true;
        }

        internal bool TryConsumeCancelRequest()
        {
            if (_cancelRequested == false)
                return false;

            _cancelRequested = false;
            return true;
        }

        internal void NotifyCastFinished()
        {
            _isCasting = false;
            _pendingCast = false;
            _trackingCastHold = false;
            _primaryReleased = false;
            _cancelRequested = false;
        }

        private double GetCurrentTime()
        {
            if (Runner != null)
            {
                return Runner.SimulationTime;
            }

            return Time.timeAsDouble;
        }
    }
}
