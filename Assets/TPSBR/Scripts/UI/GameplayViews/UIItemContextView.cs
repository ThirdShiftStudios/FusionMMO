using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TPSBR.UI
{
        public sealed class UIItemContextView : UICloseView, IUIListItemOwner
        {
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

                private readonly List<UIListItem> _spawnedSlots = new List<UIListItem>();
                private readonly List<CraftingStation.ItemData> _currentItems = new List<CraftingStation.ItemData>();
                private readonly List<CraftingStation.ItemData> _lastItems = new List<CraftingStation.ItemData>();
                private Func<List<CraftingStation.ItemData>, CraftingStation.ItemStatus> _itemProvider;
                private CraftingStation.ItemStatus _lastStatus = CraftingStation.ItemStatus.NoAgent;
                private int _selectedIndex = -1;

                public event Action<CraftingStation.ItemData> ItemSelected;

                public void Configure(Agent agent, Func<List<CraftingStation.ItemData>, CraftingStation.ItemStatus> itemProvider)
                {
                        _ = agent;
                        _itemProvider = itemProvider;
                        _selectedIndex = -1;
                        RefreshItemSlots(true);
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
                        _lastStatus = CraftingStation.ItemStatus.NoAgent;
                        _selectedIndex = -1;
                        ClearSlots();
                        SetEmptyState(string.Empty);
                }

                protected override void OnTick()
                {
                        base.OnTick();

                        RefreshItemSlots(false);
                }

                private void RefreshItemSlots(bool force)
                {
                        if (_itemSlotPrefab == null || _slotContainer == null)
                                return;

                        CraftingStation.ItemStatus status = CraftingStation.ItemStatus.NoAgent;

                        if (_itemProvider != null)
                        {
                                _currentItems.Clear();
                                status = _itemProvider.Invoke(_currentItems);
                        }

                        if (force == false && status == _lastStatus && AreItemListsEqual(_currentItems, _lastItems) == true)
                                return;

                        _lastStatus = status;

                        if (status != CraftingStation.ItemStatus.Success || _currentItems.Count == 0)
                        {
                                ClearSlots();
                                HandleEmptyState(status);
                                return;
                        }

                        EnsureSlotCapacity(_currentItems.Count);

                        for (int i = 0; i < _currentItems.Count; ++i)
                        {
                                UIListItem slot = _spawnedSlots[i];
                                CraftingStation.ItemData data = _currentItems[i];

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

                private void HandleEmptyState(CraftingStation.ItemStatus status)
                {
                        switch (status)
                        {
                                case CraftingStation.ItemStatus.NoAgent:
                                        SetEmptyState(_noAgentText);
                                        break;
                                case CraftingStation.ItemStatus.NoInventory:
                                        SetEmptyState(_noInventoryText);
                                        break;
                                case CraftingStation.ItemStatus.NoItems:
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

                private static bool AreItemListsEqual(List<CraftingStation.ItemData> current, List<CraftingStation.ItemData> previous)
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

                private static void CopyItems(List<CraftingStation.ItemData> source, List<CraftingStation.ItemData> destination)
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

                        if (slot.Index < 0 || slot.Index >= _lastItems.Count)
                        {
                                _selectedIndex = -1;
                                UpdateSelectionVisuals();
                                return;
                        }

                        if (_selectedIndex == slot.Index)
                        {
                                ItemSelected?.Invoke(_lastItems[slot.Index]);
                                return;
                        }

                        _selectedIndex = slot.Index;
                        UpdateSelectionVisuals();
                        ItemSelected?.Invoke(_lastItems[slot.Index]);
                }
        }
}
