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
        [SerializeField] private SliderMinigame _fightingMinigame;

        private Inventory _inventory;
        private bool _isHookSetMinigameVisible;
        private bool _isFightingMinigameVisible;

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
                UpdateFightingMinigameState(initialState);
            }
            else
            {
                UpdateVisibility(false);
                UpdateLifecycleLabel(FishingLifecycleState.Inactive);
                UpdateHookSetMinigameState(FishingLifecycleState.Inactive);
                UpdateFightingMinigameState(FishingLifecycleState.Inactive);
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

            if (_fightingMinigame != null)
            {
                _fightingMinigame.gameObject.SetActive(false);
                _fightingMinigame.MinigameFinished += OnFightingMinigameFinished;
            }
        }

        protected override void OnDeinitialize()
        {
            if (_hookSetMinigame != null)
            {
                _hookSetMinigame.ForceStop();
                _hookSetMinigame.MinigameFinished -= OnHookSetMinigameFinished;
            }

            if (_fightingMinigame != null)
            {
                _fightingMinigame.ForceStop();
                _fightingMinigame.MinigameFinished -= OnFightingMinigameFinished;
            }

            HideHookSetMinigame();
            HideFightingMinigame();
            Bind(null);
            base.OnDeinitialize();
        }

        private void OnFishingPoleEquippedChanged(bool isEquipped)
        {
            UpdateVisibility(isEquipped);
            var state = isEquipped == true && _inventory != null ? _inventory.FishingLifecycleState : FishingLifecycleState.Inactive;
            UpdateLifecycleLabel(state);
            UpdateHookSetMinigameState(state);
            UpdateFightingMinigameState(state);
        }

        private void OnFishingLifecycleStateChanged(FishingLifecycleState state)
        {
            UpdateLifecycleLabel(state);
            UpdateHookSetMinigameState(state);
            UpdateFightingMinigameState(state);
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

        private void UpdateFightingMinigameState(FishingLifecycleState state)
        {
            if (_fightingMinigame == null)
            {
                return;
            }

            if (state == FishingLifecycleState.Fighting)
            {
                if (_inventory != null)
                {
                    _fightingMinigame.SuccessHitsRequired = Mathf.Max(1, _inventory.FightingMinigameHitsRequired);
                }

                if (_isFightingMinigameVisible == false)
                {
                    _fightingMinigame.gameObject.SetActive(true);
                    _fightingMinigame.Begin(new List<SliderMinigameReward>());
                    _isFightingMinigameVisible = true;
                }
            }
            else
            {
                HideFightingMinigame();
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

        private void HideFightingMinigame()
        {
            if (_fightingMinigame == null || _isFightingMinigameVisible == false)
            {
                return;
            }

            _fightingMinigame.ForceStop();
            _fightingMinigame.gameObject.SetActive(false);
            _isFightingMinigameVisible = false;
        }

        private void OnHookSetMinigameFinished(bool wasSuccessful)
        {
            HideHookSetMinigame();
            Debug.Log($"OnHookSetMinigameFinished Success: {wasSuccessful}");
            if (_inventory == null)
            {
                return;
            }

            _inventory.SubmitHookSetMinigameResult(wasSuccessful);
        }

        private void OnFightingMinigameFinished(bool wasSuccessful)
        {
            Debug.Log($"OnFightingMinigameFinished Success: {wasSuccessful}");
            HideFightingMinigame();

            if (_inventory == null)
            {
                return;
            }

            _inventory.SubmitFightingMinigameResult(wasSuccessful);
        }
    }
}
