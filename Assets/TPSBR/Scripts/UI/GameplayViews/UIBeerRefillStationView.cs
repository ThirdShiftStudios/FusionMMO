using System;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
        public sealed class UIBeerRefillStationView : UIItemContextView
        {
                [SerializeField]
                private Button _purchaseButton;

                public event Action PurchaseRequested;

                protected override void OnInitialize()
                {
                        base.OnInitialize();

                        if (_purchaseButton != null)
                        {
                                _purchaseButton.onClick.AddListener(HandlePurchaseButtonClicked);
                        }
                }

                protected override void OnDeinitialize()
                {
                        if (_purchaseButton != null)
                        {
                                _purchaseButton.onClick.RemoveListener(HandlePurchaseButtonClicked);
                        }

                        base.OnDeinitialize();
                }

                private void HandlePurchaseButtonClicked()
                {
                        PurchaseRequested?.Invoke();
                }
        }
}
