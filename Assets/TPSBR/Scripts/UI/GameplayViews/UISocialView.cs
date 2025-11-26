using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;

namespace TPSBR.UI
{
    public class UISocialView : UIView
    {
        // PRIVATE MEMBERS

        [SerializeField]
        private RectTransform _friendListRoot;
        [SerializeField]
        private UISocialFriendEntry _friendEntryPrefab;
        [SerializeField]
        private TextMeshProUGUI _statusLabel;

        private readonly List<UISocialFriendEntry> _spawnedEntries = new List<UISocialFriendEntry>();
        private Callback<GameRichPresenceJoinRequested_t> _richPresenceInviteCallback;
        private Callback<GameLobbyJoinRequested_t> _lobbyInviteCallback;

        // UIView INTERFACE

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _richPresenceInviteCallback = Callback<GameRichPresenceJoinRequested_t>.Create(OnSteamInviteReceived);
            _lobbyInviteCallback = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyInviteReceived);
        }

        protected override void OnDeinitialize()
        {
            _richPresenceInviteCallback?.Dispose();
            _lobbyInviteCallback?.Dispose();

            base.OnDeinitialize();
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            RefreshFriends();
        }

        // PRIVATE METHODS

        private void RefreshFriends()
        {
            ClearEntries();

            if (SteamAPI.IsSteamRunning() == false)
            {
                UpdateStatus("Steam is not initialized.");
                return;
            }

            int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            if (friendCount <= 0)
            {
                UpdateStatus("No friends available.");
                return;
            }

            UpdateStatus(string.Empty);

            for (int i = 0; i < friendCount; i++)
            {
                CSteamID friendId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                string friendName = SteamFriends.GetFriendPersonaName(friendId);

                UISocialFriendEntry entry = CreateEntry();
                entry.Setup(friendId, friendName, SendInvite);
            }
        }

        private UISocialFriendEntry CreateEntry()
        {
            UISocialFriendEntry entry = Instantiate(_friendEntryPrefab, _friendListRoot);
            _spawnedEntries.Add(entry);
            return entry;
        }

        private void ClearEntries()
        {
            for (int i = 0; i < _spawnedEntries.Count; i++)
            {
                Destroy(_spawnedEntries[i].gameObject);
            }

            _spawnedEntries.Clear();
        }

        private void SendInvite(CSteamID friendId)
        {
            if (SteamAPI.IsSteamRunning() == false)
            {
                Debug.LogWarning("Cannot send invite because Steam is not initialized.");
                return;
            }

            string connectionString = Context?.Runner?.SessionInfo != null ? Context.Runner.SessionInfo.Name : string.Empty;
            bool result = SteamFriends.InviteUserToGame(friendId, connectionString);

            if (result == true)
            {
                Debug.Log($"Sent Steam invite to {SteamFriends.GetFriendPersonaName(friendId)}.");
            }
            else
            {
                Debug.LogWarning($"Failed to send Steam invite to {SteamFriends.GetFriendPersonaName(friendId)}.");
            }
        }

        private void OnSteamInviteReceived(GameRichPresenceJoinRequested_t data)
        {
            Debug.Log($"Received Steam invite with connection info: {data.m_rgchConnect} from {data.m_steamIDFriend}.");
        }

        private void OnLobbyInviteReceived(GameLobbyJoinRequested_t data)
        {
            Debug.Log($"Received Steam lobby invite to lobby {data.m_steamIDLobby} from {data.m_steamIDFriend}.");
        }

        private void UpdateStatus(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = message;
            }
        }
    }
}
