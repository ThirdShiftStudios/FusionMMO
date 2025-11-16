using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        private Image _iconImage;

        public UIListItem Slot
        {
            get
            {
                if (_slot == null)
                {
                    _slot = GetComponent<UIListItem>();
                }

                return _slot;
            }
        }

        public void SetAbilityDetails(ArcaneConduit.AbilityOption option)
        {
            string abilityName = option.Definition != null ? option.Definition.Name : string.Empty;
            string abilityDescription = option.Definition != null ? option.Definition.AbilityDescription : string.Empty;
            Sprite abilityIcon = option.Definition != null ? option.Definition.Icon : null;

            UIExtensions.SetTextSafe(_nameLabel, abilityName);
            UIExtensions.SetTextSafe(_descriptionLabel, abilityDescription);

            if (_descriptionLabel != null)
            {
                _descriptionLabel.gameObject.SetActive(string.IsNullOrWhiteSpace(abilityDescription) == false);
            }

            if (_iconImage != null)
            {
                _iconImage.sprite = abilityIcon;
                _iconImage.enabled = abilityIcon != null;
            }
        }
    }
}
