using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public enum ToolTier
    {
        Tier1,
        Tier2,
        Tier3,
        Tier4,
        Tier5
    }

    public abstract class Tool : ContextBehaviour, IInventoryItemDetails
    {
        private const string HASH_PREFIX = "TOL";

        private static readonly string[] NAME_PREFIXES =
        {
            "Sturdy",
            "Swift",
            "Runed",
            "Ancient",
            "Gleaming",
            "Arcane"
        };

        private static readonly string[] NAME_SUFFIXES =
        {
            "Prospector",
            "Harvester",
            "Delver",
            "Warden",
            "Whisper",
            "Forger"
        };

        [SerializeField]
        private ToolTier _tier;
        [SerializeField]
        private int _speed;
        [SerializeField]
        private int _yield;
        [SerializeField]
        private int _luck;
        [SerializeField]
        private string _configuredName;

        private bool _isInitialized;
        private bool _isEquipped;
        private NetworkObject _owner;
        private Transform _equippedParent;
        private Transform _unequippedParent;
        private string _lastConfigurationHash = string.Empty;

        [Networked]
        private NetworkString<_32> _configurationHash { get; set; }

        private NetworkString<_32> _appliedConfigurationHash;

        public ToolTier Tier => _tier;
        public int Speed => _speed;
        public int Yield => _yield;
        public int Luck => _luck;
        public string ConfiguredName => _configuredName;
        public string DisplayName => string.IsNullOrWhiteSpace(_configuredName) == false ? _configuredName : GetDefaultDisplayName();
        public Sprite Icon => GetIcon();
        public bool IsEquipped => _isEquipped;

        public override void Spawned()
        {
            base.Spawned();
            ApplyConfigurationHash(_configurationHash);
        }

        public virtual string GetDisplayName(NetworkString<_32> configurationHash)
        {
            string hash = configurationHash.ToString();

            if (TryGetStatsFromConfiguration(hash, out _, out _, out _, out _, out string configuredName) == true && string.IsNullOrWhiteSpace(configuredName) == false)
            {
                return configuredName;
            }

            return DisplayName;
        }

        public virtual string GetDescription()
        {
            if (TryGetStatsFromConfiguration(_lastConfigurationHash, out ToolTier tier, out int speed, out int itemYield, out int luck, out _))
            {
                return BuildDescription(tier, speed, itemYield, luck);
            }

            return BuildDescription(_tier, _speed, _yield, _luck);
        }

        public virtual string GetDescription(NetworkString<_32> configurationHash)
        {
            string hash = configurationHash.ToString();

            if (TryGetStatsFromConfiguration(hash, out ToolTier tier, out int speed, out int itemYield, out int luck, out _))
            {
                return BuildDescription(tier, speed, itemYield, luck);
            }

            return GetDescription();
        }

        public virtual string GenerateRandomStats()
        {
            int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            byte[] payload = BitConverter.GetBytes(seed);
            string encodedSeed = Convert.ToBase64String(payload);
            return $"{HASH_PREFIX}:{encodedSeed}";
        }

        public void SetConfigurationHash(NetworkString<_32> configurationHash)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            if (_configurationHash == configurationHash)
                return;

            _configurationHash = configurationHash;
            ApplyConfigurationHash(_configurationHash);
        }

        public void InitializeTool(NetworkObject owner, Transform equippedParent, Transform unequippedParent)
        {
            if (_isInitialized == true && _owner != owner)
                return;

            _isInitialized = true;
            _owner = owner;
            _equippedParent = equippedParent;
            _unequippedParent = unequippedParent;

            RefreshParent();
        }

        public void DeinitializeTool(NetworkObject owner)
        {
            if (_owner != null && _owner != owner)
                return;

            _isInitialized = false;
            _owner = null;
            _equippedParent = null;
            _unequippedParent = null;
        }

        public void RefreshParents(Transform equippedParent, Transform unequippedParent)
        {
            if (_isInitialized == false)
                return;

            _equippedParent = equippedParent;
            _unequippedParent = unequippedParent;

            RefreshParent();
        }

        public void SetEquipped(bool equipped)
        {
            _isEquipped = equipped;
            RefreshParent();
        }

        protected virtual void OnConfigurationHashApplied(string configurationHash)
        {
            if (TryGetStatsFromConfiguration(configurationHash, out ToolTier tier, out int speed, out int itemYield, out int luck, out string configuredName) == false)
            {
                ResetConfiguration();
                return;
            }

            _lastConfigurationHash = configurationHash;
            ApplyConfiguration(tier, speed, itemYield, luck, configuredName);
        }

        protected abstract string GetDefaultDisplayName();
        protected abstract Sprite GetIcon();

        private void RefreshParent()
        {
            if (_isInitialized == false)
                return;

            Transform target = _isEquipped == true ? _equippedParent : _unequippedParent;
            if (target == null)
                return;

            transform.SetParent(target, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        private void ApplyConfigurationHash(NetworkString<_32> configurationHash)
        {
            if (_appliedConfigurationHash == configurationHash)
                return;

            _appliedConfigurationHash = configurationHash;
            OnConfigurationHashApplied(configurationHash.ToString());
        }

        private void ApplyConfiguration(ToolTier tier, int speed, int itemYield, int luck, string configuredName)
        {
            _tier = tier;
            _speed = speed;
            _yield = itemYield;
            _luck = luck;
            _configuredName = configuredName;
        }

        private bool TryGetStatsFromConfiguration(string configurationHash, out ToolTier tier, out int speed, out int itemYield, out int luck, out string configuredName)
        {
            tier = default;
            speed = default;
            itemYield = default;
            luck = default;
            configuredName = string.Empty;

            if (TryParseSeed(configurationHash, out int seed) == false)
            {
                return false;
            }

            PopulateStats(new System.Random(seed), out tier, out speed, out itemYield, out luck, out configuredName);
            return true;
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

        private void PopulateStats(System.Random random, out ToolTier tier, out int speed, out int itemYield, out int luck, out string configuredName)
        {
            int tierIndex = random.Next(Enum.GetValues(typeof(ToolTier)).Length);
            tier = (ToolTier)tierIndex;

            speed = GenerateStat(random, 8 + tierIndex * 2, 16 + tierIndex * 3);
            itemYield = GenerateStat(random, 1 + tierIndex, 3 + tierIndex * 2);
            luck = GenerateStat(random, tierIndex, 4 + tierIndex * 2);
            configuredName = GenerateName(random);
        }

        private void ResetConfiguration()
        {
            _tier = ToolTier.Tier1;
            _speed = 0;
            _yield = 0;
            _luck = 0;
            _configuredName = string.Empty;
            _lastConfigurationHash = string.Empty;
        }

        private int GenerateStat(System.Random random, int min, int max)
        {
            if (min > max)
            {
                int temp = min;
                min = max;
                max = temp;
            }

            return random.Next(min, max + 1);
        }

        private string GenerateName(System.Random random)
        {
            string prefix = NAME_PREFIXES[random.Next(NAME_PREFIXES.Length)];
            string suffix = NAME_SUFFIXES[random.Next(NAME_SUFFIXES.Length)];
            return $"{prefix} {suffix}";
        }

        private string BuildDescription(ToolTier tier, int speed, int itemYield, int luck)
        {
            return
                $"Tier: {FormatTier(tier)}\n" +
                $"Speed: {speed}\n" +
                $"Yield: {itemYield}\n" +
                $"Luck: {luck}";
        }

        private string FormatTier(ToolTier tier)
        {
            string name = tier.ToString();
            if (name.StartsWith("Tier", StringComparison.OrdinalIgnoreCase) == true && name.Length > 4)
            {
                return $"Tier {name.Substring(4)}";
            }

            return name;
        }
    }
}
