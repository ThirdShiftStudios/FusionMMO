using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.UI
{
        public struct InventoryFeedData : IFeedData
        {
                public bool   IsAddition;
                public int    QuantityChange;
                public Sprite Icon;
                public string ItemName;
        }

        public class UIInventoryFeed : UIFeedBase
        {
                [SerializeField]
                private Sprite _fallbackIcon;
                [SerializeField]
                private Vector2 _bottomRightOffset = new Vector2(-50f, 50f);

                private Inventory _inventory;
                private readonly Dictionary<int, InventorySlot> _slotCache = new Dictionary<int, InventorySlot>();

                public void Bind(Inventory inventory)
                {
                        if (_inventory == inventory)
                                return;

                        if (_inventory != null)
                        {
                                _inventory.ItemSlotChanged -= OnItemSlotChanged;
                        }

                        _inventory = inventory;

                        _slotCache.Clear();

                        if (_inventory != null)
                        {
                                _inventory.ItemSlotChanged += OnItemSlotChanged;
                                CacheInitialSlots();
                        }

                        HideAll();
                }

                protected override void OnInitialize()
                {
                        base.OnInitialize();

                        if (RectTransform != null)
                        {
                                RectTransform.anchorMin = new Vector2(1f, 0f);
                                RectTransform.anchorMax = new Vector2(1f, 0f);
                                RectTransform.pivot     = new Vector2(1f, 0f);
                                RectTransform.anchoredPosition = _bottomRightOffset;
                        }
                }

                protected override void OnDeinitialize()
                {
                        base.OnDeinitialize();
                        Bind(null);
                }

                protected override UIFeedItemBase[] GetFeedItems()
                {
                        return GetComponentsInChildren<UIInventoryFeedItem>();
                }

                private void CacheInitialSlots()
                {
                        if (_inventory == null)
                                return;

                        int size = _inventory.InventorySize;
                        for (int i = 0; i < size; i++)
                        {
                                _slotCache[i] = _inventory.GetItemSlot(i);
                        }

                        _slotCache[Inventory.PICKAXE_SLOT_INDEX] = _inventory.GetItemSlot(Inventory.PICKAXE_SLOT_INDEX);
                        _slotCache[Inventory.WOOD_AXE_SLOT_INDEX] = _inventory.GetItemSlot(Inventory.WOOD_AXE_SLOT_INDEX);
                        _slotCache[Inventory.FISHING_POLE_SLOT_INDEX] = _inventory.GetItemSlot(Inventory.FISHING_POLE_SLOT_INDEX);
                        _slotCache[Inventory.HEAD_SLOT_INDEX] = _inventory.GetItemSlot(Inventory.HEAD_SLOT_INDEX);
                        _slotCache[Inventory.UPPER_BODY_SLOT_INDEX] = _inventory.GetItemSlot(Inventory.UPPER_BODY_SLOT_INDEX);
                        _slotCache[Inventory.LOWER_BODY_SLOT_INDEX] = _inventory.GetItemSlot(Inventory.LOWER_BODY_SLOT_INDEX);
                        _slotCache[Inventory.PIPE_SLOT_INDEX] = _inventory.GetItemSlot(Inventory.PIPE_SLOT_INDEX);
                }

                private void OnItemSlotChanged(int index, InventorySlot slot)
                {
                        if (_inventory == null)
                                return;

                        _slotCache.TryGetValue(index, out var previous);
                        _slotCache[index] = slot;

                        if (_inventory.ConsumeFeedSuppression(index) == true)
                                return;

                        HandleSlotChange(previous, slot);
                }

                private void HandleSlotChange(InventorySlot previous, InventorySlot current)
                {
                        if (previous.ItemDefinitionId == current.ItemDefinitionId && previous.Quantity == current.Quantity)
                                return;

                        if (previous.Quantity > 0 && (previous.ItemDefinitionId != current.ItemDefinitionId || current.Quantity < previous.Quantity))
                        {
                                int removalAmount = previous.ItemDefinitionId == current.ItemDefinitionId ? previous.Quantity - current.Quantity : previous.Quantity;

                                if (removalAmount > 0)
                                {
                                        ShowInventoryFeed(previous, removalAmount, false);
                                }
                        }

                        if (current.Quantity > 0 && (previous.ItemDefinitionId != current.ItemDefinitionId || current.Quantity > previous.Quantity))
                        {
                                int additionAmount = previous.ItemDefinitionId == current.ItemDefinitionId ? current.Quantity - previous.Quantity : current.Quantity;

                                if (additionAmount > 0)
                                {
                                        ShowInventoryFeed(current, additionAmount, true);
                                }
                        }
                }

                private void ShowInventoryFeed(InventorySlot slot, int amount, bool added)
                {
                        var definition = slot.GetDefinition();
                        if (definition == null)
                                return;

                        var data = new InventoryFeedData
                        {
                                IsAddition     = added,
                                QuantityChange = amount,
                                ItemName       = definition.Name,
                                Icon           = definition.Icon != null ? definition.Icon : _fallbackIcon,
                        };

                        ShowFeed(data);
                }
        }
}
