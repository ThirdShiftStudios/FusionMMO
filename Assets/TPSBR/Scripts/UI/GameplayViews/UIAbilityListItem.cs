using TMPro;
using UnityEngine;

namespace TPSBR.UI
{
    public sealed class UIAbilityListItem : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _nameLabel;
        [SerializeField]
        private TextMeshProUGUI _descriptionLabel;
        [SerializeField]
        private TextMeshProUGUI _detailLabel;

        public void SetAbility(ArcaneConduit.AbilityOption option, bool showCost)
        {
            if (_nameLabel != null)
            {
                string abilityName = option.Definition != null ? option.Definition.Name : string.Empty;
                UIExtensions.SetTextSafe(_nameLabel, abilityName);
            }

            if (_descriptionLabel != null)
            {
                string description = option.Definition != null ? option.Definition.AbilityDescription : string.Empty;
                bool hasDescription = string.IsNullOrWhiteSpace(description) == false;
                _descriptionLabel.gameObject.SetActive(hasDescription);
                UIExtensions.SetTextSafe(_descriptionLabel, description);
            }

            if (_detailLabel == null)
                return;

            string detailText = string.Empty;

            if (option.IsUnlocked == true)
            {
                detailText = "Unlocked";
            }
            else if (showCost == true)
            {
                if (option.CanPurchase == true)
                {
                    detailText = $"Cost: {option.Cost}";
                }
                else
                {
                    detailText = "Unavailable";
                }
            }

            bool hasDetail = string.IsNullOrEmpty(detailText) == false;
            _detailLabel.gameObject.SetActive(hasDetail);
            UIExtensions.SetTextSafe(_detailLabel, detailText);
        }
    }
}
