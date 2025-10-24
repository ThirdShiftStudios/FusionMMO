using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using TSS.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public sealed class UIAdminConsoleView : UICloseView
    {
        public override bool NeedsCursor => IsOpen;
        private const int MaxLogEntries = 200;

        [Header("Tabs")]
        [SerializeField]
        private UIButton _consoleTabButton;
        [SerializeField]
        private UIButton _inventoryTabButton;
        [SerializeField]
        private RectTransform _consoleContent;
        [SerializeField]
        private RectTransform _inventoryContent;

        [Header("Console")]
        [SerializeField]
        private ScrollRect _logScrollRect;
        [SerializeField]
        private TextMeshProUGUI _logText;

        [Header("Inventory Tools")]
        [SerializeField]
        private TMP_Dropdown _inventoryActionDropdown;
        [SerializeField]
        private UIButton _printInventoryButton;
        [SerializeField]
        private RectTransform _addItemContainer;
        [SerializeField]
        private TMP_InputField _itemSearchInput;
        [SerializeField]
        private TMP_Dropdown _itemDropdown;
        [SerializeField]
        private UIButton _itemSubmitButton;
        [SerializeField]
        private RectTransform _addGoldContainer;
        [SerializeField]
        private TMP_InputField _goldAmountInput;
        [SerializeField]
        private UIButton _goldSubmitButton;

        private readonly List<string> _logLines = new List<string>(MaxLogEntries + 1);
        private readonly StringBuilder _logBuilder = new StringBuilder(4096);
        private readonly List<ItemDefinition> _allItems = new List<ItemDefinition>();
        private readonly List<ItemDefinition> _filteredItems = new List<ItemDefinition>();

        private bool _shouldScrollToBottom;
        private InventoryAction _currentInventoryAction = InventoryAction.AddItem;
        private Tab _activeTab = Tab.Console;
        private bool _menuVisible;

        public bool MenuVisible => _menuVisible;

        private enum InventoryAction
        {
            AddItem = 0,
            AddGold = 1,
        }

        private enum Tab
        {
            Console,
            Inventory
        }

        // UICloseView

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_consoleTabButton != null)
            {
                _consoleTabButton.onClick.AddListener(HandleConsoleTabClicked);
            }

            if (_inventoryTabButton != null)
            {
                _inventoryTabButton.onClick.AddListener(HandleInventoryTabClicked);
            }

            if (_inventoryActionDropdown != null)
            {
                _inventoryActionDropdown.onValueChanged.AddListener(HandleInventoryActionChanged);
            }

            if (_printInventoryButton != null)
            {
                _printInventoryButton.onClick.AddListener(HandlePrintInventoryClicked);
            }

            if (_itemSearchInput != null)
            {
                _itemSearchInput.onValueChanged.AddListener(HandleItemSearchChanged);
            }

            if (_itemSubmitButton != null)
            {
                _itemSubmitButton.onClick.AddListener(HandleItemSubmitClicked);
            }

            if (_goldSubmitButton != null)
            {
                _goldSubmitButton.onClick.AddListener(HandleGoldSubmitClicked);
            }

            Application.logMessageReceived += HandleLogMessageReceived;

            LoadItems();
            RefreshItemOptions();
            ApplyInventoryActionState();
            ApplyTabState();
        }

        protected override void OnDeinitialize()
        {
            Application.logMessageReceived -= HandleLogMessageReceived;

            if (_consoleTabButton != null)
            {
                _consoleTabButton.onClick.RemoveListener(HandleConsoleTabClicked);
            }

            if (_inventoryTabButton != null)
            {
                _inventoryTabButton.onClick.RemoveListener(HandleInventoryTabClicked);
            }

            if (_inventoryActionDropdown != null)
            {
                _inventoryActionDropdown.onValueChanged.RemoveListener(HandleInventoryActionChanged);
            }

            if (_printInventoryButton != null)
            {
                _printInventoryButton.onClick.RemoveListener(HandlePrintInventoryClicked);
            }

            if (_itemSearchInput != null)
            {
                _itemSearchInput.onValueChanged.RemoveListener(HandleItemSearchChanged);
            }

            if (_itemSubmitButton != null)
            {
                _itemSubmitButton.onClick.RemoveListener(HandleItemSubmitClicked);
            }

            if (_goldSubmitButton != null)
            {
                _goldSubmitButton.onClick.RemoveListener(HandleGoldSubmitClicked);
            }

            base.OnDeinitialize();
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            RefreshItemOptions();
            ApplyInventoryActionState();
            ApplyTabState();
        }

        protected override void OnClose()
        {
            base.OnClose();
        }

        protected override void OnTick()
        {
            base.OnTick();

            if (_shouldScrollToBottom == true && _logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _logScrollRect.verticalNormalizedPosition = 0f;
                _shouldScrollToBottom = false;
            }
        }

        // PRIVATE METHODS

        private void HandleConsoleTabClicked()
        {
            _activeTab = Tab.Console;
            ApplyTabState();
        }

        private void HandleInventoryTabClicked()
        {
            _activeTab = Tab.Inventory;
            ApplyTabState();
        }

        private void HandleInventoryActionChanged(int value)
        {
            _currentInventoryAction = (InventoryAction)Mathf.Clamp(value, 0, Enum.GetValues(typeof(InventoryAction)).Length - 1);
            ApplyInventoryActionState();
        }

        private void HandlePrintInventoryClicked()
        {
            Inventory inventory = GetLocalInventory();
            if (inventory == null)
            {
                AppendSystemLog("No controlled agent to inspect inventory.", LogType.Warning);
                return;
            }

            StringBuilder builder = new StringBuilder(512);
            builder.AppendLine("Inventory snapshot:");

            builder.AppendLine("Hotbar Slots:");
            int hotbarSize = inventory.HotbarSize;
            for (int i = 0; i < hotbarSize; ++i)
            {
                Weapon weapon = inventory.GetHotbarWeapon(i);
                ItemDefinition definition = weapon != null ? weapon.Definition : null;
                string fallbackName = weapon != null ? (!string.IsNullOrEmpty(weapon.DisplayName) ? weapon.DisplayName : weapon.name) : null;
                int? fallbackId = weapon != null ? weapon.WeaponID : (int?)null;
                AppendInventorySlotLine(builder, "Hotbar", i, definition, 0, fallbackName, fallbackId);
            }

            builder.AppendLine();
            builder.AppendLine("General Inventory Slots:");
            int inventorySize = inventory.InventorySize;
            for (int i = 0; i < inventorySize; ++i)
            {
                InventorySlot slot = inventory.GetItemSlot(i);
                ItemDefinition definition = slot.GetDefinition();
                AppendInventorySlotLine(builder, "General", i, definition, slot.Quantity);
            }

            builder.AppendLine();
            builder.AppendLine("Equipment Slots:");
            AppendEquipmentSlotLine(builder, inventory, "Pickaxe", Inventory.PICKAXE_SLOT_INDEX);
            AppendEquipmentSlotLine(builder, inventory, "Wood Axe", Inventory.WOOD_AXE_SLOT_INDEX);
            AppendEquipmentSlotLine(builder, inventory, "Fishing Pole", Inventory.FISHING_POLE_SLOT_INDEX);
            AppendEquipmentSlotLine(builder, inventory, "Head", Inventory.HEAD_SLOT_INDEX);
            AppendEquipmentSlotLine(builder, inventory, "Upper Body", Inventory.UPPER_BODY_SLOT_INDEX);
            AppendEquipmentSlotLine(builder, inventory, "Lower Body", Inventory.LOWER_BODY_SLOT_INDEX);

            AppendSystemLog(builder.ToString(), LogType.Log);
        }

        private static void AppendEquipmentSlotLine(StringBuilder builder, Inventory inventory, string slotType, int slotIndex)
        {
            InventorySlot slot = inventory.GetItemSlot(slotIndex);
            ItemDefinition definition = slot.GetDefinition();
            AppendInventorySlotLine(builder, slotType, slotIndex, definition, slot.Quantity);
        }

        private static void AppendInventorySlotLine(StringBuilder builder, string slotType, int slotIndex, ItemDefinition definition, byte quantity = 0, string fallbackName = null, int? fallbackId = null)
        {
            builder.Append("  - ");
            builder.Append(slotType);
            builder.Append(" (Index ");
            builder.Append(slotIndex);
            builder.Append("): ");

            if (definition == null && string.IsNullOrEmpty(fallbackName) == true)
            {
                builder.Append("Empty");
                builder.AppendLine();
                return;
            }

            string name = definition != null ? (!string.IsNullOrEmpty(definition.Name) ? definition.Name : definition.name) : fallbackName;
            if (string.IsNullOrEmpty(name) == true)
            {
                name = "Unnamed Item";
            }

            int id = definition != null ? definition.ID : (fallbackId ?? 0);

            builder.Append(name);

            if (id != 0)
            {
                builder.Append(" (ID: ");
                builder.Append(id);
                builder.Append(')');
            }

            if (quantity > 1)
            {
                builder.Append(" x");
                builder.Append(quantity);
            }

            builder.AppendLine();
        }

        private void HandleItemSearchChanged(string value)
        {
            RefreshItemOptions();
        }

        private void HandleItemSubmitClicked()
        {
            Inventory inventory = GetLocalInventory();
            if (inventory == null)
            {
                AppendSystemLog("No controlled agent to receive items.", LogType.Warning);
                return;
            }

            if (_itemDropdown == null || _filteredItems.Count == 0)
            {
                AppendSystemLog("No item selected.", LogType.Warning);
                return;
            }

            int index = Mathf.Clamp(_itemDropdown.value, 0, _filteredItems.Count - 1);
            ItemDefinition definition = _filteredItems[index];
            if (definition == null)
            {
                AppendSystemLog("Selected item definition is missing.", LogType.Warning);
                return;
            }

            inventory.RequestAddItem(definition, 1);
            AppendSystemLog($"Requested item '{definition.Name}' added to inventory.", LogType.Log);
        }

        private void HandleGoldSubmitClicked()
        {
            Inventory inventory = GetLocalInventory();
            if (inventory == null)
            {
                AppendSystemLog("No controlled agent to receive gold.", LogType.Warning);
                return;
            }

            if (_goldAmountInput == null)
            {
                AppendSystemLog("Gold amount input not configured.", LogType.Warning);
                return;
            }

            string text = _goldAmountInput.text;
            if (string.IsNullOrWhiteSpace(text) == true)
            {
                AppendSystemLog("Enter an amount of gold to add.", LogType.Warning);
                return;
            }

            if (int.TryParse(text, out int amount) == false || amount <= 0)
            {
                AppendSystemLog("Gold amount must be a positive whole number.", LogType.Warning);
                return;
            }

            inventory.RequestAddGold(amount);
            AppendSystemLog($"Requested +{amount} gold added to inventory.", LogType.Log);
        }

        private void HandleLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            string sanitizedCondition = Sanitize(condition);
            string sanitizedStackTrace = string.IsNullOrEmpty(stackTrace) == false ? Sanitize(stackTrace) : string.Empty;

            string prefix = type switch
            {
                LogType.Error => "<color=#ff6b6b>[Error]</color> ",
                LogType.Exception => "<color=#ff6b6b>[Exception]</color> ",
                LogType.Warning => "<color=#ffd166>[Warning]</color> ",
                LogType.Assert => "<color=#ffd166>[Assert]</color> ",
                _ => "<color=#8ecae6>[Log]</color> "
            };

            string formatted = string.IsNullOrEmpty(sanitizedStackTrace) == false
                    ? $"{prefix}{sanitizedCondition}\n{sanitizedStackTrace}"
                    : $"{prefix}{sanitizedCondition}";

            _logLines.Add(formatted);
            if (_logLines.Count > MaxLogEntries)
            {
                _logLines.RemoveAt(0);
            }

            if (_logText != null)
            {
                _logBuilder.Clear();
                for (int i = 0, count = _logLines.Count; i < count; ++i)
                {
                    if (i > 0)
                    {
                        _logBuilder.Append('\n');
                    }

                    _logBuilder.Append(_logLines[i]);
                }

                _logText.text = _logBuilder.ToString();
                _shouldScrollToBottom = true;
            }
        }

        private void AppendSystemLog(string message, LogType type)
        {
            HandleLogMessageReceived(message, string.Empty, type);
        }

        private void ApplyTabState()
        {
            bool consoleActive = _activeTab == Tab.Console;

            if (_consoleContent != null)
            {
                _consoleContent.gameObject.SetActive(consoleActive);
            }

            if (_inventoryContent != null)
            {
                _inventoryContent.gameObject.SetActive(!consoleActive);
            }

            if (_consoleTabButton != null)
            {
                _consoleTabButton.interactable = !consoleActive;
            }

            if (_inventoryTabButton != null)
            {
                _inventoryTabButton.interactable = consoleActive;
            }
        }

        private void ApplyInventoryActionState()
        {
            bool addItem = _currentInventoryAction == InventoryAction.AddItem;

            if (_addItemContainer != null)
            {
                _addItemContainer.gameObject.SetActive(addItem);
            }

            if (_addGoldContainer != null)
            {
                _addGoldContainer.gameObject.SetActive(!addItem);
            }
        }

        private void RefreshItemOptions()
        {
            if (_itemDropdown == null)
                return;

            string search = _itemSearchInput != null ? _itemSearchInput.text : string.Empty;
            search = string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim().ToLowerInvariant();

            _filteredItems.Clear();

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>(_allItems.Count);

            for (int i = 0; i < _allItems.Count; ++i)
            {
                ItemDefinition definition = _allItems[i];
                if (definition == null)
                    continue;

                string name = definition.Name ?? definition.name;
                if (string.IsNullOrEmpty(name) == true)
                    continue;

                if (search.Length > 0 && name.ToLowerInvariant().Contains(search) == false)
                    continue;

                TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(name, definition.IconSprite);
                options.Add(option);
                _filteredItems.Add(definition);
            }

            if (_filteredItems.Count == 0)
            {
                options.Add(new TMP_Dropdown.OptionData("No items found"));
            }

            _itemDropdown.options = options;
            _itemDropdown.value = 0;
            _itemDropdown.RefreshShownValue();
        }

        private void LoadItems()
        {
            _allItems.Clear();

            ItemDefinition.LoadAll();
            ItemDefinition[] definitions = Resources.LoadAll<ItemDefinition>(string.Empty);
            if (definitions == null || definitions.Length == 0)
                return;

            Array.Sort(definitions, (a, b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase));
            for (int i = 0; i < definitions.Length; ++i)
            {
                if (definitions[i] != null && _allItems.Contains(definitions[i]) == false)
                {
                    _allItems.Add(definitions[i]);
                }
            }
        }

        private Inventory GetLocalInventory()
        {
            Agent agent = Context != null ? Context.ObservedAgent : null;
            return agent != null ? agent.Inventory : null;
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value) == true)
                return string.Empty;

            return value.Replace("<", "&lt;").Replace(">", "&gt;");
        }

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
    }
}
