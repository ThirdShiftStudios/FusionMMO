using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Template.CompetitiveActionMultiplayer;

namespace TPSBR.UI
{
        public class UIInventoryGrid : UIWidget, IUIItemSlotOwner
        {
                [SerializeField]
                private RectTransform _dragLayer;

                private UIItemSlot[] _slots;
                private int[] _slotIndices;
                private Dictionary<int, UIItemSlot> _slotLookup;
                private Inventory _inventory;
                private UIItemSlot _dragSource;
                private RectTransform _dragIcon;
                private Image _dragImage;
                private CanvasGroup _dragCanvasGroup;
                [SerializeField]
                private Color _selectedSlotColor = Color.white;
                internal event Action<Weapon, NetworkString<_32>> ItemSelected;

                private int _selectedSlotIndex = -1;

                protected override void OnInitialize()
                {
                        base.OnInitialize();

                        var discoveredSlots = GetComponentsInChildren<UIItemSlot>(true);
                        var generalSlots = new List<UIItemSlot>(discoveredSlots.Length);
                        UIItemSlot pickaxeSlot = null;
                        UIItemSlot woodAxeSlot = null;

                        for (int i = 0; i < discoveredSlots.Length; i++)
                        {
                                var slot = discoveredSlots[i];
                                if (IsPickaxeUISlot(slot) == true)
                                {
                                        pickaxeSlot = slot;
                                }
                                else if (IsWoodAxeUISlot(slot) == true)
                                {
                                        woodAxeSlot = slot;
                                }
                                else
                                {
                                        generalSlots.Add(slot);
                                }
                        }

                        var orderedSlots = new List<UIItemSlot>(generalSlots.Count + (pickaxeSlot != null ? 1 : 0) + (woodAxeSlot != null ? 1 : 0));
                        var indices = new List<int>(orderedSlots.Capacity);
                        _slotLookup = new Dictionary<int, UIItemSlot>(orderedSlots.Capacity);

                        for (int i = 0; i < generalSlots.Count; i++)
                        {
                                var slot = generalSlots[i];
                                slot.InitializeSlot(this, i);
                                orderedSlots.Add(slot);
                                indices.Add(i);
                                _slotLookup[i] = slot;
                        }

                        if (pickaxeSlot != null)
                        {
                                pickaxeSlot.InitializeSlot(this, Inventory.PICKAXE_SLOT_INDEX);
                                orderedSlots.Add(pickaxeSlot);
                                indices.Add(Inventory.PICKAXE_SLOT_INDEX);
                                _slotLookup[Inventory.PICKAXE_SLOT_INDEX] = pickaxeSlot;
                        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        else
                        {
                                Debug.LogWarning($"{nameof(UIInventoryGrid)} is missing a pickaxe inventory slot.");
                        }
#endif

                        if (woodAxeSlot != null)
                        {
                                woodAxeSlot.InitializeSlot(this, Inventory.WOOD_AXE_SLOT_INDEX);
                                orderedSlots.Add(woodAxeSlot);
                                indices.Add(Inventory.WOOD_AXE_SLOT_INDEX);
                                _slotLookup[Inventory.WOOD_AXE_SLOT_INDEX] = woodAxeSlot;
                        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        else
                        {
                                Debug.LogWarning($"{nameof(UIInventoryGrid)} is missing a wood axe inventory slot.");
                        }
#endif

                        _slots = orderedSlots.ToArray();
                        _slotIndices = indices.ToArray();

                        UpdateSelectionHighlight();
                }

                protected override void OnDeinitialize()
                {
                        Bind(null);
                        _slotLookup = null;
                        _slotIndices = null;
                        _slots = null;
                        base.OnDeinitialize();
                }

                internal void Bind(Inventory inventory)
                {
                        if (_inventory == inventory)
                                return;

                        if (_inventory != null)
                        {
                                _inventory.ItemSlotChanged -= OnItemSlotChanged;
                        }

                        _inventory = inventory;

                        if (_inventory != null)
                        {
                                if (_slotIndices != null)
                                {
                                        for (int i = 0; i < _slotIndices.Length; i++)
                                        {
                                                int index = _slotIndices[i];
                                                UpdateSlot(index, _inventory.GetItemSlot(index));
                                        }
                                }

                                _inventory.ItemSlotChanged += OnItemSlotChanged;
                        }
                        else if (_slots != null)
                        {
                                for (int i = 0; i < _slots.Length; i++)
                                {
                                        _slots[i].Clear();
                                }
                        }

                        SetDragVisible(false);
                        _dragSource = null;
                        ClearSelection();
                }

                void IUIItemSlotOwner.BeginSlotDrag(UIItemSlot slot, PointerEventData eventData)
                {
                        if (slot == null || _inventory == null)
                                return;

                        _dragSource = slot;
                        EnsureDragVisual();
                        UpdateDragIcon(slot.IconSprite, slot.Quantity, slot.SlotRectTransform.rect.size);
                        SetDragVisible(true);
                        UpdateDragPosition(eventData);
                }

                void IUIItemSlotOwner.UpdateSlotDrag(PointerEventData eventData)
                {
                        if (_dragSource == null)
                                return;

                        UpdateDragPosition(eventData);
                }

                void IUIItemSlotOwner.EndSlotDrag(UIItemSlot slot, PointerEventData eventData)
                {
                        if (_dragSource != slot)
                                return;

                        _dragSource = null;
                        SetDragVisible(false);
                }

                void IUIItemSlotOwner.HandleSlotDrop(UIItemSlot source, UIItemSlot target)
                {
                        if (_inventory == null)
                                return;

                        SetDragVisible(false);
                        _dragSource = null;

                        if (source == null || target == null)
                                return;

                        if (ReferenceEquals(source.Owner, this) == true)
                        {
                                if (source.Index == target.Index)
                                        return;

                                _inventory.RequestMoveItem(source.Index, target.Index);
                                return;
                        }

                        if (source.Owner is UIHotbar)
                        {
                                if (target.Index == Inventory.PICKAXE_SLOT_INDEX || target.Index == Inventory.WOOD_AXE_SLOT_INDEX)
                                        return;

                                _inventory.RequestStoreHotbar(source.Index, target.Index);
                        }
                }

                void IUIItemSlotOwner.HandleSlotDropOutside(UIItemSlot slot, PointerEventData eventData)
                {
                        if (_inventory == null || slot == null)
                                return;

                        _inventory.RequestDropInventoryItem(slot.Index);
                }

                void IUIItemSlotOwner.HandleSlotSelected(UIItemSlot slot)
                {
                        if (_inventory == null || slot == null || slot.HasItem == false)
                        {
                                ClearSelection();
                                return;
                        }

                        if (_selectedSlotIndex == slot.Index)
                        {
                                NotifySelectionChanged();
                                return;
                        }

                        _selectedSlotIndex = slot.Index;
                        UpdateSelectionHighlight();
                        NotifySelectionChanged();
                }

                internal void SetSelectionColor(Color color)
                {
                        if (_selectedSlotColor == color)
                                return;

                        _selectedSlotColor = color;
                        UpdateSelectionHighlight();
                }

                internal void ClearSelection(bool notify = true)
                {
                        if (_selectedSlotIndex < 0)
                        {
                                if (notify == true)
                                {
                                        NotifySelectionChanged();
                                }
                                return;
                        }

                        _selectedSlotIndex = -1;
                        UpdateSelectionHighlight();

                        if (notify == true)
                        {
                                NotifySelectionChanged();
                        }
                }

                private void OnItemSlotChanged(int index, InventorySlot slot)
                {
                        UpdateSlot(index, slot);

                        if (_selectedSlotIndex == index)
                        {
                                if (slot.IsEmpty)
                                {
                                        _selectedSlotIndex = -1;
                                        UpdateSelectionHighlight();
                                        NotifySelectionChanged();
                                }
                                else
                                {
                                        NotifySelectionChanged();
                                }
                        }
                }

                private void UpdateSlot(int index, InventorySlot slot)
                {
                        if (_slotLookup == null)
                                return;

                        if (_slotLookup.TryGetValue(index, out var uiSlot) == false)
                                return;

                        if (slot.IsEmpty)
                        {
                                uiSlot.Clear();
                                return;
                        }

                        var definition = slot.GetDefinition();
                        var sprite = definition != null ? definition.IconSprite : null;

                        uiSlot.SetItem(sprite, slot.Quantity);
                }

                private void UpdateSelectionHighlight()
                {
                        if (_slots == null || _slotIndices == null)
                                return;

                        for (int i = 0; i < _slots.Length; i++)
                        {
                                bool isSelected = _slotIndices[i] == _selectedSlotIndex;
                                _slots[i].SetSelectionHighlight(isSelected, _selectedSlotColor);
                        }
                }

                private void NotifySelectionChanged()
                {
                        if (ItemSelected == null)
                                return;

                        if (_inventory == null || _selectedSlotIndex < 0)
                        {
                                ItemSelected.Invoke(null, default);
                                return;
                        }

                        var slot = _inventory.GetItemSlot(_selectedSlotIndex);

                        if (slot.IsEmpty)
                        {
                                ItemSelected.Invoke(null, default);
                                return;
                        }

                        var definition = slot.GetDefinition() as WeaponDefinition;
                        ItemSelected.Invoke(definition != null ? definition.WeaponPrefab : null, slot.ConfigurationHash);
                }

                private void EnsureDragVisual()
                {
                        if (_dragIcon != null)
                                return;

                        var parent = _dragLayer != null ? _dragLayer : SceneUI.Canvas.transform as RectTransform;
                        var dragObject = new GameObject("InventoryDrag", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                        dragObject.transform.SetParent(parent, false);

                        _dragIcon = dragObject.GetComponent<RectTransform>();
                        _dragCanvasGroup = dragObject.GetComponent<CanvasGroup>();
                        _dragImage = dragObject.GetComponent<Image>();

                        _dragCanvasGroup.blocksRaycasts = false;
                        _dragCanvasGroup.interactable = false;
                        _dragImage.raycastTarget = false;
                        _dragImage.preserveAspect = true;

                        dragObject.SetActive(false);
                }

                private void UpdateDragIcon(Sprite sprite, int quantity, Vector2 size)
                {
                        if (_dragIcon == null)
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
                        if (referenceRect == null)
                                return;

                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRect, eventData.position, SceneUI.Canvas.worldCamera, out Vector2 localPoint))
                        {
                                _dragIcon.localPosition = localPoint;
                        }
                }

                private void SetDragVisible(bool visible)
                {
                        if (_dragIcon == null)
                                return;

                        _dragIcon.gameObject.SetActive(visible);
                        if (_dragCanvasGroup != null)
                        {
                                _dragCanvasGroup.alpha = visible ? 1f : 0f;
                        }
                }

                private static bool IsPickaxeUISlot(UIItemSlot slot)
                {
                        if (slot == null)
                                return false;

                        var slotObject = slot.gameObject;
                        if (slotObject == null)
                                return false;

                        var slotName = slotObject.name;
                        if (string.IsNullOrEmpty(slotName))
                                return false;

                        return slotName.IndexOf("pickaxe", StringComparison.OrdinalIgnoreCase) >= 0;
                }

                private static bool IsWoodAxeUISlot(UIItemSlot slot)
                {
                        if (slot == null)
                                return false;

                        var slotObject = slot.gameObject;
                        if (slotObject == null)
                                return false;

                        var slotName = slotObject.name;
                        if (string.IsNullOrEmpty(slotName))
                                return false;

                        slotName = slotName.ToLowerInvariant();
                        return slotName.Contains("wood") && slotName.Contains("axe");
                }
        }
}
