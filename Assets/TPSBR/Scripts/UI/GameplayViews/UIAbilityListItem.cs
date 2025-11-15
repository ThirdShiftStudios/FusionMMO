using TMPro;
using UnityEngine;

namespace TPSBR.UI
{
    public sealed class UIAbilityListItem : MonoBehaviour
    {
        [SerializeField]
        private UIListItem _slot;
        [SerializeField]
        private TextMeshProUGUI _nameLabel;
        [SerializeField]
        private TextMeshProUGUI _descriptionLabel;
        [SerializeField]
        private TextMeshProUGUI _detailLabel;

        public UIListItem Slot => _slot;

        public void InitializeSlot(IUIListItemOwner owner, int index)
        {
            _slot?.InitializeSlot(owner, index);
        }

        public void SetAbility(ArcaneConduit.AbilityOption option, bool showCost)
        {
            if (_slot != null)
            {
                Sprite abilityIcon = option.Definition != null ? option.Definition.Icon : null;
                _slot.SetItem(abilityIcon, abilityIcon != null ? 1 : 0);
            }

            string abilityName = option.Definition != null ? option.Definition.Name : string.Empty;
            UIExtensions.SetTextSafe(_nameLabel, abilityName);

            string description = option.Definition != null ? option.Definition.Description : string.Empty;
            UIExtensions.SetTextSafe(_descriptionLabel, description);
            if (_descriptionLabel != null)
            {
                _descriptionLabel.gameObject.SetActive(string.IsNullOrWhiteSpace(description) == false);
            }

            if (_detailLabel == null)
                return;

            string detailText;

            if (option.IsUnlocked == true)
            {
                detailText = "Unlocked";
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
