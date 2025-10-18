using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class Professions : ContextBehaviour
    {
        public const int Count = 9;

        public enum ProfessionIndex
        {
            // Gathering
            Mining     = 0,
            Fishing    = 1,
            Woodcutting   = 2,
            
            // Crafting
            Blacksmithing    = 3,
            Alchemy      = 4,
            Cooking       = 5,
            
            // Specialized
            Herbalism    = 6,
            Hunting      = 7,
            Runecrafting       = 8,
        }

        public const string MiningCode        = "MIN";
        public const string FishingCode       = "FSH";
        public const string WoodcuttingCode   = "WDC";
        public const string BlacksmithingCode = "BSM";
        public const string AlchemyCode       = "ALC";
        public const string CookingCode       = "CKG";
        public const string HerbalismCode     = "HRB";
        public const string HuntingCode       = "HNT";
        public const string RunecraftingCode  = "RNC";

        private static readonly string[] _codes =
        {
            MiningCode,
            FishingCode,
            WoodcuttingCode,
            BlacksmithingCode,
            AlchemyCode,
            CookingCode,
            HerbalismCode,
            HuntingCode,
            RunecraftingCode,
        };


        [SerializeField]
        private int[] _initialValues =  {
            5,
            5,
            5,
            5,
            5,
            5,
            5,
            5,
            5,
        };

        [Networked, Capacity(Count)]
        private NetworkArray<byte> _professions { get; }

        public event Action<ProfessionIndex, int, int> StatChanged;

        public int Mining        => GetProfession(ProfessionIndex.Mining);
        public int Fishing       => GetProfession(ProfessionIndex.Fishing);
        public int Woodcutting   => GetProfession(ProfessionIndex.Woodcutting);
        public int Blacksmithing => GetProfession(ProfessionIndex.Blacksmithing);
        public int Alchemy       => GetProfession(ProfessionIndex.Alchemy);
        public int Cooking       => GetProfession(ProfessionIndex.Cooking);
        public int Herbalism     => GetProfession(ProfessionIndex.Herbalism);
        public int Hunting       => GetProfession(ProfessionIndex.Hunting);
        public int Runecrafting  => GetProfession(ProfessionIndex.Runecrafting);


        private int[] _cachedProfessions;
        private bool _cacheInitialized;

        public static string GetCode(ProfessionIndex profession)
        {
            return _codes[(int)profession];
        }

        public static string GetCode(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _codes[index];
        }

        public int GetProfession(ProfessionIndex profession)
        {
            return GetProfession((int)profession);
        }

        public int GetProfession(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _professions.Get(index);
        }

        public void SetProfession(ProfessionIndex profession, int value)
        {
            SetProfession((int)profession, value);
        }

        public void SetProfession(int index, int value)
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

            byte previousValue = (byte)_cachedProfessions[index];
            byte newValue = (byte)Mathf.Clamp(value, byte.MinValue, byte.MaxValue);

            if (previousValue == newValue)
            {
                return;
            }

            _professions.Set(index, newValue);

            _cachedProfessions[index] = newValue;

            OnProfessionChanged((ProfessionIndex)index, previousValue, newValue);
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

                _professions.Set(i, (byte)Mathf.Clamp(value, byte.MinValue, byte.MaxValue));
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
                CheckForProfessionUpdates();
            }
        }

        public override void Render()
        {
            base.Render();

            CheckForProfessionUpdates();
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
            if (_cachedProfessions == null || _cachedProfessions.Length != Count)
            {
                _cachedProfessions = new int[Count];
                _cacheInitialized = false;
            }

            if (_cacheInitialized == true)
            {
                return;
            }

            for (int i = 0; i < Count; ++i)
            {
                _cachedProfessions[i] = _professions.Get(i);
            }

            _cacheInitialized = true;
        }

        private void CheckForProfessionUpdates()
        {
            if (Object == null || Object.IsValid == false)
            {
                return;
            }

            EnsureCacheInitialized();

            for (int i = 0; i < Count; ++i)
            {
                int currentValue = _professions.Get(i);
                int previousValue = _cachedProfessions[i];

                if (currentValue == previousValue)
                {
                    continue;
                }

                _cachedProfessions[i] = currentValue;
                OnProfessionChanged((ProfessionIndex)i, previousValue, currentValue);
            }
        }

        private void OnProfessionChanged(ProfessionIndex profession, int previousValue, int newValue)
        {
            StatChanged?.Invoke(profession, previousValue, newValue);
        }
    }
}