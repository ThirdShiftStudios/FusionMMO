using System;
using System.Collections.Generic;
using System.Text;
using Fusion;
using TPSBR.Abilities;
using UnityEngine;
using Unity.Template.CompetitiveActionMultiplayer;

namespace TPSBR
{
    public class StaffWeapon : Weapon
    {
        private const string HASH_PREFIX = "STF";
        public const int MaxConfiguredAbilities = 4;
        public const int AbilityControlSlotCount = 3;
        private const byte EmptyControlSlotValue = byte.MaxValue;
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
        private string _lastConfigurationHash = string.Empty;
        private bool _pendingLightAttack;
        private bool _lightAttackActive;
        private bool _isBlocking;
        private bool _blockAnimationActive;
        private bool _attackHeld;
        private bool _heavyAttackActivated;
        private bool _blockHeld;
        private readonly int[] _statBonuses = new int[Stats.Count];
        private readonly List<StaffAbilityDefinition> _configuredAbilities = new List<StaffAbilityDefinition>();
        private int[] _configuredAbilityIndexes = Array.Empty<int>();
        private readonly StaffAbilityDefinition[] _controlSlotAbilities = new StaffAbilityDefinition[AbilityControlSlotCount];
        private readonly int[] _configuredAbilityControlIndexes = { -1, -1, -1 };

        public float BaseDamage => _baseDamage;
        public float HealthRegen => _healthRegen;
        public float ManaRegen => _manaRegen;
        public string ConfiguredItemName => _configuredItemName;
        public IReadOnlyList<int> StatBonuses => _statBonuses;
        public IReadOnlyList<StaffAbilityDefinition> ConfiguredAbilities => _configuredAbilities;
        public IReadOnlyList<int> ConfiguredAbilityControlIndexes => _configuredAbilityControlIndexes;

        public enum AbilityControlSlot
        {
            LeftMouse = 0,
            RightMouse = 1,
            AbilityKey = 2,
        }

        public StaffAbilityDefinition GetAbilityForControlSlot(AbilityControlSlot slot)
        {
            int index = (int)slot;

            if (index < 0 || index >= _controlSlotAbilities.Length)
            {
                return null;
            }

            return _controlSlotAbilities[index];
        }

        public bool TryGetStatBonuses(NetworkString<_32> configurationHash, out IReadOnlyList<int> statBonuses)
        {
            string hash = configurationHash.ToString();

            if (TryGetStatsFromConfiguration(hash, out _, out _, out _, out _, out int[] configuredBonuses) == true)
            {
                statBonuses = configuredBonuses;
                return true;
            }

            statBonuses = StatBonuses;
            return false;
        }

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
                ResetAttackState(true, false);
                return;
            }

