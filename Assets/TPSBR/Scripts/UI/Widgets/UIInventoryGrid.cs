using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

                protected override void OnInitialize()
                {
                        base.OnInitialize();

                        _slots = GetComponentsInChildren<UIItemSlot>(true);

                        for (int i = 0; i < _slots.Length; i++)
                        {
                                _slots[i].InitializeSlot(this, i);
                        }
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

                private void OnItemSlotChanged(int index, InventorySlot slot)
                {
                        UpdateSlot(index, slot);
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

                        RectTransform canvasRect = SceneUI.Canvas.transform as RectTransform;
                        if (canvasRect == null)
                                return;

                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, SceneUI.Canvas.worldCamera, out Vector2 localPoint))
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
