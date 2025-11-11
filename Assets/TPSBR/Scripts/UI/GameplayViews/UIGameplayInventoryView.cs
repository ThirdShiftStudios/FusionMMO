using System;
using System.Collections.Generic;
using Fusion;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TSS.Data;

namespace TPSBR.UI
{
    public class UIGameplayInventoryView : UICloseView
    {
        // PUBLIC MEMBERS

        public override bool NeedsCursor => _menuVisible;

        public bool MenuVisible => _menuVisible;
        // PRIVATE MEMBERS
        [SerializeField] private UIButton _cancelButton;
        [SerializeField] private UIList _inventoryList;
        [SerializeField] private RectTransform _inventoryDragLayer;
        [SerializeField] private UIHotbar _hotbar;
        [SerializeField] private Color _selectedHotbarColor = Color.white;
        [SerializeField] private Color _selectedInventorySlotColor = Color.white;
        [SerializeField] private Color _selectedHotbarSlotColor = Color.white;
        [SerializeField] private UIInventoryDetailsPanel _detailsPanel;
        [SerializeField] private UIListItem _pickaxeSlot;
        [SerializeField] private UIListItem _woodAxeSlot;
        [SerializeField] private UIListItem _headSlot;
        [SerializeField] private UIListItem _upperBodySlot;
        [SerializeField] private UIListItem _lowerBodySlot;
        [SerializeField] private UIListItem _pipeSlot;
        [SerializeField] private UIListItem _bagSlotOne;
        [SerializeField] private UIListItem _bagSlotTwo;
        [SerializeField] private UIListItem _bagSlotThree;
        [SerializeField] private UIListItem _bagSlotFour;
        [SerializeField] private UIListItem _bagSlotFive;
        [SerializeField] private TextMeshProUGUI _goldLabel;
        [SerializeField] private UIInventoryItemToolTip _itemToolTip;
        [SerializeField] private UIStatToolTip _statToolTip;
        [SerializeField] private UIProfessionToolTip _professionToolTip;

        private bool _menuVisible;
        private Agent _boundAgent;
        private Inventory _boundInventory;
        private InventoryListPresenter _inventoryPresenter;
        [SerializeField]
        private UICharacterDetailsView _characterDetails;
        
        internal SceneUI GameplaySceneUI => SceneUI;

        internal void ShowItemTooltip(IInventoryItemDetails details, ItemDefinition definition, NetworkString<_32> configurationHash, Vector2 screenPosition)
        {
            _itemToolTip?.Show(details, definition, configurationHash, screenPosition);
        }

        internal void ShowItemTooltip(ItemDefinition definition, string title, string description, Vector2 screenPosition)
        {
            _itemToolTip?.Show(definition, title, description, screenPosition);
        }

        internal void ShowItemTooltip(IInventoryItemDetails details, NetworkString<_32> configurationHash, Vector2 screenPosition)
        {
            _itemToolTip?.Show(details, configurationHash, screenPosition);
        }

        internal void ShowItemTooltip(string title, string description, Vector2 screenPosition)
        {
            _itemToolTip?.Show(title, description, screenPosition);
        }

        internal void UpdateItemTooltipPosition(Vector2 screenPosition)
        {
            _itemToolTip?.UpdatePosition(screenPosition);
        }

        internal void HideItemTooltip()
        {
            _itemToolTip?.Hide();
        }

        internal void ShowStatTooltip(Stats.StatIndex statIndex, string statCode, int statValue, Vector2 screenPosition)
        {
            _statToolTip?.Show(statIndex, statCode, statValue, screenPosition);
        }

        internal void UpdateStatTooltipPosition(Vector2 screenPosition)
        {
            _statToolTip?.UpdatePosition(screenPosition);
        }

        internal void HideStatTooltip()
        {
            _statToolTip?.Hide();
        }

        internal void ShowProfessionTooltip(Professions.ProfessionIndex professionIndex, string professionCode, Professions.ProfessionSnapshot snapshot, Vector2 screenPosition)
        {
            _professionToolTip?.Show(professionIndex, professionCode, snapshot, screenPosition);
        }