            if (Character.Agent.Inventory.CurrentWeapon != this)
            {
                ResetAttackState(true, false);
            }
        }

        public override bool CanAim()
        {
            return true;
        }

        public override WeaponUseRequest EvaluateUse(bool attackActivated, bool attackHeld, bool attackReleased)
        {
            _attackHeld = attackHeld;

            if (Character == null || Character.Agent == null)
            {
                return WeaponUseRequest.None;
            }

            if (_blockHeld == false && _isBlocking == true)
            {
                CancelBlock();
            }

            if (_blockHeld == true)
            {
                if (_isBlocking == false)
                {
                    BeginBlock();
                }

                return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.Charge, false, 1f);
            }

            if (_isBlocking == true)
            {
                return WeaponUseRequest.None;
            }

            if (attackReleased == true)
            {
                _pendingLightAttack = false;
            }

            if (attackActivated == true)
            {
                _pendingLightAttack = true;
            }

            if (_heavyAttackActivated == true)
            {
                return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.HeavyAttack);
            }

            if (_pendingLightAttack == true && _lightAttackActive == false)
            {
                _pendingLightAttack = false;
                _lightAttackActive = true;
                return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.LightAttack);
            }

            return WeaponUseRequest.None;
        }

        public override void OnUseStarted(in WeaponUseRequest request)
        {
            switch (request.Animation)
            {
                case WeaponUseAnimation.Charge:
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

        public override bool HandleAnimationRequest(UseLayer attackLayer, in WeaponUseRequest request)
        {
            if (attackLayer == null)
                return false;

            StaffUseState staffAttack = attackLayer.StaffAttack;

            if (staffAttack == null)
                return false;

            switch (request.Animation)
            {
                case WeaponUseAnimation.Charge:
                    if (_isBlocking == true)
                    {
                        if (_blockAnimationActive == false)
                        {
                            _blockAnimationActive = true;
                            staffAttack.BeginCharge(this);
                        }

                        if (request.HasChargeProgress == true)
                        {
                            staffAttack.UpdateChargeProgress(this, request.ChargeProgress);
                        }
                        else
                        {
                            staffAttack.UpdateChargeProgress(this, 1f);
                        }
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

        public void ApplyExtendedInput(bool heavyAttackActivated, bool blockHeld)
        {
            if (heavyAttackActivated == true)
            {
                _heavyAttackActivated = true;
            }

            _blockHeld = blockHeld;
        }

        public override string GenerateRandomStats()
        {
            int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            byte[] payload = BitConverter.GetBytes(seed);
            string encodedSeed = Convert.ToBase64String(payload);

            var random = new System.Random(seed);

            // Advance the random state to match the deterministic roll order used when applying the configuration.
            PopulatePrimaryStats(random, out _, out _, out _, out _);

            int[] statBonuses = new int[Stats.Count];
            PopulateStatBonuses(random, statBonuses);
            string encodedStats = EncodeStatBonuses(statBonuses);

            List<int> defaultAbilityIndexes = null;
            var definition = Definition as WeaponDefinition;

            if (definition != null && TrySelectDefaultAbilityIndex(random, definition, out int selectedAbilityIndex) == true)
            {
                defaultAbilityIndexes = new List<int>(1) { selectedAbilityIndex };
            }

            string baseHash = $"{HASH_PREFIX}:{encodedSeed}:{encodedStats}";

            if (string.IsNullOrEmpty(encodedStats) == true)
            {
                baseHash = $"{HASH_PREFIX}:{encodedSeed}:";
            }

            string abilitySegment = EncodeAbilityIndexes(defaultAbilityIndexes);

            if (string.IsNullOrEmpty(abilitySegment) == false)
            {
                string candidateHash = string.IsNullOrEmpty(encodedStats)
                    ? $"{HASH_PREFIX}:{encodedSeed}::{abilitySegment}"
                    : $"{HASH_PREFIX}:{encodedSeed}:{encodedStats}:{abilitySegment}";

                if (candidateHash.Length <= 32)
                {
                    return candidateHash;
                }
            }

            return baseHash;
        }

        protected override void OnConfigurationHashApplied(string configurationHash)
        {
            base.OnConfigurationHashApplied(configurationHash);

            if (TryParseConfiguration(configurationHash, out int seed, out _, out int[] abilityIndexes, out int[] abilityControlIndexes) == false)
            {
                ResetConfiguration();
                NotifyInventoryAboutStatChange();
                return;
            }

            if (TryGetStatsFromConfiguration(configurationHash, out float baseDamage, out float healthRegen, out float manaRegen, out string configuredItemName, out int[] statBonuses, out int[] controlBindings) == false)
            {
                ResetConfiguration();
                NotifyInventoryAboutStatChange();
                return;
            }

            bool abilityIndexesMissing = abilityIndexes == null || abilityIndexes.Length == 0;

            if (abilityIndexesMissing == true && TryDeriveDefaultAbilityIndexes(seed, out int[] derivedAbilityIndexes) == true)
            {
                abilityIndexes = derivedAbilityIndexes;

                if (HasStateAuthority == true &&
                    StaffWeapon.TryApplyAbilityIndexes(configurationHash, abilityIndexes, out NetworkString<_32> updatedHash) == true &&
                    updatedHash.ToString() != configurationHash)
                {
                    SetConfigurationHash(updatedHash);
                    return;
                }
            }

            _lastConfigurationHash = configurationHash;
            ApplyConfiguration(baseDamage, healthRegen, manaRegen, statBonuses, configuredItemName, abilityIndexes, controlBindings.Length > 0 ? controlBindings : abilityControlIndexes);
            NotifyInventoryAboutStatChange();
        }

        private bool TryParseConfiguration(string configurationHash, out int seed, out int[] statBonuses, out int[] abilityIndexes, out int[] abilityControlIndexes)
        {
            seed = default;
            statBonuses = null;
            abilityIndexes = null;
            abilityControlIndexes = Array.Empty<int>();

            if (string.IsNullOrWhiteSpace(configurationHash) == true)
            {
                return false;
            }

            string[] parts = configurationHash.Split(':');

            if (parts.Length < 2)
            {
                return false;
            }

            if (string.Equals(parts[0], HASH_PREFIX, StringComparison.Ordinal) == false)
            {
                return false;
            }

            if (TryDecodeSeed(parts[1], out seed) == false)
            {
                return false;
            }

            if (parts.Length > 2 && string.IsNullOrWhiteSpace(parts[2]) == false)
            {
                if (TryDecodeStatBonuses(parts[2], out int[] decodedBonuses) == true)
                {
                    statBonuses = decodedBonuses;
                }
            }

            if (parts.Length > 3 && string.IsNullOrWhiteSpace(parts[3]) == false)
            {
                if (TryDecodeAbilityIndexes(parts[3], out int[] decodedAbilities) == true)
                {
                    abilityIndexes = decodedAbilities;
                }
            }

            if (parts.Length > 4 && string.IsNullOrWhiteSpace(parts[4]) == false)
            {
                if (TryDecodeAbilityControlIndexes(parts[4], out int[] decodedControls) == true)
                {
                    abilityControlIndexes = decodedControls;
                }
            }

            return true;
        }

        private bool TryDecodeSeed(string encodedSeed, out int seed)
        {
            seed = default;

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

        private void ApplyConfiguration(float baseDamage, float healthRegen, float manaRegen, IReadOnlyList<int> statBonuses, string configuredItemName, int[] abilityIndexes, int[] abilityControlIndexes)
        {
            _baseDamage = baseDamage;
            _healthRegen = healthRegen;
            _manaRegen = manaRegen;
            _configuredItemName = configuredItemName;

            for (int i = 0; i < _statBonuses.Length; ++i)
            {
                if (statBonuses != null && i < statBonuses.Count)
                {
                    _statBonuses[i] = statBonuses[i];
                }
                else
                {
                    _statBonuses[i] = 0;
                }
            }

            SetWeaponSize(WeaponSize.Staff);
            SetDisplayName(_configuredItemName);
            SetNameShortcut(CreateShortcut(_configuredItemName));
            ApplyConfiguredAbilities(abilityIndexes);
            ApplyAbilityControlBindings(abilityIndexes, abilityControlIndexes);
        }

        private bool TryGetStatsFromConfiguration(string configurationHash, out float baseDamage, out float healthRegen, out float manaRegen, out string configuredItemName, out int[] statBonuses, out int[] abilityControlIndexes)
        {
            baseDamage = default;
            healthRegen = default;
            manaRegen = default;
            configuredItemName = string.Empty;
            statBonuses = null;
            abilityControlIndexes = Array.Empty<int>();

            if (TryParseConfiguration(configurationHash, out int seed, out int[] explicitBonuses, out _, out abilityControlIndexes) == false)
            {
                return false;
            }

            System.Random random = new System.Random(seed);

            PopulatePrimaryStats(random, out baseDamage, out healthRegen, out manaRegen, out configuredItemName);

            if (explicitBonuses != null)
            {
                statBonuses = explicitBonuses;
            }
            else
            {
                int[] generatedBonuses = new int[Stats.Count];
                PopulateStatBonuses(random, generatedBonuses);
                statBonuses = generatedBonuses;
            }

            return true;
        }

        private void PopulatePrimaryStats(System.Random random, out float baseDamage, out float healthRegen, out float manaRegen, out string configuredItemName)
        {
            baseDamage = GenerateStat(random, 18f, 32f, 0.5f);
            healthRegen = GenerateStat(random, 1f, 6f, 0.1f);
            manaRegen = GenerateStat(random, 2f, 9f, 0.1f);
            configuredItemName = GenerateName(random);
        }

        private void PopulateStatBonuses(System.Random random, int[] statBonuses)
        {
            if (statBonuses == null)
            {
                return;
            }

            int count = Mathf.Min(statBonuses.Length, Stats.Count);
            for (int i = 0; i < count; ++i)
            {
                statBonuses[i] = random.Next(0, 6);
            }

            for (int i = count; i < statBonuses.Length; ++i)
            {
                statBonuses[i] = 0;
            }
        }

        private bool TrySelectDefaultAbilityIndex(System.Random random, WeaponDefinition definition, out int abilityIndex)
        {
            abilityIndex = -1;

            if (random == null || definition == null)
            {
                return false;
            }

            IReadOnlyList<AbilityDefinition> availableAbilities = definition.AvailableAbilities;

            if (availableAbilities == null || availableAbilities.Count == 0)
            {
                return false;
            }

            var selectableIndexes = new List<int>();

            for (int i = 0; i < availableAbilities.Count; ++i)
            {
                if (availableAbilities[i] is StaffAbilityDefinition)
                {
                    selectableIndexes.Add(i);
                }
            }

            if (selectableIndexes.Count == 0)
            {
                return false;
            }

            int selected = random.Next(selectableIndexes.Count);
            abilityIndex = selectableIndexes[selected];
            return true;
        }

        private bool TryDeriveDefaultAbilityIndexes(int seed, out int[] abilityIndexes)
        {
            abilityIndexes = null;

            var definition = Definition as WeaponDefinition;
            if (definition == null)
            {
                return false;
            }

            var random = new System.Random(seed);
            PopulatePrimaryStats(random, out _, out _, out _, out _);

            int[] statBonuses = new int[Stats.Count];
            PopulateStatBonuses(random, statBonuses);

            if (TrySelectDefaultAbilityIndex(random, definition, out int abilityIndex) == false)
            {
                return false;
            }

            abilityIndexes = new[] { abilityIndex };
            return true;
        }

        private static string EncodeStatBonuses(IReadOnlyList<int> statBonuses)
        {
            if (statBonuses == null || statBonuses.Count == 0)
            {
                return string.Empty;
            }

            byte[] payload = new byte[Stats.Count];

            int count = Mathf.Min(statBonuses.Count, Stats.Count);
            for (int i = 0; i < count; ++i)
            {
                payload[i] = (byte)Mathf.Clamp(statBonuses[i], byte.MinValue, byte.MaxValue);
            }

            return Convert.ToBase64String(payload);
        }

        private bool TryDecodeStatBonuses(string encodedStats, out int[] statBonuses)
        {
            statBonuses = null;

            try
            {
                byte[] payload = Convert.FromBase64String(encodedStats);

                int[] decoded = new int[Stats.Count];

                if (payload != null)
                {
                    int count = Mathf.Min(payload.Length, decoded.Length);
                    for (int i = 0; i < count; ++i)
                    {
                        decoded[i] = payload[i];
                    }
                }

                statBonuses = decoded;
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private void ResetConfiguration()
        {
            _baseDamage = 0f;
            _healthRegen = 0f;
            _manaRegen = 0f;
            _configuredItemName = string.Empty;
            _lastConfigurationHash = string.Empty;

            for (int i = 0; i < _statBonuses.Length; ++i)
            {
                _statBonuses[i] = 0;
            }

            SetWeaponSize(WeaponSize.Unarmed);
            SetDisplayName(string.Empty);
            SetNameShortcut(string.Empty);
            ClearConfiguredAbilities();
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

        private void BeginBlock()
        {
            if (_isBlocking == true)
            {
                return;
            }

            CancelActiveLightAttack();

            _isBlocking = true;
            _blockAnimationActive = false;

            Debug.Log($"{LogPrefix} Block initiated on right click hold.");
        }

        private void CancelBlock(bool silent = false)
        {
            if (_isBlocking == false)
            {
                return;
            }

            _isBlocking = false;
            _blockAnimationActive = false;

            if (silent == false)
            {
                Debug.Log($"{LogPrefix} Block released.");
            }
            GetAttackLayer()?.StaffAttack.CancelCharge(this);
        }

        private void CancelActiveLightAttack()
        {
            if (_lightAttackActive == false)
            {
                return;
            }

            _lightAttackActive = false;

            if (_attackHeld == true)
            {
                _pendingLightAttack = true;
            }

            GetAttackLayer()?.StaffAttack.ResetState(this);
        }

        private void PerformLightAttack()
        {
            Character.Agent.Health?.ResetRegenDelay();

            Debug.Log($"{LogPrefix} Executing light staff attack.");
        }

        public void ExecuteAbility(AbilityDefinition ability)
        {
            if (ability == null)
            {
                Debug.LogWarning($"{LogPrefix} Attempted to execute a null ability.");
                return;
            }

            if (Definition != null && Definition.HasAbility(ability) == false)
            {
                Debug.LogWarning($"{LogPrefix} Ability '{ability.Name}' is not available on weapon definition '{Definition.name}'.");
                return;
            }

            if (ability is IStaffAbilityHandler staffAbility)
            {
                staffAbility.Execute(this);
                return;
            }

            Debug.LogWarning($"{LogPrefix} Ability '{ability.Name}' is not supported by {GetType().Name}.");
        }

        public void TriggerLightAttackProjectile()
        {
            StaffAbilityDefinition ability = GetDefaultStaffAbility();

            if (ability == null)
            {
                Debug.LogWarning($"{LogPrefix} Unable to locate a staff ability to trigger the light attack projectile.");
                return;
            }

            ExecuteAbility(ability);
        }

        private void PerformHeavyAttack()
        {
            Character.Agent.Health?.ResetRegenDelay();

            Debug.Log($"{LogPrefix} Heavy attack triggered by heavy input.");
            _heavyAttackActivated = false;
            ResetAttackState(false, true);
        }

        private void PerformAbilityAttack()
        {
            Debug.Log($"{LogPrefix} Ability attack triggered.");
            ResetAttackState(false, false);
        }

        private StaffAbilityDefinition GetDefaultStaffAbility()
        {
            if (_configuredAbilities.Count > 0)
            {
                return _configuredAbilities[0];
            }

            var definition = Definition;

            if (definition == null)
            {
                return null;
            }

            IReadOnlyList<AbilityDefinition> abilities = definition.AvailableAbilities;

            if (abilities == null)
            {
                return null;
            }

            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i] is StaffAbilityDefinition staffAbility)
                {
                    return staffAbility;
                }
            }

            return null;
        }

        private void ResetAttackState(bool notifyAnimation, bool clearHeavy)
        {
            _pendingLightAttack = false;
            _lightAttackActive = false;

            if (clearHeavy == true)
            {
                _heavyAttackActivated = false;
            }

            if (_isBlocking == true)
            {
                CancelBlock(true);
            }

            _blockHeld = false;

            if (notifyAnimation == true)
            {
                GetAttackLayer()?.StaffAttack.ResetState(this);
            }
        }

        internal void NotifyLightAttackAnimationStarted()
        {
            _pendingLightAttack = false;
            _lightAttackActive = true;
        }

        internal void NotifyLightAttackAnimationFinished()
        {
            if (_lightAttackActive == false)
            {
                return;
            }

            _lightAttackActive = false;

            if (_attackHeld == true)
            {
                _pendingLightAttack = true;
            }
        }

        private UseLayer GetAttackLayer()
        {
            return Character != null ? Character.AnimationController?.AttackLayer : null;
        }

        public override string GetDescription()
        {
            if (TryGetStatsFromConfiguration(_lastConfigurationHash, out float baseDamage, out float healthRegen, out float manaRegen, out _, out _, out _))
            {
                return BuildDescription(baseDamage, healthRegen, manaRegen);
            }

            return BuildDescription(_baseDamage, _healthRegen, _manaRegen);
        }

        public override string GetDescription(NetworkString<_32> configurationHash)
        {
            string hash = configurationHash.ToString();

            if (TryGetStatsFromConfiguration(hash, out float baseDamage, out float healthRegen, out float manaRegen, out _, out _, out _))
            {
                return BuildDescription(baseDamage, healthRegen, manaRegen);
            }

            return GetDescription();
        }

        public override string GetDisplayName(NetworkString<_32> configurationHash)
        {
            string hash = configurationHash.ToString();

            if (TryGetStatsFromConfiguration(hash, out _, out _, out _, out string configuredItemName, out _, out _) == true && string.IsNullOrWhiteSpace(configuredItemName) == false)
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

        private void ApplyConfiguredAbilities(int[] abilityIndexes)
        {
            ClearConfiguredAbilities();

            if (abilityIndexes == null || abilityIndexes.Length == 0)
            {
                _configuredAbilityIndexes = Array.Empty<int>();
                return;
            }

            var definition = Definition as WeaponDefinition;
            if (definition == null)
            {
                _configuredAbilityIndexes = Array.Empty<int>();
                return;
            }

            IReadOnlyList<AbilityDefinition> availableAbilities = definition.AvailableAbilities;
            if (availableAbilities == null || availableAbilities.Count == 0)
            {
                _configuredAbilityIndexes = Array.Empty<int>();
                return;
            }

            var resolvedIndexes = new List<int>(Mathf.Min(abilityIndexes.Length, MaxConfiguredAbilities));

            for (int i = 0; i < abilityIndexes.Length && resolvedIndexes.Count < MaxConfiguredAbilities; ++i)
            {
                int index = Mathf.Clamp(abilityIndexes[i], 0, int.MaxValue);
                if (index < 0 || index >= availableAbilities.Count)
                {
                    continue;
                }

                if (resolvedIndexes.Contains(index) == true)
                {
                    continue;
                }

                if (availableAbilities[index] is StaffAbilityDefinition staffAbility)
                {
                    _configuredAbilities.Add(staffAbility);
                    resolvedIndexes.Add(index);
                }
            }

            _configuredAbilityIndexes = resolvedIndexes.Count > 0 ? resolvedIndexes.ToArray() : Array.Empty<int>();
        }

        private void ClearConfiguredAbilities()
        {
            _configuredAbilities.Clear();
            _configuredAbilityIndexes = Array.Empty<int>();
            ResetAbilityControlBindings();
        }

        private void ApplyAbilityControlBindings(int[] abilityIndexes, int[] controlIndexes)
        {
            ResetAbilityControlBindings();

            if (controlIndexes == null || controlIndexes.Length == 0)
            {
                return;
            }

            var definition = Definition as WeaponDefinition;
            if (definition == null)
            {
                return;
            }

            IReadOnlyList<AbilityDefinition> availableAbilities = definition.AvailableAbilities;
            if (availableAbilities == null || availableAbilities.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _configuredAbilityControlIndexes.Length; ++i)
            {
                int configuredIndex = i < controlIndexes.Length ? controlIndexes[i] : -1;

                if (configuredIndex < 0 || configuredIndex >= availableAbilities.Count)
                {
                    _configuredAbilityControlIndexes[i] = -1;
                    continue;
                }

                if (abilityIndexes == null || Array.IndexOf(abilityIndexes, configuredIndex) < 0)
                {
                    _configuredAbilityControlIndexes[i] = -1;
                    continue;
                }

                if (availableAbilities[configuredIndex] is StaffAbilityDefinition staffAbility)
                {
                    _configuredAbilityControlIndexes[i] = configuredIndex;
                    _controlSlotAbilities[i] = staffAbility;
                }
                else
                {
                    _configuredAbilityControlIndexes[i] = -1;
                }
            }
        }

        private void ResetAbilityControlBindings()
        {
            for (int i = 0; i < _configuredAbilityControlIndexes.Length; ++i)
            {
                _configuredAbilityControlIndexes[i] = -1;
                _controlSlotAbilities[i] = null;
            }
        }

        private static bool TryDecodeAbilityIndexes(string encoded, out int[] abilityIndexes)
        {
            abilityIndexes = null;

            if (string.IsNullOrWhiteSpace(encoded) == true)
            {
                abilityIndexes = Array.Empty<int>();
                return true;
            }

            try
            {
                byte[] payload = Convert.FromBase64String(encoded);
                if (payload == null || payload.Length == 0)
                {
                    abilityIndexes = Array.Empty<int>();
                    return true;
                }

                int maxCount = Mathf.Min(payload.Length, MaxConfiguredAbilities);
                int[] decoded = new int[maxCount];

                for (int i = 0; i < maxCount; ++i)
                {
                    decoded[i] = payload[i];
                }

                abilityIndexes = decoded;
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string EncodeAbilityIndexes(IReadOnlyList<int> abilityIndexes)
        {
            if (abilityIndexes == null || abilityIndexes.Count == 0)
            {
                return string.Empty;
            }

            int count = Mathf.Min(abilityIndexes.Count, MaxConfiguredAbilities);
            byte[] payload = new byte[count];

            for (int i = 0; i < count; ++i)
            {
                payload[i] = (byte)Mathf.Clamp(abilityIndexes[i], byte.MinValue, byte.MaxValue);
            }

            return Convert.ToBase64String(payload);
        }

        private static bool TryDecodeAbilityControlIndexes(string encoded, out int[] controlIndexes)
        {
            controlIndexes = null;

            if (string.IsNullOrWhiteSpace(encoded) == true)
            {
                controlIndexes = Array.Empty<int>();
                return true;
            }

            try
            {
                byte[] payload = Convert.FromBase64String(encoded);

                if (payload == null || payload.Length == 0)
                {
                    controlIndexes = Array.Empty<int>();
                    return true;
                }

                int[] decoded = new int[AbilityControlSlotCount];

                for (int i = 0; i < decoded.Length; ++i)
                {
                    if (i < payload.Length)
                    {
                        byte value = payload[i];
                        decoded[i] = value == EmptyControlSlotValue ? -1 : value;
                    }
                    else
                    {
                        decoded[i] = -1;
                    }
                }

                controlIndexes = decoded;
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string EncodeAbilityControlIndexes(IReadOnlyList<int> controlIndexes)
        {
            if (controlIndexes == null || controlIndexes.Count == 0)
            {
                return string.Empty;
            }

            byte[] payload = new byte[AbilityControlSlotCount];
            bool hasBinding = false;

            for (int i = 0; i < payload.Length; ++i)
            {
                int value = i < controlIndexes.Count ? controlIndexes[i] : -1;

                if (value >= 0)
                {
                    payload[i] = (byte)Mathf.Clamp(value, 0, EmptyControlSlotValue - 1);
                    hasBinding = true;
                }
                else
                {
                    payload[i] = EmptyControlSlotValue;
                }
            }

            if (hasBinding == false)
            {
                return string.Empty;
            }

            return Convert.ToBase64String(payload);
        }

        public static bool TryGetAbilityIndexes(string configurationHash, out int[] abilityIndexes)
        {
            abilityIndexes = Array.Empty<int>();

            if (string.IsNullOrWhiteSpace(configurationHash) == true)
            {
                return false;
            }

            if (configurationHash.StartsWith(HASH_PREFIX + ":", StringComparison.Ordinal) == false)
            {
                return false;
            }

            string[] parts = configurationHash.Split(':');
            if (parts.Length <= 3)
            {
                abilityIndexes = Array.Empty<int>();
                return true;
            }

            return TryDecodeAbilityIndexes(parts[3], out abilityIndexes);
        }

        public static bool TryGetAbilityIndexes(NetworkString<_32> configurationHash, out int[] abilityIndexes)
        {
            return TryGetAbilityIndexes(configurationHash.ToString(), out abilityIndexes);
        }

        public static bool TryGetAbilityControlIndexes(string configurationHash, out int[] abilityControlIndexes)
        {
            abilityControlIndexes = Array.Empty<int>();

            if (string.IsNullOrWhiteSpace(configurationHash) == true)
            {
                return false;
            }

            if (configurationHash.StartsWith(HASH_PREFIX + ":", StringComparison.Ordinal) == false)
            {
                return false;
            }

            string[] parts = configurationHash.Split(':');
            if (parts.Length <= 4)
            {
                abilityControlIndexes = Array.Empty<int>();
                return true;
            }

            return TryDecodeAbilityControlIndexes(parts[4], out abilityControlIndexes);
        }

        public static bool TryGetAbilityControlIndexes(NetworkString<_32> configurationHash, out int[] abilityControlIndexes)
        {
            return TryGetAbilityControlIndexes(configurationHash.ToString(), out abilityControlIndexes);
        }

        public static bool TryApplyAbilityIndexes(string configurationHash, IReadOnlyList<int> abilityIndexes, out string updatedHash)
        {
            updatedHash = configurationHash;

            if (string.IsNullOrWhiteSpace(configurationHash) == true)
            {
                return false;
            }

            string[] parts = configurationHash.Split(':');
            if (parts.Length < 2)
            {
                return false;
            }

            if (string.Equals(parts[0], HASH_PREFIX, StringComparison.Ordinal) == false)
            {
                return false;
            }

            string statsSegment = parts.Length >= 3 ? parts[2] : string.Empty;
            string controlSegment = parts.Length >= 5 ? parts[4] : string.Empty;

            if (string.IsNullOrEmpty(controlSegment) == false)
            {
                if (TryDecodeAbilityControlIndexes(controlSegment, out int[] decodedControls) == false)
                {
                    return false;
                }

                bool updatedControls = false;

                if (decodedControls.Length > 0)
                {
                    for (int i = 0; i < decodedControls.Length; ++i)
                    {
                        int value = decodedControls[i];
                        if (value < 0)
                        {
                            continue;
                        }

                        if (ContainsAbilityIndex(abilityIndexes, value) == false)
                        {
                            decodedControls[i] = -1;
                            updatedControls = true;
                        }
                    }
                }

                if (updatedControls == true)
                {
                    controlSegment = EncodeAbilityControlIndexes(decodedControls);
                }
            }

            string abilitySegment = EncodeAbilityIndexes(abilityIndexes);
            string composed = ComposeConfigurationHash(parts[0], parts[1], statsSegment, abilitySegment, controlSegment);

            if (composed.Length > 32)
            {
                return false;
            }

            updatedHash = composed;
            return true;
        }

        public static bool TryApplyAbilityIndexes(NetworkString<_32> configurationHash, IReadOnlyList<int> abilityIndexes, out NetworkString<_32> updatedHash)
        {
            updatedHash = configurationHash;

            if (TryApplyAbilityIndexes(configurationHash.ToString(), abilityIndexes, out string hashString) == false)
            {
                return false;
            }

            updatedHash = hashString;
            return true;
        }

        public static bool TryApplyAbilityControlIndexes(string configurationHash, IReadOnlyList<int> controlIndexes, out string updatedHash)
        {
            updatedHash = configurationHash;

            if (string.IsNullOrWhiteSpace(configurationHash) == true)
            {
                return false;
            }

            string[] parts = configurationHash.Split(':');
            if (parts.Length < 2)
            {
                return false;
            }

            string statsSegment = parts.Length >= 3 ? parts[2] : string.Empty;
            string abilitySegment = parts.Length >= 4 ? parts[3] : string.Empty;

            if (TryDecodeAbilityIndexes(abilitySegment, out int[] configuredAbilityIndexes) == false)
            {
                return false;
            }

            int[] sanitized = new int[AbilityControlSlotCount];

            for (int i = 0; i < sanitized.Length; ++i)
            {
                int value = controlIndexes != null && i < controlIndexes.Count ? controlIndexes[i] : -1;

                if (value < 0 || ContainsAbilityIndex(configuredAbilityIndexes, value) == false)
                {
                    sanitized[i] = -1;
                }
                else
                {
                    sanitized[i] = value;
                }
            }

            string controlSegment = EncodeAbilityControlIndexes(sanitized);
            string composed = ComposeConfigurationHash(parts[0], parts[1], statsSegment, abilitySegment, controlSegment);

            if (composed.Length > 32)
            {
                return false;
            }

            updatedHash = composed;
            return true;
        }

        public static bool TryApplyAbilityControlIndexes(NetworkString<_32> configurationHash, IReadOnlyList<int> controlIndexes, out NetworkString<_32> updatedHash)
        {
            updatedHash = configurationHash;

            if (TryApplyAbilityControlIndexes(configurationHash.ToString(), controlIndexes, out string hashString) == false)
            {
                return false;
            }

            updatedHash = hashString;
            return true;
        }

        private static bool ContainsAbilityIndex(IReadOnlyList<int> abilityIndexes, int value)
        {
            if (abilityIndexes == null)
            {
                return false;
            }

            for (int i = 0; i < abilityIndexes.Count; ++i)
            {
                if (abilityIndexes[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static string ComposeConfigurationHash(string prefix, string seedSegment, string statsSegment, string abilitySegment, string controlSegment)
        {
            var builder = new StringBuilder();
            builder.Append(prefix).Append(':').Append(seedSegment);

            bool hasStats = string.IsNullOrEmpty(statsSegment) == false;
            bool hasAbility = string.IsNullOrEmpty(abilitySegment) == false;
            bool hasControl = string.IsNullOrEmpty(controlSegment) == false;

            if (hasStats || hasAbility || hasControl)
            {
                builder.Append(':').Append(statsSegment);
            }

            if (hasAbility || hasControl)
            {
                builder.Append(':').Append(abilitySegment);
            }

            if (hasControl)
            {
                builder.Append(':').Append(controlSegment);
            }

            return builder.ToString();
        }

        public IReadOnlyList<int> GetConfiguredAbilityIndexes()
        {
            return _configuredAbilityIndexes;
        }

        private void NotifyInventoryAboutStatChange()
        {
            var inventory = Character != null ? Character.Agent?.Inventory : null;

            inventory?.RecalculateHotbarStats();
        }
    }
}
