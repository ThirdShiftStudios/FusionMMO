using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
        public class UIInventoryFeedItem : UIFeedItemBase
        {
                [SerializeField]
                private TextMeshProUGUI _indicator;
                [SerializeField]
                private TextMeshProUGUI _quantity;
                [SerializeField]
                private TextMeshProUGUI _itemName;
                [SerializeField]
                private Image _icon;
                [SerializeField]
                private Color _additionColor = Color.green;
                [SerializeField]
                private Color _removalColor = Color.red;

                protected override void ApplyData(IFeedData data)
                {
                        if (data is InventoryFeedData inventoryData == false)
                                return;

                        Color targetColor = inventoryData.IsAddition == true ? _additionColor : _removalColor;
                        string sign = inventoryData.IsAddition == true ? "+" : "-";

                        if (_indicator != null)
                        {
                                _indicator.text = sign;
                                _indicator.color = targetColor;
                        }

                        if (_quantity != null)
                        {
                                _quantity.text = Mathf.Abs(inventoryData.QuantityChange).ToString();
                                _quantity.color = targetColor;
                        }

                        if (_itemName != null)
                        {
                                _itemName.text = inventoryData.ItemName ?? string.Empty;
                        }

                        if (_icon != null)
                        {
                                _icon.sprite = inventoryData.Icon;
                                _icon.gameObject.SetActive(inventoryData.Icon != null);
                        }
                }
        }
}
