using UnityEngine;

namespace TPSBR.UI
{
    public class UIGameplayInventory : UICloseView
    {
        // PUBLIC MEMBERS

        public override bool NeedsCursor => _menuVisible;

        public bool MenuVisible => _menuVisible;
        // PRIVATE MEMBERS
        [SerializeField] private UIButton _cancelButton;
        [SerializeField] private UIInventoryGrid _inventoryGrid;
        [SerializeField] private UIHotbar _hotbar;
        [SerializeField] private Color _selectedHotbarColor = Color.white;

        private bool _menuVisible;
        private Agent _boundAgent;
        private Inventory _boundInventory;

        // PUBLIC METHODS

        public void Show(bool value, bool force = false)
        {
            if (_menuVisible == value && force == false)
                return;

            _menuVisible = value;
            CanvasGroup.interactable = value;

            (SceneUI as GameplayUI).RefreshCursorVisibility();

            if (value == true)
            {
                Animation.PlayForward();
            }
            else
            {
                Animation.PlayBackward();
            }
        }

        // UIView INTERFACE

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            if (_inventoryGrid == null)
            {
                _inventoryGrid = GetComponentInChildren<UIInventoryGrid>(true);
            }

            if (_hotbar == null)
            {
                _hotbar = GetComponentInChildren<UIHotbar>(true);
            }

            if (_hotbar != null)
            {
                _hotbar.SetSelectedColor(_selectedHotbarColor);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelButton);
            }
        }

        protected override void OnDeinitialize()
        {
            if (_inventoryGrid != null)
            {
                _inventoryGrid.Bind(null);
            }

            if (_hotbar != null)
            {
                _hotbar.Bind(null);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(OnCancelButton);
            }

            base.OnDeinitialize();
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            Animation.SampleStart();
            _menuVisible = false;
            CanvasGroup.interactable = false;

            RefreshInventoryBinding();
        }

        protected override void OnTick()
        {
            base.OnTick();

            RefreshInventoryBinding();
        }

        protected override void OnCloseButton()
        {
            Show(false);
        }

        protected override bool OnBackAction()
        {
            if (_menuVisible == true)
                return base.OnBackAction();

            Show(true);
            return true;
        }

        // PRIVATE MEMBERS

        private void OnLeaveButton()
        {
            var dialog = Open<UIYesNoDialogView>();

            dialog.Title.text = "LEAVE MATCH";
            dialog.Description.text = "Are you sure you want to leave current match?";

            dialog.HasClosed += (result) =>
            {
                if (result == true)
                {
                    if (Context != null && Context.GameplayMode != null)
                    {
                        Context.GameplayMode.StopGame();
                    }
                    else
                    {
                        Global.Networking.StopGame();
                    }
                }
            };
        }

        private void OnSettingsButton()
        {
            var settings = Open<UISettingsView>();
            settings.HasClosed += () => { Show(false); };
        }

        private void OnCancelButton()
        {
            OnCloseButton();
        }

        private void RefreshInventoryBinding()
        {
            if (Context == null)
            {
                if (_boundAgent != null)
                {
                    _boundAgent = null;
                    _boundInventory = null;
                    _inventoryGrid?.Bind(null);
                    _hotbar?.Bind(null);
                }
                return;
            }

            var agent = Context.ObservedAgent;
            if (_boundAgent == agent)
                return;

            _boundAgent = agent;
            _boundInventory = agent != null ? agent.Inventory : null;
            _inventoryGrid?.Bind(_boundInventory);
            _hotbar?.Bind(_boundInventory);
        }
    }
}
