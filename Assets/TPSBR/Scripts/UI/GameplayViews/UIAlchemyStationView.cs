using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public sealed class UIAlchemyStationView : UIExclusiveCloseView, IUIListItemOwner
    {
        private enum ContainerType
        {
            Flora,
            Essense,
            Ore,
            Liquid
        }

        [SerializeField]
        private UIList _inventoryList;
        [SerializeField]
        private UIAlchemyContainer _floraContainer;
        [SerializeField]
        private UIAlchemyContainer _essenseContainer;
        [SerializeField]
        private UIAlchemyContainer _oreContainer;
        [SerializeField]
        private UIAlchemyContainer _liquidContainer;
        [SerializeField]
        private Button _alchemizeButton;
        [SerializeField]
        private TextMeshProUGUI _alchemizeLabel;
        [SerializeField]
        private string _alchemizeReadyText = "Alchemizatize";
        [SerializeField]
        private string _alchemizeDisabledText = "Select all reagents";

        private readonly List<InventoryEntry> _inventoryEntries = new List<InventoryEntry>();
        private readonly Dictionary<int, InventoryEntry> _inventoryLookup = new Dictionary<int, InventoryEntry>();
        private readonly Dictionary<ContainerType, List<InventoryEntry>> _containerEntries = new Dictionary<ContainerType, List<InventoryEntry>>();
        private readonly Dictionary<UIListItem, ContainerType> _containerSlotLookup = new Dictionary<UIListItem, ContainerType>();
        private readonly HashSet<UIListItem> _inventorySlots = new HashSet<UIListItem>();

        private Agent _agent;
        private Inventory _inventory;

        private void Awake()
        {
            InitializeContainer(ContainerType.Flora, _floraContainer);
            InitializeContainer(ContainerType.Essense, _essenseContainer);
            InitializeContainer(ContainerType.Ore, _oreContainer);
            InitializeContainer(ContainerType.Liquid, _liquidContainer);

            if (_inventoryList != null)
            {
                _inventoryList.UpdateContent -= HandleUpdateInventoryContent;
                _inventoryList.UpdateContent += HandleUpdateInventoryContent;
            }

            if (_alchemizeButton != null)
            {
                _alchemizeButton.onClick.RemoveListener(HandleAlchemizeClicked);
                _alchemizeButton.onClick.AddListener(HandleAlchemizeClicked);
            }
        }

        protected override void OnOpen()
        {
            EnsureExclusiveOpen();

            base.OnOpen();

            RebuildInventoryEntries();
            RefreshContainers();
            UpdateAlchemizeState();
        }

        protected override void OnClose()
        {
            base.OnClose();

            UnsubscribeFromInventory();
            _agent = null;
            _inventory = null;
            _inventoryEntries.Clear();
            _inventoryLookup.Clear();
            _inventorySlots.Clear();

            foreach (KeyValuePair<ContainerType, List<InventoryEntry>> pair in _containerEntries)
            {
                pair.Value.Clear();
            }

            if (_inventoryList != null)
            {
                _inventoryList.Refresh(0);
            }

            RefreshContainers();
            UpdateAlchemizeState();
            TryRestoreSuppressedViews();
        }

        public void Configure(Agent agent)
        {
            _agent = agent;
            _inventory = agent != null ? agent.Inventory : null;

            foreach (KeyValuePair<ContainerType, List<InventoryEntry>> pair in _containerEntries)
            {
                pair.Value.Clear();
            }

            RebuildInventoryEntries();
            SubscribeToInventory();
            RefreshContainers();
            UpdateAlchemizeState();
        }

        private void InitializeContainer(ContainerType type, UIAlchemyContainer container)
        {
            _containerEntries[type] = new List<InventoryEntry>();

            if (container == null || container.List == null)
                return;

            container.List.UpdateContent += (index, content) => HandleUpdateContainerContent(type, index, content);
        }

        private void SubscribeToInventory()
        {
            if (_inventory == null)
                return;

            _inventory.ItemSlotChanged -= HandleInventorySlotChanged;
            _inventory.ItemSlotChanged += HandleInventorySlotChanged;
        }

        private void UnsubscribeFromInventory()
        {
            if (_inventory == null)
                return;

            _inventory.ItemSlotChanged -= HandleInventorySlotChanged;
        }

        private void HandleInventorySlotChanged(int index, InventorySlot slot)
        {
            _ = index;
            _ = slot;
            RebuildInventoryEntries();
        }

        private void RebuildInventoryEntries()
        {
            _inventoryEntries.Clear();
            _inventoryLookup.Clear();
            _inventorySlots.Clear();

            if (_inventory != null)
            {
                int generalSize = _inventory.InventorySize;
                for (int i = 0; i < generalSize; i++)
                {
                    TryAddInventorySlot(i, _inventory.GetItemSlot(i));
                }

                for (int i = 0; i < Inventory.BAG_SLOT_COUNT; i++)
                {
                    int bagIndex = Inventory.GetBagSlotIndex(i);
                    if (bagIndex >= 0)
                    {
                        TryAddInventorySlot(bagIndex, _inventory.GetItemSlot(bagIndex));
                    }
                }
            }

            if (_inventoryList != null)
            {
                _inventoryList.Refresh(_inventoryEntries.Count);
            }

            UpdateAlchemizeState();
        }

        private void TryAddInventorySlot(int slotIndex, InventorySlot slot)
        {
            if (slot.IsEmpty)
                return;

            ItemDefinition definition = slot.GetDefinition();
            if (definition == null)
                return;

            if (TryResolveContainer(definition, out ContainerType containerType) == false)
                return;

            var entry = new InventoryEntry(slotIndex, slot, definition, containerType);
            _inventoryLookup[_inventoryEntries.Count] = entry;
            _inventoryEntries.Add(entry);
        }

        private bool TryResolveContainer(ItemDefinition definition, out ContainerType containerType)
        {
            switch (definition)
            {
                case FloraResource:
                    containerType = ContainerType.Flora;
                    return true;
                case EssenseResource:
                    containerType = ContainerType.Essense;
                    return true;
                case OreResource:
                    containerType = ContainerType.Ore;
                    return true;
                case BaseLiquid:
                    containerType = ContainerType.Liquid;
                    return true;
                default:
                    containerType = default;
                    return false;
            }
        }

        private void HandleUpdateInventoryContent(int index, MonoBehaviour content)
        {
            if (_inventoryList == null)
                return;

            if (content is UIListItem slot == false)
                return;

            slot.InitializeSlot(this, index);
            _inventorySlots.Add(slot);
            slot.Clear();

            if (_inventoryLookup.TryGetValue(index, out InventoryEntry entry) == false)
                return;

            Sprite icon = entry.Definition != null ? entry.Definition.Icon : null;
            slot.SetItem(icon, entry.Slot.Quantity);
        }

        private void HandleUpdateContainerContent(ContainerType type, int index, MonoBehaviour content)
        {
            if (content is UIListItem slot == false)
                return;

            slot.InitializeSlot(this, index);
            _containerSlotLookup[slot] = type;
            slot.Clear();

            List<InventoryEntry> entries = _containerEntries[type];
            if (entries == null)
                return;

            if (entries.Count == 0)
                return;

            if (index < 0 || index >= entries.Count)
                return;

            InventoryEntry entry = entries[index];
            Sprite icon = entry.Definition != null ? entry.Definition.Icon : null;
            slot.SetItem(icon, entry.Slot.Quantity);
        }

        private void RefreshContainers()
        {
            _containerSlotLookup.Clear();
            RefreshContainer(ContainerType.Flora, _floraContainer);
            RefreshContainer(ContainerType.Essense, _essenseContainer);
            RefreshContainer(ContainerType.Ore, _oreContainer);
            RefreshContainer(ContainerType.Liquid, _liquidContainer);
        }

        private void RefreshContainer(ContainerType type, UIAlchemyContainer container)
        {
            if (container == null || container.List == null)
                return;

            List<InventoryEntry> entries = _containerEntries[type];
            int count = Mathf.Max(entries.Count, 1);
            container.List.Refresh(count);
            container.SetHighlight(false);
        }

        private void HandleAlchemizeClicked()
        {
        }

        private void UpdateAlchemizeState()
        {
            bool ready = HasRequiredReagents();

            if (_alchemizeButton != null)
            {
                _alchemizeButton.interactable = ready;
            }

            if (_alchemizeLabel != null)
            {
                _alchemizeLabel.text = ready ? _alchemizeReadyText : _alchemizeDisabledText;
            }
        }

        private bool HasRequiredReagents()
        {
            return _containerEntries[ContainerType.Flora].Count > 0 &&
                   _containerEntries[ContainerType.Essense].Count > 0 &&
                   _containerEntries[ContainerType.Ore].Count > 0 &&
                   _containerEntries[ContainerType.Liquid].Count > 0;
        }

        private bool TryGetEntry(UIListItem slot, out InventoryEntry entry)
        {
            if (slot == null)
            {
                entry = default;
                return false;
            }

            if (slot.Index < 0)
            {
                entry = default;
                return false;
            }

            return _inventoryLookup.TryGetValue(slot.Index, out entry);
        }

        private bool TryGetTargetContainer(UIListItem slot, out ContainerType type)
        {
            if (slot == null)
            {
                type = default;
                return false;
            }

            return _containerSlotLookup.TryGetValue(slot, out type);
        }

        private void ToggleHighlight(UIListItem slot, bool highlight)
        {
            if (TryGetTargetContainer(slot, out ContainerType type) == false)
                return;

            if (TryGetContainer(type, out UIAlchemyContainer container) == false)
                return;

            container.SetHighlight(highlight);
        }

        private bool TryGetContainer(ContainerType type, out UIAlchemyContainer container)
        {
            switch (type)
            {
                case ContainerType.Flora:
                    container = _floraContainer;
                    return container != null;
                case ContainerType.Essense:
                    container = _essenseContainer;
                    return container != null;
                case ContainerType.Ore:
                    container = _oreContainer;
                    return container != null;
                case ContainerType.Liquid:
                    container = _liquidContainer;
                    return container != null;
                default:
                    container = null;
                    return false;
            }
        }

        private void AddToContainer(ContainerType type, InventoryEntry entry)
        {
            if (_containerEntries.TryGetValue(type, out List<InventoryEntry> entries) == false)
                return;

            entries.Add(entry);
            RefreshContainer(type, GetContainer(type));
            UpdateAlchemizeState();
        }

        private UIAlchemyContainer GetContainer(ContainerType type)
        {
            TryGetContainer(type, out UIAlchemyContainer container);
            return container;
        }

        void IUIListItemOwner.BeginSlotDrag(UIListItem slot, PointerEventData eventData)
        {
            ToggleHighlight(slot, false);
        }

        void IUIListItemOwner.UpdateSlotDrag(PointerEventData eventData)
        {
        }

        void IUIListItemOwner.EndSlotDrag(UIListItem slot, PointerEventData eventData)
        {
            ToggleHighlight(slot, false);
        }

        void IUIListItemOwner.HandleSlotDrop(UIListItem source, UIListItem target)
        {
            if (_inventorySlots.Contains(source) == false)
                return;

            if (TryGetEntry(source, out InventoryEntry entry) == false)
                return;

            if (TryGetTargetContainer(target, out ContainerType targetContainer) == false)
                return;

            if (entry.ContainerType != targetContainer)
                return;

            AddToContainer(targetContainer, entry);
            ToggleHighlight(target, false);
        }

        void IUIListItemOwner.HandleSlotDropOutside(UIListItem slot, PointerEventData eventData)
        {
            ToggleHighlight(slot, false);
        }

        void IUIListItemOwner.HandleSlotSelected(UIListItem slot)
        {
        }

        void IUIListItemOwner.HandleSlotPointerEnter(UIListItem slot, PointerEventData eventData)
        {
            if (eventData == null)
                return;

            if (eventData.pointerDrag == null)
                return;

            if (eventData.pointerDrag.TryGetComponent(out UIListItem sourceSlot) == false)
                return;

            if (_inventorySlots.Contains(sourceSlot) == false)
                return;

            if (TryGetEntry(sourceSlot, out InventoryEntry entry) == false)
                return;

            if (TryGetTargetContainer(slot, out ContainerType containerType) == false)
                return;

            if (entry.ContainerType != containerType)
                return;

            ToggleHighlight(slot, true);
        }

        void IUIListItemOwner.HandleSlotPointerExit(UIListItem slot)
        {
            ToggleHighlight(slot, false);
        }

        void IUIListItemOwner.HandleSlotPointerMove(UIListItem slot, PointerEventData eventData)
        {
            _ = slot;
            _ = eventData;
        }

        private readonly struct InventoryEntry
        {
            public InventoryEntry(int slotIndex, InventorySlot slot, ItemDefinition definition, ContainerType containerType)
            {
                SlotIndex = slotIndex;
                Slot = slot;
                Definition = definition;
                ContainerType = containerType;
            }

            public int SlotIndex { get; }
            public InventorySlot Slot { get; }
            public ItemDefinition Definition { get; }
            public ContainerType ContainerType { get; }
        }

        [Serializable]
        private sealed class UIAlchemyContainer
        {
            [SerializeField]
            private UIList _list;
            [SerializeField]
            private Image _highlightImage;

            public UIList List => _list;

            public void SetHighlight(bool highlight)
            {
                if (_highlightImage == null)
                    return;

                _highlightImage.enabled = highlight;
            }
        }
    }
}