        internal void UpdateProfessionTooltipPosition(Vector2 screenPosition)
        {
            _professionToolTip?.UpdatePosition(screenPosition);
        }

        internal void HideProfessionTooltip()
        {
            _professionToolTip?.Hide();
        }

        private void HideAllTooltips()
        {
            _itemToolTip?.Hide();
            _statToolTip?.Hide();
            _professionToolTip?.Hide();
        }

        // PUBLIC METHODS

        public void Show(bool value, bool force = false)
        {
            if (_menuVisible == value && force == false)
                return;

            _menuVisible = value;
            CanvasGroup.interactable = value;

            NotifyInventoryCameraState(value);

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
            
            if (_inventoryList == null)
            {
                _inventoryList = GetComponentInChildren<UIList>(true);
            }

            if (_characterDetails == null)
            {
                _characterDetails = GetComponentInChildren<UICharacterDetailsView>();
            }

            if (_inventoryList != null && _inventoryPresenter == null)
            {
                _inventoryPresenter = new InventoryListPresenter(this);
                _inventoryPresenter.Initialize(
                    _inventoryList,
                    _inventoryDragLayer,
                    _pickaxeSlot,
                    _woodAxeSlot,
                    _headSlot,
                    _upperBodySlot,
                    _lowerBodySlot,
                    _pipeSlot,
                    _bagSlotOne,
                    _bagSlotTwo,
                    _bagSlotThree,
                    _bagSlotFour,
                    _bagSlotFive);
                _inventoryPresenter.SetSelectionColor(_selectedInventorySlotColor);
                _inventoryPresenter.ItemSelected += OnInventoryItemSelected;
            }

            if (_hotbar != null)
            {
                _hotbar.SetSelectedColor(_selectedHotbarColor);
                _hotbar.SetSelectionHighlightColor(_selectedHotbarSlotColor);
                _hotbar.ItemSelected += OnHotbarItemSelected;
                _hotbar.ItemPointerEnter += OnHotbarItemPointerEnter;
                _hotbar.ItemPointerMove += OnHotbarItemPointerMove;
                _hotbar.ItemPointerExit += OnHotbarItemPointerExit;
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelButton);
            }

            _detailsPanel?.Hide();
            HideAllTooltips();
        }

        protected override void OnDeinitialize()
        {
            if (_boundInventory != null)
            {
                _boundInventory.GoldChanged -= OnGoldChanged;
                _boundInventory = null;
            }

            _boundAgent = null;
            UpdateGoldLabel(0);

            if (_inventoryPresenter != null)
            {
                _inventoryPresenter.ItemSelected -= OnInventoryItemSelected;
                _inventoryPresenter.Bind(null);
                _inventoryPresenter = null;
            }

            if (_hotbar != null)
            {
                _hotbar.ItemSelected -= OnHotbarItemSelected;
                _hotbar.ItemPointerEnter -= OnHotbarItemPointerEnter;
                _hotbar.ItemPointerMove -= OnHotbarItemPointerMove;
                _hotbar.ItemPointerExit -= OnHotbarItemPointerExit;
                _hotbar.Bind(null);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(OnCancelButton);
            }

            HideAllTooltips();

            base.OnDeinitialize();
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            Animation.SampleStart();
            _menuVisible = false;
            CanvasGroup.interactable = false;

            NotifyInventoryCameraState(false);

            RefreshInventoryBinding();
            _detailsPanel?.Hide();
            HideAllTooltips();
        }

        protected override void OnClose()
        {
            if (_menuVisible == true)
            {
                _menuVisible = false;
                CanvasGroup.interactable = false;
                NotifyInventoryCameraState(false);
            }

            HideAllTooltips();

            base.OnClose();
        }

