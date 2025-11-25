using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class Stats : ContextBehaviour
    {
        public const int Count = 6;

        public enum StatIndex
        {
            Intelligence = 0,
            Strength     = 1,
            Dexterity    = 2,
            Endurance    = 3,
            Luck         = 4,
            Willpower    = 5,
        }

        public const string IntelligenceCode = "INT";
        public const string StrengthCode     = "STR";
        public const string DexterityCode    = "DEX";
        public const string EnduranceCode    = "END";
        public const string LuckCode         = "LCK";
        public const string WillpowerCode    = "WIL";

        private static readonly string[] _defaultCodes =
        {
            IntelligenceCode,
            StrengthCode,
            DexterityCode,
            EnduranceCode,
            LuckCode,
            WillpowerCode,
        };

        private static string[] _codes = (string[])_defaultCodes.Clone();
        private static StatDefinition[] _registeredDefinitions;
        private static StatDefinition[] _runtimeDefaults;

        [SerializeField]
        private int[] _initialValues =  {
            5,
            5,
            5,
            5,
            5,
            5,
        };

        [SerializeField]
        private StatDefinition[] _statDefinitions = new StatDefinition[Count];

        [Networked, Capacity(Count)]
        private NetworkArray<byte> _stats { get; }

        private bool CanAccessNetworkedStats => Object != null && Object.IsValid == true && IsSpawned == true;

        public event Action<StatIndex, int, int> StatChanged;

        public int Intelligence => GetStat(StatIndex.Intelligence);
        public int Strength     => GetStat(StatIndex.Strength);
        public int Dexterity    => GetStat(StatIndex.Dexterity);
        public int Endurance    => GetStat(StatIndex.Endurance);
        public int Luck         => GetStat(StatIndex.Luck);
        public int Willpower    => GetStat(StatIndex.Willpower);

        private int[] _cachedStats;
        private bool _cacheInitialized;

        private void Awake()
        {
            EnsureDefinitionsRegistered();
        }

        public static string GetCode(StatIndex stat)
        {
            return _codes[(int)stat];
        }

        public static string GetCode(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _codes[index];
        }

        public int GetStat(StatIndex stat)
        {
            return GetStat((int)stat);
        }

        public int GetStat(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (CanAccessNetworkedStats == false)
            {
                return GetInitialStatValue(index);
            }

            return _stats.Get(index);
        }

        public float GetTotalHealth()
        {
            return Aggregate((definition, level) => definition.GetTotalHealth(level));
        }

        public float GetTotalHealth(StatIndex stat, int overrideValue)
        {
            return AggregateWithOverride(stat, overrideValue, (definition, level) => definition.GetTotalHealth(level));
        }

        public float GetMovementSpeedMultiplier()
        {
            return Aggregate((definition, level) => definition.GetMovementSpeedMultiplier(level));
        }

        public float GetMovementSpeedMultiplier(StatIndex stat, int overrideValue)
        {
            return AggregateWithOverride(stat, overrideValue, (definition, level) => definition.GetMovementSpeedMultiplier(level));
        }

        internal PlayerStatSaveData[] CreateSaveData()
        {
            var records = new PlayerStatSaveData[Count];

            for (int i = 0; i < Count; ++i)
            {
                records[i] = new PlayerStatSaveData
                {
                    StatCode = GetCode(i),
                    Value = Mathf.Clamp(GetStat(i), 0, byte.MaxValue),
                };
            }

            return records;
        }

        internal void ApplySaveData(PlayerStatSaveData[] data)
        {
            if (HasStateAuthority == false)
            {
                return;
            }

            if (data == null || data.Length == 0)
            {
                return;
            }

            for (int i = 0; i < data.Length; ++i)
            {
                string code = data[i].StatCode;
                if (string.IsNullOrEmpty(code) == true)
                {
                    continue;
                }

                if (TryGetIndex(code, out int index) == false)
                {
                    continue;
                }

                int value = Mathf.Clamp(data[i].Value, byte.MinValue, byte.MaxValue);
                SetStat(index, value);
            }
        }

        public void SetStat(StatIndex stat, int value)
        {
            SetStat((int)stat, value);
        }

        public void SetStat(int index, int value)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (HasStateAuthority == false)
            {
                return;
            }

            EnsureCacheInitialized();

            byte previousValue = (byte)_cachedStats[index];
            byte newValue = (byte)Mathf.Clamp(value, byte.MinValue, byte.MaxValue);

            if (previousValue == newValue)
            {
                return;
            }

            _stats.Set(index, newValue);

            _cachedStats[index] = newValue;

            OnStatChanged((StatIndex)index, previousValue, newValue);
        }

        public override void CopyBackingFieldsToState(bool firstTime)
        {
            base.CopyBackingFieldsToState(firstTime);

            InvokeWeavedCode();

            for (int i = 0; i < Count; ++i)
            {
                int value = 0;

                if (_initialValues != null && i < _initialValues.Length)
                {
                    value = _initialValues[i];
                }

                _stats.Set(i, (byte)Mathf.Clamp(value, byte.MinValue, byte.MaxValue));
            }

            _cacheInitialized = false;
        }

        public override void Spawned()
        {
            base.Spawned();

            EnsureDefinitionsRegistered();
            EnsureCacheInitialized();

            Global.PlayerCloudSaveService?.RegisterStatsAndRestore(this);
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (HasStateAuthority == false)
            {
                CheckForStatUpdates();
            }
        }

        public override void Render()
        {
            base.Render();

            CheckForStatUpdates();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Global.PlayerCloudSaveService?.UnregisterStats(this);

            base.Despawned(runner, hasState);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_initialValues == null || _initialValues.Length != Count)
            {
                var values = new int[Count];
                int length = _initialValues != null ? Math.Min(_initialValues.Length, Count) : 0;

                for (int i = 0; i < length; ++i)
                {
                    values[i] = Mathf.Clamp(_initialValues[i], byte.MinValue, byte.MaxValue);
                }

                _initialValues = values;
            }
            else
            {
                for (int i = 0; i < _initialValues.Length; ++i)
                {
                    _initialValues[i] = Mathf.Clamp(_initialValues[i], byte.MinValue, byte.MaxValue);
                }
            }

            if (_statDefinitions == null || _statDefinitions.Length != Count)
            {
                var definitions = new StatDefinition[Count];
                int length = _statDefinitions != null ? Math.Min(_statDefinitions.Length, Count) : 0;

                for (int i = 0; i < length; ++i)
                {
                    definitions[i] = _statDefinitions[i];
                }

                _statDefinitions = definitions;
            }

            EnsureDefinitionsRegistered();
        }
