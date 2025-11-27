using System;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UISocialFriendEntry : MonoBehaviour
    {
        // PRIVATE MEMBERS

        [SerializeField]
        private TextMeshProUGUI _nameLabel;
        [SerializeField]
        private Button _inviteButton;

        private CSteamID _friendId;
        private Action<CSteamID> _onInvite;

        // PUBLIC METHODS

        public void Setup(CSteamID friendId, string displayName, Action<CSteamID> onInvite)
        {
            _friendId = friendId;
            _onInvite = onInvite;

            if (_nameLabel != null)
            {
                _nameLabel.text = displayName;
            }

            if (_inviteButton != null)
            {
                _inviteButton.onClick.RemoveListener(HandleInviteClicked);
                _inviteButton.onClick.AddListener(HandleInviteClicked);
            }
        }

        // PRIVATE METHODS

        private void OnDestroy()
        {
            if (_inviteButton != null)
            {
                _inviteButton.onClick.RemoveListener(HandleInviteClicked);
            }
        }

        private void HandleInviteClicked()
        {
            _onInvite?.Invoke(_friendId);
        }
    }
}
