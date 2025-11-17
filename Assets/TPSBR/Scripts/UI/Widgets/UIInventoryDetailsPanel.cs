using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIInventoryDetailsPanel : UIWidget
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private Image _iconImage;

        private UIStatDetails _statDetails;
        private UIProfessionDetails _professionDetails;
        protected override void OnInitialize()
        {
            base.OnInitialize();

            _statDetails = GetComponentInChildren<UIStatDetails>();
            _professionDetails = GetComponentInChildren<UIProfessionDetails>();
            Hide();
        }

        internal void Show(IInventoryItemDetails item, NetworkString<_64> configurationHash)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            UpdateStatDetails(item, configurationHash);

            if (_nameLabel != null)
            {
                string displayName = item.GetDisplayName(configurationHash);
                if (string.IsNullOrWhiteSpace(displayName) == true)
                {
                    displayName = item.DisplayName;
                }

                _nameLabel.SetTextSafe(displayName);
            }

            if (_descriptionLabel != null)
            {
                string description = item.GetDescription(configurationHash);
                if (string.IsNullOrWhiteSpace(description) == true)
                {
                    description = item.GetDescription();
                }

                _descriptionLabel.SetTextSafe(description);
            }

            if (_iconImage != null)
            {
                var sprite = item.Icon;
                _iconImage.sprite = sprite;
                _iconImage.enabled = sprite != null;
            }

            SetVisible(true);
        }

        internal void Hide()
        {
            if (_statDetails != null)
            {
                _statDetails.SetStats(null);
            }

            if (_nameLabel != null)
            {
                _nameLabel.SetTextSafe(string.Empty);
            }

            if (_descriptionLabel != null)
            {
                _descriptionLabel.SetTextSafe(string.Empty);
            }

            if (_iconImage != null)
            {
                _iconImage.sprite = null;
                _iconImage.enabled = false;
            }

            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.SetVisibility(visible);

                if (visible == true && gameObject.activeSelf == false)
                {
                    gameObject.SetActive(true);
                }

                return;
            }

            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        private void UpdateStatDetails(IInventoryItemDetails item, NetworkString<_64> configurationHash)
        {
            if (_statDetails == null)
            {
                return;
            }

            IReadOnlyList<int> stats = null;

            if (item is StaffWeapon staffWeapon)
            {
                if (staffWeapon.TryGetStatBonuses(configurationHash, out IReadOnlyList<int> configuredStats) == true)
                {
                    stats = configuredStats;
                }
                else
                {
                    stats = staffWeapon.StatBonuses;
                }
            }

            _statDetails.SetStats(stats);
        }

    }
}
