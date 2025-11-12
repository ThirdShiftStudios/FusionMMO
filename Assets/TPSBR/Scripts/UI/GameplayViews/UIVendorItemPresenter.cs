using TMPro;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using TPSBR;

namespace TPSBR.UI
{
        [DisallowMultipleComponent]
        public sealed class UIVendorItemPresenter : MonoBehaviour
        {
                [SerializeField]
                private TextMeshProUGUI _nameLabel;
                [SerializeField]
                private TextMeshProUGUI _priceLabel;
                [SerializeField]
                private string _priceFormat = "Price: {0}";

                internal void Apply(ItemVendor.VendorItemData data, int price)
                {
                        if (_nameLabel != null)
                        {
                                string name = data.Definition != null ? data.Definition.Name : string.Empty;
                                UIExtensions.SetTextSafe(_nameLabel, name);
                        }

                        if (_priceLabel != null)
                        {
                                UIExtensions.SetTextSafe(_priceLabel, string.Format(_priceFormat, price));
                        }
                }

                internal void Clear()
                {
                        if (_nameLabel != null)
                        {
                                UIExtensions.SetTextSafe(_nameLabel, string.Empty);
                        }

                        if (_priceLabel != null)
                        {
                                UIExtensions.SetTextSafe(_priceLabel, string.Empty);
                        }
                }
        }
}
