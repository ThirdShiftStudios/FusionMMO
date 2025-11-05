using System.Text;
using UnityEngine;

namespace TPSBR.UI
{
    public class UIProfessionToolTip : UIInventoryItemToolTip
    {
        public void Show(Professions.ProfessionIndex professionIndex, string professionCode, Professions.ProfessionSnapshot snapshot, Vector2 screenPosition)
        {
            string professionName = professionIndex.ToString();
            string title = FormatTitle(professionName, professionCode);
            string description = BuildDescription(snapshot);

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

        private static string BuildDescription(Professions.ProfessionSnapshot snapshot)
        {
            if (snapshot.Level <= 0 && snapshot.Experience <= 0 && snapshot.ExperienceToNextLevel <= 0)
            {
                return "No progress yet.";
            }

            var builder = new StringBuilder();
            builder.Append("Level: ");
            builder.Append(snapshot.Level);

            if (snapshot.ExperienceToNextLevel > 0)
            {
                builder.Append('\n');
                builder.Append("XP: ");
                builder.Append(snapshot.Experience);
                builder.Append(" / ");
                builder.Append(snapshot.ExperienceToNextLevel);
            }
            else if (snapshot.Experience > 0)
            {
                builder.Append('\n');
                builder.Append("XP: ");
                builder.Append(snapshot.Experience);
            }

            return builder.ToString();
        }
    }
}
