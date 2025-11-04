using System.Text;
using UnityEngine;

namespace TPSBR.UI
{
    public class UIProfessionToolTip : UIInventoryItemToolTip
    {
        public void Show(string professionCode, global::TPSBR.Professions.ProfessionSnapshot snapshot, Vector2 screenPosition)
        {
            if (string.IsNullOrWhiteSpace(professionCode) == true)
            {
                Hide();
                return;
            }

            string title = BuildTitle(professionCode, snapshot);
            string description = BuildDescription(snapshot);

            Show(title, description, screenPosition);
        }

        private static string BuildTitle(string professionCode, global::TPSBR.Professions.ProfessionSnapshot snapshot)
        {
            if (global::TPSBR.Professions.TryGetIndex(professionCode, out int index) == true)
            {
                var name = ((global::TPSBR.Professions.ProfessionIndex)index).ToString();
                string suffix = snapshot.Level > 0 ? $" - Level {snapshot.Level}" : string.Empty;
                return string.IsNullOrEmpty(name) ? professionCode : $"{name} ({professionCode.ToUpperInvariant()}){suffix}";
            }

            return snapshot.Level > 0 ? $"{professionCode.ToUpperInvariant()} - Level {snapshot.Level}" : professionCode.ToUpperInvariant();
        }

        private static string BuildDescription(global::TPSBR.Professions.ProfessionSnapshot snapshot)
        {
            var builder = new StringBuilder();

            if (snapshot.Level > 0 || snapshot.Experience > 0 || snapshot.ExperienceToNextLevel > 0)
            {
                builder.AppendLine($"Experience: {snapshot.Experience} / {Mathf.Max(1, snapshot.ExperienceToNextLevel)}");

                if (snapshot.ExperienceRemaining > 0)
                {
                    builder.AppendLine($"Remaining: {snapshot.ExperienceRemaining}");
                }
                else if (snapshot.ExperienceToNextLevel == 0 && snapshot.Level > 0)
                {
                    builder.AppendLine("Max level reached");
                }

                float progress = snapshot.Progress;
                builder.Append($"Progress: {(progress * 100f):0.#}%");
            }
            else
            {
                builder.Append("No experience recorded");
            }

            return builder.ToString();
        }
    }
}
