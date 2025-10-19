using System;
using System.Diagnostics;
using Fusion;
using TSS.Data;
using UnityEngine;

namespace TPSBR
{
	public interface IPlayer
	{
		string           UserID      { get; }
		string           Nickname    { get; }
		NetworkPrefabRef AgentPrefab { get; }
		string           CharacterName { get; }
		string			 UnityID     { get; }
	}

        [Serializable]
        public class PlayerData : IPlayer
        {
                // PUBLIC MEMBERS

                public string           UserID          => _userID;
                public string           UnityID         { get => _unityID; set => _unityID = value; }
                public NetworkPrefabRef AgentPrefab     => GetAgentPrefab();
                public string           Nickname        { get { return _nickname; } set { _nickname = value; IsDirty = true; } }
                public string           AgentID         { get { return _agentID; } set { _agentID = value; IsDirty = true; } }
                public string           ActiveCharacterId { get { return _activeCharacterId; } set { if (_activeCharacterId == value) return; _activeCharacterId = value; IsDirty = true; } }
                public string           ActiveCharacterName { get { return _activeCharacterName; } set { if (_activeCharacterName == value) return; _activeCharacterName = value; IsDirty = true; } }
                public string           ActiveCharacterDefinitionCode { get { return _activeCharacterDefinitionCode; } set { if (_activeCharacterDefinitionCode == value) return; _activeCharacterDefinitionCode = value; IsDirty = true; } }
                public string           CharacterName   => ActiveCharacterName.HasValue() == true ? ActiveCharacterName : Nickname;

                public int              Level           => _level;
                public int              Experience      => _experience;
                public int              ExperienceToNextLevel => GetExperienceRequiredForNextLevel();
                public int              MaxLevel        => MAX_LEVEL;
                public bool             IsMaxLevel      => _level >= MAX_LEVEL;

                public bool             IsDirty         { get; private set; }

                public event Action<int> ExperienceAdded;

                // PRIVATE MEMBERS

                private const int MAX_LEVEL                    = 100;
                private const int BASE_EXPERIENCE_PER_LEVEL     = 100;
                private const float EXPERIENCE_GROWTH_EXPONENT  = 1.5f;

                [SerializeField]
                private string _userID;
                [SerializeField]
                private string _unityID;
                [SerializeField]
                private string _nickname;
                [SerializeField]
                private string _agentID;
                [SerializeField]
                private string _activeCharacterId;
                [SerializeField]
                private string _activeCharacterName;
                [SerializeField]
                private string _activeCharacterDefinitionCode;

                [SerializeField]
                private int _level = 1;
                [SerializeField]
                private int _experience;

                [SerializeField]
                private bool _isLocked;
                [SerializeField]
                private int _lastProcessID;

                // CONSTRUCTORS

                public PlayerData(string userID)
                {
                        _userID = userID;

                        EnsureProgressInitialized();
                }

                // PUBLIC METHODS

                public void ClearDirty()
                {
                        IsDirty = false;
                }

                public bool AddExperience(int amount)
                {
                        if (amount <= 0 || _level >= MAX_LEVEL)
                                return false;

                        int previousLevel = _level;
                        int previousExperience = _experience;
                        bool leveledUp = false;

                        _experience = Mathf.Max(0, _experience + amount);

                        while (_level < MAX_LEVEL)
                        {
                                int experienceRequired = GetExperienceRequiredForNextLevel();

                                if (_experience < experienceRequired)
                                        break;

                                _experience -= experienceRequired;
                                _level++;
                                leveledUp = true;
                        }

                        if (_level >= MAX_LEVEL)
                        {
                                _level = MAX_LEVEL;
                                _experience = 0;
                        }

                        if (_level != previousLevel || _experience != previousExperience)
                        {
                                IsDirty = true;
                        }

                        if (_level != previousLevel || _experience != previousExperience)
                        {
                                ExperienceAdded?.Invoke(amount);
                        }

                        return leveledUp;
                }

