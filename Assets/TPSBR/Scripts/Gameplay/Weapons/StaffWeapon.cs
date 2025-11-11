using System;
using System.Collections.Generic;
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
        private const string LogPrefix = "[<color=#FFA500>StaffWeapon</color>]";
        private const int AbilityControlSlotCount = 3;

        public enum AbilityControlSlot
        {
            Primary = 0,
            Secondary = 1,
            Ability = 2,
        }

        public readonly struct AbilityConfiguration
        {
            public AbilityConfiguration(int[] unlockedIndexes, int[] slotAssignments)
            {
                UnlockedIndexes = unlockedIndexes != null && unlockedIndexes.Length > 0
                    ? (int[])unlockedIndexes.Clone()
                    : Array.Empty<int>();

                if (slotAssignments != null && slotAssignments.Length > 0)
                {
                    var copy = new int[AbilityControlSlotCount];
                    for (int i = 0; i < copy.Length; ++i)
                    {
                        copy[i] = i < slotAssignments.Length ? slotAssignments[i] : -1;
                    }

                    SlotAssignments = copy;
                }
                else
                {
                    SlotAssignments = CreateDefaultSlotAssignments();
                }
            }

            public IReadOnlyList<int> UnlockedIndexes { get; }
            public IReadOnlyList<int> SlotAssignments { get; }
        }

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
        private readonly int[] _assignedAbilityIndexes = CreateDefaultSlotAssignments();
        private readonly StaffAbilityDefinition[] _assignedAbilities = new StaffAbilityDefinition[AbilityControlSlotCount];

        public float BaseDamage => _baseDamage;
        public float HealthRegen => _healthRegen;
        public float ManaRegen => _manaRegen;
        public string ConfiguredItemName => _configuredItemName;
        public IReadOnlyList<int> StatBonuses => _statBonuses;
        public IReadOnlyList<StaffAbilityDefinition> ConfiguredAbilities => _configuredAbilities;
        public IReadOnlyList<int> AssignedAbilityIndexes => _assignedAbilityIndexes;

        public static int GetAbilityControlSlotCount() => AbilityControlSlotCount;

        public StaffAbilityDefinition GetAssignedAbility(AbilityControlSlot slot)
        {
            int index = (int)slot;
            if (index < 0 || index >= _assignedAbilities.Length)
            {
                return null;
            }

            return _assignedAbilities[index];
        }

        public bool HasAssignedAbility(AbilityControlSlot slot)
        {
            return GetAssignedAbility(slot) != null;
        }

        public bool TryExecuteAssignedAbility(AbilityControlSlot slot)
        {
            StaffAbilityDefinition ability = GetAssignedAbility(slot);
            if (ability == null)
            {
                return false;
            }

            WeaponUseAnimation animation = ResolveAnimationForAbility(slot, ability);
            if (animation == WeaponUseAnimation.None)
            {
                return false;
            }

            var animationController = Character != null ? Character.AnimationController : null;
            if (animationController == null)
            {
                return false;
            }

            CancelActiveLightAttack();
            _pendingLightAttack = false;

            switch (slot)
            {
                case AbilityControlSlot.Primary:
                    break;

                case AbilityControlSlot.Secondary:
                case AbilityControlSlot.Ability:
                    _heavyAttackActivated = false;
                    break;
            }

            CancelBlock(true);
            _blockHeld = false;

            WeaponUseRequest request = WeaponUseRequest.CreateAnimation(animation);

            if (animationController.StartUseItem(this, request) == false)
            {
                return false;
            }

            _lightAttackActive = animation == WeaponUseAnimation.LightAttack;

            OnUseStarted(request);
            return true;
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
                if (HasAssignedAbility(AbilityControlSlot.Secondary) == true)
                {
                    return WeaponUseRequest.None;
                }

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
                _pendingLightAttack = HasAssignedAbility(AbilityControlSlot.Primary);
            }

            if (_heavyAttackActivated == true)
            {
                if (HasAssignedAbility(AbilityControlSlot.Ability) == false)
                {
                    _heavyAttackActivated = false;
                    return WeaponUseRequest.None;
                }

                return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.HeavyAttack);
            }

            if (_pendingLightAttack == true && _lightAttackActive == false)
            {
                if (HasAssignedAbility(AbilityControlSlot.Primary) == false)
                {
                    _pendingLightAttack = false;
                    return WeaponUseRequest.None;
                }

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
                    if (staffAttack.PlayLightAttack(this) == true)
                    {
                        return true;
                    }

                    _pendingLightAttack = false;
                    _lightAttackActive = false;
                    return false;

                case WeaponUseAnimation.HeavyAttack:
                    if (staffAttack.PlayHeavyAttack(this) == true)
                    {
                        return true;
                    }

                    _heavyAttackActivated = false;
                    return false;

                case WeaponUseAnimation.Ability:
                    if (staffAttack.PlayAbilityAttack(this) == true)
                    {
                        return true;
                    }

                    return false;
            }

            return true;
        }

        public void ApplyExtendedInput(bool heavyAttackActivated, bool blockHeld)
        {
            if (HasAssignedAbility(AbilityControlSlot.Ability) == true)
            {
                if (heavyAttackActivated == true)
                {
                    _heavyAttackActivated = true;
                }
            }
            else
            {
                _heavyAttackActivated = false;
            }

            if (HasAssignedAbility(AbilityControlSlot.Secondary) == true)
            {
                _blockHeld = false;
            }
            else
            {
                _blockHeld = blockHeld;
            }
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

            if (TryParseConfiguration(configurationHash, out int seed, out _, out AbilityConfiguration abilityConfiguration) == false)
            {
                ResetConfiguration();
                NotifyInventoryAboutStatChange();
                return;
            }

            if (TryGetStatsFromConfiguration(configurationHash, out float baseDamage, out float healthRegen, out float manaRegen, out string configuredItemName, out int[] statBonuses) == false)
            {
                ResetConfiguration();
                NotifyInventoryAboutStatChange();
                return;
            }

            bool abilityIndexesMissing = abilityConfiguration.UnlockedIndexes == null || abilityConfiguration.UnlockedIndexes.Count == 0;

            if (abilityIndexesMissing == true && TryDeriveDefaultAbilityIndexes(seed, out int[] derivedAbilityIndexes) == true)
            {
                abilityConfiguration = new AbilityConfiguration(derivedAbilityIndexes, null);

                if (HasStateAuthority == true &&
                    StaffWeapon.TryApplyAbilityIndexes(configurationHash, derivedAbilityIndexes, out NetworkString<_32> updatedHash) == true &&
                    updatedHash.ToString() != configurationHash)
                {
                    SetConfigurationHash(updatedHash);
                    return;
                }
            }

            _lastConfigurationHash = configurationHash;
            ApplyConfiguration(baseDamage, healthRegen, manaRegen, statBonuses, configuredItemName, abilityConfiguration);
            NotifyInventoryAboutStatChange();
        }

        private bool TryParseConfiguration(string configurationHash, out int seed, out int[] statBonuses, out AbilityConfiguration abilityConfiguration)
        {
            seed = default;
            statBonuses = null;
            abilityConfiguration = new AbilityConfiguration(Array.Empty<int>(), null);

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

            if (parts.Length > 3)
            {
                if (TryDecodeAbilityConfiguration(parts[3], out int[] decodedAbilities, out int[] slotAssignments) == true)
                {
                    abilityConfiguration = new AbilityConfiguration(decodedAbilities, slotAssignments);
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

        private void ApplyConfiguration(float baseDamage, float healthRegen, float manaRegen, IReadOnlyList<int> statBonuses, string configuredItemName, AbilityConfiguration abilityConfiguration)
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
            ApplyConfiguredAbilities(abilityConfiguration);
        }

        private bool TryGetStatsFromConfiguration(string configurationHash, out float baseDamage, out float healthRegen, out float manaRegen, out string configuredItemName, out int[] statBonuses)
        {
            baseDamage = default;
            healthRegen = default;
            manaRegen = default;
            configuredItemName = string.Empty;
            statBonuses = null;

            if (TryParseConfiguration(configurationHash, out int seed, out int[] explicitBonuses, out _) == false)
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

        public bool IsAbilityUnlocked(AbilityDefinition ability)
        {
            if (ability == null)
            {
                return false;
            }

            if (_configuredAbilityIndexes == null || _configuredAbilityIndexes.Length == 0)
            {
                return false;
            }

            if (Definition == null)
            {
                return false;
            }

            IReadOnlyList<AbilityDefinition> availableAbilities = Definition.AvailableAbilities;
            if (availableAbilities == null || availableAbilities.Count == 0)
            {
                return false;
            }

            int abilityIndex = -1;
            for (int i = 0; i < availableAbilities.Count; ++i)
            {
                if (availableAbilities[i] == ability)
                {
                    abilityIndex = i;
                    break;
                }
            }

            if (abilityIndex < 0)
            {
                return false;
            }

            for (int i = 0; i < _configuredAbilityIndexes.Length; ++i)
            {
                if (_configuredAbilityIndexes[i] == abilityIndex)
                {
                    return true;
                }
            }

            return false;
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

            if (IsAbilityUnlocked(ability) == false)
            {
                Debug.LogWarning($"{LogPrefix} Ability '{ability.Name}' has not been unlocked for this weapon configuration.");
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

            return null;
        }

        private WeaponUseAnimation ResolveAnimationForAbility(AbilityControlSlot slot, StaffAbilityDefinition ability)
        {
            switch (slot)
            {
                case AbilityControlSlot.Primary:
                    return WeaponUseAnimation.LightAttack;

                case AbilityControlSlot.Secondary:
                    return WeaponUseAnimation.HeavyAttack;

                case AbilityControlSlot.Ability:
                    return WeaponUseAnimation.Ability;

                default:
                    return WeaponUseAnimation.None;
            }
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
            if (TryGetStatsFromConfiguration(_lastConfigurationHash, out float baseDamage, out float healthRegen, out float manaRegen, out _, out _))
            {
                return BuildDescription(baseDamage, healthRegen, manaRegen);
            }

            return BuildDescription(_baseDamage, _healthRegen, _manaRegen);
        }

        public override string GetDescription(NetworkString<_32> configurationHash)
        {
            string hash = configurationHash.ToString();

            if (TryGetStatsFromConfiguration(hash, out float baseDamage, out float healthRegen, out float manaRegen, out _, out _))
            {
                return BuildDescription(baseDamage, healthRegen, manaRegen);
            }

            return GetDescription();
        }

        public override string GetDisplayName(NetworkString<_32> configurationHash)
        {
            string hash = configurationHash.ToString();

            if (TryGetStatsFromConfiguration(hash, out _, out _, out _, out string configuredItemName, out _) == true && string.IsNullOrWhiteSpace(configuredItemName) == false)
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
            ApplyConfiguredAbilities(new AbilityConfiguration(abilityIndexes, null));
        }

        private void ApplyConfiguredAbilities(AbilityConfiguration abilityConfiguration)
        {
            ClearConfiguredAbilities();

            if (abilityConfiguration.UnlockedIndexes == null || abilityConfiguration.UnlockedIndexes.Count == 0)
            {
                _configuredAbilityIndexes = Array.Empty<int>();
                ClearAssignedAbilities();
                return;
            }

            var definition = Definition as WeaponDefinition;
            if (definition == null)
            {
                _configuredAbilityIndexes = Array.Empty<int>();
                ClearAssignedAbilities();
                return;
            }

            IReadOnlyList<AbilityDefinition> availableAbilities = definition.AvailableAbilities;
            if (availableAbilities == null || availableAbilities.Count == 0)
            {
                _configuredAbilityIndexes = Array.Empty<int>();
                ClearAssignedAbilities();
                return;
            }

            var resolvedIndexes = new List<int>(Mathf.Min(abilityConfiguration.UnlockedIndexes.Count, MaxConfiguredAbilities));

            for (int i = 0; i < abilityConfiguration.UnlockedIndexes.Count && resolvedIndexes.Count < MaxConfiguredAbilities; ++i)
            {
                int index = Mathf.Clamp(abilityConfiguration.UnlockedIndexes[i], 0, int.MaxValue);
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

            int[] resolvedArray = resolvedIndexes.Count > 0 ? resolvedIndexes.ToArray() : Array.Empty<int>();
            _configuredAbilityIndexes = resolvedArray;

            ApplyAbilityAssignments(abilityConfiguration.SlotAssignments, availableAbilities, resolvedArray);
        }

        private void ClearConfiguredAbilities()
        {
            _configuredAbilities.Clear();
            _configuredAbilityIndexes = Array.Empty<int>();
            ClearAssignedAbilities();
        }

        private void ApplyAbilityAssignments(IReadOnlyList<int> slotAssignments, IReadOnlyList<AbilityDefinition> availableAbilities, IReadOnlyList<int> unlockedIndexes)
        {
            ClearAssignedAbilities();

            if (slotAssignments == null)
            {
                return;
            }

            int slotCount = Mathf.Min(AbilityControlSlotCount, slotAssignments.Count);

            for (int i = 0; i < slotCount; ++i)
            {
                int assignment = slotAssignments[i];
                if (assignment < 0)
                    continue;

                if (IsIndexUnlocked(assignment, unlockedIndexes) == false)
                    continue;

                if (availableAbilities == null || assignment < 0 || assignment >= availableAbilities.Count)
                    continue;

                if (availableAbilities[assignment] is StaffAbilityDefinition staffAbility)
                {
                    _assignedAbilityIndexes[i] = assignment;
                    _assignedAbilities[i] = staffAbility;
                }
            }

        }

        private static bool IsIndexUnlocked(int value, IReadOnlyList<int> unlockedIndexes)
        {
            if (unlockedIndexes == null)
                return false;

            for (int i = 0; i < unlockedIndexes.Count; ++i)
            {
                if (unlockedIndexes[i] == value)
                    return true;
            }

            return false;
        }

        private void ClearAssignedAbilities()
        {
            for (int i = 0; i < AbilityControlSlotCount; ++i)
            {
                _assignedAbilityIndexes[i] = -1;
                _assignedAbilities[i] = null;
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

        private static bool TryDecodeAbilityConfiguration(string encoded, out int[] abilityIndexes, out int[] slotAssignments)
        {
            abilityIndexes = Array.Empty<int>();
            slotAssignments = CreateDefaultSlotAssignments();

            if (string.IsNullOrWhiteSpace(encoded) == true)
            {
                return true;
            }

            string unlockedSegment = encoded;
            string assignmentSegment = string.Empty;

            int separatorIndex = encoded.IndexOf('|');
            if (separatorIndex >= 0)
            {
                unlockedSegment = separatorIndex > 0 ? encoded.Substring(0, separatorIndex) : string.Empty;
                assignmentSegment = separatorIndex + 1 < encoded.Length ? encoded.Substring(separatorIndex + 1) : string.Empty;
            }

            if (string.IsNullOrEmpty(unlockedSegment) == false)
            {
                if (TryDecodeAbilityIndexes(unlockedSegment, out int[] decodedAbilities) == false)
                {
                    return false;
                }

                abilityIndexes = decodedAbilities;
            }

            if (string.IsNullOrEmpty(assignmentSegment) == false)
            {
                if (TryDecodeSlotAssignments(assignmentSegment, out int[] decodedAssignments) == false)
                {
                    return false;
                }

                slotAssignments = decodedAssignments;
            }

            return true;
        }

        private static bool TryDecodeSlotAssignments(string encoded, out int[] assignments)
        {
            assignments = CreateDefaultSlotAssignments();

            if (string.IsNullOrWhiteSpace(encoded) == true)
            {
                return true;
            }

            try
            {
                byte[] payload = Convert.FromBase64String(encoded);
                if (payload == null || payload.Length == 0)
                {
                    return true;
                }

                int limit = Mathf.Min(payload.Length, AbilityControlSlotCount);
                for (int i = 0; i < limit; ++i)
                {
                    assignments[i] = payload[i] == byte.MaxValue ? -1 : payload[i];
                }

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

        private static string EncodeSlotAssignments(IReadOnlyList<int> assignments)
        {
            if (assignments == null || assignments.Count == 0)
            {
                return string.Empty;
            }

            byte[] payload = new byte[AbilityControlSlotCount];
            bool hasAssignment = false;

            for (int i = 0; i < payload.Length; ++i)
            {
                int value = i < assignments.Count ? assignments[i] : -1;
                if (value < 0)
                {
                    payload[i] = byte.MaxValue;
                }
                else
                {
                    payload[i] = (byte)Mathf.Clamp(value, byte.MinValue, byte.MaxValue);
                    hasAssignment = true;
                }
            }

            if (hasAssignment == false)
            {
                bool allEmpty = true;
                for (int i = 0; i < payload.Length; ++i)
                {
                    if (payload[i] != byte.MaxValue)
                    {
                        allEmpty = false;
                        break;
                    }
                }

                if (allEmpty == true)
                {
                    return string.Empty;
                }
            }

            return Convert.ToBase64String(payload);
        }

        private static string EncodeAbilityConfiguration(IReadOnlyList<int> abilityIndexes, IReadOnlyList<int> slotAssignments)
        {
            string unlockedSegment = EncodeAbilityIndexes(abilityIndexes);
            string assignmentSegment = EncodeSlotAssignments(slotAssignments);

            if (string.IsNullOrEmpty(assignmentSegment) == true)
            {
                return unlockedSegment;
            }

            if (string.IsNullOrEmpty(unlockedSegment) == true)
            {
                return $"|{assignmentSegment}";
            }

            return $"{unlockedSegment}|{assignmentSegment}";
        }

        public static bool TryGetAbilityIndexes(string configurationHash, out int[] abilityIndexes)
        {
            abilityIndexes = Array.Empty<int>();

            if (TryGetAbilityConfiguration(configurationHash, out AbilityConfiguration configuration) == false)
            {
                return false;
            }

            if (configuration.UnlockedIndexes != null && configuration.UnlockedIndexes.Count > 0)
            {
                int[] copy = new int[configuration.UnlockedIndexes.Count];
                for (int i = 0; i < copy.Length; ++i)
                {
                    copy[i] = configuration.UnlockedIndexes[i];
                }

                abilityIndexes = copy;
            }

            return true;
        }

        public static bool TryGetAbilityIndexes(NetworkString<_32> configurationHash, out int[] abilityIndexes)
        {
            return TryGetAbilityIndexes(configurationHash.ToString(), out abilityIndexes);
        }

        public static bool TryGetAbilityConfiguration(string configurationHash, out AbilityConfiguration configuration)
        {
            configuration = new AbilityConfiguration(Array.Empty<int>(), null);

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
                configuration = new AbilityConfiguration(Array.Empty<int>(), null);
                return true;
            }

            if (TryDecodeAbilityConfiguration(parts[3], out int[] abilityIndexes, out int[] assignments) == false)
            {
                return false;
            }

            configuration = new AbilityConfiguration(abilityIndexes, assignments);
            return true;
        }

        public static bool TryGetAbilityConfiguration(NetworkString<_32> configurationHash, out AbilityConfiguration configuration)
        {
            return TryGetAbilityConfiguration(configurationHash.ToString(), out configuration);
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
            string abilitySegment = parts.Length >= 4 ? parts[3] : string.Empty;

            if (TryDecodeAbilityConfiguration(abilitySegment, out int[] existingIndexes, out int[] slotAssignments) == false)
            {
                existingIndexes = Array.Empty<int>();
                slotAssignments = CreateDefaultSlotAssignments();
            }

            int[] sanitizedIndexes = SanitizeAbilityIndexes(abilityIndexes);
            SanitizeAssignmentsAgainstUnlocked(slotAssignments, sanitizedIndexes);

            string newAbilitySegment = EncodeAbilityConfiguration(sanitizedIndexes, slotAssignments);
            updatedHash = ComposeConfigurationHash(parts[0], parts[1], statsSegment, newAbilitySegment);
            return updatedHash.Length <= 32;
        }

        public static bool TryApplyAbilityIndexes(NetworkString<_32> configurationHash, IReadOnlyList<int> abilityIndexes, out NetworkString<_32> updatedHash)
        {
            updatedHash = configurationHash;

            if (TryApplyAbilityIndexes(configurationHash.ToString(), abilityIndexes, out string hashString) == false)
            {
                return false;
            }

            if (hashString.Length > 32)
            {
                return false;
            }

            updatedHash = hashString;
            return true;
        }

        public static bool TryApplyAbilityAssignments(string configurationHash, IReadOnlyList<int> slotAssignments, out string updatedHash)
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
            string abilitySegment = parts.Length >= 4 ? parts[3] : string.Empty;

            if (TryDecodeAbilityConfiguration(abilitySegment, out int[] abilityIndexes, out int[] existingAssignments) == false)
            {
                abilityIndexes = Array.Empty<int>();
                existingAssignments = CreateDefaultSlotAssignments();
            }

            int[] sanitizedAssignments = SanitizeSlotAssignments(slotAssignments, abilityIndexes);
            string newAbilitySegment = EncodeAbilityConfiguration(abilityIndexes, sanitizedAssignments);
            updatedHash = ComposeConfigurationHash(parts[0], parts[1], statsSegment, newAbilitySegment);
            return updatedHash.Length <= 32;
        }

        public static bool TryApplyAbilityAssignments(NetworkString<_32> configurationHash, IReadOnlyList<int> slotAssignments, out NetworkString<_32> updatedHash)
        {
            updatedHash = configurationHash;

            if (TryApplyAbilityAssignments(configurationHash.ToString(), slotAssignments, out string hashString) == false)
            {
                return false;
            }

            if (hashString.Length > 32)
            {
                return false;
            }

            updatedHash = hashString;
            return true;
        }

        private static int[] CreateDefaultSlotAssignments()
        {
            int[] assignments = new int[AbilityControlSlotCount];
            for (int i = 0; i < assignments.Length; ++i)
            {
                assignments[i] = -1;
            }

            return assignments;
        }

        private static int[] SanitizeAbilityIndexes(IReadOnlyList<int> abilityIndexes)
        {
            if (abilityIndexes == null || abilityIndexes.Count == 0)
            {
                return Array.Empty<int>();
            }

            var sanitized = new List<int>(Mathf.Min(abilityIndexes.Count, MaxConfiguredAbilities));

            for (int i = 0; i < abilityIndexes.Count && sanitized.Count < MaxConfiguredAbilities; ++i)
            {
                int index = Mathf.Clamp(abilityIndexes[i], 0, int.MaxValue);
                if (sanitized.Contains(index) == true)
                {
                    continue;
                }

                sanitized.Add(index);
            }

            sanitized.Sort();
            return sanitized.Count > 0 ? sanitized.ToArray() : Array.Empty<int>();
        }

        private static void SanitizeAssignmentsAgainstUnlocked(int[] assignments, IReadOnlyList<int> unlockedIndexes)
        {
            if (assignments == null || unlockedIndexes == null)
            {
                return;
            }

            for (int i = 0; i < assignments.Length; ++i)
            {
                int assignment = assignments[i];
                if (assignment < 0)
                {
                    continue;
                }

                if (IsIndexUnlocked(assignment, unlockedIndexes) == false)
                {
                    assignments[i] = -1;
                }
            }
        }

        private static int[] SanitizeSlotAssignments(IReadOnlyList<int> requestedAssignments, IReadOnlyList<int> unlockedIndexes)
        {
            int[] result = CreateDefaultSlotAssignments();

            if (unlockedIndexes == null || unlockedIndexes.Count == 0)
            {
                return result;
            }

            if (requestedAssignments == null || requestedAssignments.Count == 0)
            {
                return result;
            }

            var usedAssignments = new HashSet<int>();
            int limit = Mathf.Min(AbilityControlSlotCount, requestedAssignments.Count);

            for (int i = 0; i < limit; ++i)
            {
                int assignment = requestedAssignments[i];
                if (assignment < 0)
                {
                    result[i] = -1;
                    continue;
                }

                if (IsIndexUnlocked(assignment, unlockedIndexes) == false)
                {
                    result[i] = -1;
                    continue;
                }

                if (usedAssignments.Add(assignment) == false)
                {
                    result[i] = -1;
                    continue;
                }

                result[i] = assignment;
            }

            return result;
        }

        private static string ComposeConfigurationHash(string prefix, string seedSegment, string statsSegment, string abilitySegment)
        {
            if (string.IsNullOrEmpty(abilitySegment) == true)
            {
                if (string.IsNullOrWhiteSpace(statsSegment) == false)
                {
                    return $"{prefix}:{seedSegment}:{statsSegment}";
                }

                return $"{prefix}:{seedSegment}";
            }

            if (string.IsNullOrWhiteSpace(statsSegment) == false)
            {
                return $"{prefix}:{seedSegment}:{statsSegment}:{abilitySegment}";
            }

            return $"{prefix}:{seedSegment}::{abilitySegment}";
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
