using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TPSBR.UI
{
        public sealed class UIItemContextView : UICloseView, IUIItemSlotOwner
        {
                [SerializeField]
                private TextMeshProUGUI _emptyStateLabel;
                [SerializeField]
                private string _noAgentText = "No agent available.";
                [SerializeField]
                private string _noInventoryText = "Inventory unavailable.";
                [SerializeField]
                private string _noStaffText = "No staffs available.";
                [SerializeField]
                private RectTransform _slotContainer;
                [SerializeField]
                private UIItemSlot _itemSlotPrefab;
                [SerializeField]
                private Color _selectedSlotColor = Color.white;

                private readonly List<UIItemSlot> _spawnedSlots = new List<UIItemSlot>();
                private readonly List<ArcaneConduit.StaffItemData> _currentItems = new List<ArcaneConduit.StaffItemData>();
                private readonly List<ArcaneConduit.StaffItemData> _lastItems = new List<ArcaneConduit.StaffItemData>();
                private Func<List<ArcaneConduit.StaffItemData>, ArcaneConduit.StaffItemStatus> _staffItemProvider;
                private ArcaneConduit.StaffItemStatus _lastStatus = ArcaneConduit.StaffItemStatus.NoAgent;
                private int _selectedIndex = -1;

                public event Action<ArcaneConduit.StaffItemData> StaffItemSelected;

                public void Configure(Agent agent, Func<List<ArcaneConduit.StaffItemData>, ArcaneConduit.StaffItemStatus> staffItemProvider)
                {
                        _ = agent;
                        _staffItemProvider = staffItemProvider;
                        _selectedIndex = -1;
                        RefreshStaffSlots(true);
                }

                protected override void OnOpen()
                {
                        base.OnOpen();

                        RefreshStaffSlots(true);
                }

                protected override void OnClose()
                {
                        base.OnClose();

                        _staffItemProvider = null;
                        _currentItems.Clear();
                        _lastItems.Clear();
                        _lastStatus = ArcaneConduit.StaffItemStatus.NoAgent;
                        _selectedIndex = -1;
                        ClearSlots();
                        SetEmptyState(string.Empty);
                }

                protected override void OnTick()
                {
                        base.OnTick();

                        RefreshStaffSlots(false);
                }

                private void RefreshStaffSlots(bool force)
                {
                        if (_itemSlotPrefab == null || _slotContainer == null)
                                return;

                        ArcaneConduit.StaffItemStatus status = ArcaneConduit.StaffItemStatus.NoAgent;

                        if (_staffItemProvider != null)
                        {
                                _currentItems.Clear();
                                status = _staffItemProvider.Invoke(_currentItems);
                        }

                        if (force == false && status == _lastStatus && AreItemListsEqual(_currentItems, _lastItems) == true)
                                return;

                        _lastStatus = status;

                        if (status != ArcaneConduit.StaffItemStatus.Success || _currentItems.Count == 0)
                        {
                                ClearSlots();
                                HandleEmptyState(status);
                                return;
                        }

                        EnsureSlotCapacity(_currentItems.Count);

                        for (int i = 0; i < _currentItems.Count; ++i)
                        {
                                UIItemSlot slot = _spawnedSlots[i];
                                ArcaneConduit.StaffItemData data = _currentItems[i];

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

                private void HandleEmptyState(ArcaneConduit.StaffItemStatus status)
                {
                        switch (status)
                        {
                                case ArcaneConduit.StaffItemStatus.NoAgent:
                                        SetEmptyState(_noAgentText);
                                        break;
                                case ArcaneConduit.StaffItemStatus.NoInventory:
                                        SetEmptyState(_noInventoryText);
                                        break;
                                case ArcaneConduit.StaffItemStatus.NoStaff:
                                default:
                                        SetEmptyState(_noStaffText);
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

                private static bool AreItemListsEqual(List<ArcaneConduit.StaffItemData> current, List<ArcaneConduit.StaffItemData> previous)
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

                private static void CopyItems(List<ArcaneConduit.StaffItemData> source, List<ArcaneConduit.StaffItemData> destination)
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
                        // Drag & drop is not supported within the conduit view yet.
                }

                void IUIItemSlotOwner.UpdateSlotDrag(PointerEventData eventData)
                {
                        // Drag & drop is not supported within the conduit view yet.
                }

                void IUIItemSlotOwner.EndSlotDrag(UIItemSlot slot, PointerEventData eventData)
                {
                        // Drag & drop is not supported within the conduit view yet.
                }

                void IUIItemSlotOwner.HandleSlotDrop(UIItemSlot source, UIItemSlot target)
                {
                        // Drag & drop is not supported within the conduit view yet.
                }

                void IUIItemSlotOwner.HandleSlotDropOutside(UIItemSlot slot, PointerEventData eventData)
                {
                        // Drag & drop is not supported within the conduit view yet.
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
                                StaffItemSelected?.Invoke(_lastItems[slot.Index]);
                                return;
                        }

                        _selectedIndex = slot.Index;
                        UpdateSelectionVisuals();
                        StaffItemSelected?.Invoke(_lastItems[slot.Index]);
                }
        }
}
