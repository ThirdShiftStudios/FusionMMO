using TPSBR;
using UnityEngine;

namespace TPSBR.UI
{
    public sealed class UIFishingView : UIView
    {
        public const string ResourcePath = "UI/GameplayViews/UIFishingView";

        private Inventory _inventory;

        internal void Bind(Inventory inventory)
        {
            if (_inventory == inventory)
            {
                return;
            }

            if (_inventory != null)
            {
                _inventory.FishingPoleEquippedChanged -= OnFishingPoleEquippedChanged;
            }

            _inventory = inventory;

            if (_inventory != null)
            {
                _inventory.FishingPoleEquippedChanged += OnFishingPoleEquippedChanged;
                UpdateVisibility(_inventory.IsFishingPoleEquipped);
            }
            else
            {
                UpdateVisibility(false);
            }
        }

        protected override void OnDeinitialize()
        {
            Bind(null);
            base.OnDeinitialize();
        }

        private void OnFishingPoleEquippedChanged(bool isEquipped)
        {
            UpdateVisibility(isEquipped);
        }

        private void UpdateVisibility(bool shouldBeVisible)
        {
            if (shouldBeVisible == true)
            {
                if (IsOpen == false)
                {
                    Open();
                }
            }
            else if (IsOpen == true)
            {
                Close();
            }
        }
    }
}
