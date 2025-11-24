using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using StaffWeapon = TPSBR.StaffWeapon;

namespace TPSBR.UI
{
    public class UIItemContextView : UIExclusiveCloseView, IUIListItemOwner
    {
        [Header("Items")]
        [SerializeField]
        private RectTransform _slotContainer;
        [SerializeField]
        private UIListItem _itemSlotPrefab;
        [SerializeField]
        private Color _selectedSlotColor = Color.white;

        [Header("Abilities")]
        [SerializeField]
        private RectTransform _allWeaponAbilityRoot;
        [SerializeField]
        private RectTransform _abilityConnectionRoot;
        [SerializeField]
        private Image _abilityConnectionPrefab;
        [SerializeField]
        private Vector2 _abilityTreeSpacing = new Vector2(220f, 150f);
        [SerializeField]
        private float _abilityTreeTopOffset = 30f;
        [SerializeField]
        private float _abilityConnectionThickness = 6f;
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
        [SerializeField]
        private UIAbilityControlSlot[] _abilityControlSlots;
        [SerializeField]
        private UIAbilityToolTip _abilityToolTip;

        [Header("Drag Visualization")]
        [SerializeField]
        private RectTransform _dragLayer;

        private readonly List<UIListItem> _spawnedSlots = new List<UIListItem>();
        private readonly List<UpgradeStation.ItemData> _currentItems = new List<UpgradeStation.ItemData>();
        private readonly List<UpgradeStation.ItemData> _lastItems = new List<UpgradeStation.ItemData>();
        private readonly List<UIListItem> _unlockedAbilitySlots = new List<UIListItem>();
        private readonly List<UIListItem> _lockedAbilitySlots = new List<UIListItem>();
        private readonly List<ArcaneConduit.AbilityOption> _allAbilityOptions = new List<ArcaneConduit.AbilityOption>();
        private readonly List<ArcaneConduit.AbilityOption> _lockedAbilityOptions = new List<ArcaneConduit.AbilityOption>();
        private readonly List<ArcaneConduit.AbilityOption> _unlockedAbilityOptions = new List<ArcaneConduit.AbilityOption>();
        private readonly Dictionary<int, ArcaneConduit.AbilityOption> _abilityOptionLookup = new Dictionary<int, ArcaneConduit.AbilityOption>();
        private readonly Dictionary<UIListItem, UIAbilityControlSlot> _abilityControlSlotLookup = new Dictionary<UIListItem, UIAbilityControlSlot>();
        private readonly Dictionary<int, UIListItem> _abilitySlotLookup = new Dictionary<int, UIListItem>();
        private readonly List<Image> _abilityConnections = new List<Image>();
        private Func<List<UpgradeStation.ItemData>, UpgradeStation.ItemStatus> _itemProvider;
        private UpgradeStation.ItemStatus _lastStatus = UpgradeStation.ItemStatus.NoAgent;
        private int _selectedIndex = -1;
        private int _selectedLockedAbilityIndex = -1;
        private int[] _currentAssignedAbilityIndexes;
        private UIListItem _dragSource;
        private RectTransform _dragIcon;
        private Image _dragImage;
        private CanvasGroup _dragCanvasGroup;
        private UIListItem _activeAbilityTooltipSlot;
        private string _currentConfigurationHash;

        private sealed class AbilityTreeNode
        {
            public ArcaneConduit.AbilityOption Option;
            public UIListItem Slot;
            public AbilityTreeNode Parent;
            public readonly List<AbilityTreeNode> Children = new List<AbilityTreeNode>();
            public Vector2 Position;
            public int Depth;
        }

        public event Action<UpgradeStation.ItemData> ItemSelected;
        protected event Action<int> AbilityUnlockRequested;
        protected event Action<int> AbilityLevelUpRequested;
        protected event Action<StaffWeapon.AbilityControlSlot, int> AbilityAssignmentRequested;

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

            InitializeAbilityControlSlots();

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

            HideAbilityTooltip();

            base.OnDeinitialize();
        }

        protected override void OnOpen()
        {
            EnsureExclusiveOpen();

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
            _dragSource = null;
            SetDragVisible(false);

            TryRestoreSuppressedViews();
        }

        protected override void OnTick()
        {
            base.OnTick();

            RefreshItemSlots(false);
        }

