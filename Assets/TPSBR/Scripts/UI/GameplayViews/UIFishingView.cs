using System.Collections.Generic;
using TMPro;
using TPSBR;
using UnityEngine;

namespace TPSBR.UI
{
    public sealed class UIFishingView : UIView
    {
        public const string ResourcePath = "UI/GameplayViews/UIFishingView";

        [SerializeField] private TextMeshProUGUI _lifecycleLabel;
        [SerializeField] private SliderMinigame _hookSetMinigame;

        private Inventory _inventory;
        private bool _isHookSetMinigameVisible;

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
                var initialState = _inventory.IsFishingPoleEquipped ? _inventory.FishingLifecycleState : FishingLifecycleState.Inactive;
                UpdateLifecycleLabel(initialState);
                UpdateHookSetMinigameState(initialState);
            }
            else
            {
                UpdateVisibility(false);
                UpdateLifecycleLabel(FishingLifecycleState.Inactive);
                UpdateHookSetMinigameState(FishingLifecycleState.Inactive);
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_hookSetMinigame != null)
            {
                _hookSetMinigame.gameObject.SetActive(false);
                _hookSetMinigame.MinigameFinished += OnHookSetMinigameFinished;
            }
        }

        protected override void OnDeinitialize()
        {
            if (_hookSetMinigame != null)
            {
                _hookSetMinigame.ForceStop();
                _hookSetMinigame.MinigameFinished -= OnHookSetMinigameFinished;
            }

            HideHookSetMinigame();
            Bind(null);
            base.OnDeinitialize();
        }

        private void OnFishingPoleEquippedChanged(bool isEquipped)
        {
            UpdateVisibility(isEquipped);
            var state = isEquipped == true && _inventory != null ? _inventory.FishingLifecycleState : FishingLifecycleState.Inactive;
            UpdateLifecycleLabel(state);
            UpdateHookSetMinigameState(state);
        }

        private void OnFishingLifecycleStateChanged(FishingLifecycleState state)
        {
            UpdateLifecycleLabel(state);
            UpdateHookSetMinigameState(state);
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
                HideHookSetMinigame();
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
                FishingLifecycleState.Fighting     => "Fighting",
                _                                  => state.ToString(),
            };

            _lifecycleLabel.text = $"Fishing: {status}";
        }

        private void UpdateHookSetMinigameState(FishingLifecycleState state)
        {
            if (_hookSetMinigame == null)
            {
                return;
            }

            if (state == FishingLifecycleState.Waiting)
            {
                if (_isHookSetMinigameVisible == false)
                {
                    _hookSetMinigame.gameObject.SetActive(true);
                    _hookSetMinigame.Begin(new List<SliderMinigameReward>());
                    _isHookSetMinigameVisible = true;
                }
            }
            else
            {
                HideHookSetMinigame();
            }
        }

        private void HideHookSetMinigame()
        {
            if (_hookSetMinigame == null || _isHookSetMinigameVisible == false)
            {
                return;
            }

            _hookSetMinigame.ForceStop();
            _hookSetMinigame.gameObject.SetActive(false);
            _isHookSetMinigameVisible = false;
        }

        private void OnHookSetMinigameFinished(bool wasSuccessful)
        {
            HideHookSetMinigame();

            if (_inventory == null)
            {
                return;
            }

            _inventory.SubmitHookSetMinigameResult(wasSuccessful);
        }
    }
}
