using TMPro;
using UnityEngine;

namespace TPSBR.UI
{
    public sealed class UIAbilityOptionSlot : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _nameLabel;
        [SerializeField]
        private TextMeshProUGUI _detailLabel;

        public void SetAbility(ArcaneConduit.AbilityOption option, bool showCost)
        {
            string abilityName = option.Definition != null ? option.Definition.Name : string.Empty;
            UIExtensions.SetTextSafe(_nameLabel, abilityName);

            if (_detailLabel == null)
                return;

            string detailText;

            if (option.IsUnlocked == true)
            {
                if (option.MaxLevel > 1)
                {
                    int maxLevel = Mathf.Max(1, option.MaxLevel);
                    int currentLevel = Mathf.Clamp(option.CurrentLevel, 1, maxLevel);
                    detailText = currentLevel >= maxLevel
                        ? $"Level {currentLevel}/{maxLevel} (Max)"
                        : $"Level {currentLevel}/{maxLevel}";
                }
                else
                {
                    detailText = "Unlocked";
                }
            }
            else if (showCost == false)
            {
                detailText = string.Empty;
            }
            else if (option.CanPurchase == true)
            {
                detailText = $"Cost: {option.Cost}";
            }
            else
            {
                detailText = "Unavailable";
            }

            UIExtensions.SetTextSafe(_detailLabel, detailText);
            _detailLabel.gameObject.SetActive(string.IsNullOrEmpty(detailText) == false);
        }
    }
}
