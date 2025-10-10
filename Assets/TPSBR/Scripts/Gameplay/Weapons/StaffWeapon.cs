using System;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TPSBR
{
    public class StaffWeapon : Weapon
    {
        private const string HASH_PREFIX = "STF";
        private const string LogPrefix = "[<color=#FFA500>StaffWeapon</color>]";

        [SerializeField]
        private float _baseDamage;
        [SerializeField]
        private float _healthRegen;
        [SerializeField]
        private float _manaRegen;
        [SerializeField]
        private string _configuredItemName;

        [Header("Combat")]
        [SerializeField]
        [Tooltip("Duration in seconds the left mouse button must be held to fully charge the heavy attack.")]
        private float _heavyChargeDuration = 1.25f;
        private string _lastConfigurationHash = string.Empty;
        private float _attackButtonDownTime;
        private bool _pendingLightAttack;
        private bool _isChargingHeavy;

        public float BaseDamage => _baseDamage;
        public float HealthRegen => _healthRegen;
        public float ManaRegen => _manaRegen;
        public string ConfiguredItemName => _configuredItemName;

        // Weapon INTERFACE

        public override bool CanFire(bool keyDown)
        {
            // Staff attacks are orchestrated through the animation controller flow.
            return false;
        }

        public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
        {
            // Staff attacks are processed via the animation layer rather than the generic fire path.
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (Character == null || Character.Agent == null)
            {
                ResetAttackState(true);
                return;
            }

            if (Character.Agent.Inventory.CurrentWeapon != this)
            {
                ResetAttackState(true);
                return;
            }

            if (_isChargingHeavy == true && CheckHeavyCancelRequested() == true)
            {
                CancelHeavyCharge();
            }
        }

        public override bool CanAim()
        {
            return true;
        }

        public override WeaponUseRequest EvaluateUse(bool attackActivated, bool attackHeld, bool attackReleased)
        {
            bool hasCharge = _pendingLightAttack == true || _isChargingHeavy == true;

            if (attackActivated == true)
            {
                return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.Charge, false, 0f);
            }

            if (hasCharge == false)
            {
                return WeaponUseRequest.None;
            }

            if (_isChargingHeavy == false)
            {
                if (attackHeld == true)
                {
                    float elapsed = GetCurrentTime() - _attackButtonDownTime;
                    float progress = _heavyChargeDuration > 0f ? Mathf.Clamp01(elapsed / _heavyChargeDuration) : 1f;
                    return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.Charge, false, progress);
                }

                if (attackReleased == true)
                {
                    return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.LightAttack);
                }
            }
            else
            {
                if (attackReleased == true)
                {
                    return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.HeavyAttack);
                }
            }

            return WeaponUseRequest.None;
        }

        public override void OnUseStarted(in WeaponUseRequest request)
        {
            switch (request.Animation)
            {
                case WeaponUseAnimation.Charge:
                    if (_pendingLightAttack == false && _isChargingHeavy == false)
                    {
                        BeginAttackCharge();
                    }

                    if (request.HasChargeProgress == true)
                    {
                        UpdateChargeProgress(request.ChargeProgress);
                    }
                    break;

                case WeaponUseAnimation.LightAttack:
                    PerformLightAttack();
                    break;

                case WeaponUseAnimation.HeavyAttack:
                    PerformHeavyAttack();
                    break;

                case WeaponUseAnimation.Ability:
                    PerformAbilityAttack();
                    break;
            }
        }

        public override bool HandleAnimationRequest(AttackLayer attackLayer, in WeaponUseRequest request)
        {
            if (attackLayer == null)
                return false;

            StaffAttackState staffAttack = attackLayer.StaffAttack;

            if (staffAttack == null)
                return false;

            switch (request.Animation)
            {
                case WeaponUseAnimation.Charge:
                    if (_pendingLightAttack == false && _isChargingHeavy == false)
                    {
                        staffAttack.BeginCharge(this);
                    }

                    if (request.HasChargeProgress == true)
                    {
                        staffAttack.UpdateChargeProgress(this, request.ChargeProgress);
                    }
                    return true;

                case WeaponUseAnimation.LightAttack:
                    staffAttack.PlayLightAttack(this);
                    return true;

                case WeaponUseAnimation.HeavyAttack:
                    staffAttack.PlayHeavyAttack(this);
                    return true;

                case WeaponUseAnimation.Ability:
                    staffAttack.PlayAbilityAttack(this);
                    return true;
            }

            return true;
        }

        public override string GenerateRandomStats()
        {
            int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            byte[] payload = BitConverter.GetBytes(seed);
            string encodedSeed = Convert.ToBase64String(payload);
            return $"{HASH_PREFIX}:{encodedSeed}";
        }

        protected override void OnConfigurationHashApplied(string configurationHash)
        {
            base.OnConfigurationHashApplied(configurationHash);

            if (TryGetStatsFromConfiguration(configurationHash, out float baseDamage, out float healthRegen, out float manaRegen, out string configuredItemName) == false)
            {
                ResetConfiguration();
                return;
            }

            _lastConfigurationHash = configurationHash;
            ApplyConfiguration(baseDamage, healthRegen, manaRegen, configuredItemName);
        }

        private bool TryParseSeed(string configurationHash, out int seed)
        {
            seed = default;

            if (string.IsNullOrWhiteSpace(configurationHash) == true)
            {
                return false;
            }

            if (configurationHash.StartsWith(HASH_PREFIX + ":", StringComparison.Ordinal) == false)
            {
                return false;
            }

            string encodedSeed = configurationHash.Substring(HASH_PREFIX.Length + 1);

            try
            {
                byte[] payload = Convert.FromBase64String(encodedSeed);
                if (payload.Length != sizeof(int))
                {
                    return false;
                }

                seed = BitConverter.ToInt32(payload, 0);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private void ApplyConfiguration(float baseDamage, float healthRegen, float manaRegen, string configuredItemName)
        {
            _baseDamage = baseDamage;
            _healthRegen = healthRegen;
            _manaRegen = manaRegen;
            _configuredItemName = configuredItemName;

            SetWeaponSize(WeaponSize.Staff);
            SetDisplayName(_configuredItemName);
            SetNameShortcut(CreateShortcut(_configuredItemName));
        }

        private bool TryGetStatsFromConfiguration(string configurationHash, out float baseDamage, out float healthRegen, out float manaRegen, out string configuredItemName)
        {
            baseDamage = default;
            healthRegen = default;
            manaRegen = default;
            configuredItemName = string.Empty;

            if (TryParseSeed(configurationHash, out int seed) == false)
            {
                return false;
            }

            PopulateStats(new System.Random(seed), out baseDamage, out healthRegen, out manaRegen, out configuredItemName);

            return true;
        }

        private void PopulateStats(System.Random random, out float baseDamage, out float healthRegen, out float manaRegen, out string configuredItemName)
        {
            baseDamage = GenerateStat(random, 18f, 32f, 0.5f);
            healthRegen = GenerateStat(random, 1f, 6f, 0.1f);
            manaRegen = GenerateStat(random, 2f, 9f, 0.1f);
            configuredItemName = GenerateName(random);
        }

        private void ResetConfiguration()
        {
            _baseDamage = 0f;
            _healthRegen = 0f;
            _manaRegen = 0f;
            _configuredItemName = string.Empty;
            _lastConfigurationHash = string.Empty;

            SetWeaponSize(WeaponSize.Unarmed);
            SetDisplayName(string.Empty);
            SetNameShortcut(string.Empty);
        }

        private float GenerateStat(System.Random random, float min, float max, float step)
        {
            double range = max - min;
            double value = min + random.NextDouble() * range;
            if (step > 0f)
            {
                value = Math.Round(value / step) * step;
            }

            return (float)value;
        }

        private string GenerateName(System.Random random)
        {
            string[] prefixes =
            {
                "Arcane",
                "Eldritch",
                "Mystic",
                "Solar",
                "Umbral",
                "Verdant"
            };

            string[] suffixes =
            {
                "Ember",
                "Whisper",
                "Pulse",
                "Shard",
                "Warden",
                "Wisdom"
            };

            string prefix = prefixes[random.Next(prefixes.Length)];
            string suffix = suffixes[random.Next(suffixes.Length)];

            return $"{prefix} {suffix}";
        }

        private string CreateShortcut(string name)
        {
            if (string.IsNullOrWhiteSpace(name) == true)
            {
                return string.Empty;
            }

            string condensed = name.Replace(" ", string.Empty);
            if (condensed.Length <= 4)
            {
                return condensed.ToUpperInvariant();
            }

            return condensed.Substring(0, 4).ToUpperInvariant();
        }

        private void BeginAttackCharge()
        {
            if (_pendingLightAttack == true || _isChargingHeavy == true)
            {
                return;
            }

            _pendingLightAttack = true;
            _attackButtonDownTime = GetCurrentTime();
            Debug.Log($"{LogPrefix} Left click pressed. Tracking for light attack and potential heavy charge.");
        }

        private void UpdateChargeProgress(float normalizedProgress)
        {
            if (_pendingLightAttack == false && _isChargingHeavy == false)
            {
                return;
            }

            if (_isChargingHeavy == false && normalizedProgress >= 1f)
            {
                StartHeavyCharge();
            }
        }

        private void StartHeavyCharge()
        {
            _isChargingHeavy = true;
            _pendingLightAttack = false;
            Debug.Log($"{LogPrefix} Heavy charge started after holding left click for {_heavyChargeDuration:0.00}s.");

            GetAttackLayer()?.StaffAttack.MarkChargeComplete(this);
        }

        private void PerformLightAttack()
        {
            Character.Agent.Health?.ResetRegenDelay();

            Debug.Log($"{LogPrefix} Executing light staff attack.");
            ResetAttackState(false);
        }

        private void PerformHeavyAttack()
        {
            Character.Agent.Health?.ResetRegenDelay();

            Debug.Log($"{LogPrefix} Heavy attack triggered on left click release.");
            ResetAttackState(false);
        }

        private void PerformAbilityAttack()
        {
            Debug.Log($"{LogPrefix} Ability attack triggered.");
            ResetAttackState(false);
        }

        private void CancelHeavyCharge()
        {
            Debug.Log($"{LogPrefix} Heavy charge cancelled by secondary input.");
            GetAttackLayer()?.StaffAttack.CancelCharge(this);
            ResetAttackState(true);
        }

        private void ResetAttackState(bool notifyAnimation)
        {
            _pendingLightAttack = false;
            _isChargingHeavy = false;
            _attackButtonDownTime = 0f;

            if (notifyAnimation == true)
            {
                GetAttackLayer()?.StaffAttack.ResetState(this);
            }
        }

        private bool CheckHeavyCancelRequested()
        {
            if (HasInputAuthority == false)
            {
                return false;
            }

            if (Mouse.current == null)
            {
                return false;
            }

            return Mouse.current.rightButton.wasPressedThisFrame;
        }

        private AttackLayer GetAttackLayer()
        {
            return Character != null ? Character.AnimationController?.AttackLayer : null;
        }

        private float GetCurrentTime()
        {
            if (Runner != null && Runner.IsRunning == true)
            {
                return (float)Runner.SimulationTime;
            }

            return Time.time;
        }

        public override string GetDescription()
        {
            if (TryGetStatsFromConfiguration(_lastConfigurationHash, out float baseDamage, out float healthRegen, out float manaRegen, out _))
            {
                return BuildDescription(baseDamage, healthRegen, manaRegen);
            }

            return BuildDescription(_baseDamage, _healthRegen, _manaRegen);
        }

        public override string GetDescription(NetworkString<_32> configurationHash)
        {
            string hash = configurationHash.ToString();

            if (TryGetStatsFromConfiguration(hash, out float baseDamage, out float healthRegen, out float manaRegen, out _))
            {
                return BuildDescription(baseDamage, healthRegen, manaRegen);
            }

            return GetDescription();
        }

        public override string GetDisplayName(NetworkString<_32> configurationHash)
        {
            string hash = configurationHash.ToString();

            if (TryGetStatsFromConfiguration(hash, out _, out _, out _, out string configuredItemName) == true && string.IsNullOrWhiteSpace(configuredItemName) == false)
            {
                return configuredItemName;
            }

            if (string.IsNullOrWhiteSpace(_configuredItemName) == false)
            {
                return _configuredItemName;
            }

            return base.GetDisplayName(configurationHash);
        }

        private string BuildDescription(float baseDamage, float healthRegen, float manaRegen)
        {
            return
                $"Damage: {baseDamage}\n" +
                $"Health Regen: {healthRegen}\n" +
                $"Mana Regen: {manaRegen}";
        }
    }
}
