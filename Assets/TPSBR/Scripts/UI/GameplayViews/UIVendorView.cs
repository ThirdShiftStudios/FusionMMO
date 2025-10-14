using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TPSBR.UI
{
        public sealed class UIVendorView : UICloseView, IUIItemSlotOwner
        {
                [SerializeField]
                private TextMeshProUGUI _emptyStateLabel;
                [SerializeField]
                private string _noDefinitionsText = "No vendor categories configured.";
                [SerializeField]
                private string _noItemsText = "No items available.";
                [SerializeField]
                private RectTransform _slotContainer;
                [SerializeField]
                private UIItemSlot _itemSlotPrefab;
                [SerializeField]
                private Color _selectedSlotColor = Color.white;

                private readonly List<UIItemSlot> _spawnedSlots = new List<UIItemSlot>();
                private readonly List<ItemVendor.VendorItemData> _currentItems = new List<ItemVendor.VendorItemData>();
                private readonly List<ItemVendor.VendorItemData> _lastItems = new List<ItemVendor.VendorItemData>();
                private Func<List<ItemVendor.VendorItemData>, ItemVendor.VendorItemStatus> _itemProvider;
                private ItemVendor.VendorItemStatus _lastStatus = ItemVendor.VendorItemStatus.NoDefinitions;
                private int _selectedIndex = -1;

                public event Action<ItemVendor.VendorItemData> ItemSelected;

                public void Configure(Agent agent, Func<List<ItemVendor.VendorItemData>, ItemVendor.VendorItemStatus> itemProvider)
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
                        _lastStatus = ItemVendor.VendorItemStatus.NoDefinitions;
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

                        ItemVendor.VendorItemStatus status = ItemVendor.VendorItemStatus.NoDefinitions;

                        if (_itemProvider != null)
                        {
                                _currentItems.Clear();
                                status = _itemProvider.Invoke(_currentItems);
                        }

                        if (force == false && status == _lastStatus && AreItemListsEqual(_currentItems, _lastItems) == true)
                                return;

                        _lastStatus = status;

                        if (status != ItemVendor.VendorItemStatus.Success || _currentItems.Count == 0)
                        {
                                ClearSlots();
                                HandleEmptyState(status);
                                return;
                        }

                        EnsureSlotCapacity(_currentItems.Count);

                        for (int i = 0; i < _currentItems.Count; ++i)
                        {
                                UIItemSlot slot = _spawnedSlots[i];
                                ItemVendor.VendorItemData data = _currentItems[i];

                                slot.InitializeSlot(this, i);
                                slot.SetItem(data.Icon, data.Quantity);
                                slot.gameObject.SetActive(true);
                        }

                        for (int i = _currentItems.Count; i < _spawnedSlots.Count; ++i)
                        {
                                UIItemSlot slot = _spawnedSlots[i];
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
                                UIItemSlot newSlot = Instantiate(_itemSlotPrefab, _slotContainer);
                                newSlot.InitializeSlot(this, _spawnedSlots.Count);
                                newSlot.gameObject.SetActive(false);
                                _spawnedSlots.Add(newSlot);
                        }
                }

                private void ClearSlots()
                {
                        for (int i = 0; i < _spawnedSlots.Count; ++i)
                        {
                                UIItemSlot slot = _spawnedSlots[i];
                                if (slot == null)
                                        continue;

                                slot.Clear();
                                slot.gameObject.SetActive(false);
                        }

                        _selectedIndex = -1;
                        UpdateSelectionVisuals();
                }

                private void HandleEmptyState(ItemVendor.VendorItemStatus status)
                {
                        switch (status)
                        {
                                case ItemVendor.VendorItemStatus.NoDefinitions:
                                        SetEmptyState(_noDefinitionsText);
                                        break;
                                case ItemVendor.VendorItemStatus.NoItems:
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

                private static bool AreItemListsEqual(List<ItemVendor.VendorItemData> current, List<ItemVendor.VendorItemData> previous)
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

                private static void CopyItems(List<ItemVendor.VendorItemData> source, List<ItemVendor.VendorItemData> destination)
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
                                UIItemSlot slot = _spawnedSlots[i];
                                if (slot == null || slot.gameObject.activeSelf == false)
                                        continue;

                                bool isSelected = i == _selectedIndex;
                                slot.SetSelectionHighlight(isSelected, _selectedSlotColor);
                        }
                }

                void IUIItemSlotOwner.BeginSlotDrag(UIItemSlot slot, PointerEventData eventData)
                {
                        // Drag & drop is not supported within the vendor view yet.
                }

                void IUIItemSlotOwner.UpdateSlotDrag(PointerEventData eventData)
                {
                        // Drag & drop is not supported within the vendor view yet.
                }

                void IUIItemSlotOwner.EndSlotDrag(UIItemSlot slot, PointerEventData eventData)
                {
                        // Drag & drop is not supported within the vendor view yet.
                }

                void IUIItemSlotOwner.HandleSlotDrop(UIItemSlot source, UIItemSlot target)
                {
                        // Drag & drop is not supported within the vendor view yet.
                }

                void IUIItemSlotOwner.HandleSlotDropOutside(UIItemSlot slot, PointerEventData eventData)
                {
                        // Drag & drop is not supported within the vendor view yet.
                }

                void IUIItemSlotOwner.HandleSlotSelected(UIItemSlot slot)
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
