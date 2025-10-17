namespace TPSBR
{
	using UnityEngine;
	using Fusion.Addons.AnimationController;

        public sealed class StaffAttackState : MixerState
        {
                // PRIVATE MEMBERS

                [Header("States")]
                [SerializeField] private ClipState _chargeState;
                [SerializeField] private ClipState _lightAttackState;
                [SerializeField] private ClipState _heavyAttackState;
                [SerializeField] private ClipState _abilityAttackState;

                [Header("Timing")]
                [SerializeField] private float _blendInDuration = 0.1f;
                [SerializeField] private float _blendOutDuration = 0.15f;

                private StaffWeapon _activeWeapon;
                private bool _isCharging;
                private bool _lightAttackProjectileTriggered;

                // PUBLIC METHODS

                public void BeginCharge(StaffWeapon weapon)
                {
                        if (EnsureActiveWeapon(weapon) == false || _chargeState == null)
                                return;

                        _isCharging = true;

                        _chargeState.SetAnimationTime(0.0f);
                        _chargeState.Activate(_blendInDuration);
                        Activate(_blendInDuration);
                }

                public void UpdateChargeProgress(StaffWeapon weapon, float normalizedProgress)
                {
                        if (IsValidWeapon(weapon) == false || _chargeState == null)
                                return;

                        _chargeState.SetAnimationTime(Mathf.Clamp01(normalizedProgress));
                }

                public void MarkChargeComplete(StaffWeapon weapon)
                {
                        if (IsValidWeapon(weapon) == false || _chargeState == null)
                                return;

                        _isCharging = false;
                        _chargeState.SetAnimationTime(1.0f);
                }

                public void PlayLightAttack(StaffWeapon weapon)
                {
                        if (EnsureActiveWeapon(weapon) == false || _lightAttackState == null)
                                return;

                        _isCharging = false;
                        _lightAttackProjectileTriggered = false;

                        _lightAttackState.SetAnimationTime(0.0f);
                        _lightAttackState.Activate(_blendInDuration);
                        Activate(_blendInDuration);
                }

                public void PlayHeavyAttack(StaffWeapon weapon)
                {
                        if (EnsureActiveWeapon(weapon) == false || _heavyAttackState == null)
                                return;

                        _isCharging = false;

                        _heavyAttackState.SetAnimationTime(0.0f);
                        _heavyAttackState.Activate(_blendInDuration);
                        Activate(_blendInDuration);
                }

                public void PlayAbilityAttack(StaffWeapon weapon)
                {
                        if (EnsureActiveWeapon(weapon) == false || _abilityAttackState == null)
                                return;

                        _isCharging = false;

                        _abilityAttackState.SetAnimationTime(0.0f);
                        _abilityAttackState.Activate(_blendInDuration);
                        Activate(_blendInDuration);
                }

                public void CancelCharge(StaffWeapon weapon)
                {
                        if (IsValidWeapon(weapon) == false)
                                return;

                        _isCharging = false;

                        if (_chargeState != null && _chargeState.IsActive(true) == true)
                        {
                                _chargeState.Deactivate(_blendOutDuration, true);
                        }

                        Finish();
                }

                public void ResetState(StaffWeapon weapon)
                {
                        if (IsValidWeapon(weapon) == false)
                                return;

                        _isCharging = false;
                        Finish();
                }

                // MixerState INTERFACE

                protected override void OnFixedUpdate()
                {
                        base.OnFixedUpdate();

                        Fusion.Addons.AnimationController.AnimationState activeState = GetActiveState();

                        if (activeState == null)
                                return;

                        if (activeState == _lightAttackState)
                        {
                                if (_lightAttackProjectileTriggered == false && _lightAttackState.IsFinished(0.5f) == true)
                                {
                                        _lightAttackProjectileTriggered = true;

                                        _activeWeapon?.TriggerLightAttackProjectile();
                                }

                                if (_lightAttackState.IsFinished(0.95f) == true)
                                {
                                        Finish();
                                }
                        }
                        else if (activeState == _heavyAttackState)
                        {
                                if (_heavyAttackState.IsFinished(0.95f) == true)
                                {
                                        Finish();
                                }
                        }
                        else if (activeState == _abilityAttackState)
                        {
                                if (_abilityAttackState.IsFinished(0.95f) == true)
                                {
                                        Finish();
                                }
                        }
                }

                // PRIVATE METHODS

                private bool EnsureActiveWeapon(StaffWeapon weapon)
                {
                        if (weapon == null)
                                return false;

                        if (_activeWeapon != null && _activeWeapon != weapon)
                                return false;

                        _activeWeapon = weapon;

                        if (IsActive(true) == false)
                        {
                                Activate(_blendInDuration);
                        }

                        return true;
                }

                private bool IsValidWeapon(StaffWeapon weapon)
                {
                        return weapon != null && _activeWeapon == weapon;
                }

                private void Finish()
                {
                        _activeWeapon = null;
                        _lightAttackProjectileTriggered = false;

                        if (IsActive(true) == true)
                        {
                                Deactivate(_blendOutDuration);
                        }
                }
        }
}