#endif

        private void EnsureCacheInitialized()
        {
            if (_cachedStats == null || _cachedStats.Length != Count)
            {
                _cachedStats = new int[Count];
                _cacheInitialized = false;
            }

            if (_cacheInitialized == true || CanAccessNetworkedStats == false)
            {
                return;
            }

            for (int i = 0; i < Count; ++i)
            {
                _cachedStats[i] = _stats.Get(i);
            }

            _cacheInitialized = true;
        }

        private void CheckForStatUpdates()
        {
            if (CanAccessNetworkedStats == false)
            {
                return;
            }

            EnsureCacheInitialized();

            for (int i = 0; i < Count; ++i)
            {
                int currentValue = _stats.Get(i);
                int previousValue = _cachedStats[i];

                if (currentValue == previousValue)
                {
                    continue;
                }

                _cachedStats[i] = currentValue;
                OnStatChanged((StatIndex)i, previousValue, currentValue);
            }
        }

        private void OnStatChanged(StatIndex stat, int previousValue, int newValue)
        {
            StatChanged?.Invoke(stat, previousValue, newValue);
        }

        private int GetInitialStatValue(int index)
        {
            if (_initialValues != null && index < _initialValues.Length)
            {
                return Mathf.Clamp(_initialValues[index], byte.MinValue, byte.MaxValue);
            }

            return 0;
        }

        public void OnSpawned(Agent agent)
        {

        }

        public void OnDespawned()
        {

        }

        public static bool TryGetIndex(string code, out int index)
        {
            if (string.IsNullOrEmpty(code) == true)
            {
                index = -1;
                return false;
            }

            for (int i = 0; i < Count; ++i)
            {
                if (string.Equals(_codes[i], code, StringComparison.OrdinalIgnoreCase) == true)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        private void EnsureDefinitionsRegistered()
        {
            if (_statDefinitions == null || _statDefinitions.Length != Count)
            {
                _statDefinitions = new StatDefinition[Count];
            }

            EnsureDefinitionStorage();

            bool updated = false;
            for (int i = 0; i < Count; ++i)
            {
                var definition = _statDefinitions[i];
                if (definition == null)
                {
                    if (_registeredDefinitions[i] != null)
                    {
                        _registeredDefinitions[i] = null;
                        updated = true;
                    }

                    continue;
                }

                if (ReferenceEquals(_registeredDefinitions[i], definition) == false)
                {
                    _registeredDefinitions[i] = definition;
                    updated = true;
                }
            }

            if (updated == true)
            {
                UpdateCodesFromDefinitions();
            }

            EnsureDefaultDefinitions();
        }

        private float Aggregate(Func<StatDefinition, int, float> selector)
        {
            var definitions = GetRegisteredDefinitions();
            float total = 0f;

            for (int i = 0; i < Count; ++i)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    continue;
                }

                int statLevel = Mathf.Max(0, GetStat(i));
                total += selector(definition, statLevel);
            }

            return total;
        }

        private float AggregateWithOverride(StatIndex stat, int overrideValue, Func<StatDefinition, int, float> selector)
        {
            var definitions = GetRegisteredDefinitions();
            float total = 0f;
            int overrideIndex = (int)stat;

            for (int i = 0; i < Count; ++i)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    continue;
                }

                int statLevel = i == overrideIndex ? Mathf.Max(0, overrideValue) : Mathf.Max(0, GetStat(i));
                total += selector(definition, statLevel);
            }

            return total;
        }

        private static IReadOnlyList<StatDefinition> GetRegisteredDefinitions()
        {
            EnsureDefinitionStorage();
            EnsureDefaultDefinitions();
            return _registeredDefinitions;
        }

        private static void EnsureDefinitionStorage()
        {
            if (_registeredDefinitions == null || _registeredDefinitions.Length != Count)
            {
                _registeredDefinitions = new StatDefinition[Count];
                _codes = (string[])_defaultCodes.Clone();
            }
        }

        private static void EnsureDefaultDefinitions()
        {
            if (_runtimeDefaults == null || _runtimeDefaults.Length != Count)
            {
                _runtimeDefaults = new StatDefinition[Count];
                _runtimeDefaults[(int)StatIndex.Intelligence] = CreateRuntimeDefinition<IntelligenceDefinition>("Intelligence", IntelligenceCode);
                _runtimeDefaults[(int)StatIndex.Strength] = CreateRuntimeDefinition<StrengthDefinition>("Strength", StrengthCode);
                _runtimeDefaults[(int)StatIndex.Dexterity] = CreateRuntimeDefinition<DexterityDefinition>("Dexterity", DexterityCode);
                _runtimeDefaults[(int)StatIndex.Endurance] = CreateRuntimeDefinition<EnduranceDefinition>("Endurance", EnduranceCode);
                _runtimeDefaults[(int)StatIndex.Luck] = CreateRuntimeDefinition<LuckDefinition>("Luck", LuckCode);
                _runtimeDefaults[(int)StatIndex.Willpower] = CreateRuntimeDefinition<WillpowerDefinition>("Willpower", WillpowerCode);
            }

            bool updated = false;
            for (int i = 0; i < Count; ++i)
            {
                if (_registeredDefinitions[i] == null && _runtimeDefaults[i] != null)
                {
                    _registeredDefinitions[i] = _runtimeDefaults[i];
                    updated = true;
                }
            }

            if (updated == true)
            {
                UpdateCodesFromDefinitions();
            }
        }

        private static void UpdateCodesFromDefinitions()
        {
            for (int i = 0; i < Count; ++i)
            {
                var definition = _registeredDefinitions[i];
                if (definition != null && string.IsNullOrEmpty(definition.Code) == false)
                {
                    _codes[i] = definition.Code;
                }
                else
                {
                    _codes[i] = _defaultCodes[i];
                }
            }
        }

        private static T CreateRuntimeDefinition<T>(string displayName, string code) where T : StatDefinition
        {
            var definition = ScriptableObject.CreateInstance<T>();
            definition.RuntimeInitialize(displayName, code);
            definition.hideFlags = HideFlags.HideAndDontSave;
            return definition;
        }
    }

    [Serializable]
    public struct PlayerStatSaveData
    {
        public string StatCode;
        public int Value;
    }
}
