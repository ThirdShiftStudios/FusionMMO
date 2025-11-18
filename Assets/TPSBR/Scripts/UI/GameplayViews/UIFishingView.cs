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
        [SerializeField] private SliderBalanceMinigame _reelingMinigame;

        private Inventory _inventory;
        private bool _isHookSetMinigameVisible;
        private bool _isFightingMinigameVisible;
        private bool _isReelingMinigameVisible;

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
                UpdateReelingMinigameState(initialState);
            }
            else
            {
                UpdateVisibility(false);
                UpdateLifecycleLabel(FishingLifecycleState.Inactive);
                UpdateHookSetMinigameState(FishingLifecycleState.Inactive);
                UpdateFightingMinigameState(FishingLifecycleState.Inactive);
                UpdateReelingMinigameState(FishingLifecycleState.Inactive);
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_hookSetMinigame != null)
            {
                _hookSetMinigame.gameObject.SetActive(false);
                _hookSetMinigame.MinigameFinished += OnHookSetMinigameFinished;
                _hookSetMinigame.SuccessZoneStateChanged += OnHookSetSuccessZoneStateChanged;
            }

            if (_fightingMinigame != null)
            {
                _fightingMinigame.gameObject.SetActive(false);
                _fightingMinigame.MinigameFinished += OnFightingMinigameFinished;
                _fightingMinigame.SuccessProgressed += OnFightingMinigameProgressed;
            }

            if (_reelingMinigame != null)
            {
                _reelingMinigame.gameObject.SetActive(false);
                _reelingMinigame.onSuccess.AddListener(OnReelingMinigameSucceeded);
                _reelingMinigame.onFail.AddListener(OnReelingMinigameFailed);
            }
        }

        protected override void OnDeinitialize()
        {
            if (_hookSetMinigame != null)
            {
                _hookSetMinigame.ForceStop();
                _hookSetMinigame.MinigameFinished -= OnHookSetMinigameFinished;
                _hookSetMinigame.SuccessZoneStateChanged -= OnHookSetSuccessZoneStateChanged;
            }

            if (_fightingMinigame != null)
            {
                _fightingMinigame.ForceStop();
                _fightingMinigame.MinigameFinished -= OnFightingMinigameFinished;
                _fightingMinigame.SuccessProgressed -= OnFightingMinigameProgressed;
            }

            if (_reelingMinigame != null)
            {
                _reelingMinigame.onSuccess.RemoveListener(OnReelingMinigameSucceeded);
                _reelingMinigame.onFail.RemoveListener(OnReelingMinigameFailed);
            }

            HideHookSetMinigame();
            HideFightingMinigame();
            HideReelingMinigame();
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
            UpdateReelingMinigameState(state);
        }

        private void OnFishingLifecycleStateChanged(FishingLifecycleState state)
        {
            UpdateLifecycleLabel(state);
            UpdateHookSetMinigameState(state);
            UpdateFightingMinigameState(state);
            UpdateReelingMinigameState(state);
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
                HideReelingMinigame();
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
                FishingLifecycleState.Reeling      => "Reeling",
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
                    _inventory?.UpdateHookSetSuccessZoneState(false);
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

        private void UpdateReelingMinigameState(FishingLifecycleState state)
        {
            if (_reelingMinigame == null)
            {
                return;
            }

            if (state == FishingLifecycleState.Reeling)
            {
                if (_isReelingMinigameVisible == false)
                {
                    _reelingMinigame.gameObject.SetActive(true);
                    _reelingMinigame.StartMinigame();
                    _isReelingMinigameVisible = true;
                }
            }
            else
            {
                HideReelingMinigame();
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
            _inventory?.UpdateHookSetSuccessZoneState(false);
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

        private void HideReelingMinigame()
        {
            if (_reelingMinigame == null || _isReelingMinigameVisible == false)
            {
                return;
            }

            _reelingMinigame.StopMinigame();
            _reelingMinigame.gameObject.SetActive(false);
            _isReelingMinigameVisible = false;
        }

        private void OnHookSetMinigameFinished(bool wasSuccessful)
        {
            HideHookSetMinigame();
            _inventory?.UpdateHookSetSuccessZoneState(false);
            Debug.Log($"OnHookSetMinigameFinished Success: {wasSuccessful}");
            if (_inventory == null)
            {
                return;
            }

            _inventory.SubmitHookSetMinigameResult(wasSuccessful);
        }

        private void OnHookSetSuccessZoneStateChanged(bool isInSuccessZone)
        {
            _inventory?.UpdateHookSetSuccessZoneState(isInSuccessZone);
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

        private void OnFightingMinigameProgressed(int currentHits, int requiredHits)
        {
            _inventory?.SubmitFightingMinigameProgress(currentHits, requiredHits);
        }

        private void OnReelingMinigameSucceeded()
        {
            HideReelingMinigame();

            if (_inventory == null)
            {
                return;
            }

            _inventory.SubmitReelingMinigameResult(true);
        }

        private void OnReelingMinigameFailed()
        {
            HideReelingMinigame();

            if (_inventory == null)
            {
                return;
            }

            _inventory.SubmitReelingMinigameResult(false);
        }
    }
}
