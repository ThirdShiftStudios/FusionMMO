using UnityEngine;

namespace TPSBR
{
    public class FishingPoleWeapon : Weapon
    {
        [SerializeField]
        private float _castHoldDuration = 1f;

        private float _castHoldTimer;
        private bool _pendingCastRequest;
        private bool _pendingThrow;
        private bool _isCasting;
        private FishingPoleCastParentState _activeCastState;

        private float CastDeltaTime => Runner != null ? Runner.DeltaTime : Time.deltaTime;

        public override bool CanFire(bool keyDown)
        {
            return false;
        }

        public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
        {
            // Fishing pole currently has no projectile behaviour. Casting is handled via animation states.
        }

        public override WeaponUseRequest EvaluateUse(bool attackActivated, bool attackHeld, bool attackReleased)
        {
            if (Character == null || Character.Agent == null)
            {
                CancelActiveCast();
                ResetCastingState();
                return WeaponUseRequest.None;
            }

            AgentInput agentInput = Character.Agent.AgentInput;
            if (agentInput == null)
            {
                CancelActiveCast();
                ResetCastingState();
                return WeaponUseRequest.None;
            }

            if (_isCasting == true)
            {
                if (_activeCastState == null)
                {
                    ResetCastingState();
                    return WeaponUseRequest.None;
                }

                if (attackReleased == true || _pendingThrow == true)
                {
                    _activeCastState.RequestThrow(this);
                    _pendingThrow = false;
                }

                if (agentInput.WasActivated(EGameplayInputAction.Block) == true)
                {
                    _activeCastState.RequestCancel(this);
                }

                return WeaponUseRequest.None;
            }

            if (_pendingCastRequest == true)
            {
                if (attackReleased == true)
                {
                    _pendingThrow = true;
                }

                if (agentInput.WasActivated(EGameplayInputAction.Block) == true)
                {
                    _pendingCastRequest = false;
                    _pendingThrow = false;
                    ResetChargeTimer();
                }

                return WeaponUseRequest.None;
            }

            if (agentInput.WasActivated(EGameplayInputAction.Block) == true)
            {
                ResetChargeTimer();
                return WeaponUseRequest.None;
            }

            if (attackHeld == true)
            {
                _castHoldTimer += CastDeltaTime;

                if (_castHoldTimer >= _castHoldDuration)
                {
                    _pendingCastRequest = true;
                    return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.FishingCast);
                }
            }
            else
            {
                ResetChargeTimer();
            }

            return WeaponUseRequest.None;
        }

        public override void OnUseStarted(in WeaponUseRequest request)
        {
            if (request.Animation == WeaponUseAnimation.FishingCast)
            {
                _pendingCastRequest = false;
                _isCasting = true;
                ResetChargeTimer();

                if (_pendingThrow == true && _activeCastState != null)
                {
                    _activeCastState.RequestThrow(this);
                    _pendingThrow = false;
                }
            }
        }

        protected override void OnWeaponDisarmed()
        {
            base.OnWeaponDisarmed();

            CancelActiveCast();
            ResetCastingState();
        }

        internal void OnCastRequestFailed()
        {
            ResetCastingState();
        }

        internal void OnCastStateEntered(FishingPoleCastParentState state)
        {
            _activeCastState = state;
            _isCasting = true;

            if (_pendingThrow == true)
            {
                _activeCastState.RequestThrow(this);
                _pendingThrow = false;
            }
        }

        internal void OnCastFinished(FishingPoleCastParentState state, FishingPoleCastParentState.CastResult result)
        {
            if (_activeCastState == state)
            {
                _activeCastState = null;
            }

            ResetCastingState();
        }

        private void ResetCastingState()
        {
            _castHoldTimer = 0f;
            _pendingCastRequest = false;
            _pendingThrow = false;
            _isCasting = false;
            _activeCastState = null;
        }

        private void CancelActiveCast()
        {
            if (_activeCastState != null)
            {
                _activeCastState.RequestCancel(this);
            }
        }

        private void ResetChargeTimer()
        {
            _castHoldTimer = 0f;
        }
    }
}
