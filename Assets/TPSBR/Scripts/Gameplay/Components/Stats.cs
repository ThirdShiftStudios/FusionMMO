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
            Strength     = 0,
            Dexterity    = 1,
            Intelligence = 2,
            Luck         = 3,
            Looting      = 4,
            Mining       = 5,
        }

        public const string StrengthCode     = "STR";
        public const string DexterityCode    = "DEX";
        public const string IntelligenceCode = "INT";
        public const string LuckCode         = "LCK";
        public const string LootingCode      = "LOT";
        public const string MiningCode       = "MIN";

        private static readonly string[] _codes =
        {
            StrengthCode,
            DexterityCode,
            IntelligenceCode,
            LuckCode,
            LootingCode,
            MiningCode,
        };

        [SerializeField]
        private int[] _initialValues =  {
            5,
            5,
            5,
            5,
            5,
            5,
        };

        [Networked, Capacity(Count)]
        private NetworkArray<byte> _stats { get; }

        public event Action<StatIndex, int, int> StatChanged;

        public int Strength     => GetStat(StatIndex.Strength);
        public int Dexterity    => GetStat(StatIndex.Dexterity);
        public int Intelligence => GetStat(StatIndex.Intelligence);
        public int Luck         => GetStat(StatIndex.Luck);
        public int Looting      => GetStat(StatIndex.Looting);
        public int Mining       => GetStat(StatIndex.Mining);

        private int[] _cachedBaseStats;
        private int[] _cachedEffectiveStats;
        private int[] _modifierTotals;
        private readonly Dictionary<object, int[]> _modifiers = new Dictionary<object, int[]>();
        private bool _cacheInitialized;

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

            EnsureCacheInitialized();

            return _cachedEffectiveStats[index];
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

            byte previousValue = (byte)_cachedBaseStats[index];
            byte newValue = (byte)Mathf.Clamp(value, byte.MinValue, byte.MaxValue);

            if (previousValue == newValue)
            {
                return;
            }

            _stats.Set(index, newValue);

            _cachedBaseStats[index] = newValue;

            int previousEffective = _cachedEffectiveStats[index];
            int newEffective = newValue + GetModifierTotal(index);

            _cachedEffectiveStats[index] = newEffective;

            if (previousEffective != newEffective)
            {
                OnStatChanged((StatIndex)index, previousEffective, newEffective);
            }
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

            EnsureCacheInitialized();
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
        }
