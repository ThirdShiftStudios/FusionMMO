using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIPlayer : UIBehaviour
    {
        // PRIVATE MEMBERS

        [SerializeField] private TextMeshProUGUI _playerName;
        [SerializeField] private TextMeshProUGUI _playerLevel;

        // PUBLIC MEMBERS

        public void SetData(SceneContext context, IPlayer player)
        {
            string displayName = player.CharacterName;

            if (string.IsNullOrEmpty(displayName) == true)
            {
                displayName = player.Nickname;
            }

            _playerName.text = displayName;

            UpdateLevel(context, player);
        }

        public void SetLevel(int level)
        {
            if (_playerLevel == null)
                return;

            string levelText = level > 0 ? level.ToString() : string.Empty;
            _playerLevel.SetTextSafe(levelText);
        }

        private void UpdateLevel(SceneContext context, IPlayer player)
        {
            if (_playerLevel == null)
                return;

            int? level = ResolvePlayerLevel(context, player);

            if (level.HasValue == true)
            {
                _playerLevel.SetTextSafe(level.Value.ToString());
            }
            else
            {
                _playerLevel.SetTextSafe(string.Empty);
            }
        }

        private static int? ResolvePlayerLevel(SceneContext context, IPlayer player)
        {
            if (player == null)
                return null;

            if (player is PlayerData playerData)
                return playerData.Level;

            if (context?.PlayerData != null && IsSamePlayer(context.PlayerData, player))
                return context.PlayerData.Level;

            if (player is Player runtimePlayer)
            {
                var runtimePlayerData = runtimePlayer.Context?.PlayerData;
                if (runtimePlayerData != null && IsSamePlayer(runtimePlayerData, player))
                    return runtimePlayerData.Level;
            }

            return null;
        }

        private static bool IsSamePlayer(IPlayer first, IPlayer second)
        {
            if (first == null || second == null)
                return false;

            if (string.IsNullOrEmpty(first.UnityID) == false && string.IsNullOrEmpty(second.UnityID) == false)
            {
                if (string.Equals(first.UnityID, second.UnityID, StringComparison.Ordinal) == true)
                    return true;
            }

            if (string.IsNullOrEmpty(first.UserID) == false && string.IsNullOrEmpty(second.UserID) == false)
            {
                if (string.Equals(first.UserID, second.UserID, StringComparison.Ordinal) == true)
                    return true;
            }

            return false;
        }
    }
}
