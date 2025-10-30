using System;
using UnityEngine;

namespace TPSBR.UI
{
        public sealed class UIBeerRefillStationView : UIItemContextView
        {
                [SerializeField]
                private UIButton _purchaseButton;

                public event Action PurchaseButtonClicked;

                protected override void OnInitialize()
                {
                        base.OnInitialize();

                        if (_purchaseButton != null)
                        {
                                _purchaseButton.onClick.AddListener(HandlePurchaseButtonClicked);
                                _purchaseButton.interactable = false;
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

                public void SetPurchaseButtonInteractable(bool interactable)
                {
                        if (_purchaseButton == null)
                                return;

                        _purchaseButton.interactable = interactable;
                }

                private void HandlePurchaseButtonClicked()
                {
                        PurchaseButtonClicked?.Invoke();
                }
        }
}
