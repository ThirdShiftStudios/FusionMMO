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
        private RectTransform _contentRoot;
        [SerializeField]
        private TMP_Text _descriptionText;
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

            CacheContentRoot();
            EnsureLayout();
            EnsureRuntimeControls();
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

        private void CacheContentRoot()
        {
            if (_contentRoot != null)
                return;

            var found = transform.Find("Content");
            if (found != null)
            {
                _contentRoot = found as RectTransform;
            }
        }

        private void EnsureLayout()
        {
            if (_contentRoot == null)
                return;

            var layout = _contentRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = _contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 12f;
                layout.padding = new RectOffset(8, 8, 8, 8);
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = false;
            }

            var fitter = _contentRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = _contentRoot.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        private void EnsureRuntimeControls()
        {
            if (_contentRoot == null)
                return;

            if (_descriptionText == null)
            {
                _descriptionText = _contentRoot.GetComponentInChildren<TMP_Text>();
                if (_descriptionText != null)
                {
                    _descriptionText.alignment = TextAlignmentOptions.Center;
                }
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = "Adjust your wager and press Gamble to spin the reels.";
            }

            if (_wagerInput != null && _increaseButton != null && _decreaseButton != null && _gambleButton != null)
                return;

            var wagerRow = CreateHorizontalGroup("WagerControls", 8f);
            CreateLabel(wagerRow, "Wager", 120f);

            _decreaseButton = CreateButton(wagerRow, "Decrease", "-");
            _wagerInput = CreateInputField(wagerRow);
            _increaseButton = CreateButton(wagerRow, "Increase", "+");

            var gambleRow = CreateHorizontalGroup("GambleRow", 0f);
            _gambleButton = CreateButton(gambleRow, "Gamble", "Gamble");
            var gambleLayout = _gambleButton.GetComponent<LayoutElement>();
            if (gambleLayout != null)
            {
                gambleLayout.preferredWidth = 200f;
            }
        }

        private RectTransform CreateHorizontalGroup(string name, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_contentRoot, false);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 60f;

            return rect;
        }

        private TMP_Text CreateLabel(Transform parent, string text, float preferredWidth)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 28f;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = 48f;

            return label;
        }

        private Button CreateButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(60f, 48f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.25f, 0.95f);

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;

            var textObject = new GameObject("Text", typeof(RectTransform));
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Midline;
            tmp.fontSize = 26f;

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = 80f;
            layout.preferredHeight = 48f;

            return button;
        }

        private TMP_InputField CreateInputField(Transform parent)
        {
            var go = new GameObject("WagerInput", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(160f, 48f);

            var background = go.GetComponent<Image>();
            background.color = new Color(0.1f, 0.12f, 0.15f, 0.95f);

            var viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.SetParent(rect, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 6f);
            viewportRect.offsetMax = new Vector2(-8f, -6f);

            var textObject = new GameObject("Text", typeof(RectTransform));
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(viewportRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 28f;
            text.alignment = TextAlignmentOptions.Center;

            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = viewportRect;
            input.textComponent = text;
            input.caretWidth = 0;
            input.selectionColor = new Color(0, 0, 0, 0);
            input.readOnly = true;
            input.interactable = false;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = 140f;
            layout.preferredHeight = 48f;

            return input;
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
