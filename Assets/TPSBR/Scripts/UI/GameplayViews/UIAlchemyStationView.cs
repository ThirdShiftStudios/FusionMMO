using System.Collections.Generic;
using TSS.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TPSBR.UI
{
    public enum AlchemyCategory
    {
        Flora,
        Essence,
        Ore,
        BaseLiquid,
    }

    public sealed class UIAlchemyStationView : UIExclusiveCloseView, IUIListItemOwner
    {
        public const string ResourcePath = "UI/GameplayViews/UIAlchemyStationView";

        [Header("Inventory")]
        [SerializeField]
        private RectTransform _inventorySlotContainer;
        [SerializeField]
        private UIListItem _inventorySlotPrefab;

        [Header("Containers")]
        [SerializeField]
        private UIAlchemyDropContainer _floraContainer;
        [SerializeField]
        private UIAlchemyDropContainer _essenceContainer;
        [SerializeField]
        private UIAlchemyDropContainer _oreContainer;
        [SerializeField]
        private UIAlchemyDropContainer _liquidContainer;

        [Header("Actions")]
        [SerializeField]
        private UIButton _alchemizeButton;

        public struct InventoryEntry
        {
            public Sprite Icon;
            public int Quantity;
            public ItemDefinition Definition;
            public int SlotIndex;
        }

        private readonly struct AlchemySelection
        {
            public int FloraSlotIndex { get; }
            public int EssenceSlotIndex { get; }
            public int OreSlotIndex { get; }
            public int LiquidSlotIndex { get; }

            public AlchemySelection(int floraSlotIndex, int essenceSlotIndex, int oreSlotIndex, int liquidSlotIndex)
            {
                FloraSlotIndex = floraSlotIndex;
                EssenceSlotIndex = essenceSlotIndex;
                OreSlotIndex = oreSlotIndex;
                LiquidSlotIndex = liquidSlotIndex;
            }
        }

        private readonly List<UIListItem> _inventorySlots = new List<UIListItem>();
        private readonly List<InventoryEntry> _inventoryItems = new List<InventoryEntry>();
        private readonly Dictionary<UIListItem, InventoryEntry> _slotToItem = new Dictionary<UIListItem, InventoryEntry>();
        private readonly Dictionary<UIListItem, UIAlchemyDropContainer> _dropSlotLookup = new Dictionary<UIListItem, UIAlchemyDropContainer>();
        private readonly List<UIAlchemyDropContainer> _allContainers = new List<UIAlchemyDropContainer>();

        private Agent _agent;
        private AlchemyStation _station;
        private AlchemyCategory? _activeDragCategory;

        public void Configure(Agent agent, AlchemyStation station)
        {
            _agent = agent;
            _station = station;
            ClearContainers();
            InitializeContainers();
            RefreshInventoryItems();
            UpdateAlchemizeButtonState();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_alchemizeButton != null)
            {
                _alchemizeButton.onClick.AddListener(HandleAlchemizeClicked);
                _alchemizeButton.interactable = false;
            }

            InitializeContainers();
        }

        protected override void OnDeinitialize()
        {
            if (_alchemizeButton != null)
            {
                _alchemizeButton.onClick.RemoveListener(HandleAlchemizeClicked);
            }

            ClearContainers();
            base.OnDeinitialize();
        }

        protected override void OnOpen()
        {
            EnsureExclusiveOpen();
            base.OnOpen();
            RefreshInventoryItems();
        }

        protected override void OnClose()
        {
            base.OnClose();

            TryRestoreSuppressedViews();
        }

        public void BeginSlotDrag(UIListItem slot, PointerEventData eventData)
        {
            _ = eventData;
            if (_slotToItem.TryGetValue(slot, out InventoryEntry entry) == true)
            {
                _activeDragCategory = ResolveCategory(entry.Definition);
                UpdateContainerHighlights();
            }
        }

        public void UpdateSlotDrag(PointerEventData eventData)
        {
            _ = eventData;
        }

        public void EndSlotDrag(UIListItem slot, PointerEventData eventData)
        {
            _ = slot;
            _ = eventData;
            _activeDragCategory = null;
            UpdateContainerHighlights();
        }

        public void HandleSlotDrop(UIListItem source, UIListItem target)
        {
            if (_slotToItem.TryGetValue(source, out InventoryEntry entry) == false)
                return;

            if (_dropSlotLookup.TryGetValue(target, out UIAlchemyDropContainer container) == false)
                return;

            if (container.Accepts(entry.Definition) == false)
                return;

            container.AddItem(entry);
            UpdateAlchemizeButtonState();
        }

        public void HandleSlotDropOutside(UIListItem slot, PointerEventData eventData)
        {
            _ = slot;
            _ = eventData;
        }

        public void HandleSlotSelected(UIListItem slot)
        {
            _ = slot;
        }

        public void HandleSlotPointerEnter(UIListItem slot, PointerEventData eventData)
        {
            _ = slot;
            _ = eventData;
        }

        public void HandleSlotPointerExit(UIListItem slot)
        {
            _ = slot;
        }

        public void HandleSlotPointerMove(UIListItem slot, PointerEventData eventData)
        {
            _ = slot;
            _ = eventData;
        }

        private void HandleAlchemizeClicked()
        {
            if (_agent == null || _station == null)
                return;

            if (TryResolveSelection(out AlchemySelection selection) == false)
                return;

            _station.RequestAlchemize(_agent, selection.FloraSlotIndex, selection.EssenceSlotIndex, selection.OreSlotIndex, selection.LiquidSlotIndex);

            ClearContainers();
            RefreshInventoryItems();
            UpdateAlchemizeButtonState();
        }

        private bool TryResolveSelection(out AlchemySelection selection)
        {
            selection = default;

            if (TryGetFirstItem(_floraContainer, out InventoryEntry flora) == false)
                return false;

            if (TryGetFirstItem(_essenceContainer, out InventoryEntry essence) == false)
                return false;

            if (TryGetFirstItem(_oreContainer, out InventoryEntry ore) == false)
                return false;

            if (TryGetFirstItem(_liquidContainer, out InventoryEntry liquid) == false)
                return false;

            selection = new AlchemySelection(flora.SlotIndex, essence.SlotIndex, ore.SlotIndex, liquid.SlotIndex);
            return true;
        }

        private bool TryGetFirstItem(UIAlchemyDropContainer container, out InventoryEntry entry)
        {
            entry = default;

            if (container == null)
                return false;

            IReadOnlyList<InventoryEntry> items = container.Items;
            if (items == null || items.Count == 0)
                return false;

            entry = items[0];
            return true;
        }

        private void RefreshInventoryItems()
        {
            _inventoryItems.Clear();
            _slotToItem.Clear();

            Inventory inventory = _agent != null ? _agent.Inventory : null;
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

                    if (IsAlchemyRelevant(definition) == false)
                        continue;

                    _inventoryItems.Add(new InventoryEntry
                    {
                        Icon = definition.Icon,
                        Quantity = slot.Quantity,
                        Definition = definition,
                        SlotIndex = i,
                    });
                }
            }

            EnsureInventorySlotCapacity(_inventoryItems.Count);

            for (int i = 0; i < _inventoryItems.Count; ++i)
            {
                UIListItem slot = _inventorySlots[i];
                InventoryEntry entry = _inventoryItems[i];

                slot.InitializeSlot(this, i);
                slot.SetItem(entry.Icon, entry.Quantity);
                slot.gameObject.SetActive(true);
                _slotToItem[slot] = entry;
            }

            for (int i = _inventoryItems.Count; i < _inventorySlots.Count; ++i)
            {
                _inventorySlots[i].gameObject.SetActive(false);
            }
        }

        private void EnsureInventorySlotCapacity(int required)
        {
            if (_inventorySlotPrefab == null || _inventorySlotContainer == null)
                return;

            while (_inventorySlots.Count < required)
            {
                UIListItem newSlot = Instantiate(_inventorySlotPrefab, _inventorySlotContainer);
                newSlot.InitializeSlot(this, _inventorySlots.Count);
                newSlot.gameObject.SetActive(false);
                _inventorySlots.Add(newSlot);
            }
        }

        private bool IsAlchemyRelevant(ItemDefinition definition)
        {
            if (definition == null)
                return false;

            return definition is FloraResource || definition is EssenceResource || definition is OreResource || definition is BaseLiquid;
        }

        private AlchemyCategory? ResolveCategory(ItemDefinition definition)
        {
            if (definition is FloraResource)
                return AlchemyCategory.Flora;

            if (definition is EssenceResource)
                return AlchemyCategory.Essence;

            if (definition is OreResource)
                return AlchemyCategory.Ore;

            if (definition is BaseLiquid)
                return AlchemyCategory.BaseLiquid;

            return null;
        }

        private void InitializeContainers()
        {
            _dropSlotLookup.Clear();
            _allContainers.Clear();

            AddContainer(_floraContainer);
            AddContainer(_essenceContainer);
            AddContainer(_oreContainer);
            AddContainer(_liquidContainer);
        }

        private void AddContainer(UIAlchemyDropContainer container)
        {
            if (container == null)
                return;

            int dropIndex = _dropSlotLookup.Count + 10_000;
            container.Initialize(this, dropIndex);
            _allContainers.Add(container);

            UIListItem dropSlot = container.DropSlot;
            if (dropSlot != null)
            {
                _dropSlotLookup[dropSlot] = container;
            }
        }

        private void ClearContainers()
        {
            foreach (UIAlchemyDropContainer container in _allContainers)
            {
                container?.ClearItems();
            }

            _allContainers.Clear();
            _dropSlotLookup.Clear();
        }

        private void UpdateContainerHighlights()
        {
            foreach (UIAlchemyDropContainer container in _allContainers)
            {
                bool highlight = _activeDragCategory != null && container.Category == _activeDragCategory.Value;
                container.SetHighlight(highlight);
            }
        }

        private void UpdateAlchemizeButtonState()
        {
            bool hasFlora = _floraContainer != null && _floraContainer.Items.Count > 0;
            bool hasEssence = _essenceContainer != null && _essenceContainer.Items.Count > 0;
            bool hasOre = _oreContainer != null && _oreContainer.Items.Count > 0;
            bool hasLiquid = _liquidContainer != null && _liquidContainer.Items.Count > 0;

            if (_alchemizeButton != null)
            {
                _alchemizeButton.interactable = hasFlora && hasEssence && hasOre && hasLiquid;
            }
        }
    }
}
