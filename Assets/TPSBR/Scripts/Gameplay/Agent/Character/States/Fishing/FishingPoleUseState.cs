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
        private bool _awaitingLureImpact;
        private bool _isFighting;
        private bool _isCatching;

        public bool BeginCast(FishingPoleWeapon weapon)
        {
            if (weapon == null || _castState == null)
                return false;

            if (_activeWeapon != null && _activeWeapon != weapon)
                return false;

            _activeWeapon = weapon;
            _isWaiting = false;
            _awaitingLureImpact = false;
            _isFighting = false;
            _isCatching = false;

            _castState.SetActiveWeapon(weapon);
            _waiting?.SetActiveWeapon(weapon);
            _fighting?.SetActiveWeapon(weapon);
            _catch?.SetActiveWeapon(weapon);

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
            bool fightingActive = _isFighting == true && _fighting != null && _fighting.IsActive(true) == true;
            bool catchActive = _isCatching == true && _catch != null && _catch.IsActive(true) == true;

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
                    _awaitingLureImpact = true;
                }
            }
            else if (throwActive == true)
            {
                bool secondaryActivated = agentInput.WasActivated(EGameplayInputAction.Block) ||
                                          agentInput.WasActivated(EGameplayInputAction.HeavyAttack);

                if (secondaryActivated == true)
                {
                    _awaitingLureImpact = false;
                    CancelCast();
                    return;
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
            else if (fightingActive == true)
            {
                bool secondaryActivated = agentInput.WasActivated(EGameplayInputAction.Block) ||
                                          agentInput.WasActivated(EGameplayInputAction.HeavyAttack);

                if (secondaryActivated == true)
                {
                    CancelCast();
                }
            }
            else if (catchActive == true)
            {
                if (_catch != null && _catch.IsLoopActive == true)
                {
                    bool secondaryActivated = agentInput.WasActivated(EGameplayInputAction.Block) ||
                                              agentInput.WasActivated(EGameplayInputAction.HeavyAttack);

                    if (secondaryActivated == true)
                    {
                        _catch.PlayEnd(_blendInDuration);
                    }
                }

                if (_catch != null && _catch.HasCompletedSequence == true)
                {
                    CompleteCatch();
                    return;
                }
            }
            else
            {
                if (_awaitingLureImpact == true)
                    return;

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
            _awaitingLureImpact = false;
            _isFighting = false;
            _isCatching = false;

            _waiting.SetActiveWeapon(weapon);
            _waiting.Play(_blendInDuration);

            weapon.NotifyWaitingPhaseEntered();

            if (_castState != null)
            {
                _castState.Stop(_blendOutDuration);
            }

            Activate(_blendInDuration);
        }

        internal void EnterFightingPhase(FishingPoleWeapon weapon)
        {
            if (_fighting == null || weapon == null || _activeWeapon != weapon)
                return;

            if (_isFighting == true)
                return;

            _isWaiting = false;
            _isFighting = true;
            _isCatching = false;

            if (_waiting != null)
            {
                _waiting.Stop(_blendOutDuration);
                _waiting.ClearActiveWeapon(weapon);
            }

            _fighting.SetActiveWeapon(weapon);
            _fighting.Play(_blendInDuration);

            weapon.NotifyFightingPhaseEntered();

            Activate(_blendInDuration);
        }

        internal void EnterCatchPhase(FishingPoleWeapon weapon)
        {
            if (_catch == null || weapon == null || _activeWeapon != weapon)
                return;

            if (_isCatching == true)
                return;

            _isWaiting = false;
            _isFighting = false;
            _isCatching = true;

            if (_fighting != null)
            {
                _fighting.Stop(_blendOutDuration);
                _fighting.ClearActiveWeapon(weapon);
            }

            _catch.SetActiveWeapon(weapon);
            _catch.StartCatch(_blendInDuration);

            Activate(_blendInDuration);
        }

        internal bool TryInterruptWaitingForNewCast(FishingPoleWeapon weapon)
        {
            if (_waiting == null || weapon == null)
                return false;

            if (_activeWeapon == null || _activeWeapon != weapon)
                return false;

            if (_isWaiting == false)
                return false;

            CompleteCast();

            return true;
        }

        internal bool TryCancelActiveCast(FishingPoleWeapon weapon)
        {
            if (weapon == null)
                return false;

            if (_activeWeapon == null || _activeWeapon != weapon)
                return false;

            CancelCast();

            return true;
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

            if (_fighting != null)
            {
                _fighting.Stop(_blendOutDuration);
                _fighting.ClearActiveWeapon(_activeWeapon);
            }

            if (_catch != null)
            {
                _catch.Stop(_blendOutDuration);
                _catch.ClearActiveWeapon(_activeWeapon);
            }

            _isWaiting = false;
            _awaitingLureImpact = false;
            _isFighting = false;
            _isCatching = false;

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

            if (_fighting != null)
            {
                _fighting.Stop(_blendOutDuration);
                _fighting.ClearActiveWeapon(_activeWeapon);
            }

            if (_catch != null)
            {
                _catch.Stop(_blendOutDuration);
                _catch.ClearActiveWeapon(_activeWeapon);
            }

            _isWaiting = false;
            _awaitingLureImpact = false;
            _isFighting = false;
            _isCatching = false;

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

            if (_fighting != null && _activeWeapon != null)
            {
                _fighting.ClearActiveWeapon(_activeWeapon);
            }

            if (_catch != null && _activeWeapon != null)
            {
                _catch.ClearActiveWeapon(_activeWeapon);
            }

            _activeWeapon = null;
            _isWaiting = false;
            _awaitingLureImpact = false;
            _isFighting = false;
            _isCatching = false;

            if (IsActive(true) == true)
            {
                Deactivate(_blendOutDuration);
            }
        }

        private void CompleteCatch()
        {
            _isCatching = false;

            CompleteCast();
        }
    }
}
