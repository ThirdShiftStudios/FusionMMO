using UnityEngine;

namespace TPSBR.UI
{
    public class UIStatToolTip : UIInventoryItemToolTip
    {
        public void Show(Stats.StatIndex statIndex, string statCode, int statValue, Vector2 screenPosition)
        {
            string statName = statIndex.ToString();
            string title = FormatTitle(statName, statCode);
            string description = $"Current Value: {statValue}";

            base.Show(title, description, screenPosition);
        }

        private static string FormatTitle(string name, string code)
        {
            if (string.IsNullOrEmpty(name))
            {
                return code ?? string.Empty;
            }

            if (string.IsNullOrEmpty(code))
            {
                return name;
            }

            return $"{name} ({code})";
        }
    }
}
