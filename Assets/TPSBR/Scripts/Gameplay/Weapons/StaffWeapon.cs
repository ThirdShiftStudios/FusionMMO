using System;
using UnityEngine;

namespace TPSBR
{
    public class StaffWeapon : Weapon
    {
        private const string HASH_PREFIX = "STF";

        [SerializeField]
        private float _baseDamage;
        [SerializeField]
        private float _healthRegen;
        [SerializeField]
        private float _manaRegen;
        [SerializeField]
        private string _configuredItemName;

        public float BaseDamage => _baseDamage;
        public float HealthRegen => _healthRegen;
        public float ManaRegen => _manaRegen;
        public string ConfiguredItemName => _configuredItemName;

        // Weapon INTERFACE

        public override bool CanFire(bool keyDown)
        {
            return false;
        }

        public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
        {
            throw new NotImplementedException();
        }

        public override bool CanAim()
        {
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

        public override string GetDescription()
        {
            float baseDamage = _baseDamage;
            float healthRegen = _healthRegen;
            float manaRegen = _manaRegen;

            if (TryGetStatsFromConfiguration(ConfigurationHash.ToString(), out float configBaseDamage, out float configHealthRegen, out float configManaRegen, out _))
            {
                baseDamage = configBaseDamage;
                healthRegen = configHealthRegen;
                manaRegen = configManaRegen;
            }

            return
                   $"Damage: {baseDamage}\n" +
                   $"Health Regen: {healthRegen}\n" +
                   $"Mana Regen: {manaRegen}\n" +
                   $"" ;
        }
    }
}