                public bool TryLevelUp()
                {
                        if (_level >= MAX_LEVEL)
                                return false;

                        int experienceRequired = GetExperienceRequiredForNextLevel();

                        if (_experience < experienceRequired)
                                return false;

                        _experience -= experienceRequired;
                        _level++;
                        IsDirty = true;

                        if (_level >= MAX_LEVEL)
                        {
                                _level = MAX_LEVEL;
                                _experience = 0;
                        }

                        return true;
                }

                public bool ForceLevelUp()
                {
                        if (_level >= MAX_LEVEL)
                                return false;

                        _level++;
                        _experience = 0;
                        IsDirty = true;
                        return true;
                }

                public void EnsureProgressInitialized()
                {
                        bool wasDirty = IsDirty;
                        bool progressAdjusted = false;

                        if (_level < 1)
                        {
                                _level = 1;
                                progressAdjusted = true;
                        }

                        if (_level > MAX_LEVEL)
                        {
                                _level = MAX_LEVEL;
                                progressAdjusted = true;
                        }

                        if (_experience < 0)
                        {
                                _experience = 0;
                                progressAdjusted = true;
                        }

                        if (_level >= MAX_LEVEL)
                        {
                                if (_experience != 0)
                                {
                                        _experience = 0;
                                        progressAdjusted = true;
                                }
                        }
                        else
                        {
                                int experienceRequired = GetExperienceRequiredForNextLevel();

                                if (_experience >= experienceRequired)
                                {
                                        _experience = Mathf.Clamp(_experience, 0, Mathf.Max(0, experienceRequired - 1));
                                        progressAdjusted = true;
                                }
                        }

                        if (progressAdjusted == true && wasDirty == false)
                        {
                                IsDirty = true;
                        }
                }

                public bool IsLocked(bool checkProcess = true)
                {
                        if (_isLocked == false)
                                return false;

			if (checkProcess == true)
			{
				try
				{
					var process = Process.GetProcessById(_lastProcessID);
				}
				catch (Exception)
				{
					// Process not running
					return false;
				}
			}

			return true;
		}

		public void Lock()
		{
			// When running multiple instances of the game on same machine we want to lock used player data

			_isLocked = true;
			_lastProcessID = Process.GetCurrentProcess().Id;
		}

		public void Unlock()
		{
			_isLocked = false;
		}

		// PRIVATE METHODS

                public string GetCharacterClassName()
                { 
                        var definition = CharacterDefinition.GetByStringCode(_activeCharacterDefinitionCode);
                        if (definition)
                        {
                                return definition.Name;
                        }

                        return "ERROR";
                }
                private NetworkPrefabRef GetAgentPrefab()
                {
                        if (_activeCharacterDefinitionCode.HasValue() == true)
                        {
                                var definition = CharacterDefinition.GetByStringCode(_activeCharacterDefinitionCode);
                                if (definition != null)
                                {
                                        var prefab = definition.AgentPrefab;
                                        if (prefab.IsValid == true)
                                        {
                                                return prefab;
                                        }
                                }
                        }

                        if (_agentID.HasValue() == false)
                        {
                                var fallbackSetup = Global.Settings.Agent.GetRandomAgentSetup();
                                return fallbackSetup != null ? fallbackSetup.AgentPrefab : default;
                        }

			var setup = Global.Settings.Agent.GetAgentSetup(_agentID);
			if (setup != null)
			{
				return setup.AgentPrefab;
			}

                        UnityEngine.Debug.LogWarning("AGENT SETUP NOT FOUND : Returnig random setup");
                        return Global.Settings.Agent.GetRandomAgentSetup().AgentPrefab;
                }

                private int GetExperienceRequiredForNextLevel()
                {
                        if (_level >= MAX_LEVEL)
                                return 0;

                        return Mathf.RoundToInt(BASE_EXPERIENCE_PER_LEVEL * Mathf.Pow(_level, EXPERIENCE_GROWTH_EXPONENT));
                }
        }
}
