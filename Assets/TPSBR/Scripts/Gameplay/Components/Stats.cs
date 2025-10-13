using System;
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
        private int[] _initialValues = new int[Count];

        [Networked, Capacity(Count)]
        private NetworkArray<byte> _stats { get; }

        public int Strength     => GetStat(StatIndex.Strength);
        public int Dexterity    => GetStat(StatIndex.Dexterity);
        public int Intelligence => GetStat(StatIndex.Intelligence);
        public int Luck         => GetStat(StatIndex.Luck);
        public int Looting      => GetStat(StatIndex.Looting);
        public int Mining       => GetStat(StatIndex.Mining);

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

            return _stats.Get(index);
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

            _stats.Set(index, (byte)Mathf.Clamp(value, byte.MinValue, byte.MaxValue));
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
    }
}
