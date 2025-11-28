using TMPro;
using TPSBR;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public sealed class UISlotMachineView : UIGamblingView
    {
        public const string ResourcePath = "UI/GameplayViews/UISlotMachineView";

        public SlotMachineNetworked SlotMachine { get; private set; }

        public int CurrentWager => _currentWager;

        [SerializeField]
        private TMP_InputField _wagerInput;
        [SerializeField]
        private Button _increaseButton;
        [SerializeField]
        private Button _decreaseButton;
        [SerializeField]
        private Button _gambleButton;

        private Inventory _inventory;
        private int _currentWager = SlotMachineNetworked.MinWager;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            RegisterListeners();
        }

        protected override void OnDeinitialize()
        {
            UnregisterListeners();

            base.OnDeinitialize();
        }

        protected override void OnConfigured(GamblingMachine machine, Agent agent)
        {
            base.OnConfigured(machine, agent);

            SlotMachine = machine as SlotMachineNetworked;
            _inventory = agent != null ? agent.Inventory : null;

            if (_inventory != null)
            {
                _inventory.GoldChanged -= HandleGoldChanged;
                _inventory.GoldChanged += HandleGoldChanged;
            }

            _currentWager = SlotMachineNetworked.MinWager;
            UpdateWagerDisplay();
        }

        protected override void OnCleared(GamblingMachine machine, Agent agent)
        {
            base.OnCleared(machine, agent);

            if (_inventory != null)
            {
                _inventory.GoldChanged -= HandleGoldChanged;
                _inventory = null;
            }

            if (SlotMachine == machine)
            {
                SlotMachine = null;
            }
        }

        private void RegisterListeners()
        {
            if (_increaseButton != null)
            {
                _increaseButton.onClick.RemoveListener(HandleIncreaseClicked);
                _increaseButton.onClick.AddListener(HandleIncreaseClicked);
            }

            if (_decreaseButton != null)
            {
                _decreaseButton.onClick.RemoveListener(HandleDecreaseClicked);
                _decreaseButton.onClick.AddListener(HandleDecreaseClicked);
            }

            if (_gambleButton != null)
            {
                _gambleButton.onClick.RemoveListener(HandleGambleClicked);
                _gambleButton.onClick.AddListener(HandleGambleClicked);
            }
        }

        private void UnregisterListeners()
        {
            if (_increaseButton != null)
            {
                _increaseButton.onClick.RemoveListener(HandleIncreaseClicked);
            }

            if (_decreaseButton != null)
            {
                _decreaseButton.onClick.RemoveListener(HandleDecreaseClicked);
            }

            if (_gambleButton != null)
            {
                _gambleButton.onClick.RemoveListener(HandleGambleClicked);
            }
        }

        private void HandleIncreaseClicked()
        {
            SetWager(_currentWager + SlotMachineNetworked.WagerStep);
        }

        private void HandleDecreaseClicked()
        {
            SetWager(_currentWager - SlotMachineNetworked.WagerStep);
        }

        private void HandleGambleClicked()
        {
            if (SlotMachine == null || Agent == null)
                return;

            SlotMachine.TryStartRoll(Agent, _currentWager);
        }

        private void HandleGoldChanged(int value)
        {
            _ = value;
            UpdateWagerDisplay();
        }

        private void SetWager(int amount)
        {
            _currentWager = AlignToStep(amount);
            UpdateWagerDisplay();
        }

        private int AlignToStep(int amount)
        {
            int clamped = Mathf.Clamp(amount, SlotMachineNetworked.MinWager, SlotMachineNetworked.MaxWager);
            int remainder = clamped % SlotMachineNetworked.WagerStep;
            if (remainder != 0)
            {
                clamped -= remainder;
            }

            return Mathf.Max(SlotMachineNetworked.MinWager, clamped);
        }

        internal void RefreshWagerDisplay()
        {
            UpdateWagerDisplay();
        }

        private void UpdateWagerDisplay()
        {
            int availableGold = _inventory != null ? _inventory.Gold : SlotMachineNetworked.MaxWager;
            int maximumAffordable = Mathf.Clamp(availableGold, SlotMachineNetworked.MinWager, SlotMachineNetworked.MaxWager);

            _currentWager = Mathf.Clamp(_currentWager, SlotMachineNetworked.MinWager, maximumAffordable);
            _currentWager = AlignToStep(_currentWager);

            if (_wagerInput != null)
            {
                _wagerInput.SetTextWithoutNotify(_currentWager.ToString());
            }

            bool canIncrease = _currentWager < Mathf.Min(SlotMachineNetworked.MaxWager, maximumAffordable);
            bool canDecrease = _currentWager > SlotMachineNetworked.MinWager;
            bool canGamble = SlotMachine != null && Agent != null && _inventory != null && _inventory.Gold >= _currentWager && SlotMachine.CanRoll();

            if (_increaseButton != null)
            {
                _increaseButton.interactable = canIncrease;
            }

            if (_decreaseButton != null)
            {
                _decreaseButton.interactable = canDecrease;
            }

            if (_gambleButton != null)
            {
                _gambleButton.interactable = canGamble;
            }
        }
    }
}