        protected override void OnTick()
        {
            base.OnTick();

            RefreshInventoryBinding();
            RefreshCharacterDetails();
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

        private void OnInventoryItemSelected(IInventoryItemDetails item, NetworkString<_32> configurationHash)
        {
            if (item != null)
            {
                _hotbar?.ClearSelection(false);
            }

            ShowItemDetails(item, configurationHash);
        }

        private void OnHotbarItemSelected(IInventoryItemDetails item, NetworkString<_32> configurationHash)
        {
            if (item != null)
            {
                _inventoryPresenter?.ClearSelection(false);
            }

            ShowItemDetails(item, configurationHash);
        }

        private void OnHotbarItemPointerEnter(Weapon weapon, Vector2 screenPosition)
        {
            if (weapon == null)
            {
                HideItemTooltip();
                return;
            }

            ShowItemTooltip(weapon, weapon.ConfigurationHash, screenPosition);
        }

        private void OnHotbarItemPointerMove(Vector2 screenPosition)
        {
            UpdateItemTooltipPosition(screenPosition);
        }

        private void OnHotbarItemPointerExit()
        {
            HideItemTooltip();
        }

        private void ShowItemDetails(IInventoryItemDetails item, NetworkString<_32> configurationHash)
        {
            if (_detailsPanel == null)
                return;

            if (item == null)
            {
                _detailsPanel.Hide();
                return;
            }

            _detailsPanel.Show(item, configurationHash);
        }

        private void NotifyInventoryCameraState(bool isOpen)
        {
            var observedAgent = Context != null ? Context.ObservedAgent : null;
            observedAgent?.Character?.SetInventoryOpen(isOpen);
        }

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


        private sealed class InventoryListPresenter : IUIListItemOwner
        {
            private readonly UIGameplayInventoryView _view;
            private UIList _list;
            private RectTransform _dragLayer;
            private UIListItem[] _slots;
            private int[] _slotIndices;
            private Dictionary<int, UIListItem> _slotLookup;
            private Inventory _inventory;
            private UIListItem _dragSource;
            private RectTransform _dragIcon;
            private Image _dragImage;
            private CanvasGroup _dragCanvasGroup;
            private Color _selectedSlotColor = Color.white;
            private UIListItem _pickaxeSlotOverride;
            private UIListItem _woodAxeSlotOverride;
            private UIListItem _headSlotOverride;
            private UIListItem _upperBodySlotOverride;
            private UIListItem _lowerBodySlotOverride;
            private UIListItem _pipeSlotOverride;
            private UIListItem[] _bagSlotOverrides;
            private List<UIListItem> _generalSlots;
            private UIListItem _generalSlotTemplate;
            private Transform _generalSlotParent;
            private int _visibleGeneralSlotCount;

            internal event Action<IInventoryItemDetails, NetworkString<_32>> ItemSelected;

            private int _selectedSlotIndex = -1;
            private int _hoveredSlotIndex = -1;
            private Vector2 _lastPointerPosition;

            internal InventoryListPresenter(UIGameplayInventoryView view)
            {
                _view = view;
            }

            internal void Initialize(
                UIList list,
                RectTransform dragLayer,
                UIListItem pickaxeSlot,
                UIListItem woodAxeSlot,
                UIListItem headSlot,
                UIListItem upperBodySlot,
                UIListItem lowerBodySlot,
                UIListItem pipeSlot,
                UIListItem bagSlotOne,
                UIListItem bagSlotTwo,
                UIListItem bagSlotThree,
                UIListItem bagSlotFour,
                UIListItem bagSlotFive)
            {
                _list = list;
                _dragLayer = dragLayer;
                _pickaxeSlotOverride = pickaxeSlot;
                _woodAxeSlotOverride = woodAxeSlot;
                _headSlotOverride = headSlot;
                _upperBodySlotOverride = upperBodySlot;
                _lowerBodySlotOverride = lowerBodySlot;
                _pipeSlotOverride = pipeSlot;
                _bagSlotOverrides = new[] { bagSlotOne, bagSlotTwo, bagSlotThree, bagSlotFour, bagSlotFive };

                if (_list == null)
                {
                    _generalSlots = new List<UIListItem>();
                    _slots = Array.Empty<UIListItem>();
                    _slotIndices = Array.Empty<int>();
                    _slotLookup = new Dictionary<int, UIListItem>();
                    _visibleGeneralSlotCount = 0;
                    UpdateSelectionHighlight();
                    return;
                }

                var discoveredSlots = _list.GetComponentsInChildren<UIListItem>(true);
                var specialSlots = new HashSet<UIListItem>();

                AddSpecialSlot(_pickaxeSlotOverride, specialSlots);
                AddSpecialSlot(_woodAxeSlotOverride, specialSlots);
                AddSpecialSlot(_headSlotOverride, specialSlots);
                AddSpecialSlot(_upperBodySlotOverride, specialSlots);
                AddSpecialSlot(_lowerBodySlotOverride, specialSlots);
                AddSpecialSlot(_pipeSlotOverride, specialSlots);

                if (_bagSlotOverrides != null)
                {
                    for (int i = 0; i < _bagSlotOverrides.Length; i++)
                    {
                        AddSpecialSlot(_bagSlotOverrides[i], specialSlots);
                    }
                }

                _generalSlots = new List<UIListItem>(discoveredSlots.Length);
                for (int i = 0; i < discoveredSlots.Length; i++)
                {
                    var slot = discoveredSlots[i];
                    if (slot == null)
                        continue;

                    if (specialSlots.Contains(slot) == true)
                        continue;

                    _generalSlots.Add(slot);
                }

                _visibleGeneralSlotCount = _generalSlots.Count;

                _generalSlotTemplate = null;
                for (int i = 0; i < _generalSlots.Count; i++)
                {
                    if (_generalSlots[i] != null)
                    {
                        _generalSlotTemplate = _generalSlots[i];
                        break;
                    }
                }

                _generalSlotParent = _generalSlotTemplate != null ? _generalSlotTemplate.transform.parent : _list.transform;

                RebuildSlotCache();
                UpdateSelectionHighlight();
            }

            internal void Bind(Inventory inventory)
            {
                if (_inventory == inventory)
                    return;

                if (_inventory != null)
                {
                    _inventory.ItemSlotChanged -= OnItemSlotChanged;
                    _inventory.GeneralInventorySizeChanged -= OnGeneralInventorySizeChanged;
                }

                _inventory = inventory;

                if (_inventory != null)
                {
                    _inventory.GeneralInventorySizeChanged += OnGeneralInventorySizeChanged;
                    OnGeneralInventorySizeChanged(_inventory.InventorySize);

                    if (_slotIndices != null)
                    {
                        for (int i = 0; i < _slotIndices.Length; i++)
                        {
                            int index = _slotIndices[i];
                            UpdateSlot(index, _inventory.GetItemSlot(index));
                        }
                    }

                    _inventory.ItemSlotChanged += OnItemSlotChanged;
                }
                else if (_slots != null)
                {
                    for (int i = 0; i < _slots.Length; i++)
                    {
                        _slots[i].Clear();
                    }
                }

                SetDragVisible(false);
                _dragSource = null;
                ClearSelection();
                _hoveredSlotIndex = -1;
                _view?.HideItemTooltip();
            }

            private void OnGeneralInventorySizeChanged(int size)
            {
                EnsureGeneralSlotCount(size);

                if (_inventory != null && _slotIndices != null)
                {
                    for (int i = 0; i < _slotIndices.Length; i++)
                    {
                        int index = _slotIndices[i];
                        UpdateSlot(index, _inventory.GetItemSlot(index));
                    }
                }

                if (_selectedSlotIndex >= size)
                {
                    _selectedSlotIndex = -1;
                    UpdateSelectionHighlight();
                    NotifySelectionChanged();
                }
                else
                {
                    UpdateSelectionHighlight();
                }

                if (_hoveredSlotIndex >= size)
                {
                    _hoveredSlotIndex = -1;
                    _view?.HideItemTooltip();
                }
            }

            private void EnsureGeneralSlotCount(int desiredCount)
            {
                if (_generalSlots == null)
                {
                    _generalSlots = new List<UIListItem>();
                }

                _visibleGeneralSlotCount = Mathf.Max(0, desiredCount);

                if (_generalSlotTemplate == null)
                {
                    for (int i = 0; i < _generalSlots.Count; i++)
                    {
                        if (_generalSlots[i] != null)
                        {
                            _generalSlotTemplate = _generalSlots[i];
                            break;
                        }
                    }
                }

                if (_generalSlotTemplate != null && _generalSlotParent == null)
                {
                    _generalSlotParent = _generalSlotTemplate.transform.parent;
                }

                while (_generalSlots.Count < _visibleGeneralSlotCount)
                {
                    if (_generalSlotTemplate == null || _generalSlotParent == null)
                        break;

                    var newSlot = UnityEngine.Object.Instantiate(_generalSlotTemplate, _generalSlotParent);
                    newSlot.name = $"{_generalSlotTemplate.name}_{_generalSlots.Count}";
                    newSlot.Clear();
                    _generalSlots.Add(newSlot);
                }

                RebuildSlotCache();
            }

            private void RebuildSlotCache()
            {
                int estimatedCapacity = _visibleGeneralSlotCount;

                if (_pickaxeSlotOverride != null) estimatedCapacity++;
                if (_woodAxeSlotOverride != null) estimatedCapacity++;
                if (_headSlotOverride != null) estimatedCapacity++;
                if (_upperBodySlotOverride != null) estimatedCapacity++;
                if (_lowerBodySlotOverride != null) estimatedCapacity++;
                if (_pipeSlotOverride != null) estimatedCapacity++;

                if (_bagSlotOverrides != null)
                {
                    for (int i = 0; i < _bagSlotOverrides.Length; i++)
                    {
                        if (_bagSlotOverrides[i] != null)
                        {
                            estimatedCapacity++;
                        }
                    }
                }

                var orderedSlots = new List<UIListItem>(estimatedCapacity);
                var indices = new List<int>(estimatedCapacity);
                _slotLookup = new Dictionary<int, UIListItem>(estimatedCapacity);

                if (_generalSlots != null)
                {
                    for (int i = 0; i < _generalSlots.Count; i++)
                    {
                        var slot = _generalSlots[i];
                        bool isVisible = i < _visibleGeneralSlotCount;
                        if (slot != null)
                        {
                            slot.gameObject.SetActive(isVisible);

                            if (isVisible == true)
                            {
                                slot.InitializeSlot(this, i);
                                orderedSlots.Add(slot);
                                indices.Add(i);
                                _slotLookup[i] = slot;
                            }
                        }
                    }
                }

                AddSpecialSlotMapping(
                    _pickaxeSlotOverride,
                    Inventory.PICKAXE_SLOT_INDEX,
                    orderedSlots,
                    indices,
                    $"{nameof(UIList)} inventory list is missing a pickaxe inventory slot.");

                AddSpecialSlotMapping(
                    _woodAxeSlotOverride,
                    Inventory.WOOD_AXE_SLOT_INDEX,
                    orderedSlots,
                    indices,
                    $"{nameof(UIList)} inventory list is missing a wood axe inventory slot.");

                AddSpecialSlotMapping(
                    _headSlotOverride,
                    Inventory.HEAD_SLOT_INDEX,
                    orderedSlots,
                    indices,
                    $"{nameof(UIList)} inventory list is missing a head equipment inventory slot.");

                AddSpecialSlotMapping(
                    _upperBodySlotOverride,
                    Inventory.UPPER_BODY_SLOT_INDEX,
                    orderedSlots,
                    indices,
                    $"{nameof(UIList)} inventory list is missing an upper body inventory slot.");

                AddSpecialSlotMapping(
                    _lowerBodySlotOverride,
                    Inventory.LOWER_BODY_SLOT_INDEX,
                    orderedSlots,
                    indices,
                    $"{nameof(UIList)} inventory list is missing a lower body inventory slot.");

                AddSpecialSlotMapping(
                    _pipeSlotOverride,
                    Inventory.PIPE_SLOT_INDEX,
                    orderedSlots,
                    indices,
                    $"{nameof(UIList)} inventory list is missing a pipe inventory slot.");

                if (_bagSlotOverrides != null)
                {
                    for (int i = 0; i < _bagSlotOverrides.Length; i++)
                    {
                        int bagIndex = Inventory.GetBagSlotIndex(i);
                        if (bagIndex < 0)
                            continue;

                        AddSpecialSlotMapping(
                            _bagSlotOverrides[i],
                            bagIndex,
                            orderedSlots,
                            indices,
                            $"{nameof(UIList)} inventory list is missing bag inventory slot {i + 1}.");
                    }
                }

                _slots = orderedSlots.ToArray();
                _slotIndices = indices.ToArray();
            }

            private void AddSpecialSlotMapping(
                UIListItem slot,
                int slotIndex,
                List<UIListItem> orderedSlots,
                List<int> indices,
                string warningMessage)
            {
                if (slot == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (string.IsNullOrEmpty(warningMessage) == false)
                    {
                        Debug.LogWarning(warningMessage);
                    }
#endif
                    return;
                }

                slot.InitializeSlot(this, slotIndex);
                orderedSlots.Add(slot);
                indices.Add(slotIndex);
                _slotLookup[slotIndex] = slot;
            }

            internal void SetSelectionColor(Color color)
            {
                if (_selectedSlotColor == color)
                    return;

                _selectedSlotColor = color;
                UpdateSelectionHighlight();
            }

            internal void ClearSelection(bool notify = true)
            {
                if (_selectedSlotIndex < 0)
                {
                    if (notify == true)
                    {
                        NotifySelectionChanged();
                    }
                    return;
                }

                _selectedSlotIndex = -1;
                UpdateSelectionHighlight();

                if (notify == true)
                {
                    NotifySelectionChanged();
                }
            }

            void IUIListItemOwner.BeginSlotDrag(UIListItem slot, PointerEventData eventData)
            {
                if (slot == null || _inventory == null)
                    return;

                _dragSource = slot;
                EnsureDragVisual();
                UpdateDragIcon(slot.IconSprite, slot.Quantity, slot.SlotRectTransform.rect.size);
                SetDragVisible(true);
                UpdateDragPosition(eventData);
            }

            void IUIListItemOwner.UpdateSlotDrag(PointerEventData eventData)
            {
                if (_dragSource == null)
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
                if (_inventory == null)
                    return;

                SetDragVisible(false);
                _dragSource = null;

                if (source == null || target == null)
                    return;

                if (ReferenceEquals(source.Owner, this) == true)
                {
                    if (source.Index == target.Index)
                        return;

                    _inventory.RequestMoveItem(source.Index, target.Index);
                    return;
                }

                if (source.Owner is UIHotbar)
                {
                    if (target.Index == Inventory.PICKAXE_SLOT_INDEX || target.Index == Inventory.WOOD_AXE_SLOT_INDEX ||
                        target.Index == Inventory.HEAD_SLOT_INDEX || target.Index == Inventory.UPPER_BODY_SLOT_INDEX ||
                        target.Index == Inventory.LOWER_BODY_SLOT_INDEX || target.Index == Inventory.PIPE_SLOT_INDEX || Inventory.IsBagSlotIndex(target.Index))
                        return;

                    _inventory.RequestStoreHotbar(source.Index, target.Index);
                }
            }

            void IUIListItemOwner.HandleSlotDropOutside(UIListItem slot, PointerEventData eventData)
            {
                if (_inventory == null || slot == null)
                    return;

                _inventory.RequestDropInventoryItem(slot.Index);
            }

            void IUIListItemOwner.HandleSlotSelected(UIListItem slot)
            {
                if (_inventory == null || slot == null || slot.HasItem == false)
                {
                    ClearSelection();
                    return;
                }

                if (_selectedSlotIndex == slot.Index)
                {
                    NotifySelectionChanged();
                    return;
                }

                _selectedSlotIndex = slot.Index;
                UpdateSelectionHighlight();
                NotifySelectionChanged();
            }

            void IUIListItemOwner.HandleSlotPointerEnter(UIListItem slot, PointerEventData eventData)
            {
                if (slot == null || eventData == null || _inventory == null || _view == null)
                {
                    _hoveredSlotIndex = -1;
                    HideTooltip();
                    return;
                }

                int index = slot.Index;
                if (index < 0)
                {
                    _hoveredSlotIndex = -1;
                    HideTooltip();
                    return;
                }

                _lastPointerPosition = eventData.position;

                if (TryShowTooltip(index, _lastPointerPosition))
                {
                    _hoveredSlotIndex = index;
                }
                else
                {
                    _hoveredSlotIndex = -1;
                    HideTooltip();
                }
            }

            void IUIListItemOwner.HandleSlotPointerExit(UIListItem slot)
            {
                if (slot != null && _hoveredSlotIndex == slot.Index)
                {
                    _hoveredSlotIndex = -1;
                    HideTooltip();
                }
            }

            void IUIListItemOwner.HandleSlotPointerMove(UIListItem slot, PointerEventData eventData)
            {
                if (slot == null || eventData == null)
                    return;

                if (slot.Index != _hoveredSlotIndex)
                    return;

                _lastPointerPosition = eventData.position;

                if (_inventory == null)
                {
                    _hoveredSlotIndex = -1;
                    HideTooltip();
                    return;
                }

                var hoveredSlot = _inventory.GetItemSlot(_hoveredSlotIndex);
                if (hoveredSlot.IsEmpty)
                {
                    _hoveredSlotIndex = -1;
                    HideTooltip();
                    return;
                }

                _view?.UpdateItemTooltipPosition(_lastPointerPosition);
            }

            private void OnItemSlotChanged(int index, InventorySlot slot)
            {
                UpdateSlot(index, slot);

                if (_selectedSlotIndex == index)
                {
                    if (slot.IsEmpty)
                    {
                        _selectedSlotIndex = -1;
                        UpdateSelectionHighlight();
                        NotifySelectionChanged();
                    }
                    else
                    {
                        NotifySelectionChanged();
                    }
                }

                if (_hoveredSlotIndex == index)
                {
                    if (slot.IsEmpty)
                    {
                        _hoveredSlotIndex = -1;
                        HideTooltip();
                    }
                    else if (TryShowTooltip(index, _lastPointerPosition) == false)
                    {
                        _hoveredSlotIndex = -1;
                        HideTooltip();
                    }
                }
            }

            private void UpdateSlot(int index, InventorySlot slot)
            {
                if (_slotLookup == null)
                    return;

                if (_slotLookup.TryGetValue(index, out var uiSlot) == false)
                    return;

                if (slot.IsEmpty)
                {
                    uiSlot.Clear();
                    return;
                }

                var definition = slot.GetDefinition();
                var sprite = definition != null ? definition.Icon : null;

                uiSlot.SetItem(sprite, slot.Quantity);
            }

            private void UpdateSelectionHighlight()
            {
                if (_slots == null || _slotIndices == null)
                    return;

                for (int i = 0; i < _slots.Length; i++)
                {
                    bool isSelected = _slotIndices[i] == _selectedSlotIndex;
                    _slots[i].SetSelectionHighlight(isSelected, _selectedSlotColor);
                }
            }

            private void NotifySelectionChanged()
            {
                if (ItemSelected == null)
                    return;

                if (_inventory == null || _selectedSlotIndex < 0)
                {
                    ItemSelected.Invoke(null, default);
                    return;
                }

                var slot = _inventory.GetItemSlot(_selectedSlotIndex);

                if (slot.IsEmpty)
                {
                    ItemSelected.Invoke(null, default);
                    return;
                }

                var definition = slot.GetDefinition();
                var itemDetails = ResolveItemDetails(definition);

                ItemSelected.Invoke(itemDetails, slot.ConfigurationHash);
            }

            private IInventoryItemDetails ResolveItemDetails(ItemDefinition definition)
            {
                if (definition is WeaponDefinition weaponDefinition && weaponDefinition.WeaponPrefab != null)
                {
                    return weaponDefinition.WeaponPrefab;
                }

                if (definition is PickaxeDefinition pickaxeDefinition && pickaxeDefinition.PickaxePrefab != null)
                {
                    return pickaxeDefinition.PickaxePrefab;
                }

                if (definition is WoodAxeDefinition woodAxeDefinition && woodAxeDefinition.WoodAxePrefab != null)
                {
                    return woodAxeDefinition.WoodAxePrefab;
                }

                if (definition is FishingPoleDefinition fishingPoleDefinition && fishingPoleDefinition.FishingPolePrefab != null)
                {
                    return fishingPoleDefinition.FishingPolePrefab;
                }

                return null;
            }

            private bool TryShowTooltip(int slotIndex, Vector2 screenPosition)
            {
                if (_inventory == null || _view == null || slotIndex < 0)
                    return false;

                var slot = _inventory.GetItemSlot(slotIndex);
                if (slot.IsEmpty)
                    return false;

                var definition = slot.GetDefinition();
                if (definition == null)
                    return false;

                var details = ResolveItemDetails(definition);
                if (details != null)
                {
                    _view.ShowItemTooltip(details, definition, slot.ConfigurationHash, screenPosition);
                    return true;
                }

                string title = definition.Name;
                if (slot.Quantity > 1)
                {
                    title = string.IsNullOrEmpty(title) ? $"x{slot.Quantity}" : $"{title} x{slot.Quantity}";
                }

                if (string.IsNullOrEmpty(title))
                    return false;

                _view.ShowItemTooltip(definition, title, string.Empty, screenPosition);
                return true;
            }

            private void HideTooltip()
            {
                _view?.HideItemTooltip();
            }

            private void EnsureDragVisual()
            {
                if (_dragIcon != null)
                    return;

                var parent = _dragLayer != null ? _dragLayer : _view?.GameplaySceneUI?.Canvas.transform as RectTransform;
                if (parent == null)
                    return;

                var dragObject = new GameObject("InventoryDrag", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
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

            private void UpdateDragIcon(Sprite sprite, int quantity, Vector2 size)
            {
                if (_dragIcon == null)
                    return;

                if (sprite == null || quantity <= 0)
                {
                    SetDragVisible(false);
                    return;
                }

                _dragImage.sprite = sprite;
                _dragImage.color = Color.white;
                _dragIcon.sizeDelta = size;
            }

            private void UpdateDragPosition(PointerEventData eventData)
            {
                if (_dragIcon == null)
                    return;

                RectTransform referenceRect = _dragIcon.parent as RectTransform;
                if (referenceRect == null)
                    return;

                var sceneUI = _view?.GameplaySceneUI;
                if (sceneUI == null)
                    return;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRect, eventData.position, sceneUI.Canvas.worldCamera, out Vector2 localPoint))
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

            private static void AddSpecialSlot(UIListItem slot, HashSet<UIListItem> specialSlots)
            {
                if (slot == null || specialSlots == null)
                    return;

                specialSlots.Add(slot);
            }
        }

        private void RefreshInventoryBinding()
        {
            if (Context == null)
            {
                if (_boundAgent != null)
                {
                    if (_boundInventory != null)
                    {
                        _boundInventory.GoldChanged -= OnGoldChanged;
                    }

                    _boundAgent = null;
                    _boundInventory = null;
                    _inventoryPresenter?.Bind(null);
                    _hotbar?.Bind(null);
                    UpdateGoldLabel(0);
                    HideAllTooltips();
                }
                return;
            }

            var agent = Context.ObservedAgent;
            if (_boundAgent == agent)
                return;

            if (_boundInventory != null)
            {
                _boundInventory.GoldChanged -= OnGoldChanged;
            }

            _boundAgent = agent;
            _boundInventory = agent != null ? agent.Inventory : null;
            _inventoryPresenter?.Bind(_boundInventory);
            _hotbar?.Bind(_boundInventory);
            HideAllTooltips();

            if (_boundInventory != null)
            {
                _boundInventory.GoldChanged += OnGoldChanged;
                UpdateGoldLabel(_boundInventory.Gold);
            }
            else
            {
                UpdateGoldLabel(0);
            }
        }

        private void RefreshCharacterDetails()
        {
            if (_boundAgent == null)
                return;
            if(_characterDetails == false)
                return;

            _characterDetails.UpdateCharacterDetails(Global.PlayerService.PlayerData);
            _characterDetails.UpdateStats(_boundAgent.Stats);
            _characterDetails.UpdateProfessions(_boundAgent.Professions);
        }

        private void OnGoldChanged(int amount)
        {
            UpdateGoldLabel(amount);
        }

        private void UpdateGoldLabel(int amount)
        {
            if (_goldLabel == null)
                return;

            _goldLabel.text = amount.ToString();
        }
    }
}
