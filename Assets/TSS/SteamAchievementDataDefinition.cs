using System;
using Steamworks;
using TSS.Data;
using UnityEngine;

namespace TPSBR
{
    [CreateAssetMenu(fileName = "SteamAchievementDataDefinition", menuName = "TSS/Data Definitions/Steam Achievement")]
    public class SteamAchievementDataDefinition : DataDefinition
    {
        [Header("Display")]
        [SerializeField]
        private string _displayName;
        [SerializeField, TextArea]
        private string _description;
        [SerializeField]
        private Texture2D _icon;

        [Header("Steam")]
        [SerializeField]
        private string _achievementId;

        public override string Name => _displayName;
        public override Texture2D Icon => _icon;

        public string AchievementId => _achievementId;
        public string Description => _description;

        public bool GrantAchievement()
        {
#if !(UNITY_STANDALONE || UNITY_EDITOR)
            Debug.LogWarning($"[{nameof(SteamAchievementDataDefinition)}] Steam achievements are not supported on this platform.");
            return false;
#else
            if (string.IsNullOrEmpty(_achievementId) == true)
            {
                Debug.LogWarning($"[{nameof(SteamAchievementDataDefinition)}] Unable to grant achievement for '{name}' because the achievement ID is not set.");
                return false;
            }

            try
            {
                if (SteamAPI.IsSteamRunning() == false)
                {
                    Debug.LogWarning($"[{nameof(SteamAchievementDataDefinition)}] Unable to grant achievement '{_achievementId}' because Steam is not running.");
                    return false;
                }

                if (SteamUserStats.RequestCurrentStats() == false)
                {
                    Debug.LogWarning($"[{nameof(SteamAchievementDataDefinition)}] Unable to request current stats before granting achievement '{_achievementId}'.");
                    return false;
                }

                if (SteamUserStats.GetAchievement(_achievementId, out var achieved) == false)
                {
                    Debug.LogWarning($"[{nameof(SteamAchievementDataDefinition)}] Steam does not recognize the achievement identifier '{_achievementId}'.");
                    return false;
                }

                if (achieved == true)
                {
                    return true;
                }

                if (SteamUserStats.SetAchievement(_achievementId) == false)
                {
                    Debug.LogWarning($"[{nameof(SteamAchievementDataDefinition)}] Failed to set achievement '{_achievementId}'.");
                    return false;
                }

                if (SteamUserStats.StoreStats() == false)
                {
                    Debug.LogWarning($"[{nameof(SteamAchievementDataDefinition)}] Failed to store stats after granting achievement '{_achievementId}'.");
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(SteamAchievementDataDefinition)}] Exception thrown while granting achievement '{_achievementId}': {exception}");
                return false;
            }
#endif
        }
    }
}
