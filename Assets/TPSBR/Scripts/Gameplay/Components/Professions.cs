using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public sealed class Professions : ContextBehaviour
    {
        public const int Count = 10;
        public const int MinLevel = 1;
        public const int MaxLevel = 100;
        public const int BaseExperienceRequirement = 50;
        public const int ExperienceIncrementPerLevel = 100;

        public enum ProfessionIndex
        {
            Mining       = 0,
            Woodcutting  = 1,
            Fishing      = 2,
            Gambling     = 3,
            Runecrafting = 4,
            Herbalism    = 5,
            Smithing     = 6,
            Carpentry    = 7,
            Alchemy      = 8,
            Brewing      = 9,
        }

        public readonly struct ProfessionSnapshot
        {
            public static ProfessionSnapshot Empty => new ProfessionSnapshot(0, 0, 0);

            public ProfessionSnapshot(int level, int experience, int experienceToNextLevel)
            {
                Level = Mathf.Max(0, level);
                Experience = Mathf.Max(0, experience);
                ExperienceToNextLevel = Mathf.Max(0, experienceToNextLevel);
            }

            public int Level { get; }
            public int Experience { get; }
            public int ExperienceToNextLevel { get; }

            public int ExperienceRemaining => ExperienceToNextLevel > 0 ? Mathf.Max(0, ExperienceToNextLevel - Experience) : 0;
            public float Progress => ExperienceToNextLevel > 0 ? Mathf.Clamp01((float)Experience / ExperienceToNextLevel) : (Level > 0 ? 1f : 0f);
        }

        public const string MiningCode       = "MIN";
        public const string WoodcuttingCode  = "WDC";
        public const string FishingCode      = "FSH";
        public const string GamblingCode     = "GMB";
        public const string RunecraftingCode = "RNC";
        public const string HerbalismCode    = "HRB";
        public const string SmithingCode     = "SMT";
        public const string CarpentryCode    = "CRP";
        public const string AlchemyCode      = "ALC";
        public const string BrewingCode      = "BRW";

        private static readonly string[] _codes =
        {
            MiningCode,
            WoodcuttingCode,
            FishingCode,
            GamblingCode,
            RunecraftingCode,
            HerbalismCode,
            SmithingCode,
            CarpentryCode,
            AlchemyCode,
            BrewingCode,
        };

        [SerializeField]
        private int[] _initialLevels =
        {
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
        };

        [Networked, Capacity(Count)]
        private NetworkArray<byte> _levels { get; }

        [Networked, Capacity(Count)]
        private NetworkArray<ushort> _experience { get; }

        public event Action<ProfessionIndex, ProfessionSnapshot, ProfessionSnapshot> ProfessionChanged;
        public event Action<ProfessionIndex, int, ProfessionSnapshot, ProfessionSnapshot> ExperienceGained;

        public ProfessionSnapshot Mining      => GetSnapshot(ProfessionIndex.Mining);
        public ProfessionSnapshot Woodcutting => GetSnapshot(ProfessionIndex.Woodcutting);
        public ProfessionSnapshot Fishing     => GetSnapshot(ProfessionIndex.Fishing);
        public ProfessionSnapshot Gambling    => GetSnapshot(ProfessionIndex.Gambling);
        public ProfessionSnapshot Runecrafting => GetSnapshot(ProfessionIndex.Runecrafting);
        public ProfessionSnapshot Herbalism   => GetSnapshot(ProfessionIndex.Herbalism);
        public ProfessionSnapshot Smithing    => GetSnapshot(ProfessionIndex.Smithing);
        public ProfessionSnapshot Carpentry   => GetSnapshot(ProfessionIndex.Carpentry);
        public ProfessionSnapshot Alchemy     => GetSnapshot(ProfessionIndex.Alchemy);
        public ProfessionSnapshot Brewing     => GetSnapshot(ProfessionIndex.Brewing);

        private int[] _cachedLevels;
        private int[] _cachedExperience;
        private bool _cacheInitialized;
        private bool _isSpawned;
        private Agent _agent;

        private bool CanReadNetworkedData => Object != null && Object.IsValid == true && _isSpawned == true;


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

        public static bool TryGetIndex(string professionCode, out int index)
        {
            index = -1;

            if (string.IsNullOrEmpty(professionCode) == true)
            {
                return false;
            }

            for (int i = 0; i < _codes.Length; i++)
            {
                if (string.Equals(_codes[i], professionCode, StringComparison.OrdinalIgnoreCase) == true)
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        public ProfessionSnapshot GetSnapshot(ProfessionIndex profession)
        {
            return GetSnapshot((int)profession);
        }

        public ProfessionSnapshot GetSnapshot(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            EnsureCacheInitialized();

            int level = _cachedLevels[index];
            int experience = _cachedExperience[index];

            return CreateSnapshot(level, experience);
        }

        public int GetLevel(ProfessionIndex profession)
        {
            return GetLevel((int)profession);
        }

        public int GetLevel(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            EnsureCacheInitialized();

            return _cachedLevels[index];
        }

        public int GetProfession(ProfessionIndex profession)
        {
            return GetLevel(profession);
        }

        public int GetProfession(int index)
        {
            return GetLevel(index);
        }

        private void Awake()
        {
            _agent = GetComponent<Agent>();
        }

        public int GetExperience(ProfessionIndex profession)
        {
            return GetExperience((int)profession);
        }

        public int GetExperience(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            EnsureCacheInitialized();

            return _cachedExperience[index];
        }

        public void SetProfession(ProfessionIndex profession, int level)
        {
            SetProfessionLevel(profession, level);
        }

        public void SetProfession(int index, int level)
        {
            SetProfessionLevel(index, level);
        }

        public void SetProfessionLevel(ProfessionIndex profession, int level, int experience = 0)
        {
            SetProfessionLevel((int)profession, level, experience);
        }

        public void SetProfessionLevel(int index, int level, int experience = 0)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (HasStateAuthority == false)
            {
                return;
            }

            if (CanReadNetworkedData == false)
            {
                return;
            }

            EnsureCacheInitialized();

            level = Mathf.Clamp(level, MinLevel, MaxLevel);
            int requiredExperience = GetExperienceRequiredForNextLevel(level);
            if (level >= MaxLevel)
            {
                experience = 0;
            }
            else
            {
                experience = Mathf.Clamp(experience, 0, requiredExperience);
            }

            int previousLevel = _cachedLevels[index];
            int previousExperience = _cachedExperience[index];

            if (previousLevel == level && previousExperience == experience)
            {
                return;
            }

            _levels.Set(index, (byte)level);
            _experience.Set(index, (ushort)experience);

            _cachedLevels[index] = level;
            _cachedExperience[index] = experience;

            OnProfessionChanged((ProfessionIndex)index, CreateSnapshot(previousLevel, previousExperience), CreateSnapshot(level, experience));
        }

        public void AddExperience(ProfessionIndex profession, int amount)
        {
            AddExperience((int)profession, amount);
        }

        public void AddExperience(int index, int amount)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (amount <= 0)
            {
                return;
            }

            if (HasStateAuthority == false)
            {
                return;
            }

            if (CanReadNetworkedData == false)
            {
                return;
            }

            amount = AdjustExperienceAmount(amount);
            if (amount <= 0)
            {
                return;
            }

            EnsureCacheInitialized();

            int currentLevel = _levels.Get(index);
            int currentExperience = _experience.Get(index);

            if (currentLevel >= MaxLevel)
            {
                return;
            }

            int startingLevel = currentLevel;
            int startingExperience = currentExperience;
            bool changed = false;

            while (amount > 0 && currentLevel < MaxLevel)
            {
                int requiredExperience = GetExperienceRequiredForNextLevel(currentLevel);
                if (requiredExperience <= 0)
                {
                    currentLevel = MaxLevel;
                    currentExperience = 0;
                    break;
                }

                int remaining = Mathf.Max(0, requiredExperience - currentExperience);

                if (amount < remaining)
                {
                    currentExperience += amount;
                    amount = 0;
                    changed = true;
                }
                else
                {
                    amount -= remaining;
                    currentLevel++;
                    currentExperience = 0;
                    changed = true;

                    if (currentLevel >= MaxLevel)
                    {
                        currentLevel = MaxLevel;
                        currentExperience = 0;
                        break;
                    }
                }
            }

            if (changed == false)
            {
                return;
            }

            _levels.Set(index, (byte)currentLevel);
            _experience.Set(index, (ushort)Mathf.Clamp(currentExperience, 0, ushort.MaxValue));

            _cachedLevels[index] = currentLevel;
            _cachedExperience[index] = currentExperience;

            var previousSnapshot = CreateSnapshot(startingLevel, startingExperience);
            var newSnapshot      = CreateSnapshot(currentLevel, currentExperience);

            OnProfessionChanged((ProfessionIndex)index, previousSnapshot, newSnapshot);

            int gainedExperience = CalculateTotalExperience(currentLevel, currentExperience) - CalculateTotalExperience(startingLevel, startingExperience);
            if (gainedExperience > 0)
            {
                ExperienceGained?.Invoke((ProfessionIndex)index, gainedExperience, previousSnapshot, newSnapshot);
            }
        }

        internal PlayerProfessionSaveData[] CreateSaveData()
        {
            EnsureCacheInitialized();

            var records = new PlayerProfessionSaveData[Count];

            for (int i = 0; i < Count; ++i)
            {
                records[i] = new PlayerProfessionSaveData
                {
                    ProfessionCode = GetCode(i),
                    Level = (byte)Mathf.Clamp(_cachedLevels[i], 0, byte.MaxValue),
                    Experience = _cachedExperience[i],
                };
            }

            return records;
        }

        internal void ApplySaveData(PlayerProfessionSaveData[] data)
        {
            if (HasStateAuthority == false)
                return;

            if (data == null || data.Length == 0)
                return;

            EnsureCacheInitialized();

            for (int i = 0; i < data.Length; ++i)
            {
                var entry = data[i];
                if (string.IsNullOrEmpty(entry.ProfessionCode) == true)
                    continue;

                if (TryGetIndex(entry.ProfessionCode, out int index) == false)
                    continue;

                int level = Mathf.Clamp(entry.Level, MinLevel, MaxLevel);
                int experience = Mathf.Max(0, entry.Experience);

                SetProfessionLevel(index, level, experience);
            }
        }

        public override void CopyBackingFieldsToState(bool firstTime)
        {
            base.CopyBackingFieldsToState(firstTime);

            InvokeWeavedCode();

            for (int i = 0; i < Count; ++i)
            {
                int level = MinLevel;

                if (_initialLevels != null && i < _initialLevels.Length)
                {
                    level = Mathf.Clamp(_initialLevels[i], MinLevel, MaxLevel);
                }

                _levels.Set(i, (byte)level);

                int experience = 0;
                if (level < MaxLevel)
                {
                    experience = Mathf.Clamp(experience, 0, GetExperienceRequiredForNextLevel(level));
                }

                _experience.Set(i, (ushort)experience);
            }

            _cacheInitialized = false;
        }

        public override void Spawned()
        {
            base.Spawned();

            _isSpawned = true;
            _cacheInitialized = false;
            EnsureCacheInitialized();

            Global.PlayerCloudSaveService?.RegisterProfessionsAndRestore(this);
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

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _isSpawned = false;
            Global.PlayerCloudSaveService?.UnregisterProfessions(this);

            base.Despawned(runner, hasState);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_initialLevels == null || _initialLevels.Length != Count)
            {
                var values = new int[Count];
                int length = _initialLevels != null ? Math.Min(_initialLevels.Length, Count) : 0;

                for (int i = 0; i < length; ++i)
                {
                    values[i] = Mathf.Clamp(_initialLevels[i], MinLevel, MaxLevel);
                }

                for (int i = length; i < Count; ++i)
                {
                    values[i] = MinLevel;
                }

                _initialLevels = values;
            }
            else
            {
                for (int i = 0; i < _initialLevels.Length; ++i)
                {
                    _initialLevels[i] = Mathf.Clamp(_initialLevels[i], MinLevel, MaxLevel);
                }
            }
        }
