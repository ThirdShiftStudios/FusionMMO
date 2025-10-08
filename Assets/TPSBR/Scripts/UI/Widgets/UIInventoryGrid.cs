using System;
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

                        _slots = GetComponentsInChildren<UIItemSlot>(true);

                        for (int i = 0; i < _slots.Length; i++)
                        {
                                _slots[i].InitializeSlot(this, i);
                        }

                        UpdateSelectionHighlight();
                }

                protected override void OnDeinitialize()
                {
                        Bind(null);
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
                                for (int i = 0; i < _slots.Length; i++)
                                {
                                        UpdateSlot(i, _inventory.GetItemSlot(i));
                                }

                                _inventory.ItemSlotChanged += OnItemSlotChanged;
                        }
                        else
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
                        if (_slots == null || index < 0 || index >= _slots.Length)
                                return;

                        if (slot.IsEmpty)
                        {
                                _slots[index].Clear();
                                return;
                        }

                        var definition = slot.GetDefinition();
                        var sprite = definition != null ? definition.IconSprite : null;

                        _slots[index].SetItem(sprite, slot.Quantity);
                }

                private void UpdateSelectionHighlight()
                {
                        if (_slots == null)
                                return;

                        for (int i = 0; i < _slots.Length; i++)
                        {
                                bool isSelected = i == _selectedSlotIndex;
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
        }
}
