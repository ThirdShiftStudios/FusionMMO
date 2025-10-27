using System;
using System.Collections.Generic;
using Fusion;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIGameplayInventory : UICloseView
    {
        // PUBLIC MEMBERS

        public override bool NeedsCursor => _menuVisible;

        public bool MenuVisible => _menuVisible;
        // PRIVATE MEMBERS
        [SerializeField] private UIButton _cancelButton;
        [SerializeField] private UIList _inventoryList;
        [SerializeField] private RectTransform _inventoryDragLayer;
        [SerializeField] private UIHotbar _hotbar;
        [Header("Hotbar Slots")]
        [SerializeField] private UIListItem _hotbarSlot0;
        [SerializeField] private UIListItem _hotbarSlot1;
        [SerializeField] private UIListItem _hotbarSlot2;
        [SerializeField] private UIListItem _hotbarSlot3;
        [SerializeField] private UIListItem _hotbarSlot4;
        [SerializeField] private UIListItem _hotbarSlot5;
        [SerializeField] private UIListItem _hotbarSlot6;
        [Header("Special Inventory Slots")]
        [SerializeField] private UIListItem _pickaxeInventorySlot;
        [SerializeField] private UIListItem _woodAxeInventorySlot;
        [SerializeField] private UIListItem _headInventorySlot;
        [SerializeField] private UIListItem _upperBodyInventorySlot;
        [SerializeField] private UIListItem _lowerBodyInventorySlot;
        [SerializeField] private Color _selectedHotbarColor = Color.white;
        [SerializeField] private Color _selectedInventorySlotColor = Color.white;
        [SerializeField] private Color _selectedHotbarSlotColor = Color.white;
        [SerializeField] private UIInventoryDetailsPanel _detailsPanel;

        private bool _menuVisible;
        private Agent _boundAgent;
        private Inventory _boundInventory;
        private InventoryListPresenter _inventoryPresenter;
        private UICharacterDetailsView _characterDetails;
        private UIListItem[] _visibleHotbarSlots;

        internal SceneUI GameplaySceneUI => SceneUI;

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
                _inventoryPresenter.Initialize(_inventoryList, _inventoryDragLayer);
                _inventoryPresenter.SetSelectionColor(_selectedInventorySlotColor);
                _inventoryPresenter.ItemSelected += OnInventoryItemSelected;
            }

            if (_hotbar == null)
            {
                _hotbar = GetComponentInChildren<UIHotbar>(true);
            }

            if (_hotbar != null)
            {
                ConfigureHotbarSlots();
                _hotbar.SetSelectedColor(_selectedHotbarColor);
                _hotbar.SetSelectionHighlightColor(_selectedHotbarSlotColor);
                _hotbar.ItemSelected += OnHotbarItemSelected;
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelButton);
            }

            _detailsPanel?.Hide();
        }

        protected override void OnDeinitialize()
        {
            if (_inventoryPresenter != null)
            {
                _inventoryPresenter.ItemSelected -= OnInventoryItemSelected;
                _inventoryPresenter.Bind(null);
                _inventoryPresenter = null;
            }

            if (_hotbar != null)
            {
                _hotbar.ItemSelected -= OnHotbarItemSelected;
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
            _detailsPanel?.Hide();
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

        private void ConfigureHotbarSlots()
        {
            if (_hotbar == null)
                return;

            if (_visibleHotbarSlots == null)
            {
                var visibleSlots = new List<UIListItem>(6);
                AddVisibleHotbarSlot(_hotbarSlot1, 1, visibleSlots);
                AddVisibleHotbarSlot(_hotbarSlot2, 2, visibleSlots);
                AddVisibleHotbarSlot(_hotbarSlot3, 3, visibleSlots);
                AddVisibleHotbarSlot(_hotbarSlot4, 4, visibleSlots);
                AddVisibleHotbarSlot(_hotbarSlot5, 5, visibleSlots);
                AddVisibleHotbarSlot(_hotbarSlot6, 6, visibleSlots);

                _visibleHotbarSlots = visibleSlots.ToArray();
            }

            _hotbar.ConfigureSlots(_visibleHotbarSlots);
        }

        private static void AddVisibleHotbarSlot(UIListItem slot, int hotbarIndex, List<UIListItem> destination)
        {
            if (destination == null)
                return;

            if (slot == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"Hotbar slot {hotbarIndex} is not assigned on {nameof(UIGameplayInventory)}.");
#endif
                return;
            }

            destination.Add(slot);
        }


        private sealed class InventoryListPresenter : IUIListItemOwner
        {
            private readonly UIGameplayInventory _view;
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

            internal event Action<IInventoryItemDetails, NetworkString<_32>> ItemSelected;

            private int _selectedSlotIndex = -1;

            internal InventoryListPresenter(UIGameplayInventory view)
            {
                _view = view;
            }

            internal void Initialize(UIList list, RectTransform dragLayer)
            {
                _list = list;
                _dragLayer = dragLayer;

                if (_list == null)
                {
                    _slots = Array.Empty<UIListItem>();
                    _slotIndices = Array.Empty<int>();
                    _slotLookup = new Dictionary<int, UIListItem>();
                    UpdateSelectionHighlight();
                    return;
                }

                var discoveredSlots = _list.GetComponentsInChildren<UIListItem>(true);
                var reservedSlots = new HashSet<UIListItem>();

                UIListItem pickaxeSlot = _view._pickaxeInventorySlot;
                UIListItem woodAxeSlot = _view._woodAxeInventorySlot;
                UIListItem headSlot = _view._headInventorySlot;
                UIListItem upperBodySlot = _view._upperBodyInventorySlot;
                UIListItem lowerBodySlot = _view._lowerBodyInventorySlot;

                RegisterReservedSlot(pickaxeSlot, reservedSlots);
                RegisterReservedSlot(woodAxeSlot, reservedSlots);
                RegisterReservedSlot(headSlot, reservedSlots);
                RegisterReservedSlot(upperBodySlot, reservedSlots);
                RegisterReservedSlot(lowerBodySlot, reservedSlots);

                var generalSlots = new List<UIListItem>(discoveredSlots.Length);

                for (int i = 0; i < discoveredSlots.Length; i++)
                {
                    var slot = discoveredSlots[i];
                    if (slot == null)
                        continue;

                    if (reservedSlots.Contains(slot) == true)
                        continue;

                    generalSlots.Add(slot);
                }

                var orderedSlots = new List<UIListItem>(generalSlots.Count +
                                                         (pickaxeSlot != null ? 1 : 0) +
                                                         (woodAxeSlot != null ? 1 : 0) +
                                                         (headSlot != null ? 1 : 0) +
                                                         (upperBodySlot != null ? 1 : 0) +
                                                         (lowerBodySlot != null ? 1 : 0));
                var indices = new List<int>(orderedSlots.Capacity);
                _slotLookup = new Dictionary<int, UIListItem>(orderedSlots.Capacity);

                for (int i = 0; i < generalSlots.Count; i++)
                {
                    var slot = generalSlots[i];
                    slot.InitializeSlot(this, i);
                    orderedSlots.Add(slot);
                    indices.Add(i);
                    _slotLookup[i] = slot;
                }

                if (pickaxeSlot != null)
                {
                    pickaxeSlot.InitializeSlot(this, Inventory.PICKAXE_SLOT_INDEX);
                    orderedSlots.Add(pickaxeSlot);
                    indices.Add(Inventory.PICKAXE_SLOT_INDEX);
                    _slotLookup[Inventory.PICKAXE_SLOT_INDEX] = pickaxeSlot;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else
                {
                    Debug.LogWarning($"{nameof(UIList)} inventory list is missing a pickaxe inventory slot.");
                }
#endif

                if (woodAxeSlot != null)
                {
                    woodAxeSlot.InitializeSlot(this, Inventory.WOOD_AXE_SLOT_INDEX);
                    orderedSlots.Add(woodAxeSlot);
                    indices.Add(Inventory.WOOD_AXE_SLOT_INDEX);
                    _slotLookup[Inventory.WOOD_AXE_SLOT_INDEX] = woodAxeSlot;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else
                {
                    Debug.LogWarning($"{nameof(UIList)} inventory list is missing a wood axe inventory slot.");
                }
#endif

                if (headSlot != null)
                {
                    headSlot.InitializeSlot(this, Inventory.HEAD_SLOT_INDEX);
                    orderedSlots.Add(headSlot);
                    indices.Add(Inventory.HEAD_SLOT_INDEX);
                    _slotLookup[Inventory.HEAD_SLOT_INDEX] = headSlot;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else
                {
                    Debug.LogWarning($"{nameof(UIList)} inventory list is missing a head equipment inventory slot.");
                }
#endif

                if (upperBodySlot != null)
                {
                    upperBodySlot.InitializeSlot(this, Inventory.UPPER_BODY_SLOT_INDEX);
                    orderedSlots.Add(upperBodySlot);
                    indices.Add(Inventory.UPPER_BODY_SLOT_INDEX);
                    _slotLookup[Inventory.UPPER_BODY_SLOT_INDEX] = upperBodySlot;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else
                {
                    Debug.LogWarning($"{nameof(UIList)} inventory list is missing an upper body inventory slot.");
                }
#endif

                if (lowerBodySlot != null)
                {
                    lowerBodySlot.InitializeSlot(this, Inventory.LOWER_BODY_SLOT_INDEX);
                    orderedSlots.Add(lowerBodySlot);
                    indices.Add(Inventory.LOWER_BODY_SLOT_INDEX);
                    _slotLookup[Inventory.LOWER_BODY_SLOT_INDEX] = lowerBodySlot;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else
                {
                    Debug.LogWarning($"{nameof(UIList)} inventory list is missing a lower body inventory slot.");
                }
#endif

                _slots = orderedSlots.ToArray();
                _slotIndices = indices.ToArray();

                UpdateSelectionHighlight();
            }

            private static void RegisterReservedSlot(UIListItem slot, HashSet<UIListItem> reservedSlots)
            {
                if (slot == null || reservedSlots == null)
                    return;

                reservedSlots.Add(slot);
            }

            internal void Bind(Inventory inventory)
            {
                if (_inventory == inventory)
                    return;

                if (_inventory != null)
                {
                    _inventory.ItemSlotChanged -= OnItemSlotChanged;
                }

                _inventory = inventory;

                if (_inventory != null)
                {
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
                        target.Index == Inventory.FISHING_POLE_SLOT_INDEX ||
                        target.Index == Inventory.HEAD_SLOT_INDEX || target.Index == Inventory.UPPER_BODY_SLOT_INDEX ||
                        target.Index == Inventory.LOWER_BODY_SLOT_INDEX)
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
                var sprite = definition != null ? definition.IconSprite : null;

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

                IInventoryItemDetails itemDetails = null;

                if (definition is WeaponDefinition weaponDefinition && weaponDefinition.WeaponPrefab != null)
                {
                    itemDetails = weaponDefinition.WeaponPrefab;
                }
                else if (definition is PickaxeDefinition pickaxeDefinition && pickaxeDefinition.PickaxePrefab != null)
                {
                    itemDetails = pickaxeDefinition.PickaxePrefab;
                }
                else if (definition is WoodAxeDefinition woodAxeDefinition && woodAxeDefinition.WoodAxePrefab != null)
                {
                    itemDetails = woodAxeDefinition.WoodAxePrefab;
                }
                else if (definition is FishingPoleDefinition fishingPoleDefinition && fishingPoleDefinition.FishingPolePrefab != null)
                {
                    itemDetails = fishingPoleDefinition.FishingPolePrefab;
                }

                ItemSelected.Invoke(itemDetails, slot.ConfigurationHash);
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

        }

        private void RefreshInventoryBinding()
        {
            if (Context == null)
            {
                if (_boundAgent != null)
                {
                    _boundAgent = null;
                    _boundInventory = null;
                    _inventoryPresenter?.Bind(null);
                    _hotbar?.Bind(null);
                }
                return;
            }

            var agent = Context.ObservedAgent;
            if (_boundAgent == agent)
                return;

            _boundAgent = agent;
            _boundInventory = agent != null ? agent.Inventory : null;
            _inventoryPresenter?.Bind(_boundInventory);
            _hotbar?.Bind(_boundInventory);
        }
        private void RefreshCharacterDetails()
        {
            if(_boundAgent == null)
                return;
            
            _characterDetails.UpdateCharacterDetails(Global.PlayerService.PlayerData);
            _characterDetails.UpdateStats(_boundAgent.Stats);
            _characterDetails.UpdateProfessions(_boundAgent.Professions);
        }
    }
}
