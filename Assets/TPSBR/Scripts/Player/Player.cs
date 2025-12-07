namespace TPSBR
{
        using System.Collections.Generic;
        using UnityEngine;
        using Fusion;
        using Fusion.Addons.InterestManagement;

	public struct PlayerStatistics : INetworkStruct
	{
		public PlayerRef PlayerRef;
		public short     ExtraLives;
		public short     Kills;
		public short     Deaths;
		public short     Score;
		public TickTimer RespawnTimer;
		public byte      Position;

		public byte      KillsInRow;
		public TickTimer KillsInRowCooldown;
		public byte      KillsWithoutDeath;

		public bool      IsValid         => PlayerRef.IsRealPlayer;
		public bool      IsAlive         { get { return _flags.IsBitSet(0); } set { _flags.SetBit(0, value); } }
		public bool      IsEliminated    { get { return _flags.IsBitSet(1); } set { _flags.SetBit(1, value); } }

		private byte     _flags;
	}

	public sealed class Player : PlayerInterestManager, IPlayer, IContextBehaviour
	{
		// PUBLIC MEMBERS

		public string           UserID         { get; private set; }
		public string			UnityID        { get; private set; }
		public string           Nickname       { get; private set; }
		public string           CharacterName { get; private set; }
		public SceneContext     Context        { get; set; }
                public bool             IsInitialized => _initCounter <= 0;

		[Networked]
		public Agent            ActiveAgent    { get; private set; }
		[Networked]
		public NetworkPrefabRef AgentPrefab    { get; set; }
                [Networked]
                public PlayerStatistics Statistics     { get; private set; }
                [Networked]
                public int              CurrentLocationID { get; set; }

		// PRIVATE MEMBERS
		[Networked]
		private byte SyncToken { get; set; }
		[Networked]
		private NetworkString<_64> NetworkedUserID { get; set; }
		[Networked]
		private NetworkString<_32> NetworkedNickname { get; set; }
		[Networked]
		private NetworkString<_32> NetworkedCharacterName { get; set; }
		private byte      _syncToken;
                private Agent     _activeAgent;
                private PlayerRef _observePlayer;
                private Vector3   _lastViewPosition;
                private Vector3   _lastViewDirection;
                private Agent     _platformAgent;
                private bool      _playerDataSent;
                private int       _initCounter;
                private bool      _spawned;

		// PUBLIC METHODS

		public void SetActiveAgent(Agent agent)
		{
			ActiveAgent = agent;

			UpdateLocalState();
		}

		public void DespawnAgent()
		{
			if (Runner.IsServer == false)
				return;

			if (ActiveAgent != null && ActiveAgent.Object != null)
			{
				Runner.Despawn(ActiveAgent.Object);
				ActiveAgent = null;
				_activeAgent = null;
				_platformAgent = null;
			}
		}

		public void UpdateStatistics(PlayerStatistics statistics)
		{
			Statistics = statistics;
		}

		public void SetObservedPlayer(PlayerRef playerRef)
		{
			if (playerRef.IsRealPlayer == false)
			{
				playerRef = Object.InputAuthority;
			}

			if (playerRef == _observePlayer)
				return;

			RPC_SetObservedPlayer(playerRef);
		}

                public void Refresh()
                {
                        if (_spawned == false)
                                return;

                        PlayerStatistics statistics = Statistics;
                        statistics.PlayerRef = Object.InputAuthority;
                        Statistics = statistics;
                }

                public void GrantExperience(int amount, IExperienceGiver experienceGiver = null)
                {
                        if (amount <= 0)
                                return;

                        int adjustedAmount = GetExperienceAdjustedAmount(amount);
                        if (adjustedAmount <= 0)
                                return;

                        if (HasStateAuthority == true)
                        {
                                bool hasExperienceGiver = experienceGiver != null;
                                Vector3 experiencePosition = hasExperienceGiver == true ? experienceGiver.ExperiencePosition : Vector3.zero;
                                RPC_AddExperience(adjustedAmount, experiencePosition, hasExperienceGiver);
                        }
                        else if (HasInputAuthority == true && Context?.PlayerData != null)
                        {
                                Context.PlayerData.AddExperience(adjustedAmount, experienceGiver);
                        }
                }

                public void OnReconnect(Player newPlayer)
                {
                        UserID            = newPlayer.UserID;
                        Nickname          = newPlayer.Nickname;
                        NetworkedUserID   = newPlayer.NetworkedUserID;
                        NetworkedNickname = newPlayer.NetworkedNickname;
                        CharacterName     = newPlayer.CharacterName;
                        NetworkedCharacterName = newPlayer.NetworkedCharacterName;
                        AgentPrefab       = newPlayer.AgentPrefab;
                        UnityID           = newPlayer.UnityID;
                }

		// PlayerInterestManager INTERFACE

		private int GetExperienceAdjustedAmount(int baseAmount)
		{
			if (baseAmount <= 0)
				return 0;

			Agent activeAgent = ActiveAgent != null ? ActiveAgent : _activeAgent;
			BuffSystem buffSystem = activeAgent != null ? activeAgent.BuffSystem : null;
			float multiplier = buffSystem != null ? buffSystem.GetExperienceMultiplier() : 1f;

			if (multiplier <= 0f)
				return 0;

			if (Mathf.Approximately(multiplier, 1f) == true)
				return baseAmount;

			return Mathf.Max(1, Mathf.CeilToInt(baseAmount * multiplier));
		}

                protected override void OnSpawned()
                {
                        _spawned       = true;
                        _syncToken      = default;
                        _activeAgent    = ActiveAgent;
                        _observePlayer  = Object.InputAuthority;
                        _playerDataSent = false;
                        _initCounter    = 10;

			if (HasInputAuthority == true)
			{
				Context.LocalPlayerRef = Object.InputAuthority;
			}

			UpdateLocalState();

			Runner.SetIsSimulated(Object, true);
		}

                protected override void OnDespawned(NetworkRunner runner, bool hasState)
                {
                        _spawned = false;
                        DespawnAgent();
                }

		protected override void OnFixedUpdateNetwork()
		{
			UpdateLocalState();

			if (_syncToken != default && Runner.IsForward == true)
			{
				_initCounter = Mathf.Max(0, _initCounter - 1);
			}

			if (IsProxy == true)
				return;

			var observedAgent = ActiveAgent;

			var observedPlayer = Context.NetworkGame.GetPlayer(_observePlayer);
			if (observedPlayer != null && observedPlayer.ActiveAgent != null && observedPlayer.ActiveAgent.Object != null)
			{
				observedAgent = observedPlayer.ActiveAgent;
			}

			var observedPlayerRef = observedAgent != null ? observedAgent.Object.InputAuthority : Object.InputAuthority;

			if (HasStateAuthority == true)
			{
				ObservedPlayer = observedPlayerRef;
			}

			if (HasInputAuthority == true)
			{
				Context.ObservedAgent     = observedAgent;
				Context.ObservedPlayerRef = observedPlayerRef;
                                Context.LocalPlayerRef    = Object.InputAuthority;

                                if (_playerDataSent == false && Runner.IsForward == true && Context.PlayerData != null)
                                {
                                        List<string> missingFields = null;

                                        if (Context.PeerUserID == null)
                                                (missingFields ??= new List<string>()).Add(nameof(Context.PeerUserID));
                                        if (Context.PlayerData.Nickname == null)
                                                (missingFields ??= new List<string>()).Add(nameof(Context.PlayerData.Nickname));
                                        if (Context.PlayerData.CharacterName == null)
                                                (missingFields ??= new List<string>()).Add(nameof(Context.PlayerData.CharacterName));
                                        if (Context.PlayerData.UnityID == null)
                                                (missingFields ??= new List<string>()).Add(nameof(Context.PlayerData.UnityID));

                                        if (missingFields != null)
                                                Debug.LogError($"[Player] Missing player data fields: {string.Join(", ", missingFields)} for player {Object.InputAuthority}.");

                                        var userID        = Context.PeerUserID ?? string.Empty;
                                        var nickname      = Context.PlayerData.Nickname ?? string.Empty;
                                        var characterName = Context.PlayerData.CharacterName ?? string.Empty;
                                        var unityID       = Context.PlayerData.UnityID ?? string.Empty;

                                        RPC_SendPlayerData(userID, nickname, characterName, Context.PlayerData.AgentPrefab, unityID);
                                        _playerDataSent = true;
                                }
			}
		}

		public override bool CanUpdatePlayerInterest(bool isUpdateRequested)
		{
			if (isUpdateRequested == true)
				return true;

			int interlacing = 50;
			int currentTick = Runner.Tick;

			if ((currentTick - LastUpdateTick) >= interlacing)
			{
				if ((currentTick % interlacing) == (Player.AsIndex % interlacing))
					return true;
			}

			if (TryGetObservedPlayerView(out PlayerInterestView observedPlayerView) == true)
			{
				if (Vector3.SqrMagnitude(_lastViewPosition - observedPlayerView.CameraPosition) > 4.0f) // 2 meter distance change
					return true;
				if (Vector3.Dot(_lastViewDirection, observedPlayerView.CameraDirection) < 0.99f) // Roughly 8 degrees change
					return true;
			}

			return false;
		}

		protected override void AfterPlayerInterestUpdate(bool success, PlayerInterestView playerView)
		{
			if (success == true)
			{
				_lastViewPosition  = playerView.CameraPosition;
				_lastViewDirection = playerView.CameraDirection;
			}
		}

		// PRIVATE METHODS

		private void UpdateLocalState()
		{
			if (_activeAgent != ActiveAgent)
			{
				_activeAgent   = ActiveAgent;
				_observePlayer = Object.InputAuthority;

				if (_activeAgent != null)
				{
					InterestView = _activeAgent.InterestView;
				}
			}

                        if (_syncToken != SyncToken)
                        {
                                _syncToken = SyncToken;

                                UserID   = NetworkedUserID.Value;
                                Nickname = NetworkedNickname.Value;
                                CharacterName = NetworkedCharacterName.Value;
                        }

			if (ReferenceEquals(_platformAgent, _activeAgent) == false && _activeAgent != null)
			{
				if (_activeAgent.Character.CharacterController.IsSpawned == true)
				{
					BRPlatformProcessor platformProcessor = GetComponent<BRPlatformProcessor>();
					_activeAgent.Character.CharacterController.AddLocalProcessor(platformProcessor);
					_platformAgent = _activeAgent;
				}
			}
		}

		[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
		private void RPC_SendPlayerData(string userID, string nickname, string characterName, NetworkPrefabRef agentPrefab, string unityID)
		{
			#if UNITY_EDITOR
			nickname += $" {Object.InputAuthority}";
			#endif

			++SyncToken;
			if (SyncToken == default)
			{
				SyncToken = 1;
			}

			_syncToken = SyncToken;

			UserID = userID;
			Nickname = nickname;
			CharacterName = characterName;
			NetworkedCharacterName = characterName;
			NetworkedUserID = userID;
			NetworkedNickname = nickname;
			AgentPrefab = agentPrefab;
			UnityID = unityID;
		}

		[Rpc(RpcSources.StateAuthority | RpcSources.InputAuthority, RpcTargets.StateAuthority | RpcTargets.InputAuthority, Channel = RpcChannel.Reliable)]
                private void RPC_SetObservedPlayer(PlayerRef player)
                {
                        _observePlayer = player;
                }

                [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, Channel = RpcChannel.Reliable)]
                private void RPC_AddExperience(int amount, Vector3 experiencePosition, bool hasExperiencePosition)
                {
                        if (amount <= 0)
                                return;

                        if (Context?.PlayerData == null)
                                return;

                        IExperienceGiver experienceGiver = hasExperiencePosition == true ? new StaticExperienceGiver(experiencePosition) : null;
                        Context.PlayerData.AddExperience(amount, experienceGiver);
                }

                // DEBUG

                [ContextMenu("Debug/Add Experience")]
                private void Debug_AddExperience()
                {
                        if (Context?.PlayerData == null)
                                return;

                        const int debugExperienceAmount = 500;

                        IExperienceGiver experienceGiver = null;

                        if (_activeAgent != null)
                        {
                                experienceGiver = _activeAgent.Health as IExperienceGiver;
                        }

                        if (experienceGiver == null)
                        {
                                experienceGiver = new StaticExperienceGiver(transform.position);
                        }

                        Context.PlayerData.AddExperience(debugExperienceAmount, experienceGiver);

                        int experienceToNextLevel = Context.PlayerData.ExperienceToNextLevel;
                        if (experienceToNextLevel <= 0)
                        {
                                Debug.Log($"[Player Debug] Added {debugExperienceAmount} XP. Player is at max level {Context.PlayerData.Level}.");
                        }
                        else
                        {
                                Debug.Log($"[Player Debug] Added {debugExperienceAmount} XP. Level {Context.PlayerData.Level} ({Context.PlayerData.Experience}/{experienceToNextLevel}).");
                        }
                }

                [ContextMenu("Debug/Level Up")]
                private void Debug_LevelUp()
                {
                        if (Context?.PlayerData == null)
                                return;

                        if (Context.PlayerData.ForceLevelUp() == false)
                        {
                                Debug.Log($"[Player Debug] Player already at max level ({Context.PlayerData.Level}).");
                                return;
                        }

                        Debug.Log($"[Player Debug] Player leveled up to {Context.PlayerData.Level}.");
                }
        }
}
