using TMPro;
using TPSBR;
using UnityEngine;

namespace TPSBR.UI
{
    public sealed class UIFishingView : UIView
    {
        public const string ResourcePath = "UI/GameplayViews/UIFishingView";

        [SerializeField] private TextMeshProUGUI _lifecycleLabel;

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
                _inventory.FishingLifecycleStateChanged -= OnFishingLifecycleStateChanged;
            }

            _inventory = inventory;

            if (_inventory != null)
            {
                _inventory.FishingPoleEquippedChanged += OnFishingPoleEquippedChanged;
                _inventory.FishingLifecycleStateChanged += OnFishingLifecycleStateChanged;
                UpdateVisibility(_inventory.IsFishingPoleEquipped);
                UpdateLifecycleLabel(_inventory.IsFishingPoleEquipped ? _inventory.FishingLifecycleState : FishingLifecycleState.Inactive);
            }
            else
            {
                UpdateVisibility(false);
                UpdateLifecycleLabel(FishingLifecycleState.Inactive);
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
            UpdateLifecycleLabel(isEquipped == true && _inventory != null ? _inventory.FishingLifecycleState : FishingLifecycleState.Inactive);
        }

        private void OnFishingLifecycleStateChanged(FishingLifecycleState state)
        {
            UpdateLifecycleLabel(state);
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

        private void UpdateLifecycleLabel(FishingLifecycleState state)
        {
            if (_lifecycleLabel == null)
            {
                return;
            }

            string status = state switch
            {
                FishingLifecycleState.Inactive     => "Inactive",
                FishingLifecycleState.Ready        => "Ready",
                FishingLifecycleState.Casting      => "Casting",
                FishingLifecycleState.LureInFlight => "Line In Flight",
                FishingLifecycleState.Waiting      => "Waiting",
                _                                  => state.ToString(),
            };

            _lifecycleLabel.text = $"Fishing: {status}";
        }
    }
}
