using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
        public sealed class UIBeerRefillStationView : UIItemContextView
        {
                [SerializeField]
                private Button _purchaseButton;

                public Button PurchaseButton => _purchaseButton;
        }
}
