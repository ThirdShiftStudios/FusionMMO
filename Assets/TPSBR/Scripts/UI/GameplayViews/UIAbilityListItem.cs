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
        [SerializeField]
        private TextMeshProUGUI _statusLabel;
        [SerializeField]
        private UIButton _unlockButton;
        [SerializeField]
        private UIButton _levelUpButton;

        private UIInventorySlotListItem _inventorySlot;
        private ArcaneConduit.AbilityOption _currentOption;
        private bool _hasOption;

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

        private void Awake()
        {
            if (_unlockButton != null)
            {
                _unlockButton.onClick.AddListener(HandleUnlockClicked);
            }

            if (_levelUpButton != null)
            {
                _levelUpButton.onClick.AddListener(HandleLevelUpClicked);
            }
        }

        private void OnDestroy()
        {
            if (_unlockButton != null)
            {
                _unlockButton.onClick.RemoveListener(HandleUnlockClicked);
            }

            if (_levelUpButton != null)
            {
                _levelUpButton.onClick.RemoveListener(HandleLevelUpClicked);
            }
        }

        public void SetAbilityDetails(ArcaneConduit.AbilityOption option)
        {
            _currentOption = option;
            _hasOption = true;
            bool unlockedState = option.IsUnlocked;

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

            if (_inventorySlot == null)
            {
                _inventorySlot = Slot;
            }

            _inventorySlot?.SetItemMetadata(abilityIcon, abilityIcon != null ? 1 : 0);

            if (_statusLabel != null)
            {
                string statusText;
                if (unlockedState == true)
                {
                    int maxLevel = Mathf.Max(1, option.MaxLevel);
                    int currentLevel = Mathf.Clamp(option.CurrentLevel, 1, maxLevel);

                    if (maxLevel > 1)
                    {
                        statusText = currentLevel >= maxLevel
                            ? $"Level {currentLevel}/{maxLevel} (Max)"
                            : $"Level {currentLevel}/{maxLevel}";
                    }
                    else
                    {
                        statusText = "Unlocked";
                    }
                }
                else
                {
                    statusText = option.CanPurchase ? $"Unlock ({option.Cost})" : "Locked";
                }

                UIExtensions.SetTextSafe(_statusLabel, statusText);
                _statusLabel.gameObject.SetActive(true);
            }

            UpdateButtonVisibility(option);
        }

        private void UpdateButtonVisibility(ArcaneConduit.AbilityOption option)
        {
            bool isUnlocked = option.IsUnlocked;

            if (_unlockButton != null)
            {
                _unlockButton.gameObject.SetActive(isUnlocked == false);
                _unlockButton.interactable = option.CanPurchase;
            }

            if (_levelUpButton != null)
            {
                _levelUpButton.gameObject.SetActive(isUnlocked);
                _levelUpButton.interactable = option.CanLevelUp;
            }
        }

        private void HandleUnlockClicked()
        {
            if (_hasOption == false)
                return;

            UIItemContextView contextView = GetContextView();
            contextView?.RequestAbilityUnlockByAbilityIndex(_currentOption.Index);
        }

        private void HandleLevelUpClicked()
        {
            if (_hasOption == false)
                return;

            UIItemContextView contextView = GetContextView();
            contextView?.RequestAbilityLevelUpByAbilityIndex(_currentOption.Index);
        }

        private UIItemContextView GetContextView()
        {
            if (_inventorySlot == null)
            {
                _inventorySlot = Slot;
            }

            return _inventorySlot?.Owner as UIItemContextView;
        }
    }
}
