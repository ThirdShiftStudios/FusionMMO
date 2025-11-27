using TPSBR;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public sealed class UISlotMachineView : UIGamblingView
    {
        public const string ResourcePath = "UI/GameplayViews/UISlotMachineView";

        public SlotMachineNetworked SlotMachine { get; private set; }

        [Header("Betting")]
        [SerializeField]
        private TMP_InputField _betInput;
        [SerializeField]
        private Button _betIncreaseButton;
        [SerializeField]
        private Button _betDecreaseButton;
        [SerializeField]
        private Button _gambleButton;

        private int _betAmount = SlotMachineNetworked.MinBetAmount;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            EnsureBetUI();
            SubscribeToBetUI();
            RefreshBetUI();
        }

        protected override void OnConfigured(GamblingMachine machine, Agent agent)
        {
            base.OnConfigured(machine, agent);

            SlotMachine = machine as SlotMachineNetworked;

            SetBetAmount(SlotMachine != null ? SlotMachine.CurrentBet : SlotMachineNetworked.MinBetAmount);
        }

        protected override void OnCleared(GamblingMachine machine, Agent agent)
        {
            base.OnCleared(machine, agent);

            if (SlotMachine == machine)
            {
                SlotMachine = null;
            }
        }

        protected override void OnDeinitialize()
        {
            UnsubscribeFromBetUI();

            base.OnDeinitialize();
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            SetBetAmount(SlotMachine != null ? SlotMachine.CurrentBet : SlotMachineNetworked.MinBetAmount);
        }

        protected override void OnTick()
        {
            base.OnTick();

            RefreshBetUI();
        }

        private void EnsureBetUI()
        {
            if (_betInput != null && _betIncreaseButton != null && _betDecreaseButton != null && _gambleButton != null)
                return;

            var content = transform.Find("Content") as RectTransform ?? transform as RectTransform;
            TMP_FontAsset font = null;

            var title = transform.Find("Title")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                font = title.font;
            }

            var description = content.GetComponent<TMP_Text>();
            if (description != null)
            {
                description.enabled = false;
            }

            var verticalLayout = content.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout == null)
            {
                verticalLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                verticalLayout.childAlignment = TextAnchor.UpperCenter;
                verticalLayout.padding = new RectOffset(16, 16, 16, 16);
                verticalLayout.spacing = 12f;
                verticalLayout.childControlWidth = true;
                verticalLayout.childControlHeight = true;
                verticalLayout.childForceExpandWidth = true;
                verticalLayout.childForceExpandHeight = false;
            }

            var labelGO = CreateLabel(content, font, "Bet amount");
            var labelLayout = labelGO.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredHeight = 30f;

            var betRowGO = new GameObject("BetRow", typeof(RectTransform));
            betRowGO.transform.SetParent(content, false);
            var rowLayout = betRowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childControlWidth = false;

            var rowLayoutElement = betRowGO.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 50f;
            rowLayoutElement.minHeight = 50f;

            _betDecreaseButton = CreateButton(betRowGO.transform, font, "-5");

            var inputGO = TMPro.TMP_DefaultControls.CreateInputField(CreateTMPResources(font));
            inputGO.name = "BetInput";
            inputGO.transform.SetParent(betRowGO.transform, false);

            _betInput = inputGO.GetComponent<TMP_InputField>();
            if (_betInput != null)
            {
                _betInput.readOnly = true;
                _betInput.interactable = false;
                _betInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            }

            var inputLayout = inputGO.AddComponent<LayoutElement>();
            inputLayout.preferredWidth = 180f;
            inputLayout.minHeight = 44f;

            _betIncreaseButton = CreateButton(betRowGO.transform, font, "+5");

            _gambleButton = CreateButton(content, font, "Gamble");
            var gambleLayout = _gambleButton.gameObject.AddComponent<LayoutElement>();
            gambleLayout.minHeight = 52f;
        }

        private void SubscribeToBetUI()
        {
            if (_betIncreaseButton != null)
                _betIncreaseButton.onClick.AddListener(HandleIncreaseBet);

            if (_betDecreaseButton != null)
                _betDecreaseButton.onClick.AddListener(HandleDecreaseBet);

            if (_gambleButton != null)
                _gambleButton.onClick.AddListener(HandleGamble);
        }

        private void UnsubscribeFromBetUI()
        {
            if (_betIncreaseButton != null)
                _betIncreaseButton.onClick.RemoveListener(HandleIncreaseBet);

            if (_betDecreaseButton != null)
                _betDecreaseButton.onClick.RemoveListener(HandleDecreaseBet);

            if (_gambleButton != null)
                _gambleButton.onClick.RemoveListener(HandleGamble);
        }

        private void HandleIncreaseBet()
        {
            SetBetAmount(_betAmount + SlotMachineNetworked.BetStep);
        }

        private void HandleDecreaseBet()
        {
            SetBetAmount(_betAmount - SlotMachineNetworked.BetStep);
        }

        private void HandleGamble()
        {
            SlotMachine?.TryPlaceBetAndRoll();
        }

        private void SetBetAmount(int amount)
        {
            int maxAffordable = SlotMachineNetworked.MaxBetAmount;

            if (Agent != null && Agent.Inventory != null)
            {
                maxAffordable = Mathf.Min(maxAffordable, Agent.Inventory.Gold);
                maxAffordable = Mathf.Max(maxAffordable, SlotMachineNetworked.MinBetAmount);
            }

            if (_betAmount > maxAffordable)
            {
                _betAmount = maxAffordable;

                if (SlotMachine != null)
                {
                    SlotMachine.SetBetAmount(_betAmount);
                }
            }

            _betAmount = Mathf.Clamp(amount, SlotMachineNetworked.MinBetAmount, maxAffordable);

            if (SlotMachine != null)
            {
                SlotMachine.SetBetAmount(_betAmount);
            }

            RefreshBetUI();
        }

        private void RefreshBetUI()
        {
            if (_betInput != null)
            {
                _betInput.text = _betAmount.ToString();
            }

            int maxAffordable = SlotMachineNetworked.MaxBetAmount;

            if (Agent != null && Agent.Inventory != null)
            {
                maxAffordable = Mathf.Min(maxAffordable, Agent.Inventory.Gold);
                maxAffordable = Mathf.Max(maxAffordable, SlotMachineNetworked.MinBetAmount);
            }

            if (_betIncreaseButton != null)
                _betIncreaseButton.interactable = _betAmount < maxAffordable;

            if (_betDecreaseButton != null)
                _betDecreaseButton.interactable = _betAmount > SlotMachineNetworked.MinBetAmount;

            bool canGamble = SlotMachine != null && Agent != null && Agent.Inventory != null && Agent.Inventory.Gold >= SlotMachineNetworked.MinBetAmount;
            if (_gambleButton != null)
                _gambleButton.interactable = canGamble;
        }

        private Button CreateButton(Transform parent, TMP_FontAsset font, string label)
        {
            var buttonGO = TMPro.TMP_DefaultControls.CreateButton(CreateTMPResources(font));
            buttonGO.name = label + "Button";
            buttonGO.transform.SetParent(parent, false);

            var text = buttonGO.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = label;
            }

            var layout = buttonGO.AddComponent<LayoutElement>();
            layout.preferredWidth = 100f;
            layout.minHeight = 44f;

            return buttonGO.GetComponent<Button>();
        }

        private TMP_Text CreateLabel(RectTransform parent, TMP_FontAsset font, string text)
        {
            var labelGO = new GameObject("BetLabel", typeof(RectTransform));
            labelGO.transform.SetParent(parent, false);

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = 28f;
            label.color = new Color(0.85f, 0.85f, 0.85f);
            label.alignment = TextAlignmentOptions.Center;

            return label;
        }

        private static TMPro.TMP_DefaultControls.Resources CreateTMPResources(TMP_FontAsset font)
        {
            return new TMPro.TMP_DefaultControls.Resources
            {
                font = font,
            };
        }
    }
}
