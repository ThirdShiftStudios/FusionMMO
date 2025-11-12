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
        public sealed class UIVendorView : UIExclusiveCloseView, IUIListItemOwner
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
                private UIListItem _itemSlotPrefab;
                [SerializeField]
                private RectTransform _inventorySlotContainer;
                [SerializeField]
                private UIListItem _inventoryItemSlotPrefab;
                [SerializeField]
                private Color _selectedSlotColor = Color.white;
                [SerializeField]
                private UIInventoryDetailsPanel _detailsPanel;
                [SerializeField]
                private Button _buyButton;
                [SerializeField]
                private Button _sellButton;

                private readonly List<UIListItem> _vendorSlots = new List<UIListItem>();
                private readonly List<UIListItem> _inventorySlots = new List<UIListItem>();
                private readonly List<ItemVendor.VendorItemData> _currentVendorItems = new List<ItemVendor.VendorItemData>();
                private readonly List<ItemVendor.VendorItemData> _lastVendorItems = new List<ItemVendor.VendorItemData>();
                private readonly List<ItemVendor.VendorItemData> _currentInventoryItems = new List<ItemVendor.VendorItemData>();
                private readonly List<ItemVendor.VendorItemData> _lastInventoryItems = new List<ItemVendor.VendorItemData>();
                private readonly Dictionary<UIListItem, SlotCategory> _slotCategories = new Dictionary<UIListItem, SlotCategory>();
                private Func<List<ItemVendor.VendorItemData>, ItemVendor.VendorItemStatus> _itemProvider;
                private ItemVendor.VendorItemStatus _lastStatus = ItemVendor.VendorItemStatus.NoDefinitions;
                private int _selectedVendorIndex = -1;
                private int _selectedInventoryIndex = -1;
                private Agent _agent;
                private ItemVendor _vendor;

                private enum SlotCategory
                {
                        Vendor,
                        Inventory
                }

                public event Action<ItemVendor.VendorItemData> ItemSelected;

                public void Configure(Agent agent, ItemVendor vendor, Func<List<ItemVendor.VendorItemData>, ItemVendor.VendorItemStatus> itemProvider)
                {
                        _agent = agent;
                        _vendor = vendor;
                        _itemProvider = itemProvider;
                        _selectedVendorIndex = -1;
                        _selectedInventoryIndex = -1;

                        if (_buyButton != null)
                        {
                                _buyButton.onClick.RemoveListener(HandleBuyButtonClicked);
                                _buyButton.onClick.AddListener(HandleBuyButtonClicked);
                        }

                        if (_sellButton != null)
                        {
                                _sellButton.onClick.RemoveListener(HandleSellButtonClicked);
                                _sellButton.onClick.AddListener(HandleSellButtonClicked);
                        }

                        RefreshVendorItems(true);
                        RefreshInventoryItems(true);
                        _detailsPanel?.Hide();
                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                protected override void OnOpen()
                {
                        EnsureExclusiveOpen();

                        base.OnOpen();

                        RefreshVendorItems(true);
                        RefreshInventoryItems(true);
                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                protected override void OnClose()
                {
                        base.OnClose();

                        _itemProvider = null;
                        _agent = null;
                        _vendor = null;
                        _currentVendorItems.Clear();
                        _lastVendorItems.Clear();
                        _currentInventoryItems.Clear();
                        _lastInventoryItems.Clear();
                        _lastStatus = ItemVendor.VendorItemStatus.NoDefinitions;
                        _selectedVendorIndex = -1;
                        _selectedInventoryIndex = -1;
                        ClearVendorSlots();
                        ClearInventorySlots();
                        SetEmptyState(string.Empty);
                        _detailsPanel?.Hide();

                        if (_buyButton != null)
                        {
                                _buyButton.onClick.RemoveListener(HandleBuyButtonClicked);
                                _buyButton.interactable = false;
                        }

                        if (_sellButton != null)
                        {
                                _sellButton.onClick.RemoveListener(HandleSellButtonClicked);
                                _sellButton.interactable = false;
                        }

                        TryRestoreSuppressedViews();
                }

                protected override void OnTick()
                {
                        base.OnTick();

                        RefreshVendorItems(false);
                        RefreshInventoryItems(false);
                }

                private void RefreshVendorItems(bool force)
                {
                        if (_itemSlotPrefab == null || _slotContainer == null)
                                return;

                        ItemVendor.VendorItemStatus status = ItemVendor.VendorItemStatus.NoDefinitions;

                        if (_itemProvider != null)
                        {
                                _currentVendorItems.Clear();
                                status = _itemProvider.Invoke(_currentVendorItems);
                        }

                        if (force == false && status == _lastStatus && AreItemListsEqual(_currentVendorItems, _lastVendorItems) == true)
                                return;

                        _lastStatus = status;

                        if (status != ItemVendor.VendorItemStatus.Success || _currentVendorItems.Count == 0)
                        {
                                ClearVendorSlots();
                                HandleEmptyState(status);
                                RefreshSelectedItemDetails();
                                return;
                        }

                        EnsureVendorSlotCapacity(_currentVendorItems.Count);

                        for (int i = 0; i < _currentVendorItems.Count; ++i)
                        {
                                UIListItem slot = _vendorSlots[i];
                                ItemVendor.VendorItemData data = _currentVendorItems[i];

                                slot.InitializeSlot(this, i);
                                slot.SetItem(data.Icon, data.Quantity);
                                var presenter = slot.GetComponent<UIVendorItemPresenter>();
                                presenter?.Apply(data, ItemVendor.ITEM_COST);
                                slot.gameObject.SetActive(true);
                                _slotCategories[slot] = SlotCategory.Vendor;
                        }

                        for (int i = _currentVendorItems.Count; i < _vendorSlots.Count; ++i)
                        {
                                UIListItem slot = _vendorSlots[i];
                                slot.Clear();
                                slot.GetComponent<UIVendorItemPresenter>()?.Clear();
                                slot.gameObject.SetActive(false);
                        }

                        CopyItems(_currentVendorItems, _lastVendorItems);
                        SetEmptyState(string.Empty);

                        if (_selectedVendorIndex >= _currentVendorItems.Count)
                        {
                                _selectedVendorIndex = -1;
                        }

                        UpdateSelectionVisuals();
                        RefreshSelectedItemDetails();
                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                private void RefreshInventoryItems(bool force)
                {
                        if (_agent == null || (_inventorySlotContainer == null && _slotContainer == null))
                                return;

                        Inventory inventory = _agent.Inventory;

                        _currentInventoryItems.Clear();

                        if (inventory != null)
                        {
                                int slotCount = inventory.InventorySize;

                                for (int i = 0; i < slotCount; ++i)
                                {
                                        InventorySlot slot = inventory.GetItemSlot(i);

                                        if (slot.IsEmpty == true)
                                                continue;

                                        ItemDefinition definition = ItemDefinition.Get(slot.ItemDefinitionId);
                                        if (definition == null)
                                                continue;

                                        Sprite icon = definition.Icon;
                                        string configurationHash = slot.ConfigurationHash.ToString();
                                        _currentInventoryItems.Add(new ItemVendor.VendorItemData(icon, slot.Quantity, definition, configurationHash, i));
                                }
                        }

                        if (force == false && AreItemListsEqual(_currentInventoryItems, _lastInventoryItems) == true)
                                return;

                        EnsureInventorySlotCapacity(_currentInventoryItems.Count);

                        for (int i = 0; i < _currentInventoryItems.Count; ++i)
                        {
                                UIListItem slot = _inventorySlots[i];
                                ItemVendor.VendorItemData data = _currentInventoryItems[i];

                                slot.InitializeSlot(this, i);
                                slot.SetItem(data.Icon, data.Quantity);
                                slot.gameObject.SetActive(true);
                                _slotCategories[slot] = SlotCategory.Inventory;
                        }

                        for (int i = _currentInventoryItems.Count; i < _inventorySlots.Count; ++i)
                        {
                                UIListItem slot = _inventorySlots[i];
                                slot.Clear();
                                slot.gameObject.SetActive(false);
                        }

                        CopyItems(_currentInventoryItems, _lastInventoryItems);

                        if (_selectedInventoryIndex >= _currentInventoryItems.Count)
                        {
                                _selectedInventoryIndex = -1;
                        }

                        UpdateSelectionVisuals();
                        RefreshSelectedItemDetails();
                        UpdateSellButtonState();
                }

                private void EnsureVendorSlotCapacity(int required)
                {
                        while (_vendorSlots.Count < required)
                        {
                                UIListItem newSlot = Instantiate(_itemSlotPrefab, _slotContainer);
                                newSlot.InitializeSlot(this, _vendorSlots.Count);
                                newSlot.gameObject.SetActive(false);
                                _vendorSlots.Add(newSlot);
                                _slotCategories[newSlot] = SlotCategory.Vendor;
                        }
                }

                private void EnsureInventorySlotCapacity(int required)
                {
                        UIListItem prefab = _inventoryItemSlotPrefab != null ? _inventoryItemSlotPrefab : _itemSlotPrefab;

                        if (prefab == null)
                                return;

                        RectTransform parent = _inventorySlotContainer != null ? _inventorySlotContainer : _slotContainer;

                        while (_inventorySlots.Count < required)
                        {
                                UIListItem newSlot = Instantiate(prefab, parent);
                                newSlot.InitializeSlot(this, _inventorySlots.Count);
                                newSlot.gameObject.SetActive(false);
                                _inventorySlots.Add(newSlot);
                                _slotCategories[newSlot] = SlotCategory.Inventory;
                        }
                }

                private void ClearVendorSlots()
                {
                        for (int i = 0; i < _vendorSlots.Count; ++i)
                        {
                                UIListItem slot = _vendorSlots[i];
                                if (slot == null)
                                        continue;

                                slot.Clear();
                                slot.GetComponent<UIVendorItemPresenter>()?.Clear();
                                slot.gameObject.SetActive(false);
                        }

                        _selectedVendorIndex = -1;
                        UpdateSelectionVisuals();
                        UpdateDetailsPanel(null);
                        UpdateBuyButtonState();
                }

                private void ClearInventorySlots()
                {
                        for (int i = 0; i < _inventorySlots.Count; ++i)
                        {
                                UIListItem slot = _inventorySlots[i];
                                if (slot == null)
                                        continue;

                                slot.Clear();
                                slot.gameObject.SetActive(false);
                        }

                        _selectedInventoryIndex = -1;
                        UpdateSelectionVisuals();
                        UpdateDetailsPanel(null);
                        UpdateSellButtonState();
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
                        for (int i = 0; i < _vendorSlots.Count; ++i)
                        {
                                UIListItem slot = _vendorSlots[i];
                                if (slot == null || slot.gameObject.activeSelf == false)
                                        continue;

                                bool isSelected = i == _selectedVendorIndex;
                                slot.SetSelectionHighlight(isSelected, _selectedSlotColor);
                        }

                        for (int i = 0; i < _inventorySlots.Count; ++i)
                        {
                                UIListItem slot = _inventorySlots[i];
                                if (slot == null || slot.gameObject.activeSelf == false)
                                        continue;

                                bool isSelected = i == _selectedInventoryIndex;
                                slot.SetSelectionHighlight(isSelected, _selectedSlotColor);
                        }
                }

                void IUIListItemOwner.BeginSlotDrag(UIListItem slot, PointerEventData eventData)
                {
                        // Drag & drop is not supported within the vendor view yet.
                }

                void IUIListItemOwner.UpdateSlotDrag(PointerEventData eventData)
                {
                        // Drag & drop is not supported within the vendor view yet.
                }

                void IUIListItemOwner.EndSlotDrag(UIListItem slot, PointerEventData eventData)
                {
                        // Drag & drop is not supported within the vendor view yet.
                }

                void IUIListItemOwner.HandleSlotDrop(UIListItem source, UIListItem target)
                {
                        // Drag & drop is not supported within the vendor view yet.
                }

                void IUIListItemOwner.HandleSlotDropOutside(UIListItem slot, PointerEventData eventData)
                {
                        // Drag & drop is not supported within the vendor view yet.
                }

                void IUIListItemOwner.HandleSlotSelected(UIListItem slot)
                {
                        if (slot == null)
                        {
                                return;
                        }

                        if (_slotCategories.TryGetValue(slot, out SlotCategory category) == false)
                        {
                                return;
                        }

                        switch (category)
                        {
                                case SlotCategory.Vendor:
                                        HandleVendorSlotSelected(slot);
                                        break;
                                case SlotCategory.Inventory:
                                HandleInventorySlotSelected(slot);
                                break;
                        }
                }

                void IUIListItemOwner.HandleSlotPointerEnter(UIListItem slot, PointerEventData eventData)
                {
                }

                void IUIListItemOwner.HandleSlotPointerExit(UIListItem slot)
                {
                }

                void IUIListItemOwner.HandleSlotPointerMove(UIListItem slot, PointerEventData eventData)
                {
                }

                private void RefreshSelectedItemDetails()
                {
                        if (_selectedVendorIndex >= 0 && _selectedVendorIndex < _lastVendorItems.Count)
                        {
                                UpdateDetailsPanel(_lastVendorItems[_selectedVendorIndex]);
                                UpdateBuyButtonState();
                                UpdateSellButtonState();
                                return;
                        }

                        if (_selectedInventoryIndex >= 0 && _selectedInventoryIndex < _lastInventoryItems.Count)
                        {
                                UpdateDetailsPanel(_lastInventoryItems[_selectedInventoryIndex]);
                                UpdateBuyButtonState();
                                UpdateSellButtonState();
                                return;
                        }

                        UpdateDetailsPanel(null);
                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                private void HandleVendorSlotSelected(UIListItem slot)
                {
                        if (slot.Index < 0 || slot.Index >= _lastVendorItems.Count)
                        {
                                _selectedVendorIndex = -1;
                                UpdateSelectionVisuals();
                                UpdateDetailsPanel(null);
                                UpdateBuyButtonState();
                                UpdateSellButtonState();
                                return;
                        }

                        if (_selectedVendorIndex == slot.Index)
                        {
                                ItemVendor.VendorItemData data = _lastVendorItems[slot.Index];
                                UpdateDetailsPanel(data);
                                ItemSelected?.Invoke(data);
                                UpdateBuyButtonState();
                                UpdateSellButtonState();
                                return;
                        }

                        _selectedVendorIndex = slot.Index;
                        _selectedInventoryIndex = -1;
                        UpdateSelectionVisuals();
                        ItemVendor.VendorItemData selectedData = _lastVendorItems[slot.Index];
                        UpdateDetailsPanel(selectedData);
                        ItemSelected?.Invoke(selectedData);
                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                private void HandleInventorySlotSelected(UIListItem slot)
                {
                        if (slot.Index < 0 || slot.Index >= _lastInventoryItems.Count)
                        {
                                _selectedInventoryIndex = -1;
                                UpdateSelectionVisuals();
                                UpdateDetailsPanel(null);
                                UpdateSellButtonState();
                                UpdateBuyButtonState();
                                return;
                        }

                        if (_selectedInventoryIndex == slot.Index)
                        {
                                ItemVendor.VendorItemData data = _lastInventoryItems[slot.Index];
                                UpdateDetailsPanel(data);
                                UpdateSellButtonState();
                                UpdateBuyButtonState();
                                return;
                        }

                        _selectedInventoryIndex = slot.Index;
                        _selectedVendorIndex = -1;
                        UpdateSelectionVisuals();
                        ItemVendor.VendorItemData selectedData = _lastInventoryItems[slot.Index];
                        UpdateDetailsPanel(selectedData);
                        UpdateSellButtonState();
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
                        if (_vendor == null || _agent == null)
                                return;

                        if (_selectedVendorIndex < 0 || _selectedVendorIndex >= _lastVendorItems.Count)
                                return;

                        ItemVendor.VendorItemData selectedItem = _lastVendorItems[_selectedVendorIndex];
                        _vendor.RequestPurchase(_agent, selectedItem.SourceIndex);
                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                private void UpdateBuyButtonState()
                {
                        if (_buyButton == null)
                                return;

                        bool isInteractable = false;

                        if (_agent != null && _vendor != null && _selectedVendorIndex >= 0 && _selectedVendorIndex < _lastVendorItems.Count && _selectedInventoryIndex < 0)
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

                        if (inventory.Gold < ItemVendor.ITEM_COST)
                                return false;

                        if (HasEmptyInventorySlot(inventory) == false)
                                return false;

                        if (_selectedVendorIndex < 0 || _selectedVendorIndex >= _lastVendorItems.Count)
                                return false;

                        ItemVendor.VendorItemData selectedItem = _lastVendorItems[_selectedVendorIndex];

                        return selectedItem.Definition != null;
                }

                private void HandleSellButtonClicked()
                {
                        if (_vendor == null || _agent == null)
                                return;

                        if (_selectedInventoryIndex < 0 || _selectedInventoryIndex >= _lastInventoryItems.Count)
                                return;

                        ItemVendor.VendorItemData selectedItem = _lastInventoryItems[_selectedInventoryIndex];
                        _vendor.RequestSell(_agent, selectedItem.SourceIndex);
                        UpdateSellButtonState();
                        UpdateBuyButtonState();
                }

                private void UpdateSellButtonState()
                {
                        if (_sellButton == null)
                                return;

                        bool isInteractable = false;

                        if (_agent != null && _vendor != null && _selectedInventoryIndex >= 0 && _selectedInventoryIndex < _lastInventoryItems.Count)
                        {
                                ItemVendor.VendorItemData selectedItem = _lastInventoryItems[_selectedInventoryIndex];

                                if (selectedItem.Definition != null && selectedItem.Definition is PickaxeDefinition == false && selectedItem.Definition is WoodAxeDefinition == false)
                                {
                                        isInteractable = true;
                                }
                        }

                        _sellButton.interactable = isInteractable;
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
