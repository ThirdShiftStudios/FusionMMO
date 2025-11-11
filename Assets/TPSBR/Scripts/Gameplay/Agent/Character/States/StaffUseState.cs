namespace TPSBR
{
        using UnityEngine;
        using Fusion.Addons.AnimationController;
        using TPSBR.Abilities;

        public sealed class StaffUseState : MixerState
        {
                // PRIVATE MEMBERS

                [Header("States")]
                [SerializeField] private ClipState _chargeState;
                [SerializeField] private AbilityClipState _lightAttackState;
                [SerializeField] private AbilityClipState _heavyAttackState;
                [SerializeField] private AbilityClipState _abilityAttackState;

                [Header("Timing")]
                [SerializeField] private float _blendInDuration = 0.1f;
                [SerializeField] private float _blendOutDuration = 0.15f;

                private StaffWeapon _activeWeapon;
                private bool _isCharging;
                private bool _lightAttackAbilityTriggered;
                private bool _heavyAttackAbilityTriggered;
                private bool _abilityAttackTriggered;

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

                public bool PlayLightAttack(StaffWeapon weapon)
                {
                        if (EnsureActiveWeapon(weapon) == false || _lightAttackState == null)
                                return false;

                        if (IsAbilityStateUnlocked(weapon, _lightAttackState, "light attack") == false)
                                return false;

                        _isCharging = false;
                        _lightAttackAbilityTriggered = false;

                        _lightAttackState.SetAnimationTime(0.0f);
                        _lightAttackState.Activate(_blendInDuration);
                        weapon?.NotifyLightAttackAnimationStarted();
                        Activate(_blendInDuration);

                        return true;
                }

                public bool PlayHeavyAttack(StaffWeapon weapon)
                {
                        if (EnsureActiveWeapon(weapon) == false || _heavyAttackState == null)
                                return false;

                        if (IsAbilityStateUnlocked(weapon, _heavyAttackState, "heavy attack") == false)
                                return false;

                        _isCharging = false;
                        _heavyAttackAbilityTriggered = false;
                        _abilityAttackTriggered = false;

                        _heavyAttackState.SetAnimationTime(0.0f);
                        _heavyAttackState.Activate(_blendInDuration);
                        Activate(_blendInDuration);

                        return true;
                }

                public bool PlayAbilityAttack(StaffWeapon weapon)
                {
                        if (EnsureActiveWeapon(weapon) == false || _abilityAttackState == null)
                                return false;

                        if (IsAbilityStateUnlocked(weapon, _abilityAttackState, "ability attack") == false)
                                return false;

                        _isCharging = false;

                        _abilityAttackState.SetAnimationTime(0.0f);
                        _abilityAttackState.Activate(_blendInDuration);
                        Activate(_blendInDuration);

                        return true;
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
                                TryTriggerAbility(_lightAttackState, ref _lightAttackAbilityTriggered);

                                if (_lightAttackState.IsFinished(0.95f) == true)
                                {
                                        _activeWeapon?.NotifyLightAttackAnimationFinished();
                                        Finish();
                                }
                        }
                        else if (activeState == _heavyAttackState)
                        {
                                TryTriggerAbility(_heavyAttackState, ref _heavyAttackAbilityTriggered);

                                if (_heavyAttackState.IsFinished(0.95f) == true)
                                {
                                        Finish();
                                }
                        }
                        else if (activeState == _abilityAttackState)
                        {
                                TryTriggerAbility(_abilityAttackState, ref _abilityAttackTriggered);

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
                        _lightAttackAbilityTriggered = false;
                        _heavyAttackAbilityTriggered = false;
                        _abilityAttackTriggered = false;

                        if (IsActive(true) == true)
                        {
                                Deactivate(_blendOutDuration);
                        }
                }

                private void TryTriggerAbility(AbilityClipState state, ref bool hasTriggered)
                {
                        if (state == null || hasTriggered == true)
                                return;

                        float triggerTime = Mathf.Clamp01(state.AbilityTriggerNormalizedTime);

                        if (state.IsFinished(triggerTime) == false)
                                return;

                        hasTriggered = true;

                        if (_activeWeapon == null)
                                return;

                        AbilityDefinition ability = state.Ability;

                        if (ability != null)
                        {
                                _activeWeapon.ExecuteAbility(ability);
                                return;
                        }

                        // Legacy fallback for configurations that have not yet been migrated to ability definitions.
                        if (state == _lightAttackState)
                        {
                                _activeWeapon.TriggerLightAttackProjectile();
                        }
                }

                private bool IsAbilityStateUnlocked(StaffWeapon weapon, AbilityClipState state, string animationName)
                {
                        if (weapon == null || state == null)
                                return false;

                        AbilityDefinition ability = state.Ability;

                        if (ability == null)
                        {
                                var configuredAbilities = weapon.ConfiguredAbilities;

                                if (configuredAbilities != null && configuredAbilities.Count > 0)
                                        return true;

                                Debug.LogWarning($"StaffUseState prevented {animationName} animation because the staff has no unlocked abilities configured.");
                                return false;
                        }

                        if (weapon.IsAbilityUnlocked(ability) == true)
                                return true;

                        Debug.LogWarning($"StaffUseState prevented {animationName} animation because ability '{ability.Name}' is not unlocked for this staff weapon.");
                        return false;
                }
        }
}
