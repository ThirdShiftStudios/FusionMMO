using UnityEngine;

namespace TPSBR.UI
{
    using ArcaneConduit = TPSBR.ArcaneConduit;

    public class UIAbilityToolTip : UIInventoryItemToolTip
    {
        public void Show(ArcaneConduit.AbilityOption option, Vector2 screenPosition)
        {
            string title = option.Definition != null ? option.Definition.Name : string.Empty;
            string description = option.Definition != null ? option.Definition.AbilityDescription : string.Empty;
            Sprite icon = option.Definition != null ? option.Definition.Icon : null;
            string status = FormatStatus(option);

            if (string.IsNullOrWhiteSpace(status) == false)
            {
                description = string.IsNullOrWhiteSpace(description) == true
                    ? status
                    : $"{description}\n\n{status}";
            }

            base.Show(title, description, icon, screenPosition);
        }

        private static string FormatStatus(ArcaneConduit.AbilityOption option)
        {
            if (option.IsUnlocked == true)
            {
                int maxLevel = Mathf.Max(1, option.MaxLevel);
                int currentLevel = Mathf.Clamp(option.CurrentLevel, 1, maxLevel);
                string status = maxLevel > 1 ? $"Level {currentLevel}/{maxLevel}" : "Unlocked";

                if (maxLevel > 1)
                {
                    if (currentLevel >= maxLevel)
                    {
                        status = $"{status} (Max)";
                    }
                    else if (option.LevelUpCost > 0)
                    {
                        status = $"{status} (Level Up: {option.LevelUpCost})";
                    }
                }

                return status;
            }

            if (option.CanPurchase == true)
                return $"Cost: {option.Cost}";

            return "Unavailable";
        }
    }
}
