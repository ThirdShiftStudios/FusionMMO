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
                return "Unlocked";

            if (option.CanPurchase == true)
                return $"Cost: {option.Cost}";

            return "Unavailable";
        }
    }
}
