using System;
using System.Collections.Generic;
using Fusion;
using TMPro;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TSS.Data;

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
                [SerializeField]
                private UIInventoryDetailsPanel _detailsPanel;
                [SerializeField]
                private Button _buyButton;

                private readonly List<UIItemSlot> _spawnedSlots = new List<UIItemSlot>();
                private readonly List<ItemVendor.VendorItemData> _currentItems = new List<ItemVendor.VendorItemData>();
                private readonly List<ItemVendor.VendorItemData> _lastItems = new List<ItemVendor.VendorItemData>();
                private Func<List<ItemVendor.VendorItemData>, ItemVendor.VendorItemStatus> _itemProvider;
                private ItemVendor.VendorItemStatus _lastStatus = ItemVendor.VendorItemStatus.NoDefinitions;
                private int _selectedIndex = -1;
                private Agent _agent;

                private const int ITEM_COST = 10;

                public event Action<ItemVendor.VendorItemData> ItemSelected;

                public void Configure(Agent agent, Func<List<ItemVendor.VendorItemData>, ItemVendor.VendorItemStatus> itemProvider)
                {
                        _agent = agent;
                        _itemProvider = itemProvider;
                        _selectedIndex = -1;

                        if (_buyButton != null)
                        {
                                _buyButton.onClick.RemoveListener(HandleBuyButtonClicked);
                                _buyButton.onClick.AddListener(HandleBuyButtonClicked);
                        }

                        RefreshItemSlots(true);
                        _detailsPanel?.Hide();
                        UpdateBuyButtonState();
                }

                protected override void OnOpen()
                {
                        base.OnOpen();

                        RefreshItemSlots(true);
                        UpdateBuyButtonState();
                }

                protected override void OnClose()
                {
                        base.OnClose();

                        _itemProvider = null;
                        _agent = null;
                        _currentItems.Clear();
                        _lastItems.Clear();
                        _lastStatus = ItemVendor.VendorItemStatus.NoDefinitions;
                        _selectedIndex = -1;
                        ClearSlots();
                        SetEmptyState(string.Empty);
                        _detailsPanel?.Hide();

                        if (_buyButton != null)
                        {
                                _buyButton.onClick.RemoveListener(HandleBuyButtonClicked);
                                _buyButton.interactable = false;
                        }
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
                        RefreshSelectedItemDetails();
                        UpdateBuyButtonState();
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
                        UpdateDetailsPanel(null);
                        UpdateBuyButtonState();
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
                                UpdateDetailsPanel(null);
                                return;
                        }

                        if (_selectedIndex == slot.Index)
                        {
                                ItemVendor.VendorItemData data = _lastItems[slot.Index];
                                UpdateDetailsPanel(data);
                                ItemSelected?.Invoke(data);
                                UpdateBuyButtonState();
                                return;
                        }

                        _selectedIndex = slot.Index;
                        UpdateSelectionVisuals();
                        ItemVendor.VendorItemData selectedData = _lastItems[slot.Index];
                        UpdateDetailsPanel(selectedData);
                        ItemSelected?.Invoke(selectedData);
                        UpdateBuyButtonState();
                }

                private void RefreshSelectedItemDetails()
                {
                        if (_selectedIndex >= 0 && _selectedIndex < _lastItems.Count)
                        {
                                UpdateDetailsPanel(_lastItems[_selectedIndex]);
                        }
                        else
                        {
                                UpdateDetailsPanel(null);
                        }

                        UpdateBuyButtonState();
                }

                private void UpdateDetailsPanel(ItemVendor.VendorItemData? itemData)
                {
                        if (_detailsPanel == null)
                                return;

                        if (itemData.HasValue == false)
                        {
                                _detailsPanel.Hide();
                                return;
                        }

                        if (TryGetInventoryDetails(itemData.Value, out IInventoryItemDetails itemDetails, out NetworkString<_32> configurationHash) == false || itemDetails == null)
                        {
                                _detailsPanel.Hide();
                                return;
                        }

                        _detailsPanel.Show(itemDetails, configurationHash);
                }

                private bool TryGetInventoryDetails(ItemVendor.VendorItemData itemData, out IInventoryItemDetails itemDetails, out NetworkString<_32> configurationHash)
                {
                        itemDetails = null;
                        configurationHash = default;

                        ItemDefinition definition = itemData.Definition;

                        if (definition == null)
                        {
                                return false;
                        }

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
                        else if (definition is IInventoryItemDetails definitionDetails)
                        {
                                itemDetails = definitionDetails;
                        }

                        if (itemDetails == null)
                        {
                                return false;
                        }

                        if (string.IsNullOrWhiteSpace(itemData.ConfigurationHash) == false)
                        {
                                configurationHash = itemData.ConfigurationHash;
                        }

                        return true;
                }

                private void HandleBuyButtonClicked()
                {
                        _ = TryPurchaseSelectedItem();
                        UpdateBuyButtonState();
                }

                private bool TryPurchaseSelectedItem()
                {
                        if (_agent == null)
                                return false;

                        if (_selectedIndex < 0 || _selectedIndex >= _lastItems.Count)
                                return false;

                        Inventory inventory = _agent.Inventory;

                        if (inventory == null)
                                return false;

                        if (CanPurchaseSelectedItem(inventory) == false)
                                return false;

                        ItemVendor.VendorItemData selectedItem = _lastItems[_selectedIndex];

                        if (selectedItem.Definition == null)
                                return false;

                        if (inventory.TrySpendGold(ITEM_COST) == false)
                                return false;

                        byte quantity = (byte)Mathf.Clamp(selectedItem.Quantity, 0, byte.MaxValue);

                        if (quantity == 0)
                        {
                                quantity = 1;
                        }

                        NetworkString<_32> configurationHash = default;

                        if (string.IsNullOrWhiteSpace(selectedItem.ConfigurationHash) == false)
                        {
                                configurationHash = selectedItem.ConfigurationHash;
                        }

                        inventory.AddItem(selectedItem.Definition, quantity, configurationHash);

                        return true;
                }

                private void UpdateBuyButtonState()
                {
                        if (_buyButton == null)
                                return;

                        bool isInteractable = false;

                        if (_agent != null && _selectedIndex >= 0 && _selectedIndex < _lastItems.Count)
                        {
                                Inventory inventory = _agent.Inventory;

                                if (inventory != null && CanPurchaseSelectedItem(inventory) == true)
                                {
                                        isInteractable = true;
                                }
                        }

                        _buyButton.interactable = isInteractable;
                }

                private bool CanPurchaseSelectedItem(Inventory inventory)
                {
                        if (inventory == null)
                                return false;

                        if (inventory.Gold < ITEM_COST)
                                return false;

                        if (HasEmptyInventorySlot(inventory) == false)
                                return false;

                        if (_selectedIndex < 0 || _selectedIndex >= _lastItems.Count)
                                return false;

                        ItemVendor.VendorItemData selectedItem = _lastItems[_selectedIndex];

                        return selectedItem.Definition != null;
                }

                private static bool HasEmptyInventorySlot(Inventory inventory)
                {
                        if (inventory == null)
                                return false;

                        int slotCount = inventory.InventorySize;

                        for (int i = 0; i < slotCount; ++i)
                        {
                                InventorySlot slot = inventory.GetItemSlot(i);

                                if (slot.IsEmpty == true)
                                        return true;
                        }

                        return false;
                }
        }
}
