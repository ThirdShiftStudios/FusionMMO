using System;
using UnityEngine;

namespace TPSBR
{
        public class PlayerService : IGlobalService
        {
                // PUBLIC MEMBERS

                public Action<PlayerData> PlayerDataChanged;

                public PlayerData PlayerData
                {
                        get
                        {
                                if (_playerData == null)
                                {
                                        _playerData = LoadPlayer();
                                }

                                return _playerData;
                        }
                        private set => _playerData = value;
                }
                public bool       IsInitialized { get; private set; }

                // IGlobalService INTERFACE

                void IGlobalService.Initialize()
                {
                        var playerData = PlayerData;

                        try
                        {
                                playerData.UnityID = Global.PlayerAuthenticationService.GetPlayerId();
                        }
                        catch (Exception exception)
                        {
                                playerData.UnityID = default;
                                Debug.LogException(exception);
                                Debug.LogWarning("Exception raised when authenticating player with Steam.");
                        }

                        playerData.Lock();
                        SavePlayer();

                        IsInitialized = true;
                }

                void IGlobalService.Tick()
                {
                        if (PlayerData.IsDirty == true)
                        {
                                SavePlayer();
                                PlayerData.ClearDirty();

                                PlayerDataChanged?.Invoke(PlayerData);
                        }
                }

                void IGlobalService.Deinitialize()
                {
                        if (_playerData != null)
                        {
                                _playerData.Unlock();
                        }

                        SavePlayer();

                        PlayerDataChanged = null;
                        IsInitialized = false;
                }

                // PRIVATE METHODS

                private PlayerData LoadPlayer()
                {
                        var baseUserID = GetUserID();
                        var userID = baseUserID;

                        var playerData = PersistentStorage.GetObject<PlayerData>($"PlayerData-{userID}");

                        if (Application.isMobilePlatform == false || Application.isEditor == true)
                        {
                                int clientIndex = 1;
                                while (playerData != null && playerData.IsLocked() == true)
                                {
                                        // We are probably running multiple clients, let's create unique player data for each one

                                        userID = $"{baseUserID}.{clientIndex}";
                                        playerData = PersistentStorage.GetObject<PlayerData>($"PlayerData-{userID}");

                                        clientIndex++;
                                }
                        }

                        if (playerData == null)
                        {
                                playerData = new PlayerData(userID);
                                playerData.AgentID = Global.Settings.Agent.GetRandomAgentSetup().ID;
                        };

                        return playerData;
                }

                private void SavePlayer()
                {
                        if (_playerData == null)
                                return;

                        PersistentStorage.SetObject($"PlayerData-{_playerData.UserID}", _playerData, true);
                }

                private string GetUserID()
                {
                        var userID = SystemInfo.deviceUniqueIdentifier;

                        if (ApplicationSettings.UseRandomDeviceID == true)
                        {
                                userID = Guid.NewGuid().ToString();
                        }
                        if (ApplicationSettings.HasCustomDeviceID == true)
                        {
                                userID = ApplicationSettings.CustomDeviceID;
                        }

#if UNITY_EDITOR
                        userID = $"{userID}_{Application.dataPath.GetHashCode()}";
#endif

                        return userID;
                }

                // PRIVATE MEMBERS

                private PlayerData _playerData;
        }
}
