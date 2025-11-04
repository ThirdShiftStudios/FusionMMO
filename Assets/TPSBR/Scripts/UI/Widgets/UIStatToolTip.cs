using System.Text;
using UnityEngine;

namespace TPSBR.UI
{
    public class UIStatToolTip : UIInventoryItemToolTip
    {
        private const string ValueFormat = "{0}";

        public void Show(string statCode, int statValue, Vector2 screenPosition)
        {
            if (string.IsNullOrWhiteSpace(statCode) == true)
            {
                Hide();
                return;
            }

            string title = BuildTitle(statCode);
            string description = BuildDescription(statValue);

            Show(title, description, screenPosition);
        }

        private static string BuildTitle(string statCode)
        {
            if (global::TPSBR.Stats.TryGetIndex(statCode, out int index) == true)
            {
                var name = ((global::TPSBR.Stats.StatIndex)index).ToString();
                return string.IsNullOrEmpty(name) ? statCode : $"{name} ({statCode.ToUpperInvariant()})";
            }

            return statCode.ToUpperInvariant();
        }

        private static string BuildDescription(int statValue)
        {
            var builder = new StringBuilder();
            builder.Append("Value: ");
            builder.AppendFormat(ValueFormat, Mathf.Max(0, statValue));
            return builder.ToString();
        }
    }
}
