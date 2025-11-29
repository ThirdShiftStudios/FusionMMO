using System.Collections.Generic;
using TSS.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
            public IReadOnlyList<InventoryEntry> Items { get; }

            public AlchemySelection(IReadOnlyList<InventoryEntry> items)
            {
                Items = items;
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
        private UIListItem _dragSource;
        private RectTransform _dragIcon;
        private Canvas _dragCanvas;
        private CanvasGroup _dragCanvasGroup;
        private Image _dragImage;
        [SerializeField]
        private RectTransform _dragLayer;

        public void Configure(Agent agent, AlchemyStation station)
        {
            _agent = agent;
            _station = station;
            ClearContainerItems();
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
            ClearContainerItems();
            RefreshInventoryItems();
        }

        protected override void OnClose()
        {
            base.OnClose();

            ClearContainerItems();
            ResetDragState();
            TryRestoreSuppressedViews();
        }

        protected new void OnDisable()
        {
            ClearContainerItems();
            ResetDragState();

            base.OnDisable();
        }

        public void BeginSlotDrag(UIListItem slot, PointerEventData eventData)
        {
            if (slot == null || eventData == null)
                return;

            _dragSource = slot;

            if (_slotToItem.TryGetValue(slot, out InventoryEntry entry) == true)
            {
                _activeDragCategory = ResolveCategory(entry.Definition);
                UpdateContainerHighlights();
            }

            EnsureDragVisual();
            UpdateDragIcon(slot.IconSprite, slot.Quantity, GetSlotSize(slot));
            SetDragVisible(true);
            UpdateDragPosition(eventData);
        }

        public void UpdateSlotDrag(PointerEventData eventData)
        {
            if (_dragSource == null || eventData == null)
                return;

            UpdateDragPosition(eventData);
        }

        public void EndSlotDrag(UIListItem slot, PointerEventData eventData)
        {
            _ = eventData;
            if (_dragSource == slot)
            {
                _dragSource = null;
                SetDragVisible(false);
            }

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

            int totalUsedQuantity = GetTotalUsedQuantity(entry.SlotIndex);
            int remainingQuantity = entry.Quantity - totalUsedQuantity;
            if (remainingQuantity <= 0)
                return;

            if (container.AddItem(entry, entry.Quantity))
            {
                RefreshInventoryItems();
                ResetDragState();
            }
            UpdateAlchemizeButtonState();
        }

        public void HandleSlotDropOutside(UIListItem slot, PointerEventData eventData)
        {
            _ = slot;
            _ = eventData;

            ResetDragState();
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

            _station.RequestAlchemize(_agent, selection.Items);

            ClearContainerItems();
            RefreshInventoryItems();
            UpdateAlchemizeButtonState();
        }

        private bool TryResolveSelection(out AlchemySelection selection)
        {
            selection = default;

            if (TryCollectContainerItems(_floraContainer, out List<InventoryEntry> flora) == false)
                return false;

            if (TryCollectContainerItems(_essenceContainer, out List<InventoryEntry> essence) == false)
                return false;

            if (TryCollectContainerItems(_oreContainer, out List<InventoryEntry> ore) == false)
                return false;

            if (TryCollectContainerItems(_liquidContainer, out List<InventoryEntry> liquid) == false)
                return false;

            List<InventoryEntry> allItems = new List<InventoryEntry>(flora.Count + essence.Count + ore.Count + liquid.Count);
            allItems.AddRange(flora);
            allItems.AddRange(essence);
            allItems.AddRange(ore);
            allItems.AddRange(liquid);

            selection = new AlchemySelection(allItems);
            return true;
        }

        private bool TryCollectContainerItems(UIAlchemyDropContainer container, out List<InventoryEntry> items)
        {
            items = null;

            if (container == null)
                return false;

            IReadOnlyList<InventoryEntry> containerItems = container.Items;
            if (containerItems == null || containerItems.Count == 0)
                return false;

            items = new List<InventoryEntry>(containerItems.Count);
            items.AddRange(containerItems);
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

                    int usedQuantity = GetTotalUsedQuantity(i);
                    int remainingQuantity = slot.Quantity - usedQuantity;
                    if (remainingQuantity <= 0)
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
                int remainingQuantity = GetRemainingQuantity(entry);
                slot.SetItem(entry.Icon, remainingQuantity);
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
            ClearContainerItems();

            _allContainers.Clear();
            _dropSlotLookup.Clear();
        }

        private void ClearContainerItems()
        {
            HashSet<UIAlchemyDropContainer> visited = new HashSet<UIAlchemyDropContainer>();
            CollectContainer(_floraContainer, visited);
            CollectContainer(_essenceContainer, visited);
            CollectContainer(_oreContainer, visited);
            CollectContainer(_liquidContainer, visited);

            foreach (UIAlchemyDropContainer container in _allContainers)
            {
                CollectContainer(container, visited);
            }

            foreach (UIAlchemyDropContainer container in visited)
            {
                container?.ClearItems();
            }

            UpdateAlchemizeButtonState();
        }

        private static void CollectContainer(UIAlchemyDropContainer container, HashSet<UIAlchemyDropContainer> visited)
        {
            if (container != null)
            {
                visited.Add(container);
            }
        }

        private void UpdateContainerHighlights()
        {
            foreach (UIAlchemyDropContainer container in _allContainers)
            {
                bool highlight = _activeDragCategory != null && container.Category == _activeDragCategory.Value;
                container.SetHighlight(highlight);
            }
        }

        private void ResetDragState()
        {
            _dragSource = null;
            _activeDragCategory = null;

            SetDragVisible(false);
            UpdateContainerHighlights();
            UIInventorySlotListItem.ResetAllDragStates();
        }

        private void EnsureDragVisual()
        {
            if (_dragIcon != null)
                return;

            RectTransform parent = _dragLayer != null ? _dragLayer : SceneUI?.Canvas.transform as RectTransform;
            if (parent == null)
                return;

            GameObject dragObject = new GameObject("AlchemyDrag", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            dragObject.transform.SetParent(parent, false);

            _dragIcon = dragObject.GetComponent<RectTransform>();
            _dragCanvas = dragObject.AddComponent<Canvas>();
            _dragCanvasGroup = dragObject.GetComponent<CanvasGroup>();
            _dragImage = dragObject.GetComponent<Image>();

            Canvas parentCanvas = SceneUI?.Canvas;
            _dragCanvas.overrideSorting = true;
            _dragCanvas.sortingOrder = short.MaxValue;
            if (parentCanvas != null)
            {
                _dragCanvas.sortingLayerID = parentCanvas.sortingLayerID;
                _dragCanvas.worldCamera = parentCanvas.worldCamera;
            }

            _dragCanvasGroup.blocksRaycasts = false;
            _dragCanvasGroup.interactable = false;
            _dragImage.raycastTarget = false;
            _dragImage.preserveAspect = true;

            dragObject.SetActive(false);
        }

        private void UpdateDragIcon(Sprite sprite, int quantity, Vector2 size)
        {
            if (_dragIcon == null || _dragImage == null)
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
            Canvas canvas = SceneUI?.Canvas;
            if (referenceRect == null || canvas == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRect, eventData.position, canvas.worldCamera, out Vector2 localPoint))
            {
                _dragIcon.localPosition = localPoint;
            }
        }

        private void SetDragVisible(bool visible)
        {
            if (_dragIcon == null)
                return;

            if (visible == true)
            {
                BringDragVisualToFront();
            }

            _dragIcon.gameObject.SetActive(visible);
            if (_dragCanvasGroup != null)
            {
                _dragCanvasGroup.alpha = visible ? 1f : 0f;
            }
        }

        private void BringDragVisualToFront()
        {
            if (_dragIcon == null)
                return;

            _dragIcon.SetAsLastSibling();

            if (_dragCanvas != null)
            {
                _dragCanvas.overrideSorting = true;
                _dragCanvas.sortingOrder = short.MaxValue;
            }
        }

        private static Vector2 GetSlotSize(UIListItem slot)
        {
            if (slot != null && slot.SlotRectTransform != null)
            {
                Rect rect = slot.SlotRectTransform.rect;
                if (rect.width > 0f && rect.height > 0f)
                {
                    return rect.size;
                }
            }

            return new Vector2(64f, 64f);
        }

        private int GetRemainingQuantity(InventoryEntry entry)
        {
            int totalUsed = GetTotalUsedQuantity(entry.SlotIndex);
            return Mathf.Max(0, entry.Quantity - totalUsed);
        }

        private int GetTotalUsedQuantity(int slotIndex)
        {
            int usedQuantity = 0;

            for (int i = 0; i < _allContainers.Count; ++i)
            {
                usedQuantity += _allContainers[i].GetUsedQuantityForSlot(slotIndex);
            }

            return usedQuantity;
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
