using TMPro;
using TSS.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
        public sealed class UIRecipeItemIcon : MonoBehaviour
        {
                [SerializeField]
                private Image _iconImage;
                [SerializeField]
                private TextMeshProUGUI _quantityLabel;

                public void Configure(ItemDefinition definition, int quantity)
                {
                        Sprite icon = definition != null ? definition.Icon : null;
                        UpdateIcon(icon);
                        UpdateQuantity(quantity);
                }

                private void UpdateIcon(Sprite icon)
                {
                        if (_iconImage == null)
                                return;

                        if (icon != null)
                        {
                                _iconImage.sprite = icon;
                                _iconImage.enabled = true;
                                _iconImage.color = Color.white;
                        }
                        else
                        {
                                _iconImage.sprite = null;
                                _iconImage.enabled = false;
                        }
                }

		private void UpdateQuantity(int quantity)
		{
			if (_quantityLabel == null)
				return;

			if (quantity > 0)
			{
				_quantityLabel.text = $"x{quantity}";
				_quantityLabel.gameObject.SetActive(true);
			}
			else
			{
				_quantityLabel.text = string.Empty;
				_quantityLabel.gameObject.SetActive(false);
			}
		}
        }
}