#endif

        private void EnsureCacheInitialized()
        {
            if (_cachedBaseStats == null || _cachedBaseStats.Length != Count)
            {
                _cachedBaseStats = new int[Count];
                _cacheInitialized = false;
            }

            if (_cachedEffectiveStats == null || _cachedEffectiveStats.Length != Count)
            {
                _cachedEffectiveStats = new int[Count];
                _cacheInitialized = false;
            }

            InitializeModifierTotals();

            if (_cacheInitialized == true)
            {
                return;
            }

            for (int i = 0; i < Count; ++i)
            {
                int baseValue = _stats.Get(i);
                _cachedBaseStats[i] = baseValue;
                _cachedEffectiveStats[i] = baseValue + GetModifierTotal(i);
            }

            _cacheInitialized = true;
        }

        private void CheckForStatUpdates()
        {
            if (Object == null || Object.IsValid == false)
            {
                return;
            }

            EnsureCacheInitialized();

            for (int i = 0; i < Count; ++i)
            {
                int currentValue = _stats.Get(i);
                int previousValue = _cachedBaseStats[i];

                if (currentValue == previousValue)
                {
                    continue;
                }

                _cachedBaseStats[i] = currentValue;

                int previousEffective = _cachedEffectiveStats[i];
                int newEffective = currentValue + GetModifierTotal(i);

                _cachedEffectiveStats[i] = newEffective;

                if (previousEffective != newEffective)
                {
                    OnStatChanged((StatIndex)i, previousEffective, newEffective);
                }
            }
        }

        private void OnStatChanged(StatIndex stat, int previousValue, int newValue)
        {
            StatChanged?.Invoke(stat, previousValue, newValue);
        }

        public void SetAdditiveModifiers(object source, IReadOnlyList<int> modifiers)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            EnsureCacheInitialized();

            if (modifiers == null)
            {
                modifiers = Array.Empty<int>();
            }

            if (_modifiers.TryGetValue(source, out int[] storedValues) == false)
            {
                storedValues = new int[Count];
                _modifiers[source] = storedValues;
            }

            int modifierCount = Math.Min(modifiers.Count, Count);

            bool hasChanges = false;

            for (int i = 0; i < Count; ++i)
            {
                int nextValue = i < modifierCount ? modifiers[i] : 0;
                int previousValue = storedValues[i];

                if (previousValue == nextValue)
                {
                    continue;
                }

                storedValues[i] = nextValue;
                int modifierTotal = GetModifierTotal(i) + (nextValue - previousValue);
                _modifierTotals[i] = modifierTotal;

                int previousEffective = _cachedEffectiveStats[i];
                int newEffective = _cachedBaseStats[i] + modifierTotal;

                _cachedEffectiveStats[i] = newEffective;

                if (previousEffective != newEffective)
                {
                    OnStatChanged((StatIndex)i, previousEffective, newEffective);
                }

                hasChanges = true;
            }

            if (hasChanges == false)
            {
                return;
            }

            bool removeSource = true;
            for (int i = 0; i < storedValues.Length; ++i)
            {
                if (storedValues[i] != 0)
                {
                    removeSource = false;
                    break;
                }
            }

            if (removeSource == true)
            {
                _modifiers.Remove(source);
            }
        }

        public void ClearAdditiveModifiers(object source)
        {
            if (source == null)
            {
                return;
            }

            if (_modifiers.TryGetValue(source, out int[] storedValues) == false)
            {
                return;
            }

            EnsureCacheInitialized();

            for (int i = 0; i < storedValues.Length && i < Count; ++i)
            {
                int previousContribution = storedValues[i];

                if (previousContribution == 0)
                {
                    continue;
                }

                storedValues[i] = 0;

                int modifierTotal = GetModifierTotal(i) - previousContribution;
                _modifierTotals[i] = modifierTotal;

                int previousEffective = _cachedEffectiveStats[i];
                int newEffective = _cachedBaseStats[i] + modifierTotal;

                _cachedEffectiveStats[i] = newEffective;

                if (previousEffective != newEffective)
                {
                    OnStatChanged((StatIndex)i, previousEffective, newEffective);
                }
            }

            _modifiers.Remove(source);
        }

        public int GetBaseStat(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _stats.Get(index);
        }

        private void InitializeModifierTotals()
        {
            bool rebuildTotals = false;

            if (_modifierTotals == null || _modifierTotals.Length != Count)
            {
                _modifierTotals = new int[Count];
                rebuildTotals = true;
            }

            if (_modifiers.Count == 0)
            {
                if (rebuildTotals == true)
                {
                    Array.Clear(_modifierTotals, 0, _modifierTotals.Length);
                }

                return;
            }

            bool needsResize = false;

            foreach (KeyValuePair<object, int[]> entry in _modifiers)
            {
                int[] values = entry.Value;

                if (values == null || values.Length != Count)
                {
                    needsResize = true;
                    break;
                }
            }

            if (needsResize == true)
            {
                var keys = new List<object>(_modifiers.Keys);

                foreach (object key in keys)
                {
                    int[] values = _modifiers[key];
                    int[] resized = new int[Count];

                    if (values != null)
                    {
                        Array.Copy(values, resized, Math.Min(values.Length, Count));
                    }

                    _modifiers[key] = resized;
                }

                rebuildTotals = true;
            }

            if (rebuildTotals == true)
            {
                Array.Clear(_modifierTotals, 0, _modifierTotals.Length);

                foreach (int[] values in _modifiers.Values)
                {
                    if (values == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < Count && i < values.Length; ++i)
                    {
                        _modifierTotals[i] += values[i];
                    }
                }
            }
        }

        private int GetModifierTotal(int index)
        {
            if (_modifierTotals == null || index < 0 || index >= _modifierTotals.Length)
            {
                return 0;
            }

            return _modifierTotals[index];
        }
    }
}
