using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TPSBR.UI
{
    public class UIItemContextView : UICloseView, IUIListItemOwner
    {
        [Header("Items")]
        [SerializeField]
        private TextMeshProUGUI _emptyStateLabel;
        [SerializeField]
        private string _noAgentText = "No agent available.";
        [SerializeField]
        private string _noInventoryText = "Inventory unavailable.";
        [SerializeField]
        private string _noItemsText = "No items available.";
        [SerializeField]
        private RectTransform _slotContainer;
        [SerializeField]
        private UIListItem _itemSlotPrefab;
        [SerializeField]
        private Color _selectedSlotColor = Color.white;

        [Header("Abilities")]
        [SerializeField]
        private RectTransform _unlockedAbilityContainer;
        [SerializeField]
        private RectTransform _lockedAbilityContainer;
        [SerializeField]
        private UIListItem _abilitySlotPrefab;
        [SerializeField]
        private TextMeshProUGUI _noLockedAbilitiesLabel;
        [SerializeField]
        private TextMeshProUGUI _noUnlockedAbilitiesLabel;
        [SerializeField]
        private UIButton _unlockAbilityButton;
        [SerializeField]
        private TextMeshProUGUI _unlockAbilityButtonLabel;
        [SerializeField]
        private TextMeshProUGUI _selectedAbilityLabel;
        [SerializeField]
        private string _noAbilitySelectedText = "Select an ability to unlock.";
        [SerializeField]
        private string _unlockAbilityFormat = "Unlock ({0})";
        [SerializeField]
        private string _unlockAbilityUnavailableText = "Unlock";

        private readonly List<UIListItem> _spawnedSlots = new List<UIListItem>();
        private readonly List<UpgradeStation.ItemData> _currentItems = new List<UpgradeStation.ItemData>();
        private readonly List<UpgradeStation.ItemData> _lastItems = new List<UpgradeStation.ItemData>();
        private readonly List<UIListItem> _unlockedAbilitySlots = new List<UIListItem>();
        private readonly List<UIListItem> _lockedAbilitySlots = new List<UIListItem>();
        private readonly List<ArcaneConduit.AbilityOption> _allAbilityOptions = new List<ArcaneConduit.AbilityOption>();
        private readonly List<ArcaneConduit.AbilityOption> _lockedAbilityOptions = new List<ArcaneConduit.AbilityOption>();
        private readonly List<ArcaneConduit.AbilityOption> _unlockedAbilityOptions = new List<ArcaneConduit.AbilityOption>();
        private Func<List<UpgradeStation.ItemData>, UpgradeStation.ItemStatus> _itemProvider;
        private UpgradeStation.ItemStatus _lastStatus = UpgradeStation.ItemStatus.NoAgent;
        private int _selectedIndex = -1;
        private int _selectedLockedAbilityIndex = -1;

        public event Action<UpgradeStation.ItemData> ItemSelected;
        protected event Action<int> AbilityUnlockRequested;

        public void Configure(Agent agent, Func<List<UpgradeStation.ItemData>, UpgradeStation.ItemStatus> itemProvider)
        {
            _ = agent;
            _itemProvider = itemProvider;
            _selectedIndex = -1;
            RefreshItemSlots(true);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_unlockAbilityButton != null)
            {
                _unlockAbilityButton.onClick.AddListener(HandleUnlockAbilityClicked);
                _unlockAbilityButton.interactable = false;
            }

            UpdateSelectedAbilityDetails(null);
            UpdateUnlockButtonLabel(null);
        }

        protected override void OnDeinitialize()
        {
            if (_unlockAbilityButton != null)
            {
                _unlockAbilityButton.onClick.RemoveListener(HandleUnlockAbilityClicked);
            }

            base.OnDeinitialize();
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            RefreshItemSlots(true);
        }

        protected override void OnClose()
        {
            base.OnClose();

            _itemProvider = null;
            _currentItems.Clear();
            _lastItems.Clear();
            _lastStatus = UpgradeStation.ItemStatus.NoAgent;
            _selectedIndex = -1;
            _selectedLockedAbilityIndex = -1;
            ClearSlots();
            ClearAbilityOptions();
            SetEmptyState(string.Empty);
        }

        protected override void OnTick()
        {
            base.OnTick();

            RefreshItemSlots(false);
        }

        public void SetAbilityOptions(IReadOnlyList<ArcaneConduit.AbilityOption> options)
        {
            _allAbilityOptions.Clear();
            _lockedAbilityOptions.Clear();
            _unlockedAbilityOptions.Clear();

            if (options != null)
            {
                for (int i = 0; i < options.Count; ++i)
                {
                    ArcaneConduit.AbilityOption option = options[i];
                    _allAbilityOptions.Add(option);

                    if (option.IsUnlocked == true)
                    {
                        _unlockedAbilityOptions.Add(option);
                    }
                    else
                    {
                        _lockedAbilityOptions.Add(option);
                    }
                }
            }

            if (_selectedLockedAbilityIndex >= _lockedAbilityOptions.Count)
            {
                _selectedLockedAbilityIndex = -1;
            }

            RefreshAbilityLists();
        }

        public void ClearAbilityOptions()
        {
            _allAbilityOptions.Clear();
            _lockedAbilityOptions.Clear();
            _unlockedAbilityOptions.Clear();
            _selectedLockedAbilityIndex = -1;

            RefreshAbilityLists();
        }

        public IReadOnlyList<ArcaneConduit.AbilityOption> GetAbilityOptions()
        {
            return _allAbilityOptions;
        }

        public void RequestAbilityUnlockByOptionIndex(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= _allAbilityOptions.Count)
                return;

            ArcaneConduit.AbilityOption option = _allAbilityOptions[optionIndex];

            if (option.IsUnlocked == true)
                return;

            RequestAbilityUnlockByAbilityIndex(option.Index);
        }

        public void RequestAbilityUnlockByAbilityIndex(int abilityIndex)
        {
            AbilityUnlockRequested?.Invoke(abilityIndex);
        }

        private void RefreshItemSlots(bool force)
        {
            if (_itemSlotPrefab == null || _slotContainer == null)
                return;

            UpgradeStation.ItemStatus status = UpgradeStation.ItemStatus.NoAgent;

            if (_itemProvider != null)
            {
                _currentItems.Clear();
                status = _itemProvider.Invoke(_currentItems);
            }

            if (force == false && status == _lastStatus && AreItemListsEqual(_currentItems, _lastItems) == true)
                return;

            _lastStatus = status;

            if (status != UpgradeStation.ItemStatus.Success || _currentItems.Count == 0)
            {
                ClearSlots();
                HandleEmptyState(status);
                return;
            }

            EnsureSlotCapacity(_currentItems.Count);

            for (int i = 0; i < _currentItems.Count; ++i)
            {
                UIListItem slot = _spawnedSlots[i];
                UpgradeStation.ItemData data = _currentItems[i];

                slot.InitializeSlot(this, i);
                slot.SetItem(data.Icon, data.Quantity);
                slot.gameObject.SetActive(true);
            }

            for (int i = _currentItems.Count; i < _spawnedSlots.Count; ++i)
            {
                UIListItem slot = _spawnedSlots[i];
                slot.Clear();
                slot.gameObject.SetActive(false);
            }

            CopyItems(_currentItems, _lastItems);
            SetEmptyState(string.Empty);

            if (_selectedIndex >= _currentItems.Count)
            {
                _selectedIndex = -1;
            }

            UpdateSelectionVisuals();
        }

        private void EnsureSlotCapacity(int required)
        {
            while (_spawnedSlots.Count < required)
            {
                UIListItem newSlot = Instantiate(_itemSlotPrefab, _slotContainer);
                newSlot.InitializeSlot(this, _spawnedSlots.Count);
                newSlot.gameObject.SetActive(false);
                _spawnedSlots.Add(newSlot);
            }
        }

        private void ClearSlots()
        {
            for (int i = 0; i < _spawnedSlots.Count; ++i)
            {
                UIListItem slot = _spawnedSlots[i];
                if (slot == null)
                    continue;

                slot.Clear();
                slot.gameObject.SetActive(false);
            }

            _selectedIndex = -1;
            UpdateSelectionVisuals();
        }

        private void RefreshAbilityLists()
        {
            RefreshAbilitySection(_unlockedAbilityOptions, _unlockedAbilitySlots, _unlockedAbilityContainer, false);
            RefreshAbilitySection(_lockedAbilityOptions, _lockedAbilitySlots, _lockedAbilityContainer, true);

            bool hasLockedAbilities = _lockedAbilityOptions.Count > 0;
            bool hasUnlockedAbilities = _unlockedAbilityOptions.Count > 0;

            if (_noLockedAbilitiesLabel != null)
            {
                _noLockedAbilitiesLabel.gameObject.SetActive(hasLockedAbilities == false && _lockedAbilityContainer != null);
            }

            if (_noUnlockedAbilitiesLabel != null)
            {
                _noUnlockedAbilitiesLabel.gameObject.SetActive(hasUnlockedAbilities == false && _unlockedAbilityContainer != null);
            }

            UpdateLockedAbilitySelectionVisuals();
            UpdateUnlockButtonState();
        }

        private void RefreshAbilitySection(List<ArcaneConduit.AbilityOption> options, List<UIListItem> slots, RectTransform container, bool allowSelection)
        {
            if (container == null || _abilitySlotPrefab == null)
            {
                for (int i = 0; i < slots.Count; ++i)
                {
                    if (slots[i] != null)
                    {
                        slots[i].gameObject.SetActive(false);
                    }
                }

                return;
            }

            container.gameObject.SetActive(true);
            EnsureAbilitySlotCapacity(slots, container, options.Count);

            for (int i = 0; i < options.Count; ++i)
            {
                UIListItem slot = slots[i];
                ArcaneConduit.AbilityOption option = options[i];

                slot.InitializeSlot(this, i);
                slot.gameObject.SetActive(true);
                slot.SetSelectionHighlight(false, _selectedSlotColor);

                UIAbilityOptionSlot abilityContent = slot.Content as UIAbilityOptionSlot;

                if (abilityContent == null)
                {
                    abilityContent = slot.GetComponent<UIAbilityOptionSlot>();
                }

                if (abilityContent == null)
                {
                    abilityContent = slot.GetComponentInChildren<UIAbilityOptionSlot>(true);
                }

                abilityContent?.SetAbility(option, allowSelection);
            }

            for (int i = options.Count; i < slots.Count; ++i)
            {
                UIListItem slot = slots[i];
                if (slot == null)
                    continue;

                slot.gameObject.SetActive(false);
            }

            if (allowSelection == false)
            {
                for (int i = 0; i < slots.Count; ++i)
                {
                    UIListItem slot = slots[i];
                    if (slot == null)
                        continue;

                    slot.SetSelectionHighlight(false, _selectedSlotColor);
                }
            }
        }

        private void EnsureAbilitySlotCapacity(List<UIListItem> slots, RectTransform container, int required)
        {
            if (container == null || _abilitySlotPrefab == null)
                return;

            while (slots.Count < required)
            {
                UIListItem newSlot = Instantiate(_abilitySlotPrefab, container);
                newSlot.InitializeSlot(this, slots.Count);
                newSlot.gameObject.SetActive(false);
                slots.Add(newSlot);
            }
        }

        private void UpdateLockedAbilitySelectionVisuals()
        {
            for (int i = 0; i < _lockedAbilitySlots.Count; ++i)
            {
                UIListItem slot = _lockedAbilitySlots[i];
                if (slot == null || slot.gameObject.activeSelf == false)
                    continue;

                bool isSelected = i == _selectedLockedAbilityIndex;
                slot.SetSelectionHighlight(isSelected, _selectedSlotColor);
            }

            ArcaneConduit.AbilityOption? selectedOption = null;

            if (_selectedLockedAbilityIndex >= 0 && _selectedLockedAbilityIndex < _lockedAbilityOptions.Count)
            {
                selectedOption = _lockedAbilityOptions[_selectedLockedAbilityIndex];
            }

            UpdateSelectedAbilityDetails(selectedOption);
        }

        private void UpdateUnlockButtonState()
        {
            if (_unlockAbilityButton == null)
                return;

            if (_selectedLockedAbilityIndex < 0 || _selectedLockedAbilityIndex >= _lockedAbilityOptions.Count)
            {
                _unlockAbilityButton.interactable = false;
                UpdateUnlockButtonLabel(null);
                return;
            }

            ArcaneConduit.AbilityOption option = _lockedAbilityOptions[_selectedLockedAbilityIndex];
            _unlockAbilityButton.interactable = option.CanPurchase;
            UpdateUnlockButtonLabel(option);
        }

        private void UpdateSelectedAbilityDetails(ArcaneConduit.AbilityOption? option)
        {
            if (_selectedAbilityLabel == null)
                return;

            if (option.HasValue == false)
            {
                UIExtensions.SetTextSafe(_selectedAbilityLabel, _noAbilitySelectedText);
                return;
            }

            ArcaneConduit.AbilityOption abilityOption = option.Value;
            string abilityName = abilityOption.Definition != null ? abilityOption.Definition.Name : string.Empty;
            string status;

            if (abilityOption.IsUnlocked == true)
            {
                status = "Unlocked";
            }
            else if (abilityOption.CanPurchase == true)
            {
                status = string.Format(_unlockAbilityFormat, abilityOption.Cost);
            }
            else
            {
                status = _unlockAbilityUnavailableText;
            }

            if (string.IsNullOrWhiteSpace(abilityName) == false)
            {
                UIExtensions.SetTextSafe(_selectedAbilityLabel, $"{abilityName} - {status}");
            }
            else
            {
                UIExtensions.SetTextSafe(_selectedAbilityLabel, status);
            }
        }

        private void UpdateUnlockButtonLabel(ArcaneConduit.AbilityOption? option)
        {
            if (_unlockAbilityButtonLabel == null)
                return;

            if (option.HasValue == false)
            {
                UIExtensions.SetTextSafe(_unlockAbilityButtonLabel, _unlockAbilityUnavailableText);
                return;
            }

            ArcaneConduit.AbilityOption abilityOption = option.Value;

            string label = abilityOption.CanPurchase == true
                ? string.Format(_unlockAbilityFormat, abilityOption.Cost)
                : _unlockAbilityUnavailableText;

            UIExtensions.SetTextSafe(_unlockAbilityButtonLabel, label);
        }

        private void HandleUnlockAbilityClicked()
        {
            if (_selectedLockedAbilityIndex < 0 || _selectedLockedAbilityIndex >= _lockedAbilityOptions.Count)
                return;

            ArcaneConduit.AbilityOption option = _lockedAbilityOptions[_selectedLockedAbilityIndex];
            if (option.CanPurchase == false)
                return;

            RequestAbilityUnlockByAbilityIndex(option.Index);
        }

        private void HandleEmptyState(UpgradeStation.ItemStatus status)
        {
            switch (status)
            {
                case UpgradeStation.ItemStatus.NoAgent:
                    SetEmptyState(_noAgentText);
                    break;
                case UpgradeStation.ItemStatus.NoInventory:
                    SetEmptyState(_noInventoryText);
                    break;
                case UpgradeStation.ItemStatus.NoItems:
                default:
                    SetEmptyState(_noItemsText);
                    break;
            }
        }

        private void SetEmptyState(string message)
        {
            if (_emptyStateLabel == null)
                return;

            UIExtensions.SetTextSafe(_emptyStateLabel, message);
            _emptyStateLabel.gameObject.SetActive(string.IsNullOrWhiteSpace(message) == false);
        }

        private static bool AreItemListsEqual(List<UpgradeStation.ItemData> current, List<UpgradeStation.ItemData> previous)
        {
            if (current == null || previous == null)
                return false;

            if (current.Count != previous.Count)
                return false;

            for (int i = 0; i < current.Count; ++i)
            {
                if (current[i].Equals(previous[i]) == false)
                    return false;
            }

            return true;
        }

        private static void CopyItems(List<UpgradeStation.ItemData> source, List<UpgradeStation.ItemData> destination)
        {
            destination.Clear();

            if (source == null)
                return;

            destination.AddRange(source);
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < _spawnedSlots.Count; ++i)
            {
                UIListItem slot = _spawnedSlots[i];
                if (slot == null || slot.gameObject.activeSelf == false)
                    continue;

                bool isSelected = i == _selectedIndex;
                slot.SetSelectionHighlight(isSelected, _selectedSlotColor);
            }
        }

        void IUIListItemOwner.BeginSlotDrag(UIListItem slot, PointerEventData eventData)
        {
            // Drag & drop is not supported within the conduit view yet.
        }

        void IUIListItemOwner.UpdateSlotDrag(PointerEventData eventData)
        {
            // Drag & drop is not supported within the conduit view yet.
        }

        void IUIListItemOwner.EndSlotDrag(UIListItem slot, PointerEventData eventData)
        {
            // Drag & drop is not supported within the conduit view yet.
        }

        void IUIListItemOwner.HandleSlotDrop(UIListItem source, UIListItem target)
        {
            // Drag & drop is not supported within the conduit view yet.
        }

        void IUIListItemOwner.HandleSlotDropOutside(UIListItem slot, PointerEventData eventData)
        {
            // Drag & drop is not supported within the conduit view yet.
        }

        void IUIListItemOwner.HandleSlotSelected(UIListItem slot)
        {
            if (slot == null)
            {
                return;
            }

            int slotIndex = slot.Index;

            int itemSlotIndex = _spawnedSlots.IndexOf(slot);
            if (itemSlotIndex >= 0)
            {
                if (slotIndex < 0 || slotIndex >= _lastItems.Count)
                {
                    _selectedIndex = -1;
                    UpdateSelectionVisuals();
                    return;
                }

                if (_selectedIndex == slotIndex)
                {
                    ItemSelected?.Invoke(_lastItems[slotIndex]);
                    return;
                }

                _selectedIndex = slotIndex;
                UpdateSelectionVisuals();
                ItemSelected?.Invoke(_lastItems[slotIndex]);
                return;
            }

            int lockedSlotIndex = _lockedAbilitySlots.IndexOf(slot);
            if (lockedSlotIndex >= 0)
            {
                if (_selectedLockedAbilityIndex == lockedSlotIndex)
                {
                    UpdateLockedAbilitySelectionVisuals();
                }
                else
                {
                    _selectedLockedAbilityIndex = lockedSlotIndex;
                    UpdateLockedAbilitySelectionVisuals();
                    UpdateUnlockButtonState();
                }

                return;
            }

            int unlockedSlotIndex = _unlockedAbilitySlots.IndexOf(slot);
            if (unlockedSlotIndex >= 0)
            {
                if (unlockedSlotIndex < _unlockedAbilityOptions.Count)
                {
                    ArcaneConduit.AbilityOption option = _unlockedAbilityOptions[unlockedSlotIndex];
                    UpdateSelectedAbilityDetails(option);
                }
            }
        }
    }
}
