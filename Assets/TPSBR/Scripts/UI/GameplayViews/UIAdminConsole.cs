using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using TSS.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIAdminConsole : UICloseView
    {
        private const int MAX_LOG_ENTRIES = 200;

        private enum Tab
        {
            Console,
            Inventory
        }

        private enum InventoryAction
        {
            AddItem = 0,
            AddGold = 1
        }

        [Header("Tabs")]
        [SerializeField] private UIButton _consoleTabButton;
        [SerializeField] private UIButton _inventoryTabButton;
        [SerializeField] private GameObject _consolePanel;
        [SerializeField] private GameObject _inventoryPanel;

        [Header("Console")]
        [SerializeField] private ScrollRect _logScrollRect;
        [SerializeField] private TextMeshProUGUI _logText;

        [Header("Inventory")]
        [SerializeField] private TMP_Dropdown _inventoryActionDropdown;
        [SerializeField] private GameObject _addItemContainer;
        [SerializeField] private TMP_Dropdown _itemDropdown;
        [SerializeField] private TMP_InputField _itemFilterInput;
        [SerializeField] private GameObject _addGoldContainer;
        [SerializeField] private TMP_InputField _goldAmountInput;
        [SerializeField] private UIButton _submitButton;
        [SerializeField] private TextMeshProUGUI _statusLabel;

        private readonly List<string> _logEntries = new List<string>(MAX_LOG_ENTRIES);
        private readonly StringBuilder _logBuilder = new StringBuilder(4096);
        private readonly List<ItemDefinition> _allItemDefinitions = new List<ItemDefinition>(128);
        private readonly List<ItemDefinition> _filteredItemDefinitions = new List<ItemDefinition>(128);

        private Tab _currentTab = Tab.Console;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_consoleTabButton != null)
            {
                _consoleTabButton.onClick.AddListener(OnConsoleTabClicked);
            }

            if (_inventoryTabButton != null)
            {
                _inventoryTabButton.onClick.AddListener(OnInventoryTabClicked);
            }

            if (_inventoryActionDropdown != null)
            {
                _inventoryActionDropdown.onValueChanged.AddListener(OnInventoryActionChanged);
            }

            if (_itemFilterInput != null)
            {
                _itemFilterInput.onValueChanged.AddListener(OnItemFilterChanged);
            }

            if (_goldAmountInput != null)
            {
                _goldAmountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                _goldAmountInput.onValueChanged.AddListener(OnGoldAmountChanged);
            }

            if (_submitButton != null)
            {
                _submitButton.onClick.AddListener(OnSubmitClicked);
            }

            Application.logMessageReceived += HandleLogMessage;

            LoadItemDefinitions();
            ApplyItemFilter();
            RefreshInventoryAction();
            ShowTab(Tab.Console);
            UpdateSubmitState();
            SetStatusMessage(string.Empty);
        }

        protected override void OnDeinitialize()
        {
            Application.logMessageReceived -= HandleLogMessage;

            if (_consoleTabButton != null)
            {
                _consoleTabButton.onClick.RemoveListener(OnConsoleTabClicked);
            }

            if (_inventoryTabButton != null)
            {
                _inventoryTabButton.onClick.RemoveListener(OnInventoryTabClicked);
            }

            if (_inventoryActionDropdown != null)
            {
                _inventoryActionDropdown.onValueChanged.RemoveListener(OnInventoryActionChanged);
            }

            if (_itemFilterInput != null)
            {
                _itemFilterInput.onValueChanged.RemoveListener(OnItemFilterChanged);
            }

            if (_goldAmountInput != null)
            {
                _goldAmountInput.onValueChanged.RemoveListener(OnGoldAmountChanged);
            }

            if (_submitButton != null)
            {
                _submitButton.onClick.RemoveListener(OnSubmitClicked);
            }

            base.OnDeinitialize();
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            ShowTab(Tab.Console);
            RefreshInventoryAction();
            ApplyItemFilter();
            UpdateSubmitState();
        }

        private void OnConsoleTabClicked()
        {
            ShowTab(Tab.Console);
        }

        private void OnInventoryTabClicked()
        {
            ShowTab(Tab.Inventory);
        }

        private void OnInventoryActionChanged(int _)
        {
            RefreshInventoryAction();
            UpdateSubmitState();
        }

        private void OnItemFilterChanged(string _)
        {
            ApplyItemFilter();
            UpdateSubmitState();
        }

        private void OnGoldAmountChanged(string _)
        {
            UpdateSubmitState();
        }

        private void OnSubmitClicked()
        {
            var agent = Context != null ? Context.ObservedAgent : null;
            if (agent == null)
            {
                AppendLocalMessage("No observed agent is available.");
                return;
            }

            var inventory = agent.Inventory;
            if (inventory == null)
            {
                AppendLocalMessage("Observed agent has no inventory component.");
                return;
            }

            switch (GetSelectedAction())
            {
                case InventoryAction.AddItem:
                    HandleAddItem(inventory);
                    break;
                case InventoryAction.AddGold:
                    HandleAddGold(inventory);
                    break;
            }
        }

        private void HandleAddItem(Inventory inventory)
        {
            if (_filteredItemDefinitions.Count == 0)
            {
                AppendLocalMessage("No items match the current filter.");
                return;
            }

            int selectedIndex = 0;
            if (_itemDropdown != null && _filteredItemDefinitions.Count > 0)
            {
                selectedIndex = Mathf.Clamp(_itemDropdown.value, 0, _filteredItemDefinitions.Count - 1);
            }

            var definition = _filteredItemDefinitions[selectedIndex];
            if (definition == null)
            {
                AppendLocalMessage("Unable to determine the selected item definition.");
                return;
            }

            inventory.RequestAddItem(definition, 1);
            AppendLocalMessage($"Requested '{definition.Name}' (ID: {definition.ID}) for the player.");
        }

        private void HandleAddGold(Inventory inventory)
        {
            if (_goldAmountInput == null)
            {
                AppendLocalMessage("Gold input field is missing.");
                return;
            }

            if (int.TryParse(_goldAmountInput.text, out int amount) == false || amount <= 0)
            {
                AppendLocalMessage("Enter a valid gold amount greater than zero.");
                return;
            }

            inventory.RequestAddGold(amount);
            AppendLocalMessage($"Requested {amount} gold for the player.");
            _goldAmountInput.SetTextWithoutNotify(string.Empty);
            UpdateSubmitState();
        }

        private void ShowTab(Tab tab)
        {
            _currentTab = tab;

            if (_consolePanel != null)
            {
                _consolePanel.SetActive(tab == Tab.Console);
            }

            if (_inventoryPanel != null)
            {
                _inventoryPanel.SetActive(tab == Tab.Inventory);
            }

            if (_consoleTabButton != null)
            {
                _consoleTabButton.interactable = tab != Tab.Console;
            }

            if (_inventoryTabButton != null)
            {
                _inventoryTabButton.interactable = tab != Tab.Inventory;
            }
        }

        private void RefreshInventoryAction()
        {
            var action = GetSelectedAction();

            if (_addItemContainer != null)
            {
                _addItemContainer.SetActive(action == InventoryAction.AddItem);
            }

            if (_addGoldContainer != null)
            {
                _addGoldContainer.SetActive(action == InventoryAction.AddGold);
            }
        }

        private InventoryAction GetSelectedAction()
        {
            int value = _inventoryActionDropdown != null ? _inventoryActionDropdown.value : 0;
            return value == (int)InventoryAction.AddGold ? InventoryAction.AddGold : InventoryAction.AddItem;
        }

        private void UpdateSubmitState()
        {
            if (_submitButton == null)
                return;

            bool canSubmit = false;
            switch (GetSelectedAction())
            {
                case InventoryAction.AddItem:
                    canSubmit = _filteredItemDefinitions.Count > 0;
                    break;
                case InventoryAction.AddGold:
                    if (_goldAmountInput != null && int.TryParse(_goldAmountInput.text, out int amount) && amount > 0)
                    {
                        canSubmit = true;
                    }
                    break;
            }

            _submitButton.interactable = canSubmit;
        }

        private void LoadItemDefinitions()
        {
            _allItemDefinitions.Clear();

            ItemDefinition.LoadAll();
            var definitions = Resources.LoadAll<ItemDefinition>(string.Empty);

            for (int i = 0; i < definitions.Length; ++i)
            {
                var definition = definitions[i];
                if (definition == null)
                    continue;

                bool alreadyAdded = false;
                for (int j = 0; j < _allItemDefinitions.Count; ++j)
                {
                    if (_allItemDefinitions[j].ID == definition.ID)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (alreadyAdded == false)
                {
                    _allItemDefinitions.Add(definition);
                }
            }

            _allItemDefinitions.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyItemFilter()
        {
            string filter = _itemFilterInput != null ? _itemFilterInput.text : string.Empty;

            _filteredItemDefinitions.Clear();

            for (int i = 0; i < _allItemDefinitions.Count; ++i)
            {
                var definition = _allItemDefinitions[i];
                if (definition == null)
                    continue;

                if (string.IsNullOrEmpty(filter) == false &&
                    definition.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                _filteredItemDefinitions.Add(definition);
            }

            RefreshItemDropdownOptions();
        }

        private void RefreshItemDropdownOptions()
        {
            if (_itemDropdown == null)
                return;

            _itemDropdown.options.Clear();

            for (int i = 0; i < _filteredItemDefinitions.Count; ++i)
            {
                var definition = _filteredItemDefinitions[i];
                var option = new TMP_Dropdown.OptionData(definition.Name, definition.IconSprite);
                _itemDropdown.options.Add(option);
            }

            int optionCount = _itemDropdown.options.Count;
            if (optionCount > 0)
            {
                _itemDropdown.SetValueWithoutNotify(Mathf.Clamp(_itemDropdown.value, 0, optionCount - 1));
            }
            else
            {
                _itemDropdown.SetValueWithoutNotify(0);
            }

            _itemDropdown.RefreshShownValue();
        }

        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string message = $"[{timestamp}] [{type}] {condition}";

            if (type == LogType.Error || type == LogType.Exception)
            {
                if (string.IsNullOrEmpty(stackTrace) == false)
                {
                    message += Environment.NewLine + stackTrace;
                }
            }

            AppendLogEntry(message);
        }

        private void AppendLocalMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            SetStatusMessage(message);
            AppendLogEntry($"[Admin] {message}");
        }

        private void SetStatusMessage(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = message;
            }
        }

        private void AppendLogEntry(string entry)
        {
            if (string.IsNullOrEmpty(entry))
                return;

            if (_logEntries.Count >= MAX_LOG_ENTRIES)
            {
                int removeCount = _logEntries.Count - MAX_LOG_ENTRIES + 1;
                _logEntries.RemoveRange(0, removeCount);
            }

            _logEntries.Add(entry);
            RefreshLogOutput();
        }

        private void RefreshLogOutput()
        {
            if (_logText == null)
                return;

            _logBuilder.Clear();

            for (int i = 0; i < _logEntries.Count; ++i)
            {
                if (i > 0)
                {
                    _logBuilder.AppendLine();
                }

                _logBuilder.Append(_logEntries[i]);
            }

            _logText.text = _logBuilder.ToString();

            if (_logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _logScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
}
