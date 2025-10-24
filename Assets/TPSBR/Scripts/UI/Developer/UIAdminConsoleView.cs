using System;
using System.Collections.Generic;
using System.Text;
using TSS.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIAdminConsoleView : UIView
    {
        private enum Tab
        {
            Console,
            Inventory,
        }

        private enum InventoryAction
        {
            AddItem,
            AddGold,
        }

        private struct DropdownItemComponents
        {
            public Text Text;
            public Image Icon;
        }

        private UIButton _consoleTabButton;
        private UIButton _inventoryTabButton;
        private RectTransform _consolePanel;
        private RectTransform _inventoryPanel;

        private ScrollRect _logScrollRect;
        private Text _logLabel;

        private Dropdown _inventoryActionDropdown;
        private GameObject _addItemPanel;
        private GameObject _addGoldPanel;

        private InputField _itemSearchField;
        private Dropdown _itemDropdown;
        private Image _itemDropdownCaptionIcon;
        private UIButton _addItemSubmitButton;

        private InputField _goldAmountField;
        private UIButton _addGoldSubmitButton;

        private readonly StringBuilder _logBuilder = new StringBuilder(1024);
        private readonly Queue<string> _pendingLogLines = new Queue<string>();
        private readonly object _logLock = new object();

        private readonly List<ItemDefinition> _filteredItemDefinitions = new List<ItemDefinition>();
        private ItemDefinition[] _allItemDefinitions;

        private Tab _currentTab = Tab.Console;
        private InventoryAction _currentInventoryAction = InventoryAction.AddItem;
        private bool _isLogDirty;
        private bool _isSubscribedToLogs;
        private bool _layoutBuilt;
        private Font _defaultFont;

        // UIView -------------------------------------------------------------------------------

        protected override void OnInitialize()
        {
            base.OnInitialize();

            BuildLayout();

            if (_consoleTabButton != null)
            {
                _consoleTabButton.onClick.AddListener(() => SetActiveTab(Tab.Console));
            }

            if (_inventoryTabButton != null)
            {
                _inventoryTabButton.onClick.AddListener(() => SetActiveTab(Tab.Inventory));
            }

            if (_inventoryActionDropdown != null)
            {
                _inventoryActionDropdown.onValueChanged.AddListener(OnInventoryActionChanged);
                _inventoryActionDropdown.ClearOptions();
                _inventoryActionDropdown.AddOptions(new List<Dropdown.OptionData>
                {
                    new Dropdown.OptionData("Add Item"),
                    new Dropdown.OptionData("Add Gold"),
                });
            }

            if (_itemSearchField != null)
            {
                _itemSearchField.onValueChanged.AddListener(OnItemSearchChanged);
            }

            if (_itemDropdown != null)
            {
                _itemDropdown.onValueChanged.AddListener(OnItemSelected);
            }

            if (_addItemSubmitButton != null)
            {
                _addItemSubmitButton.onClick.AddListener(OnAddItemClicked);
            }

            if (_addGoldSubmitButton != null)
            {
                _addGoldSubmitButton.onClick.AddListener(OnAddGoldClicked);
            }

            LoadItemDefinitions();
            RefreshItemDropdown();
            SetActiveTab(_currentTab);
            ApplyInventoryAction(_currentInventoryAction);
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();

            if (_consoleTabButton != null)
            {
                _consoleTabButton.onClick.RemoveAllListeners();
            }

            if (_inventoryTabButton != null)
            {
                _inventoryTabButton.onClick.RemoveAllListeners();
            }

            if (_inventoryActionDropdown != null)
            {
                _inventoryActionDropdown.onValueChanged.RemoveListener(OnInventoryActionChanged);
            }

            if (_itemSearchField != null)
            {
                _itemSearchField.onValueChanged.RemoveListener(OnItemSearchChanged);
            }

            if (_itemDropdown != null)
            {
                _itemDropdown.onValueChanged.RemoveListener(OnItemSelected);
            }

            if (_addItemSubmitButton != null)
            {
                _addItemSubmitButton.onClick.RemoveListener(OnAddItemClicked);
            }

            if (_addGoldSubmitButton != null)
            {
                _addGoldSubmitButton.onClick.RemoveListener(OnAddGoldClicked);
            }
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            SubscribeToLogs();
            SetActiveTab(Tab.Console);
            ApplyInventoryAction(_currentInventoryAction);
        }

        protected override void OnClose()
        {
            base.OnClose();

            UnsubscribeFromLogs();
        }

        protected override void OnTick()
        {
            base.OnTick();

            FlushLogQueue();
        }

        // PRIVATE -----------------------------------------------------------------------------

        private void BuildLayout()
        {
            if (_layoutBuilt == true)
                return;

            _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var root = RectTransform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var background = CreateUIObject("Background", root);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.05f, 0.05f);
            backgroundRect.anchorMax = new Vector2(0.95f, 0.95f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

            var tabBar = CreateUIObject("TabBar", backgroundRect);
            var tabBarRect = tabBar.GetComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0f, 1f);
            tabBarRect.anchorMax = new Vector2(1f, 1f);
            tabBarRect.pivot = new Vector2(0.5f, 1f);
            tabBarRect.sizeDelta = new Vector2(0f, 50f);
            tabBarRect.anchoredPosition = new Vector2(0f, -10f);

            _consoleTabButton = CreateButton("ConsoleTab", tabBarRect, "Console");
            var consoleButtonRect = _consoleTabButton.GetComponent<RectTransform>();
            consoleButtonRect.anchorMin = new Vector2(0f, 0f);
            consoleButtonRect.anchorMax = new Vector2(0f, 1f);
            consoleButtonRect.pivot = new Vector2(0f, 0.5f);
            consoleButtonRect.sizeDelta = new Vector2(160f, 40f);
            consoleButtonRect.anchoredPosition = new Vector2(20f, 0f);

            _inventoryTabButton = CreateButton("InventoryTab", tabBarRect, "Inventory");
            var inventoryButtonRect = _inventoryTabButton.GetComponent<RectTransform>();
            inventoryButtonRect.anchorMin = new Vector2(0f, 0f);
            inventoryButtonRect.anchorMax = new Vector2(0f, 1f);
            inventoryButtonRect.pivot = new Vector2(0f, 0.5f);
            inventoryButtonRect.sizeDelta = new Vector2(160f, 40f);
            inventoryButtonRect.anchoredPosition = new Vector2(200f, 0f);

            var content = CreateUIObject("Content", backgroundRect);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(20f, 20f);
            contentRect.offsetMax = new Vector2(-20f, -80f);

            _consolePanel = CreateUIObject("ConsolePanel", contentRect).GetComponent<RectTransform>();
            _consolePanel.anchorMin = Vector2.zero;
            _consolePanel.anchorMax = Vector2.one;
            _consolePanel.offsetMin = Vector2.zero;
            _consolePanel.offsetMax = Vector2.zero;

            BuildConsolePanel(_consolePanel);

            _inventoryPanel = CreateUIObject("InventoryPanel", contentRect).GetComponent<RectTransform>();
            _inventoryPanel.anchorMin = Vector2.zero;
            _inventoryPanel.anchorMax = Vector2.one;
            _inventoryPanel.offsetMin = Vector2.zero;
            _inventoryPanel.offsetMax = Vector2.zero;

            BuildInventoryPanel(_inventoryPanel);

            _layoutBuilt = true;
        }

        private void BuildConsolePanel(RectTransform parent)
        {
            var scrollObject = CreateUIObject("LogScrollView", parent);
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;

            var scrollImage = scrollObject.AddComponent<Image>();
            scrollImage.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

            _logScrollRect = scrollObject.AddComponent<ScrollRect>();
            _logScrollRect.horizontal = false;
            _logScrollRect.vertical = true;
            _logScrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateUIObject("Viewport", scrollRectTransform);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(10f, 10f);
            viewportRect.offsetMax = new Vector2(-10f, -10f);

            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);

            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = CreateUIObject("Content", viewportRect);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = new Vector2(0f, 0f);
            contentRect.sizeDelta = new Vector2(0f, 0f);

            _logLabel = content.AddComponent<Text>();
            _logLabel.font = _defaultFont;
            _logLabel.fontSize = 18;
            _logLabel.color = Color.white;
            _logLabel.alignment = TextAnchor.UpperLeft;
            _logLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _logLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _logLabel.text = string.Empty;

            _logScrollRect.content = contentRect;
            _logScrollRect.viewport = viewportRect;
        }

        private void BuildInventoryPanel(RectTransform parent)
        {
            var layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.UpperLeft;

            var contentSize = parent.gameObject.AddComponent<ContentSizeFitter>();
            contentSize.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var actionRow = CreateUIObject("ActionRow", parent);
            var actionRowLayout = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionRowLayout.spacing = 10f;
            actionRowLayout.childAlignment = TextAnchor.MiddleLeft;
            actionRowLayout.childForceExpandWidth = false;

            var actionLabel = CreateLabel("ActionLabel", actionRow.GetComponent<RectTransform>(), "Action:");
            actionLabel.alignment = TextAnchor.MiddleLeft;

            _inventoryActionDropdown = CreateDropdown("ActionDropdown", actionRow.GetComponent<RectTransform>(), out var actionCaptionIcon, out var actionContent);
            var actionTemplateComponents = CreateDropdownItem(_inventoryActionDropdown.template, actionContent);
            _inventoryActionDropdown.itemText = actionTemplateComponents.Text;
            _inventoryActionDropdown.itemImage = actionTemplateComponents.Icon;
            _inventoryActionDropdown.captionImage = actionCaptionIcon;

            _addItemPanel = CreateUIObject("AddItemPanel", parent);
            var addItemLayout = _addItemPanel.AddComponent<VerticalLayoutGroup>();
            addItemLayout.spacing = 8f;
            addItemLayout.childAlignment = TextAnchor.UpperLeft;
            addItemLayout.childForceExpandWidth = false;

            var searchRow = CreateUIObject("SearchRow", _addItemPanel.GetComponent<RectTransform>());
            var searchRowLayout = searchRow.AddComponent<HorizontalLayoutGroup>();
            searchRowLayout.spacing = 8f;
            searchRowLayout.childAlignment = TextAnchor.MiddleLeft;
            searchRowLayout.childForceExpandWidth = false;

            var searchLabel = CreateLabel("SearchLabel", searchRow.GetComponent<RectTransform>(), "Search:");
            searchLabel.alignment = TextAnchor.MiddleLeft;

            _itemSearchField = CreateInputField("SearchField", searchRow.GetComponent<RectTransform>(), "Filter items");
            _itemSearchField.characterValidation = InputField.CharacterValidation.None;

            var dropdownRow = CreateUIObject("ItemDropdownRow", _addItemPanel.GetComponent<RectTransform>());
            var dropdownLayout = dropdownRow.AddComponent<HorizontalLayoutGroup>();
            dropdownLayout.spacing = 8f;
            dropdownLayout.childAlignment = TextAnchor.MiddleLeft;
            dropdownLayout.childForceExpandWidth = false;

            var dropdownLabel = CreateLabel("ItemLabel", dropdownRow.GetComponent<RectTransform>(), "Item:");
            dropdownLabel.alignment = TextAnchor.MiddleLeft;

            _itemDropdown = CreateDropdown("ItemDropdown", dropdownRow.GetComponent<RectTransform>(), out _itemDropdownCaptionIcon, out var itemContent);
            var itemTemplateComponents = CreateDropdownItem(_itemDropdown.template, itemContent);
            _itemDropdown.itemText = itemTemplateComponents.Text;
            _itemDropdown.itemImage = itemTemplateComponents.Icon;

            _addItemSubmitButton = CreateButton("SubmitItemButton", _addItemPanel.GetComponent<RectTransform>(), "Add Item");

            _addGoldPanel = CreateUIObject("AddGoldPanel", parent);
            var addGoldLayout = _addGoldPanel.AddComponent<HorizontalLayoutGroup>();
            addGoldLayout.spacing = 8f;
            addGoldLayout.childAlignment = TextAnchor.MiddleLeft;
            addGoldLayout.childForceExpandWidth = false;

            var goldLabel = CreateLabel("GoldLabel", _addGoldPanel.GetComponent<RectTransform>(), "Amount:");
            goldLabel.alignment = TextAnchor.MiddleLeft;

            _goldAmountField = CreateInputField("GoldAmount", _addGoldPanel.GetComponent<RectTransform>(), "Enter gold amount");
            _goldAmountField.characterValidation = InputField.CharacterValidation.Integer;

            _addGoldSubmitButton = CreateButton("SubmitGoldButton", _addGoldPanel.GetComponent<RectTransform>(), "Add Gold");
        }

        private GameObject CreateUIObject(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            return go;
        }

        private UIButton CreateButton(string name, RectTransform parent, string text)
        {
            var go = CreateUIObject(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 40f);

            go.AddComponent<CanvasRenderer>();
            var image = go.AddComponent<Image>();
            image.color = new Color(0.24f, 0.32f, 0.44f, 1f);

            var button = go.AddComponent<UIButton>();
            button.targetGraphic = image;

            var label = CreateUIObject("Label", rect);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 5f);
            labelRect.offsetMax = new Vector2(-10f, -5f);

            var textComponent = label.AddComponent<Text>();
            textComponent.font = _defaultFont;
            textComponent.fontSize = 18;
            textComponent.color = Color.white;
            textComponent.text = text;
            textComponent.alignment = TextAnchor.MiddleCenter;

            return button;
        }

        private Text CreateLabel(string name, RectTransform parent, string text)
        {
            var go = CreateUIObject(name, parent);
            go.AddComponent<CanvasRenderer>();
            var textComponent = go.AddComponent<Text>();
            textComponent.font = _defaultFont;
            textComponent.fontSize = 18;
            textComponent.color = Color.white;
            textComponent.text = text;
            textComponent.alignment = TextAnchor.MiddleLeft;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 30f);
            return textComponent;
        }

        private InputField CreateInputField(string name, RectTransform parent, string placeholder)
        {
            var go = CreateUIObject(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 36f);

            go.AddComponent<CanvasRenderer>();
            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var input = go.AddComponent<InputField>();
            input.targetGraphic = image;

            var textObject = CreateUIObject("Text", rect);
            textObject.AddComponent<CanvasRenderer>();
            var textComponent = textObject.AddComponent<Text>();
            textComponent.font = _defaultFont;
            textComponent.fontSize = 18;
            textComponent.color = Color.white;
            textComponent.supportRichText = false;
            textComponent.alignment = TextAnchor.MiddleLeft;
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(10f, 6f);
            textRect.offsetMax = new Vector2(-10f, -6f);

            var placeholderObject = CreateUIObject("Placeholder", rect);
            placeholderObject.AddComponent<CanvasRenderer>();
            var placeholderText = placeholderObject.AddComponent<Text>();
            placeholderText.font = _defaultFont;
            placeholderText.fontSize = 18;
            placeholderText.color = new Color(1f, 1f, 1f, 0.4f);
            placeholderText.text = placeholder;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            var placeholderRect = placeholderObject.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0f, 0f);
            placeholderRect.anchorMax = new Vector2(1f, 1f);
            placeholderRect.offsetMin = new Vector2(10f, 6f);
            placeholderRect.offsetMax = new Vector2(-10f, -6f);

            input.textComponent = textComponent;
            input.placeholder = placeholderText;

            return input;
        }

        private Dropdown CreateDropdown(string name, RectTransform parent, out Image captionIcon, out RectTransform contentRect)
        {
            var go = CreateUIObject(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 36f);

            go.AddComponent<CanvasRenderer>();
            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var dropdown = go.AddComponent<Dropdown>();
            dropdown.targetGraphic = image;

            var captionRoot = CreateUIObject("Label", rect);
            captionRoot.AddComponent<CanvasRenderer>();
            var captionText = captionRoot.AddComponent<Text>();
            captionText.font = _defaultFont;
            captionText.fontSize = 18;
            captionText.color = Color.white;
            captionText.alignment = TextAnchor.MiddleLeft;
            var captionRect = captionRoot.GetComponent<RectTransform>();
            captionRect.anchorMin = new Vector2(0f, 0f);
            captionRect.anchorMax = new Vector2(1f, 1f);
            captionRect.offsetMin = new Vector2(40f, 6f);
            captionRect.offsetMax = new Vector2(-25f, -6f);

            var iconObject = CreateUIObject("Icon", rect);
            iconObject.AddComponent<CanvasRenderer>();
            captionIcon = iconObject.AddComponent<Image>();
            captionIcon.color = Color.white;
            captionIcon.enabled = false;
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.sizeDelta = new Vector2(32f, 32f);
            iconRect.anchoredPosition = new Vector2(4f, 0f);

            var arrowObject = CreateUIObject("Arrow", rect);
            arrowObject.AddComponent<CanvasRenderer>();
            var arrowText = arrowObject.AddComponent<Text>();
            arrowText.font = _defaultFont;
            arrowText.fontSize = 18;
            arrowText.color = Color.white;
            arrowText.text = "▼";
            arrowText.alignment = TextAnchor.MiddleCenter;
            var arrowRect = arrowObject.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = new Vector2(1f, 1f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(20f, 0f);
            arrowRect.anchoredPosition = new Vector2(-10f, 0f);

            var templateObject = CreateUIObject("Template", rect);
            templateObject.SetActive(false);
            templateObject.AddComponent<CanvasRenderer>();
            var templateImage = templateObject.AddComponent<Image>();
            templateImage.color = new Color(0.18f, 0.18f, 0.18f, 1f);
            var templateRect = templateObject.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, 2f);
            templateRect.sizeDelta = new Vector2(0f, 150f);

            var viewportObject = CreateUIObject("Viewport", templateRect);
            viewportObject.AddComponent<CanvasRenderer>();
            var viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            var mask = viewportObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-4f, -4f);

            var contentObject = CreateUIObject("Content", viewportRect);
            contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 28f);

            var layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            var fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = templateObject.AddComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            dropdown.template = templateRect;
            dropdown.captionText = captionText;

            return dropdown;
        }

        private DropdownItemComponents CreateDropdownItem(RectTransform templateRect, RectTransform contentRect)
        {
            if (contentRect == null)
            {
                return default;
            }

            var item = CreateUIObject("Item", contentRect);
            item.AddComponent<CanvasRenderer>();
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.sizeDelta = new Vector2(0f, 32f);

            var toggle = item.AddComponent<Toggle>();
            toggle.toggleTransition = Toggle.ToggleTransition.None;

            var background = CreateUIObject("Item Background", itemRect);
            background.AddComponent<CanvasRenderer>();
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var checkmark = CreateUIObject("Item Checkmark", backgroundRect);
            checkmark.AddComponent<CanvasRenderer>();
            var checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            var checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
            checkmarkRect.sizeDelta = new Vector2(20f, 20f);
            checkmarkRect.anchoredPosition = new Vector2(10f, 0f);

            var iconObject = CreateUIObject("Item Icon", backgroundRect);
            iconObject.AddComponent<CanvasRenderer>();
            var iconImage = iconObject.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.enabled = false;
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(28f, 28f);
            iconRect.anchoredPosition = new Vector2(40f, 0f);

            var label = CreateUIObject("Item Label", backgroundRect);
            label.AddComponent<CanvasRenderer>();
            var labelText = label.AddComponent<Text>();
            labelText.font = _defaultFont;
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(80f, 5f);
            labelRect.offsetMax = new Vector2(-10f, -5f);

            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;

            return new DropdownItemComponents
            {
                Text = labelText,
                Icon = iconImage,
            };
        }

        private void SubscribeToLogs()
        {
            if (_isSubscribedToLogs == true)
                return;

            Application.logMessageReceivedThreaded += HandleLogMessage;
            _isSubscribedToLogs = true;
        }

        private void UnsubscribeFromLogs()
        {
            if (_isSubscribedToLogs == false)
                return;

            Application.logMessageReceivedThreaded -= HandleLogMessage;
            _isSubscribedToLogs = false;
        }

        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            string prefix = type switch
            {
                LogType.Warning => "[Warning]",
                LogType.Error => "[Error]",
                LogType.Exception => "[Exception]",
                LogType.Assert => "[Assert]",
                _ => "[Info]",
            };

            var builder = new StringBuilder(256);
            builder.Append('[').Append(timestamp).Append("] ")
                   .Append(prefix).Append(' ').Append(condition);

            if (type == LogType.Exception)
            {
                builder.Append('\n').Append(stackTrace);
            }

            lock (_logLock)
            {
                _pendingLogLines.Enqueue(builder.ToString());
                if (_pendingLogLines.Count > 128)
                {
                    _pendingLogLines.Dequeue();
                }

                _isLogDirty = true;
            }
        }

        private void FlushLogQueue()
        {
            if (_isLogDirty == false)
                return;

            lock (_logLock)
            {
                while (_pendingLogLines.Count > 0)
                {
                    _logBuilder.AppendLine(_pendingLogLines.Dequeue());
                    if (_logBuilder.Length > 16000)
                    {
                        _logBuilder.Remove(0, Mathf.Max(0, _logBuilder.Length - 12000));
                    }
                }

                _isLogDirty = false;
            }

            if (_logLabel != null)
            {
                _logLabel.text = _logBuilder.ToString();
            }

            if (_logScrollRect != null)
            {
                _logScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void SetActiveTab(Tab tab)
        {
            _currentTab = tab;

            if (_consolePanel != null)
            {
                _consolePanel.gameObject.SetActive(tab == Tab.Console);
            }

            if (_inventoryPanel != null)
            {
                _inventoryPanel.gameObject.SetActive(tab == Tab.Inventory);
            }
        }

        private void OnInventoryActionChanged(int index)
        {
            _currentInventoryAction = index == 0 ? InventoryAction.AddItem : InventoryAction.AddGold;
            ApplyInventoryAction(_currentInventoryAction);
        }

        private void ApplyInventoryAction(InventoryAction action)
        {
            if (_addItemPanel != null)
            {
                _addItemPanel.SetActive(action == InventoryAction.AddItem);
            }

            if (_addGoldPanel != null)
            {
                _addGoldPanel.SetActive(action == InventoryAction.AddGold);
            }
        }

        private void LoadItemDefinitions()
        {
            ItemDefinition.LoadAll();
            _allItemDefinitions = Resources.LoadAll<ItemDefinition>(string.Empty);
            Array.Sort(_allItemDefinitions, (a, b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshItemDropdown()
        {
            if (_itemDropdown == null)
                return;

            string search = _itemSearchField != null ? _itemSearchField.text : string.Empty;
            _filteredItemDefinitions.Clear();

            if (_allItemDefinitions == null || _allItemDefinitions.Length == 0)
            {
                _itemDropdown.options = new List<Dropdown.OptionData>();
                _itemDropdown.RefreshShownValue();
                UpdateItemCaption(null);
                return;
            }

            for (int i = 0; i < _allItemDefinitions.Length; ++i)
            {
                var definition = _allItemDefinitions[i];
                if (definition == null)
                    continue;

                if (string.IsNullOrEmpty(search) == false && definition.Name != null)
                {
                    if (definition.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                _filteredItemDefinitions.Add(definition);
            }

            if (_filteredItemDefinitions.Count == 0)
            {
                foreach (var definition in _allItemDefinitions)
                {
                    if (definition != null)
                    {
                        _filteredItemDefinitions.Add(definition);
                    }
                }
            }

            var options = new List<Dropdown.OptionData>(_filteredItemDefinitions.Count);
            for (int i = 0; i < _filteredItemDefinitions.Count; ++i)
            {
                var definition = _filteredItemDefinitions[i];
                options.Add(new Dropdown.OptionData(definition.Name, definition.IconSprite));
            }

            _itemDropdown.ClearOptions();
            _itemDropdown.AddOptions(options);
            _itemDropdown.RefreshShownValue();

            if (_filteredItemDefinitions.Count > 0)
            {
                UpdateItemCaption(_filteredItemDefinitions[_itemDropdown.value]);
            }
            else
            {
                UpdateItemCaption(null);
            }
        }

        private void OnItemSearchChanged(string _)
        {
            RefreshItemDropdown();
        }

        private void OnItemSelected(int index)
        {
            if (index < 0 || index >= _filteredItemDefinitions.Count)
                return;

            var definition = _filteredItemDefinitions[index];
            UpdateItemCaption(definition);
        }

        private void UpdateItemCaption(ItemDefinition definition)
        {
            if (_itemDropdownCaptionIcon != null)
            {
                _itemDropdownCaptionIcon.sprite = definition != null ? definition.IconSprite : null;
                _itemDropdownCaptionIcon.enabled = definition != null && definition.IconSprite != null;
            }
        }

        private void OnAddItemClicked()
        {
            if (_filteredItemDefinitions.Count == 0)
                return;

            int selectedIndex = _itemDropdown != null ? _itemDropdown.value : 0;
            if (selectedIndex < 0 || selectedIndex >= _filteredItemDefinitions.Count)
            {
                selectedIndex = 0;
            }

            var definition = _filteredItemDefinitions[selectedIndex];
            if (definition == null)
                return;

            var inventory = Context?.ObservedAgent?.Inventory;
            if (inventory == null)
            {
                AppendLocalMessage("Inventory unavailable for the observed agent.");
                return;
            }

            inventory.AddItem(definition, 1);
            AppendLocalMessage($"Added item '{definition.Name}' to inventory.");
        }

        private void OnAddGoldClicked()
        {
            if (_goldAmountField == null)
                return;

            var inventory = Context?.ObservedAgent?.Inventory;
            if (inventory == null)
            {
                AppendLocalMessage("Inventory unavailable for the observed agent.");
                return;
            }

            if (int.TryParse(_goldAmountField.text, out int amount) == false || amount <= 0)
            {
                AppendLocalMessage("Invalid gold amount. Please enter a positive number.");
                return;
            }

            inventory.AddGold(amount);
            AppendLocalMessage($"Added {amount} gold to inventory.");
            _goldAmountField.text = string.Empty;
        }

        private void AppendLocalMessage(string message)
        {
            HandleLogMessage(message, string.Empty, LogType.Log);
        }
    }
}

