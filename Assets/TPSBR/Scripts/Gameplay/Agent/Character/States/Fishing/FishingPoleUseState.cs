using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class FishingPoleUseState : MixerState
    {
        [SerializeField] private FishingCastParentState _castState;
        [SerializeField] private FishingWaitingState _waiting;
        [SerializeField] private FishingFightingState _fighting;
        [SerializeField] private FishingCatchParentState _catch;

        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.15f;

        private FishingPoleWeapon _activeWeapon;
        private bool _isWaiting;

        public bool BeginCast(FishingPoleWeapon weapon)
        {
            if (weapon == null || _castState == null)
                return false;

            if (_activeWeapon != null && _activeWeapon != weapon)
                return false;

            _activeWeapon = weapon;
            _isWaiting = false;

            _castState.SetActiveWeapon(weapon);
            _waiting?.SetActiveWeapon(weapon);

            _castState.PlayBegin(_blendInDuration);
            Activate(_blendInDuration);

            _activeWeapon.NotifyCastStarted();

            return true;
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (_activeWeapon == null || _castState == null)
                return;

            Agent agent = _activeWeapon.Character != null ? _activeWeapon.Character.Agent : null;

            if (agent == null || agent.AgentInput == null)
            {
                CancelCast();
                return;
            }

            AgentInput agentInput = agent.AgentInput;

            bool beginActive = _castState.IsBeginActive;
            bool throwActive = _castState.IsThrowActive;
            bool waitingActive = _isWaiting == true && _waiting != null && _waiting.IsActive(true) == true;

            if (beginActive == true)
            {
                bool primaryReleased = agentInput.WasDeactivated(EGameplayInputAction.Attack);
                bool secondaryActivated = agentInput.WasActivated(EGameplayInputAction.Block) ||
                                          agentInput.WasActivated(EGameplayInputAction.HeavyAttack);

                if (secondaryActivated == true)
                {
                    CancelCast();
                    return;
                }

                if (primaryReleased == true)
                {
                    _castState.PlayThrow(_blendInDuration);
                    _activeWeapon.NotifyCastThrown();
                }
            }
            else if (throwActive == true)
            {
                if (_castState.IsThrowFinished == true)
                {
                    CompleteCast();
                }
            }
            else if (waitingActive == true)
            {
                bool secondaryActivated = agentInput.WasActivated(EGameplayInputAction.Block) ||
                                          agentInput.WasActivated(EGameplayInputAction.HeavyAttack);

                if (secondaryActivated == true)
                {
                    CancelCast();
                }
            }
            else
            {
                // No active cast clips, ensure the layer is reset.
                CompleteCast();
            }
        }

        internal void EnterWaitingPhase(FishingPoleWeapon weapon)
        {
            if (_waiting == null || weapon == null || _activeWeapon != weapon)
                return;

            if (_isWaiting == true)
                return;

            _isWaiting = true;

            _waiting.SetActiveWeapon(weapon);
            _waiting.Play(_blendInDuration);

            if (_castState != null)
            {
                _castState.Stop(_blendOutDuration);
            }

            Activate(_blendInDuration);
        }

        private void CompleteCast()
        {
            if (_castState != null)
            {
                _castState.Stop(_blendOutDuration);
            }

            if (_waiting != null)
            {
                _waiting.Stop(_blendOutDuration);
                _waiting.ClearActiveWeapon(_activeWeapon);
            }

            _isWaiting = false;

            if (_activeWeapon != null)
            {
                _activeWeapon.NotifyCastCompleted();
            }

            Finish();
        }

        private void CancelCast()
        {
            if (_castState != null)
            {
                _castState.Stop(_blendOutDuration);
            }

            if (_waiting != null)
            {
                _waiting.Stop(_blendOutDuration);
                _waiting.ClearActiveWeapon(_activeWeapon);
            }

            _isWaiting = false;

            if (_activeWeapon != null)
            {
                _activeWeapon.NotifyCastCancelled();
            }

            Finish();
        }

        private void Finish()
        {
            if (_castState != null && _activeWeapon != null)
            {
                _castState.ClearActiveWeapon(_activeWeapon);
            }

            if (_waiting != null && _activeWeapon != null)
            {
                _waiting.ClearActiveWeapon(_activeWeapon);
            }

            _activeWeapon = null;
            _isWaiting = false;

            if (IsActive(true) == true)
            {
                Deactivate(_blendOutDuration);
            }
        }
    }
}
