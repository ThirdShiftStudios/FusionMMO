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
                [SerializeField] private AbilityClipState[] _abilityStates;

                [Header("Timing")]
                [SerializeField] private float _blendInDuration = 0.1f;
                [SerializeField] private float _blendOutDuration = 0.15f;

                private StaffWeapon _activeWeapon;
                private bool _isCharging;
                private AbilityClipState _activeAbilityState;
                private WeaponUseAnimation _activeAbilityAnimation = WeaponUseAnimation.None;
                private bool _abilityTriggered;

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
                        if (EnsureActiveWeapon(weapon) == false)
                                return false;

                        AbilityDefinition assignedAbility = weapon != null ? weapon.GetAssignedAbility(StaffWeapon.AbilityControlSlot.Primary) : null;
                        AbilityClipState state = ResolveAbilityState(assignedAbility, WeaponUseAnimation.LightAttack);

                        if (state == null)
                        {
                                string abilityLabel = assignedAbility != null ? $"ability '{assignedAbility.Name}'" : "the primary ability input";
                                Debug.LogWarning($"StaffUseState prevented light attack animation because no clip state is configured for {abilityLabel}.");
                                return false;
                        }

                        if (IsAbilityStateUnlocked(weapon, state, assignedAbility, "light attack") == false)
                                return false;

                        if (PlayAbilityState(weapon, state, WeaponUseAnimation.LightAttack, true) == false)
                                return false;

                        return true;
                }

                public bool PlayHeavyAttack(StaffWeapon weapon)
                {
                        if (EnsureActiveWeapon(weapon) == false)
                                return false;

                        AbilityDefinition assignedAbility = weapon != null ? weapon.GetAssignedAbility(StaffWeapon.AbilityControlSlot.Secondary) : null;
                        AbilityClipState state = ResolveAbilityState(assignedAbility, WeaponUseAnimation.HeavyAttack);

                        if (state == null)
                        {
                                string abilityLabel = assignedAbility != null ? $"ability '{assignedAbility.Name}'" : "the secondary ability input";
                                Debug.LogWarning($"StaffUseState prevented heavy attack animation because no clip state is configured for {abilityLabel}.");
                                return false;
                        }

                        if (IsAbilityStateUnlocked(weapon, state, assignedAbility, "heavy attack") == false)
                                return false;

                        if (PlayAbilityState(weapon, state, WeaponUseAnimation.HeavyAttack, false) == false)
                                return false;

                        return true;
                }

                public bool PlayAbilityAttack(StaffWeapon weapon)
                {
                        if (EnsureActiveWeapon(weapon) == false)
                                return false;

                        AbilityDefinition assignedAbility = weapon != null ? weapon.GetAssignedAbility(StaffWeapon.AbilityControlSlot.Ability) : null;
                        AbilityClipState state = ResolveAbilityState(assignedAbility, WeaponUseAnimation.Ability);

                        if (state == null)
                        {
                                string abilityLabel = assignedAbility != null ? $"ability '{assignedAbility.Name}'" : "the ability input";
                                Debug.LogWarning($"StaffUseState prevented ability animation because no clip state is configured for {abilityLabel}.");
                                return false;
                        }

                        if (IsAbilityStateUnlocked(weapon, state, assignedAbility, "ability attack") == false)
                                return false;

                        if (PlayAbilityState(weapon, state, WeaponUseAnimation.Ability, false) == false)
                                return false;

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

                        AbilityClipState abilityState = activeState as AbilityClipState;

                        if (abilityState == null)
                                return;

                        if (_activeAbilityState != abilityState)
                        {
                                _activeAbilityState = abilityState;
                                _activeAbilityAnimation = abilityState.AnimationType;
                                _abilityTriggered = false;
                        }

                        TryTriggerActiveAbility();

                        if (abilityState.IsFinished(0.95f) == true)
                        {
                                if (_activeAbilityAnimation == WeaponUseAnimation.LightAttack)
                                {
                                        _activeWeapon?.NotifyLightAttackAnimationFinished();
                                }

                                Finish();
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
                        _activeAbilityState = null;
                        _activeAbilityAnimation = WeaponUseAnimation.None;
                        _abilityTriggered = false;

                        if (IsActive(true) == true)
                        {
                                Deactivate(_blendOutDuration);
                        }
                }

                private bool PlayAbilityState(StaffWeapon weapon, AbilityClipState state, WeaponUseAnimation animationType, bool notifyLightAttack)
                {
                        if (state == null)
                                return false;

                        _isCharging = false;
                        _activeAbilityState = state;
                        _activeAbilityAnimation = animationType;
                        _abilityTriggered = false;

                        state.SetAnimationTime(0.0f);
                        state.Activate(_blendInDuration);

                        if (notifyLightAttack == true)
                        {
                                weapon?.NotifyLightAttackAnimationStarted();
                        }

                        Activate(_blendInDuration);

                        return true;
                }

                private AbilityClipState ResolveAbilityState(AbilityDefinition assignedAbility, WeaponUseAnimation animationType)
                {
                        if (_abilityStates == null || _abilityStates.Length == 0)
                                return null;

                        AbilityClipState fallback = null;

                        for (int i = 0; i < _abilityStates.Length; ++i)
                        {
                                AbilityClipState candidate = _abilityStates[i];

                                if (candidate == null)
                                        continue;

                                if (candidate.AnimationType != animationType)
                                        continue;

                                if (assignedAbility != null && candidate.Ability == assignedAbility)
                                        return candidate;

                                if (fallback == null && candidate.Ability == null)
                                        fallback = candidate;
                        }

                        return fallback;
                }

                private void TryTriggerActiveAbility()
                {
                        if (_activeAbilityState == null || _abilityTriggered == true)
                                return;

                        float triggerTime = Mathf.Clamp01(_activeAbilityState.AbilityTriggerNormalizedTime);

                        if (_activeAbilityState.IsFinished(triggerTime) == false)
                                return;

                        _abilityTriggered = true;

                        if (_activeWeapon == null)
                                return;

                        AbilityDefinition ability = _activeAbilityState.Ability;

                        if (ability != null)
                        {
                                _activeWeapon.ExecuteAbility(ability);
                                return;
                        }

                        if (_activeAbilityAnimation == WeaponUseAnimation.LightAttack)
                        {
                                _activeWeapon.TriggerLightAttackProjectile();
                        }
                }

                private bool IsAbilityStateUnlocked(StaffWeapon weapon, AbilityClipState state, AbilityDefinition assignedAbility, string animationName)
                {
                        if (weapon == null || state == null)
                                return false;

                        AbilityDefinition ability = state.Ability ?? assignedAbility;

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

                public WeaponUseAnimation GetAnimationForAbility(AbilityDefinition ability)
                {
                        if (ability == null || _abilityStates == null)
                                return WeaponUseAnimation.None;

                        for (int i = 0; i < _abilityStates.Length; ++i)
                        {
                                AbilityClipState state = _abilityStates[i];

                                if (state == null)
                                        continue;

                                if (state.Ability == ability)
                                        return state.AnimationType;
                        }

                        return WeaponUseAnimation.None;
                }
        }
}