#endif

        public static int GetExperienceRequiredForNextLevel(int level)
        {
            if (level < MinLevel)
            {
                return BaseExperienceRequirement;
            }

            if (level >= MaxLevel)
            {
                return 0;
            }

            return BaseExperienceRequirement + (level - MinLevel) * ExperienceIncrementPerLevel;
        }

        private static int CalculateTotalExperience(int level, int experience)
        {
            level = Mathf.Clamp(level, 0, MaxLevel);

            if (level <= 0)
            {
                return Mathf.Clamp(experience, 0, GetExperienceRequiredForNextLevel(MinLevel));
            }

            int totalExperience = 0;

            for (int currentLevel = MinLevel; currentLevel < level; ++currentLevel)
            {
                totalExperience += Mathf.Max(0, GetExperienceRequiredForNextLevel(currentLevel));
            }

            if (level < MaxLevel)
            {
                totalExperience += Mathf.Clamp(experience, 0, GetExperienceRequiredForNextLevel(level));
            }

            return totalExperience;
        }

        private ProfessionSnapshot CreateSnapshot(int level, int experience)
        {
            int clampedLevel = Mathf.Clamp(level, 0, MaxLevel);
            int experienceToNextLevel = GetExperienceRequiredForNextLevel(clampedLevel);

            int clampedExperience = experienceToNextLevel > 0 ? Mathf.Clamp(experience, 0, experienceToNextLevel) : 0;

            return new ProfessionSnapshot(clampedLevel, clampedExperience, experienceToNextLevel);
        }

        private int AdjustExperienceAmount(int baseAmount)
        {
            if (baseAmount <= 0)
            {
                return 0;
            }

            BuffSystem buffSystem = _agent != null ? _agent.BuffSystem : null;
            float multiplier = buffSystem != null ? buffSystem.GetExperienceMultiplier() : 1f;

            if (multiplier <= 0f)
            {
                return 0;
            }

            if (Mathf.Approximately(multiplier, 1f) == true)
            {
                return baseAmount;
            }

            return Mathf.Max(1, Mathf.CeilToInt(baseAmount * multiplier));
        }

        private void EnsureCacheInitialized()
        {
            if (_cachedLevels == null || _cachedLevels.Length != Count)
            {
                _cachedLevels = new int[Count];
                _cacheInitialized = false;
            }

            if (_cachedExperience == null || _cachedExperience.Length != Count)
            {
                _cachedExperience = new int[Count];
                _cacheInitialized = false;
            }

            if (_cacheInitialized == true)
            {
                return;
            }

            for (int i = 0; i < Count; ++i)
            {
                if (CanReadNetworkedData == true)
                {
                    _cachedLevels[i] = _levels.Get(i);
                    _cachedExperience[i] = _experience.Get(i);
                }
                else
                {
                    int level = _initialLevels != null && i < _initialLevels.Length
                        ? Mathf.Clamp(_initialLevels[i], MinLevel, MaxLevel)
                        : MinLevel;

                    _cachedLevels[i] = level;
                    _cachedExperience[i] = 0;
                }
            }

            _cacheInitialized = true;
        }

        private void CheckForProfessionUpdates()
        {
            if (Object == null || Object.IsValid == false)
            {
                return;
            }

            if (CanReadNetworkedData == false)
            {
                return;
            }

            EnsureCacheInitialized();

            for (int i = 0; i < Count; ++i)
            {
                int currentLevel = _levels.Get(i);
                int currentExperience = _experience.Get(i);
                int previousLevel = _cachedLevels[i];
                int previousExperience = _cachedExperience[i];

                if (currentLevel == previousLevel && currentExperience == previousExperience)
                {
                    continue;
                }

                var previousSnapshot = CreateSnapshot(previousLevel, previousExperience);
                var newSnapshot      = CreateSnapshot(currentLevel, currentExperience);

                _cachedLevels[i]     = currentLevel;
                _cachedExperience[i] = currentExperience;

                OnProfessionChanged((ProfessionIndex)i, previousSnapshot, newSnapshot);

                int gainedExperience = CalculateTotalExperience(currentLevel, currentExperience) - CalculateTotalExperience(previousLevel, previousExperience);
                if (gainedExperience > 0)
                {
                    ExperienceGained?.Invoke((ProfessionIndex)i, gainedExperience, previousSnapshot, newSnapshot);
                }
            }
        }

        private void OnProfessionChanged(ProfessionIndex profession, ProfessionSnapshot previousSnapshot, ProfessionSnapshot newSnapshot)
        {
            ProfessionChanged?.Invoke(profession, previousSnapshot, newSnapshot);
        }

        public void OnSpawned(Agent agent)
        {
            
        }

        public void OnDespawned()
        {
            
        }
    }
}