        public void SetAbilityOptions(IReadOnlyList<ArcaneConduit.AbilityOption> options, string configurationHash = null)
        {
            _allAbilityOptions.Clear();
            _lockedAbilityOptions.Clear();
            _unlockedAbilityOptions.Clear();
            _abilityOptionLookup.Clear();
            _currentConfigurationHash = configurationHash;

            if (options != null)
            {
                for (int i = 0; i < options.Count; ++i)
                {
                    ArcaneConduit.AbilityOption option = options[i];
                    _allAbilityOptions.Add(option);
                    _abilityOptionLookup[option.Index] = option;

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
            RefreshAbilityAssignmentsView();
        }

        public void ClearAbilityOptions()
        {
            _allAbilityOptions.Clear();
            _lockedAbilityOptions.Clear();
            _unlockedAbilityOptions.Clear();
            _selectedLockedAbilityIndex = -1;
            _abilityOptionLookup.Clear();
            _abilitySlotLookup.Clear();
            _currentConfigurationHash = null;

            HideAbilityTooltip();

            RefreshAbilityLists();
            ClearAbilityAssignments();
        }

        public IReadOnlyList<ArcaneConduit.AbilityOption> GetAbilityOptions()
        {
            return _allAbilityOptions;
        }

        public void SetAbilityAssignments(IReadOnlyList<int> assignments)
        {
            EnsureAssignmentArray();

            if (_currentAssignedAbilityIndexes == null)
                return;

            if (assignments == null)
            {
                ClearAssignmentArray(_currentAssignedAbilityIndexes);
            }
            else
            {
                int count = _currentAssignedAbilityIndexes.Length;
                int providedCount = assignments.Count;

                for (int i = 0; i < count; ++i)
                {
                    int value = i < providedCount ? assignments[i] : -1;
                    _currentAssignedAbilityIndexes[i] = value;
                }
            }

            RefreshAbilityAssignmentsView();
        }

        public void ClearAbilityAssignments()
        {
            EnsureAssignmentArray();

            if (_currentAssignedAbilityIndexes != null)
            {
                ClearAssignmentArray(_currentAssignedAbilityIndexes);
            }

            RefreshAbilityAssignmentsView();
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

        public void RequestAbilityLevelUpByAbilityIndex(int abilityIndex)
        {
            AbilityLevelUpRequested?.Invoke(abilityIndex);
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
            int siblingIndex = 0;
            _abilitySlotLookup.Clear();
            siblingIndex = RefreshAbilitySection(_unlockedAbilityOptions, _unlockedAbilitySlots, siblingIndex, false, true);
            siblingIndex = RefreshAbilitySection(_lockedAbilityOptions, _lockedAbilitySlots, siblingIndex, true, false);

            bool hasLockedAbilities = _lockedAbilityOptions.Count > 0;
            bool hasUnlockedAbilities = _unlockedAbilityOptions.Count > 0;

            if (_noLockedAbilitiesLabel != null)
            {
                _noLockedAbilitiesLabel.gameObject.SetActive(hasLockedAbilities == false);
            }

            if (_noUnlockedAbilitiesLabel != null)
            {
                _noUnlockedAbilitiesLabel.gameObject.SetActive(hasUnlockedAbilities == false);
            }

            UpdateLockedAbilitySelectionVisuals();
            UpdateUnlockButtonState();
            ArrangeAbilityTree();
        }

        private int RefreshAbilitySection(List<ArcaneConduit.AbilityOption> options, List<UIListItem> slots, int siblingIndex, bool allowSelection, bool isUnlockedSection)
        {
            if (_allWeaponAbilityRoot == null || _abilitySlotPrefab == null)
            {
                for (int i = 0; i < slots.Count; ++i)
                {
                    if (slots[i] != null)
                    {
                        slots[i].gameObject.SetActive(false);
                    }
                }

                return siblingIndex;
            }

            RectTransform rootTransform = _allWeaponAbilityRoot;
            rootTransform.gameObject.SetActive(true);

            EnsureAbilitySlotCapacity(slots, options.Count);

            for (int i = 0; i < options.Count; ++i)
            {
                UIListItem slot = slots[i];
                ArcaneConduit.AbilityOption option = options[i];

                RectTransform slotTransform = slot.transform as RectTransform;
                if (slotTransform != null && slotTransform.parent != rootTransform)
                {
                    slotTransform.SetParent(rootTransform, false);
                }

                if (slotTransform != null)
                {
                    slotTransform.SetSiblingIndex(siblingIndex++);
                    slotTransform.anchorMin = new Vector2(0.5f, 1f);
                    slotTransform.anchorMax = new Vector2(0.5f, 1f);
                    slotTransform.pivot = new Vector2(0.5f, 0.5f);
                }

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

                UIAbilityListItem abilityListItem = slot.GetComponent<UIAbilityListItem>();
                abilityListItem?.SetAbilityDetails(option);

                _abilitySlotLookup[option.Index] = slot;
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

            return siblingIndex;
        }

        private void EnsureAbilitySlotCapacity(List<UIListItem> slots, int required)
        {
            if (_allWeaponAbilityRoot == null || _abilitySlotPrefab == null)
                return;

            while (slots.Count < required)
            {
                UIListItem newSlot = Instantiate(_abilitySlotPrefab, _allWeaponAbilityRoot);
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

        private void ArrangeAbilityTree()
        {
            if (_allWeaponAbilityRoot == null)
                return;

            ClearAbilityConnections();

            if (_allAbilityOptions.Count == 0)
                return;

            AbilityTreeNode rootNode = BuildAbilityTree();
            if (rootNode == null)
                return;

            List<AbilityTreeNode> orderedNodes = new List<AbilityTreeNode>();
            CollectNodes(rootNode, orderedNodes);

            foreach (AbilityTreeNode node in orderedNodes)
            {
                PositionNode(node);
            }

            DrawConnections(orderedNodes);
            UpdateDependencyAvailability(orderedNodes);
        }

        private AbilityTreeNode BuildAbilityTree()
        {
            ArcaneConduit.AbilityOption? startingOption = GetDeterministicRootOption();

            if (startingOption.HasValue == false)
                return null;

            ArcaneConduit.AbilityOption rootOption = startingOption.Value;
            AbilityTreeNode root = CreateNode(rootOption, 0, null);

            List<ArcaneConduit.AbilityOption> remainingOptions = _allAbilityOptions
                .Where(option => option.Index != rootOption.Index)
                .OrderBy(option => option.Index)
                .ToList();

            if (remainingOptions.Count == 0)
            {
                return root;
            }

            int seed = GetDeterministicSeed();
            System.Random random = new System.Random(seed);

            if (remainingOptions.Count == 1)
            {
                AbilityTreeNode child = CreateNode(remainingOptions[0], 1, root);
                root.Children.Add(child);
                return root;
            }

            bool createBranch = random.Next(0, 100) % 2 == 0;
            if (createBranch == true)
            {
                for (int i = 0; i < remainingOptions.Count; ++i)
                {
                    AbilityTreeNode child = CreateNode(remainingOptions[i], 1, root);
                    root.Children.Add(child);
                }
            }
            else
            {
                AbilityTreeNode second = CreateNode(remainingOptions[0], 1, root);
                root.Children.Add(second);

                AbilityTreeNode third = CreateNode(remainingOptions[1], 2, second);
                second.Children.Add(third);
            }

            return root;
        }

        private ArcaneConduit.AbilityOption? GetDeterministicRootOption()
        {
            if (_allAbilityOptions.Count == 0)
            {
                return null;
            }

            ArcaneConduit.AbilityOption? firstUnlocked = _unlockedAbilityOptions
                .OrderBy(option => option.Index)
                .Cast<ArcaneConduit.AbilityOption?>()
                .FirstOrDefault();

            if (firstUnlocked.HasValue == true)
            {
                return firstUnlocked;
            }

            ArcaneConduit.AbilityOption? firstByIndex = _allAbilityOptions
                .OrderBy(option => option.Index)
                .Cast<ArcaneConduit.AbilityOption?>()
                .FirstOrDefault();

            return firstByIndex;
        }

        private int GetDeterministicSeed()
        {
            if (string.IsNullOrEmpty(_currentConfigurationHash) == false)
            {
                return _currentConfigurationHash.GetHashCode();
            }

            unchecked
            {
                int seed = 17;
                for (int i = 0; i < _allAbilityOptions.Count; ++i)
                {
                    seed = (seed * 31) + _allAbilityOptions[i].Index;
                }

                return seed;
            }
        }

        private AbilityTreeNode CreateNode(ArcaneConduit.AbilityOption option, int depth, AbilityTreeNode parent)
        {
            _abilitySlotLookup.TryGetValue(option.Index, out UIListItem slot);

            return new AbilityTreeNode
            {
                Option = option,
                Slot = slot,
                Parent = parent,
                Depth = depth,
            };
        }

        private void CollectNodes(AbilityTreeNode node, List<AbilityTreeNode> orderedNodes)
        {
            if (node == null)
                return;

            orderedNodes.Add(node);
            for (int i = 0; i < node.Children.Count; ++i)
            {
                CollectNodes(node.Children[i], orderedNodes);
            }
        }

        private void PositionNode(AbilityTreeNode node)
        {
            if (node?.Slot == null)
                return;

            RectTransform rectTransform = node.Slot.transform as RectTransform;
            if (rectTransform == null)
                return;

            Vector2 position = Vector2.zero;
            if (node.Parent == null)
            {
                position = new Vector2(0f, -_abilityTreeTopOffset);
            }
            else if (node.Parent.Children.Count == 1)
            {
                position = node.Parent.Position + new Vector2(0f, -_abilityTreeSpacing.y);
            }
            else
            {
                int index = node.Parent.Children.IndexOf(node);
                float horizontalOffset = _abilityTreeSpacing.x * (index == 0 ? -0.5f : 0.5f);
                position = node.Parent.Position + new Vector2(horizontalOffset, -_abilityTreeSpacing.y);
            }

            rectTransform.anchoredPosition = position;
            node.Position = position;
        }

        private void DrawConnections(List<AbilityTreeNode> nodes)
        {
            if (_abilityConnectionRoot == null || _abilityConnectionPrefab == null)
                return;

            int connectionIndex = 0;

            foreach (AbilityTreeNode node in nodes)
            {
                if (node.Parent == null)
                    continue;

                Image connector = GetOrCreateConnector(connectionIndex++);
                RectTransform connectionTransform = connector.rectTransform;

                Vector2 start = node.Parent.Position;
                Vector2 end = node.Position;
                Vector2 delta = end - start;
                float length = delta.magnitude;

                connectionTransform.anchorMin = new Vector2(0.5f, 1f);
                connectionTransform.anchorMax = new Vector2(0.5f, 1f);
                connectionTransform.pivot = new Vector2(0.5f, 0.5f);
                connectionTransform.sizeDelta = new Vector2(_abilityConnectionThickness, length);
                connectionTransform.anchoredPosition = start + delta * 0.5f;
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f;
                connectionTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

                connector.gameObject.SetActive(true);
            }

            for (int i = connectionIndex; i < _abilityConnections.Count; ++i)
            {
                if (_abilityConnections[i] != null)
                {
                    _abilityConnections[i].gameObject.SetActive(false);
                }
            }
        }

        private void UpdateDependencyAvailability(List<AbilityTreeNode> nodes)
        {
            foreach (AbilityTreeNode node in nodes)
            {
                if (node.Slot == null)
                    continue;

                UIAbilityListItem listItem = node.Slot.GetComponent<UIAbilityListItem>();
                if (listItem == null)
                    continue;

                bool parentUnlocked = node.Parent == null || node.Parent.Option.IsUnlocked;
                bool canPurchase = parentUnlocked && node.Option.CanPurchase;

                listItem.SetDependencyState(parentUnlocked, canPurchase);
            }
        }

        private Image GetOrCreateConnector(int index)
        {
            while (_abilityConnections.Count <= index)
            {
                Image connector = Instantiate(_abilityConnectionPrefab, _abilityConnectionRoot);
                connector.raycastTarget = false;
                connector.gameObject.SetActive(false);
                _abilityConnections.Add(connector);
            }

            return _abilityConnections[index];
        }

        private void ClearAbilityConnections()
        {
            for (int i = 0; i < _abilityConnections.Count; ++i)
            {
                Image connector = _abilityConnections[i];
                if (connector != null)
                {
                    connector.gameObject.SetActive(false);
                }
            }
        }

        private void HandleEmptyState(UpgradeStation.ItemStatus status)
        {
            
        }

        private void InitializeAbilityControlSlots()
        {
            _abilityControlSlotLookup.Clear();

            if (_abilityControlSlots != null)
            {
                for (int i = 0; i < _abilityControlSlots.Length; ++i)
                {
                    UIAbilityControlSlot controlSlot = _abilityControlSlots[i];
                    if (controlSlot == null)
                        continue;

                    UIListItem slot = controlSlot.Slot;
                    if (slot == null)
                        continue;

                    slot.InitializeSlot(this, (int)controlSlot.SlotType);

                    if (_abilityControlSlotLookup.ContainsKey(slot) == false)
                    {
                        _abilityControlSlotLookup.Add(slot, controlSlot);
                    }
                }
            }

            EnsureAssignmentArray();
            RefreshAbilityAssignmentsView();
        }

        private void EnsureAssignmentArray()
        {
            int slotCount = StaffWeapon.GetAbilityControlSlotCount();
            if (_currentAssignedAbilityIndexes == null || _currentAssignedAbilityIndexes.Length != slotCount)
            {
                _currentAssignedAbilityIndexes = new int[slotCount];
                ClearAssignmentArray(_currentAssignedAbilityIndexes);
            }
        }

        private static void ClearAssignmentArray(int[] assignments)
        {
            if (assignments == null)
                return;

            for (int i = 0; i < assignments.Length; ++i)
            {
                assignments[i] = -1;
            }
        }

        private void RefreshAbilityAssignmentsView()
        {
            if (_abilityControlSlots == null)
                return;

            for (int i = 0; i < _abilityControlSlots.Length; ++i)
            {
                UIAbilityControlSlot controlSlot = _abilityControlSlots[i];
                if (controlSlot == null)
                    continue;

                int assignedIndex = GetAssignedAbilityIndex(controlSlot.SlotType);
                ArcaneConduit.AbilityOption? assignedOption = assignedIndex >= 0 ? FindAbilityOption(assignedIndex) : null;
                controlSlot.SetAssignedAbility(assignedOption);
            }
        }

        private void ApplyLocalAssignment(StaffWeapon.AbilityControlSlot slotType, int abilityIndex)
        {
            EnsureAssignmentArray();

            int slotIndex = (int)slotType;
            if (_currentAssignedAbilityIndexes == null || slotIndex < 0 || slotIndex >= _currentAssignedAbilityIndexes.Length)
                return;

            if (abilityIndex >= 0)
            {
                for (int i = 0; i < _currentAssignedAbilityIndexes.Length; ++i)
                {
                    if (_currentAssignedAbilityIndexes[i] == abilityIndex)
                    {
                        _currentAssignedAbilityIndexes[i] = -1;
                    }
                }
            }

            _currentAssignedAbilityIndexes[slotIndex] = abilityIndex;
        }

        private int GetAssignedAbilityIndex(StaffWeapon.AbilityControlSlot slotType)
        {
            EnsureAssignmentArray();

            int slotIndex = (int)slotType;
            if (_currentAssignedAbilityIndexes == null || slotIndex < 0 || slotIndex >= _currentAssignedAbilityIndexes.Length)
                return -1;

            return _currentAssignedAbilityIndexes[slotIndex];
        }

        private ArcaneConduit.AbilityOption? FindAbilityOption(int abilityIndex)
        {
            if (_abilityOptionLookup.TryGetValue(abilityIndex, out ArcaneConduit.AbilityOption cachedOption) == true)
            {
                return cachedOption;
            }

            for (int i = 0; i < _allAbilityOptions.Count; ++i)
            {
                ArcaneConduit.AbilityOption option = _allAbilityOptions[i];
                if (option.Index == abilityIndex)
                {
                    _abilityOptionLookup[abilityIndex] = option;
                    return option;
                }
            }

            return null;
        }

        private bool TryGetControlSlot(UIListItem slot, out UIAbilityControlSlot controlSlot)
        {
            if (slot != null && _abilityControlSlotLookup.TryGetValue(slot, out controlSlot) == true)
            {
                return true;
            }

            controlSlot = null;
            return false;
        }

        private bool TryGetAbilityOptionForSlot(UIListItem slot, out ArcaneConduit.AbilityOption option)
        {
            option = default;

            if (slot == null)
                return false;

            int unlockedIndex = _unlockedAbilitySlots.IndexOf(slot);
            if (unlockedIndex >= 0 && unlockedIndex < _unlockedAbilityOptions.Count)
            {
                option = _unlockedAbilityOptions[unlockedIndex];
                return true;
            }

            int lockedIndex = _lockedAbilitySlots.IndexOf(slot);
            if (lockedIndex >= 0 && lockedIndex < _lockedAbilityOptions.Count)
            {
                option = _lockedAbilityOptions[lockedIndex];
                return true;
            }

            if (TryGetControlSlot(slot, out UIAbilityControlSlot controlSlot) == true && controlSlot.AssignedOption.HasValue == true)
            {
                option = controlSlot.AssignedOption.Value;
                return true;
            }

            return false;
        }

        private bool TryShowAbilityTooltip(UIListItem slot, PointerEventData eventData)
        {
            if (_abilityToolTip == null || eventData == null)
                return false;

            if (TryGetAbilityOptionForSlot(slot, out ArcaneConduit.AbilityOption option) == false)
                return false;

            _activeAbilityTooltipSlot = slot;
            _abilityToolTip.Show(option, eventData.position);
            return true;
        }

        private void HideAbilityTooltip(UIListItem slot = null)
        {
            if (slot != null && _activeAbilityTooltipSlot != slot)
                return;

            _activeAbilityTooltipSlot = null;
            _abilityToolTip?.Hide();
        }

        private bool TryResolveAbilityDragSource(UIListItem slot, out int abilityIndex, out StaffWeapon.AbilityControlSlot? controlSlot)
        {
            abilityIndex = -1;
            controlSlot = null;

            if (slot == null)
                return false;

            int unlockedSlotIndex = _unlockedAbilitySlots.IndexOf(slot);
            if (unlockedSlotIndex >= 0)
            {
                if (unlockedSlotIndex < _unlockedAbilityOptions.Count)
                {
                    abilityIndex = _unlockedAbilityOptions[unlockedSlotIndex].Index;
                    return abilityIndex >= 0;
                }

                return false;
            }

            if (TryGetControlSlot(slot, out UIAbilityControlSlot abilityControlSlot) == true)
            {
                controlSlot = abilityControlSlot.SlotType;
                abilityIndex = GetAssignedAbilityIndex(controlSlot.Value);
                return abilityIndex >= 0;
            }

            return false;
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

        void IUIListItemOwner.HandleSlotPointerEnter(UIListItem slot, PointerEventData eventData)
        {
            if (TryShowAbilityTooltip(slot, eventData) == true)
                return;

            HideAbilityTooltip();
        }

        void IUIListItemOwner.HandleSlotPointerExit(UIListItem slot)
        {
            HideAbilityTooltip(slot);
        }

        void IUIListItemOwner.HandleSlotPointerMove(UIListItem slot, PointerEventData eventData)
        {
            if (_activeAbilityTooltipSlot != slot)
                return;

            if (_abilityToolTip == null || eventData == null)
                return;

            _abilityToolTip.UpdatePosition(eventData.position);
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
            HideAbilityTooltip();

            if (slot == null || eventData == null)
                return;

            if (TryResolveAbilityDragSource(slot, out int abilityIndex, out StaffWeapon.AbilityControlSlot? _) == false)
                return;

            Sprite dragSprite = ResolveAbilityIcon(slot, abilityIndex);
            if (dragSprite == null)
                return;

            _dragSource = slot;
            EnsureDragVisual();
            UpdateDragIcon(dragSprite, GetSlotSize(slot));
            SetDragVisible(true);
            UpdateDragPosition(eventData);
        }

        void IUIListItemOwner.UpdateSlotDrag(PointerEventData eventData)
        {
            if (_dragSource == null || eventData == null)
                return;

            UpdateDragPosition(eventData);
        }

        void IUIListItemOwner.EndSlotDrag(UIListItem slot, PointerEventData eventData)
        {
            if (_dragSource != slot)
                return;

            _dragSource = null;
            SetDragVisible(false);
        }

        void IUIListItemOwner.HandleSlotDrop(UIListItem source, UIListItem target)
        {
            if (source == null || target == null)
                return;

            if (TryResolveAbilityDragSource(source, out int abilityIndex, out StaffWeapon.AbilityControlSlot? sourceControlSlot) == false)
                return;

            if (TryGetControlSlot(target, out UIAbilityControlSlot targetControlSlot) == false)
                return;

            StaffWeapon.AbilityControlSlot targetSlotType = targetControlSlot.SlotType;
            int existingTargetAbility = GetAssignedAbilityIndex(targetSlotType);

            if (sourceControlSlot.HasValue == true)
            {
                StaffWeapon.AbilityControlSlot sourceSlotType = sourceControlSlot.Value;

                if (sourceSlotType == targetSlotType)
                    return;

                if (abilityIndex == existingTargetAbility && existingTargetAbility >= 0)
                    return;

                AbilityAssignmentRequested?.Invoke(targetSlotType, abilityIndex);

                if (existingTargetAbility >= 0)
                {
                    AbilityAssignmentRequested?.Invoke(sourceSlotType, existingTargetAbility);
                    ApplyLocalAssignment(sourceSlotType, existingTargetAbility);
                }
                else
                {
                    ApplyLocalAssignment(sourceSlotType, -1);
                }

                ApplyLocalAssignment(targetSlotType, abilityIndex);
                RefreshAbilityAssignmentsView();
                return;
            }

            if (abilityIndex == existingTargetAbility)
                return;

            AbilityAssignmentRequested?.Invoke(targetSlotType, abilityIndex);
            ApplyLocalAssignment(targetSlotType, abilityIndex);
            RefreshAbilityAssignmentsView();
        }

        void IUIListItemOwner.HandleSlotDropOutside(UIListItem slot, PointerEventData eventData)
        {
            if (TryResolveAbilityDragSource(slot, out int _, out StaffWeapon.AbilityControlSlot? sourceControlSlot) == false)
                return;

            if (sourceControlSlot.HasValue == false)
                return;

            StaffWeapon.AbilityControlSlot slotType = sourceControlSlot.Value;
            AbilityAssignmentRequested?.Invoke(slotType, -1);
            ApplyLocalAssignment(slotType, -1);
            RefreshAbilityAssignmentsView();
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
                return;
            }

            if (TryGetControlSlot(slot, out UIAbilityControlSlot controlSlot) == true)
            {
                UpdateSelectedAbilityDetails(controlSlot.AssignedOption);
                return;
            }
        }

        private Sprite ResolveAbilityIcon(UIListItem slot, int abilityIndex)
        {
            if (slot != null && slot.IconSprite != null)
                return slot.IconSprite;

            ArcaneConduit.AbilityOption? option = FindAbilityOption(abilityIndex);
            if (option.HasValue == true && option.Value.Definition != null)
                return option.Value.Definition.Icon;

            return null;
        }

        private Vector2 GetSlotSize(UIListItem slot)
        {
            if (slot != null && slot.SlotRectTransform != null)
            {
                Rect rect = slot.SlotRectTransform.rect;
                if (rect.width > 0f && rect.height > 0f)
                {
                    return rect.size;
                }
            }

            return new Vector2(64f, 64f);
        }

        private void EnsureDragVisual()
        {
            if (_dragIcon != null)
                return;

            RectTransform parent = _dragLayer != null ? _dragLayer : SceneUI?.Canvas.transform as RectTransform;
            if (parent == null)
                return;

            GameObject dragObject = new GameObject("AbilityDrag", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            dragObject.transform.SetParent(parent, false);

            _dragIcon = dragObject.GetComponent<RectTransform>();
            _dragCanvasGroup = dragObject.GetComponent<CanvasGroup>();
            _dragImage = dragObject.GetComponent<Image>();

            _dragCanvasGroup.blocksRaycasts = false;
            _dragCanvasGroup.interactable = false;
            _dragImage.raycastTarget = false;
            _dragImage.preserveAspect = true;

            dragObject.SetActive(false);
        }

        private void UpdateDragIcon(Sprite sprite, Vector2 size)
        {
            if (_dragIcon == null || _dragImage == null)
                return;

            _dragImage.sprite = sprite;
            _dragImage.color = sprite != null ? Color.white : Color.clear;
            _dragIcon.sizeDelta = size;
        }

        private void UpdateDragPosition(PointerEventData eventData)
        {
            if (_dragIcon == null || eventData == null)
                return;

            RectTransform referenceRect = _dragIcon.parent as RectTransform;
            if (referenceRect == null)
                return;

            Canvas canvas = SceneUI?.Canvas;
            if (canvas == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRect, eventData.position, canvas.worldCamera, out Vector2 localPoint) == true)
            {
                _dragIcon.localPosition = localPoint;
            }
        }

        private void SetDragVisible(bool visible)
        {
            if (_dragIcon == null)
                return;

            _dragIcon.gameObject.SetActive(visible);
            if (_dragCanvasGroup != null)
            {
                _dragCanvasGroup.alpha = visible ? 1f : 0f;
            }
        }
    }
}
