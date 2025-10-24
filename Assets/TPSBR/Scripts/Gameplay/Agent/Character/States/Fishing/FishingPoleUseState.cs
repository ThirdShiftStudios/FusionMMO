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

        public bool BeginCast(FishingPoleWeapon weapon)
        {
            if (weapon == null || _castState == null)
                return false;

            if (_activeWeapon != null && _activeWeapon != weapon)
                return false;

            _activeWeapon = weapon;

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
            else
            {
                // No active cast clips, ensure the layer is reset.
                CompleteCast();
            }
        }

        private void CompleteCast()
        {
            if (_castState != null)
            {
                _castState.Stop(_blendOutDuration);
            }

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

            if (_activeWeapon != null)
            {
                _activeWeapon.NotifyCastCancelled();
            }

            Finish();
        }

        private void Finish()
        {
            _activeWeapon = null;

            if (IsActive(true) == true)
            {
                Deactivate(_blendOutDuration);
            }
        }
    }
}
