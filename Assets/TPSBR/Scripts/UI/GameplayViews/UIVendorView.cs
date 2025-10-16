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
                [SerializeField]
                private RectTransform _playerSlotContainer;
                [SerializeField]
                private UIItemSlot _playerItemSlotPrefab;
                [SerializeField]
                private Button _sellButton;

                private readonly List<UIItemSlot> _vendorSlots = new List<UIItemSlot>();
                private readonly List<UIItemSlot> _playerSlots = new List<UIItemSlot>();
                private readonly Dictionary<UIItemSlot, SlotCategory> _slotCategories = new Dictionary<UIItemSlot, SlotCategory>();
                private readonly List<ItemVendor.VendorItemData> _currentVendorItems = new List<ItemVendor.VendorItemData>();
                private readonly List<ItemVendor.VendorItemData> _lastVendorItems = new List<ItemVendor.VendorItemData>();
                private readonly List<PlayerInventoryItemData> _currentPlayerItems = new List<PlayerInventoryItemData>();
                private readonly List<PlayerInventoryItemData> _lastPlayerItems = new List<PlayerInventoryItemData>();
                private Func<List<ItemVendor.VendorItemData>, ItemVendor.VendorItemStatus> _itemProvider;
                private ItemVendor.VendorItemStatus _lastStatus = ItemVendor.VendorItemStatus.NoDefinitions;
                private SelectionSource _selectionSource = SelectionSource.None;
                private int _selectedVendorIndex = -1;
                private int _selectedPlayerIndex = -1;
                private ItemVendor _vendor;
                private Agent _agent;

                public event Action<ItemVendor.VendorItemData> ItemSelected;

                private enum SlotCategory
                {
                        Vendor,
                        Player
                }

                private enum SelectionSource
                {
                        None,
                        Vendor,
                        Player
                }

                private readonly struct PlayerInventoryItemData : IEquatable<PlayerInventoryItemData>
                {
                        public PlayerInventoryItemData(int inventoryIndex, InventorySlot slot, ItemDefinition definition)
                        {
                                InventoryIndex = inventoryIndex;
                                Slot = slot;
                                Definition = definition;
                        }

                        public int InventoryIndex { get; }
                        public InventorySlot Slot { get; }
                        public ItemDefinition Definition { get; }
                        public int Quantity => Slot.Quantity;
                        public Sprite Icon => Definition != null ? Definition.IconSprite : null;
                        public string ConfigurationHash => Slot.ConfigurationHash.ToString();

                        public bool Equals(PlayerInventoryItemData other)
                        {
                                return InventoryIndex == other.InventoryIndex && Slot.Equals(other.Slot);
                        }

                        public override bool Equals(object obj)
                        {
                                return obj is PlayerInventoryItemData other && Equals(other);
                        }

                        public override int GetHashCode()
                        {
                                unchecked
                                {
                                        int hashCode = InventoryIndex;
                                        hashCode = (hashCode * 397) ^ Slot.GetHashCode();
                                        return hashCode;
                                }
                        }
                }

                public void Configure(ItemVendor vendor, Agent agent, Func<List<ItemVendor.VendorItemData>, ItemVendor.VendorItemStatus> itemProvider)
                {
                        _vendor = vendor;
                        _agent = agent;
                        _itemProvider = itemProvider;
                        _selectionSource = SelectionSource.None;
                        _selectedVendorIndex = -1;
                        _selectedPlayerIndex = -1;

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

                        RefreshAllSlots(true);
                        _detailsPanel?.Hide();
                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                protected override void OnOpen()
                {
                        base.OnOpen();

                        RefreshAllSlots(true);
                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                protected override void OnClose()
                {
                        base.OnClose();

                        _vendor = null;
                        _itemProvider = null;
                        _agent = null;
                        _currentVendorItems.Clear();
                        _lastVendorItems.Clear();
                        _currentPlayerItems.Clear();
                        _lastPlayerItems.Clear();
                        _lastStatus = ItemVendor.VendorItemStatus.NoDefinitions;
                        _selectionSource = SelectionSource.None;
                        _selectedVendorIndex = -1;
                        _selectedPlayerIndex = -1;
                        ClearSlots();
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
                }

                protected override void OnTick()
                {
                        base.OnTick();

                        RefreshAllSlots(false);
                }

                private void RefreshAllSlots(bool force)
                {
                        RefreshVendorSlots(force);
                        RefreshPlayerSlots(force);
                }

                private void RefreshVendorSlots(bool force)
                {
                        if (_itemSlotPrefab == null || _slotContainer == null)
                                return;

                        ItemVendor.VendorItemStatus status = ItemVendor.VendorItemStatus.NoDefinitions;

                        if (_itemProvider != null)
                        {
                                _currentVendorItems.Clear();
                                status = _itemProvider.Invoke(_currentVendorItems);
                        }

                        if (force == false && status == _lastStatus && AreVendorItemListsEqual(_currentVendorItems, _lastVendorItems) == true)
                                return;

                        _lastStatus = status;

                        if (status != ItemVendor.VendorItemStatus.Success || _currentVendorItems.Count == 0)
                        {
                                ClearVendorSlots();
                                HandleEmptyState(status);
                                UpdateSelectionVisuals();
                                RefreshSelectedItemDetails();
                                UpdateBuyButtonState();
                                return;
                        }

                        SetEmptyState(string.Empty);
                        EnsureVendorSlotCapacity(_currentVendorItems.Count);

                        for (int i = 0; i < _vendorSlots.Count; ++i)
                        {
                                UIItemSlot slot = _vendorSlots[i];
                                if (slot == null)
                                        continue;

                                if (i < _currentVendorItems.Count)
                                {
                                        ItemVendor.VendorItemData data = _currentVendorItems[i];
                                        slot.gameObject.SetActive(true);
                                        slot.SetItem(data.Icon, data.Quantity);
                                }
                                else
                                {
                                        slot.Clear();
                                        slot.gameObject.SetActive(false);
                                }
                        }

                        CopyVendorItems(_currentVendorItems, _lastVendorItems);

                        if (_selectionSource == SelectionSource.Vendor && (_selectedVendorIndex < 0 || _selectedVendorIndex >= _lastVendorItems.Count))
                        {
                                _selectionSource = SelectionSource.None;
                                _selectedVendorIndex = -1;
                        }

                        UpdateSelectionVisuals();
                        RefreshSelectedItemDetails();
                        UpdateBuyButtonState();
                }

                private void RefreshPlayerSlots(bool force)
                {
                        if (_playerSlotContainer == null)
                                return;

                        _currentPlayerItems.Clear();

                        if (_agent != null)
                        {
                                Inventory inventory = _agent.Inventory;

                                if (inventory != null)
                                {
                                        int slotCount = inventory.InventorySize;

                                        for (int i = 0; i < slotCount; ++i)
                                        {
                                                InventorySlot slot = inventory.GetItemSlot(i);

                                                if (slot.IsEmpty == true)
                                                        continue;

                                                ItemDefinition definition = slot.GetDefinition();

                                                if (definition == null)
                                                        continue;

                                                _currentPlayerItems.Add(new PlayerInventoryItemData(i, slot, definition));
                                        }
                                }
                        }

                        if (force == false && ArePlayerItemListsEqual(_currentPlayerItems, _lastPlayerItems) == true)
                                return;

                        EnsurePlayerSlotCapacity(_currentPlayerItems.Count);

                        for (int i = 0; i < _playerSlots.Count; ++i)
                        {
                                UIItemSlot slot = _playerSlots[i];
                                if (slot == null)
                                        continue;

                                if (i < _currentPlayerItems.Count)
                                {
                                        PlayerInventoryItemData data = _currentPlayerItems[i];
                                        slot.gameObject.SetActive(true);
                                        slot.SetItem(data.Icon, data.Quantity);
                                }
                                else
                                {
                                        slot.Clear();
                                        slot.gameObject.SetActive(false);
                                }
                        }

                        CopyPlayerItems(_currentPlayerItems, _lastPlayerItems);

                        if (_selectionSource == SelectionSource.Player && (_selectedPlayerIndex < 0 || _selectedPlayerIndex >= _lastPlayerItems.Count))
                        {
                                _selectionSource = SelectionSource.None;
                                _selectedPlayerIndex = -1;
                        }

                        UpdateSelectionVisuals();
                        RefreshSelectedItemDetails();
                        UpdateSellButtonState();
                }

                private void EnsureVendorSlotCapacity(int required)
                {
                        if (_itemSlotPrefab == null || _slotContainer == null)
                                return;

                        while (_vendorSlots.Count < required)
                        {
                                UIItemSlot newSlot = Instantiate(_itemSlotPrefab, _slotContainer);
                                newSlot.InitializeSlot(this, _vendorSlots.Count);
                                newSlot.gameObject.SetActive(false);
                                _vendorSlots.Add(newSlot);
                                _slotCategories[newSlot] = SlotCategory.Vendor;
                        }
                }

                private void EnsurePlayerSlotCapacity(int required)
                {
                        if (_playerSlotContainer == null)
                                return;

                        UIItemSlot prefab = _playerItemSlotPrefab != null ? _playerItemSlotPrefab : _itemSlotPrefab;

                        if (prefab == null)
                                return;

                        while (_playerSlots.Count < required)
                        {
                                UIItemSlot newSlot = Instantiate(prefab, _playerSlotContainer);
                                newSlot.InitializeSlot(this, _playerSlots.Count);
                                newSlot.gameObject.SetActive(false);
                                _playerSlots.Add(newSlot);
                                _slotCategories[newSlot] = SlotCategory.Player;
                        }
                }

                private void ClearSlots()
                {
                        ClearVendorSlots();
                        ClearPlayerSlots();

                        _selectionSource = SelectionSource.None;
                        _selectedVendorIndex = -1;
                        _selectedPlayerIndex = -1;
                        UpdateSelectionVisuals();
                        UpdateDetailsPanel(null, null);
                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                private void ClearVendorSlots()
                {
                        for (int i = 0; i < _vendorSlots.Count; ++i)
                        {
                                UIItemSlot slot = _vendorSlots[i];
                                if (slot == null)
                                        continue;

                                slot.Clear();
                                slot.gameObject.SetActive(false);
                        }

                        _currentVendorItems.Clear();
                        _lastVendorItems.Clear();
                }

                private void ClearPlayerSlots()
                {
                        for (int i = 0; i < _playerSlots.Count; ++i)
                        {
                                UIItemSlot slot = _playerSlots[i];
                                if (slot == null)
                                        continue;

                                slot.Clear();
                                slot.gameObject.SetActive(false);
                        }

                        _currentPlayerItems.Clear();
                        _lastPlayerItems.Clear();
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

                private static bool AreVendorItemListsEqual(List<ItemVendor.VendorItemData> current, List<ItemVendor.VendorItemData> previous)
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

                private static bool ArePlayerItemListsEqual(List<PlayerInventoryItemData> current, List<PlayerInventoryItemData> previous)
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

                private static void CopyVendorItems(List<ItemVendor.VendorItemData> source, List<ItemVendor.VendorItemData> destination)
                {
                        destination.Clear();

                        if (source == null)
                                return;

                        destination.AddRange(source);
                }

                private static void CopyPlayerItems(List<PlayerInventoryItemData> source, List<PlayerInventoryItemData> destination)
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
                                UIItemSlot slot = _vendorSlots[i];
                                if (slot == null || slot.gameObject.activeSelf == false)
                                        continue;

                                bool isSelected = _selectionSource == SelectionSource.Vendor && i == _selectedVendorIndex;
                                slot.SetSelectionHighlight(isSelected, _selectedSlotColor);
                        }

                        for (int i = 0; i < _playerSlots.Count; ++i)
                        {
                                UIItemSlot slot = _playerSlots[i];
                                if (slot == null || slot.gameObject.activeSelf == false)
                                        continue;

                                bool isSelected = _selectionSource == SelectionSource.Player && i == _selectedPlayerIndex;
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
                                return;

                        if (_slotCategories.TryGetValue(slot, out SlotCategory category) == false)
                                return;

                        switch (category)
                        {
                                case SlotCategory.Vendor:
                                        HandleVendorSlotSelected(slot.Index);
                                        break;
                                case SlotCategory.Player:
                                        HandlePlayerSlotSelected(slot.Index);
                                        break;
                        }
                }

                private void HandleVendorSlotSelected(int index)
                {
                        if (index < 0 || index >= _lastVendorItems.Count)
                        {
                                if (_selectionSource == SelectionSource.Vendor)
                                {
                                        _selectionSource = SelectionSource.None;
                                        _selectedVendorIndex = -1;
                                        UpdateSelectionVisuals();
                                        RefreshSelectedItemDetails();
                                }
                                return;
                        }

                        if (_selectionSource == SelectionSource.Vendor && _selectedVendorIndex == index)
                        {
                                ItemVendor.VendorItemData data = _lastVendorItems[index];
                                UpdateDetailsPanel(data.Definition, data.ConfigurationHash);
                                ItemSelected?.Invoke(data);
                                UpdateBuyButtonState();
                                return;
                        }

                        _selectionSource = SelectionSource.Vendor;
                        _selectedVendorIndex = index;
                        _selectedPlayerIndex = -1;

                        UpdateSelectionVisuals();

                        ItemVendor.VendorItemData selectedData = _lastVendorItems[index];
                        UpdateDetailsPanel(selectedData.Definition, selectedData.ConfigurationHash);
                        ItemSelected?.Invoke(selectedData);
                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                private void HandlePlayerSlotSelected(int index)
                {
                        if (index < 0 || index >= _lastPlayerItems.Count)
                        {
                                if (_selectionSource == SelectionSource.Player)
                                {
                                        _selectionSource = SelectionSource.None;
                                        _selectedPlayerIndex = -1;
                                        UpdateSelectionVisuals();
                                        RefreshSelectedItemDetails();
                                }
                                return;
                        }

                        if (_selectionSource == SelectionSource.Player && _selectedPlayerIndex == index)
                        {
                                PlayerInventoryItemData data = _lastPlayerItems[index];
                                UpdateDetailsPanel(data.Definition, data.ConfigurationHash);
                                UpdateSellButtonState();
                                return;
                        }

                        _selectionSource = SelectionSource.Player;
                        _selectedPlayerIndex = index;
                        _selectedVendorIndex = -1;

                        UpdateSelectionVisuals();

                        PlayerInventoryItemData selectedData = _lastPlayerItems[index];
                        UpdateDetailsPanel(selectedData.Definition, selectedData.ConfigurationHash);
                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                private void RefreshSelectedItemDetails()
                {
                        switch (_selectionSource)
                        {
                                case SelectionSource.Vendor:
                                        if (_selectedVendorIndex >= 0 && _selectedVendorIndex < _lastVendorItems.Count)
                                        {
                                                ItemVendor.VendorItemData vendorItem = _lastVendorItems[_selectedVendorIndex];
                                                UpdateDetailsPanel(vendorItem.Definition, vendorItem.ConfigurationHash);
                                        }
                                        else
                                        {
                                                UpdateDetailsPanel(null, null);
                                        }
                                        break;
                                case SelectionSource.Player:
                                        if (_selectedPlayerIndex >= 0 && _selectedPlayerIndex < _lastPlayerItems.Count)
                                        {
                                                PlayerInventoryItemData playerItem = _lastPlayerItems[_selectedPlayerIndex];
                                                UpdateDetailsPanel(playerItem.Definition, playerItem.ConfigurationHash);
                                        }
                                        else
                                        {
                                                UpdateDetailsPanel(null, null);
                                        }
                                        break;
                                default:
                                        UpdateDetailsPanel(null, null);
                                        break;
                        }

                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                private void UpdateDetailsPanel(ItemDefinition definition, string configurationHash)
                {
                        if (_detailsPanel == null)
                                return;

                        if (definition == null)
                        {
                                _detailsPanel.Hide();
                                return;
                        }

                        if (TryGetInventoryDetails(definition, configurationHash, out IInventoryItemDetails itemDetails, out NetworkString<_32> networkConfigurationHash) == false || itemDetails == null)
                        {
                                _detailsPanel.Hide();
                                return;
                        }

                        _detailsPanel.Show(itemDetails, networkConfigurationHash);
                }

                private bool TryGetInventoryDetails(ItemDefinition definition, string configurationHash, out IInventoryItemDetails itemDetails, out NetworkString<_32> networkConfigurationHash)
                {
                        itemDetails = null;
                        networkConfigurationHash = default;

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

                        if (string.IsNullOrWhiteSpace(configurationHash) == false)
                        {
                                networkConfigurationHash = configurationHash;
                        }

                        return true;
                }

                private void HandleBuyButtonClicked()
                {
                        RequestPurchaseSelectedItem();
                        UpdateBuyButtonState();
                        UpdateSellButtonState();
                }

                private void HandleSellButtonClicked()
                {
                        RequestSellSelectedItem();
                        UpdateSellButtonState();
                        UpdateBuyButtonState();
                }

                private void RequestPurchaseSelectedItem()
                {
                        if (_vendor == null || _agent == null)
                                return;

                        if (_selectionSource != SelectionSource.Vendor)
                                return;

                        if (_selectedVendorIndex < 0 || _selectedVendorIndex >= _lastVendorItems.Count)
                                return;

                        ItemVendor.VendorItemData selectedItem = _lastVendorItems[_selectedVendorIndex];
                        _vendor.RequestPurchase(_agent, selectedItem.VendorIndex);
                }

                private void RequestSellSelectedItem()
                {
                        if (_vendor == null || _agent == null)
                                return;

                        if (_selectionSource != SelectionSource.Player)
                                return;

                        if (_selectedPlayerIndex < 0 || _selectedPlayerIndex >= _lastPlayerItems.Count)
                                return;

                        PlayerInventoryItemData selectedItem = _lastPlayerItems[_selectedPlayerIndex];
                        _vendor.RequestSell(_agent, selectedItem.InventoryIndex);
                }

                private void UpdateBuyButtonState()
                {
                        if (_buyButton == null)
                                return;

                        bool isInteractable = false;

                        if (_agent != null && _vendor != null && _selectionSource == SelectionSource.Vendor && _selectedVendorIndex >= 0 && _selectedVendorIndex < _lastVendorItems.Count)
                        {
                                Inventory inventory = _agent.Inventory;

                                if (inventory != null && CanPurchaseSelectedItem(inventory) == true)
                                {
                                        isInteractable = true;
                                }
                        }

                        _buyButton.interactable = isInteractable;
                }

                private void UpdateSellButtonState()
                {
                        if (_sellButton == null)
                                return;

                        bool isInteractable = false;

                        if (_vendor != null && _agent != null && _selectionSource == SelectionSource.Player && _selectedPlayerIndex >= 0 && _selectedPlayerIndex < _lastPlayerItems.Count)
                        {
                                if (_vendor.HasAvailableItemSlot() == true)
                                {
                                        isInteractable = true;
                                }
                        }

                        _sellButton.interactable = isInteractable;
                }

                private bool CanPurchaseSelectedItem(Inventory inventory)
                {
                        if (inventory == null || _vendor == null)
                                return false;

                        if (_selectionSource != SelectionSource.Vendor)
                                return false;

                        if (_selectedVendorIndex < 0 || _selectedVendorIndex >= _lastVendorItems.Count)
                                return false;

                        if (inventory.Gold < _vendor.PurchaseCost)
                                return false;

                        if (HasEmptyInventorySlot(inventory) == false)
                                return false;

                        ItemVendor.VendorItemData selectedItem = _lastVendorItems[_selectedVendorIndex];

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
