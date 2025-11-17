using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIInventorySlotListItem : UIListItemBase<MonoBehaviour>, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
        {
                private static readonly Dictionary<int, UIInventorySlotListItem> _lastDropTargets = new Dictionary<int, UIInventorySlotListItem>();
                private static readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
                private static readonly List<UIInventorySlotListItem> _allSlots = new List<UIInventorySlotListItem>();
                private static UIInventorySlotListItem _activeDragSlot;

                // PUBLIC MEMBERS

                public int Index { get; private set; } = -1;
                public RectTransform SlotRectTransform => CachedRectTransform;
                internal Sprite IconSprite => _iconSprite;
                internal int Quantity => _quantity;
                internal bool HasItem => _iconSprite != null && _quantity > 0;
                internal IUIListItemOwner Owner => _owner;

                private UIListItem OwnerSlot => this as UIListItem;

                // PRIVATE MEMBERS

                private IUIListItemOwner _owner;
                private CanvasGroup _canvasGroup;
                private Image _iconImage;
                private TextMeshProUGUI _quantityLabel;
                [SerializeField]
                private RectTransform _tooltipHotspot;
                private Sprite _iconSprite;
                private int _quantity;
                private bool _isDragging;
                private bool _isPointerOver;
                private bool _isPointerInsideTooltipRegion;

                // MONOBEHAVIOR

                protected override void RuntimeInitialize()
                {
                        base.RuntimeInitialize();

                        if (_allSlots.Contains(this) == false)
                        {
                                _allSlots.Add(this);
                        }
                }

                protected override void OnDeinitialize()
                {
                        _owner = null;
                        Index = -1;

                        _isPointerOver = false;

                        _allSlots.Remove(this);

                        base.OnDeinitialize();
                }

                protected override void OnDestroy()
                {
                        _isPointerOver = false;
                        _allSlots.Remove(this);

                        base.OnDestroy();
                }

                // PUBLIC METHODS

                internal void InitializeSlot(IUIListItemOwner owner, int index)
                {
                        _owner = owner;
                        Index = index;
                        Clear();
                }

                internal void SetItem(Sprite icon, int quantity)
                {
                        _iconSprite = icon;
                        _quantity = quantity;

                        if (icon != null)
                        {
                                EnsureIconImage();
                                _iconImage.sprite = icon;
                                _iconImage.color = Color.white;
                                _iconImage.enabled = true;
                        }
                        else if (_iconImage != null)
                        {
                                _iconImage.sprite = null;
                                _iconImage.enabled = false;
                        }

                        if (quantity > 1)
                        {
                                EnsureQuantityLabel();
                                _quantityLabel.text = quantity.ToString();
                                _quantityLabel.gameObject.SetActive(true);
                        }
                        else if (_quantityLabel != null)
                        {
                                _quantityLabel.gameObject.SetActive(false);
                        }
                }

                internal void Clear()
                {
                        SetItem(null, 0);
                }

                internal void SetItemMetadata(Sprite icon, int quantity)
                {
                        _iconSprite = icon;
                        _quantity = quantity;
                }

                // EVENT HANDLERS

                public void OnBeginDrag(PointerEventData eventData)
                {
                        if (_owner == null)
                                return;

                        if (HasItem == false)
                                return;

                        _isDragging = true;
                        if (_isPointerOver == true)
                        {
                                _isPointerOver = false;
                                if (_isPointerInsideTooltipRegion == true)
                                {
                                        _isPointerInsideTooltipRegion = false;
                                        var hoveredSlot = OwnerSlot;
                                        if (hoveredSlot != null)
                                        {
                                                _owner.HandleSlotPointerExit(hoveredSlot);
                                        }
                                }
                        }
                        _activeDragSlot = this;
                        EnsureCanvasGroup();
                        _canvasGroup.alpha = 0.35f;
                        _canvasGroup.blocksRaycasts = false;

                        var ownerSlot = OwnerSlot;
                        if (ownerSlot == null)
                                return;

                        _owner.BeginSlotDrag(ownerSlot, eventData);
                }

                public void OnDrag(PointerEventData eventData)
                {
                        if (_owner == null || _isDragging == false)
                                return;

                        _owner.UpdateSlotDrag(eventData);
                }

                public void OnEndDrag(PointerEventData eventData)
                {
                        if (_owner == null || _isDragging == false)
                                return;

                        _isDragging = false;
                        if (_activeDragSlot == this)
                        {
                                _activeDragSlot = null;
                        }
                        EnsureCanvasGroup();
                        _canvasGroup.alpha = 1f;
                        _canvasGroup.blocksRaycasts = true;

                        var ownerSlot = OwnerSlot;
                        if (ownerSlot == null)
                                return;

                        _owner.EndSlotDrag(ownerSlot, eventData);

                        if (eventData == null)
                                return;

                        if (TryConsumeDropTarget(eventData.pointerId))
                                return;

                        var targetSlot = FindDropTarget(eventData);
                        if (targetSlot == null)
                        {
                                _owner?.HandleSlotDropOutside(ownerSlot, eventData);
                                TryRestoreHoverState(eventData);
                                return;
                        }

                        if (ReferenceEquals(targetSlot, this))
                        {
                                TryRestoreHoverState(eventData);
                                return;
                        }

                        var targetOwnerSlot = targetSlot as UIListItem;
                        if (targetOwnerSlot == null)
                        {
                                TryRestoreHoverState(eventData);
                                return;
                        }

                        var targetOwner = targetSlot.Owner;
                        targetOwner?.HandleSlotDrop(ownerSlot, targetOwnerSlot);
                        TryRestoreHoverState(eventData);
                }

                public void OnDrop(PointerEventData eventData)
                {
                        if (_owner == null)
                                return;

                        var sourceSlot = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<UIInventorySlotListItem>() : null;
                        if (sourceSlot == null)
                        {
                                sourceSlot = _activeDragSlot;
                        }
                        if (sourceSlot == null || ReferenceEquals(sourceSlot, this))
                                return;

                        CacheDropTarget(eventData.pointerId, this);
                        var sourceOwnerSlot = sourceSlot as UIListItem;
                        var targetOwnerSlot = OwnerSlot;
                        if (sourceOwnerSlot == null || targetOwnerSlot == null)
                                return;

                        _owner.HandleSlotDrop(sourceOwnerSlot, targetOwnerSlot);
                }

                public void OnPointerClick(PointerEventData eventData)
                {
                        if (_owner == null || eventData == null)
                                return;

                        if (eventData.button != PointerEventData.InputButton.Left)
                                return;

                        var ownerSlot = OwnerSlot;
                        if (ownerSlot == null)
                                return;

                        _owner.HandleSlotSelected(ownerSlot);

                        ButtonWrapper?.PlayClickSound();
                }

                public void OnPointerEnter(PointerEventData eventData)
                {
                        if (_owner == null || eventData == null)
                                return;

                        if (_isDragging == true)
                                return;

                        var ownerSlot = OwnerSlot;
                        if (ownerSlot == null)
                                return;

                        _isPointerOver = true;
                        UpdateTooltipHoverState(ownerSlot, eventData, allowMove: false);
                }

                public void OnPointerExit(PointerEventData eventData)
                {
                        if (_owner == null)
                                return;

                        var ownerSlot = OwnerSlot;
                        if (ownerSlot == null)
                                return;

                        _isPointerOver = false;
                        if (_isPointerInsideTooltipRegion == true)
                        {
                                _isPointerInsideTooltipRegion = false;
                                _owner.HandleSlotPointerExit(ownerSlot);
                        }
                }

                public void OnPointerMove(PointerEventData eventData)
                {
                        if (_owner == null || eventData == null)
                                return;

                        if (_isDragging == true)
                                return;

                        if (_isPointerOver == false)
                                return;

                        var ownerSlot = OwnerSlot;
                        if (ownerSlot == null)
                                return;

                        UpdateTooltipHoverState(ownerSlot, eventData, allowMove: true);
                }

                // PRIVATE METHODS

                private void EnsureCanvasGroup()
                {
                        if (_canvasGroup != null)
                                return;

                        _canvasGroup = GetComponent<CanvasGroup>();
                        if (_canvasGroup == null)
                        {
                                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                        }
                }

                private void EnsureIconImage()
                {
                        if (_iconImage != null)
                                return;

                        var existing = transform.Find("Icon") as RectTransform;
                        if (existing != null)
                        {
                                _iconImage = existing.GetComponent<Image>();

                                if (_iconImage == null)
                                {
                                        _iconImage = existing.gameObject.AddComponent<Image>();
                                }

                                _iconImage.preserveAspect = true;
                                _iconImage.raycastTarget = false;
                                return;
                        }

                        var iconObject = new GameObject("Icon", typeof(RectTransform));
                        iconObject.transform.SetParent(transform, false);

                        var rect = iconObject.GetComponent<RectTransform>();
                        rect.anchorMin = Vector2.zero;
                        rect.anchorMax = Vector2.one;
                        rect.offsetMin = Vector2.zero;
                        rect.offsetMax = Vector2.zero;

                        _iconImage = iconObject.AddComponent<Image>();
                        _iconImage.preserveAspect = true;
                        _iconImage.raycastTarget = false;
                }

                private void EnsureQuantityLabel()
                {
                        if (_quantityLabel != null)
                                return;

                        var labelObject = new GameObject("Quantity", typeof(RectTransform));
                        labelObject.transform.SetParent(transform, false);

                        var rect = labelObject.GetComponent<RectTransform>();
                        rect.anchorMin = Vector2.zero;
                        rect.anchorMax = Vector2.one;
                        rect.offsetMin = new Vector2(6f, 6f);
                        rect.offsetMax = new Vector2(-6f, -6f);

                        _quantityLabel = labelObject.AddComponent<TextMeshProUGUI>();
                        _quantityLabel.raycastTarget = false;
                        _quantityLabel.alignment = TextAlignmentOptions.BottomRight;
                        _quantityLabel.fontSize = 24f;

                        if (TMP_Settings.defaultFontAsset != null)
                        {
                                _quantityLabel.font = TMP_Settings.defaultFontAsset;
                        }
                }

                private static void CacheDropTarget(int pointerId, UIInventorySlotListItem slot)
                {
                        if (slot == null)
                                return;

                        _lastDropTargets[pointerId] = slot;
                }

                private static bool TryConsumeDropTarget(int pointerId)
                {
                        if (_lastDropTargets.Remove(pointerId))
                                return true;

                        if (_activeDragSlot != null)
                        {
                                _activeDragSlot = null;
                        }
                        return false;
                }

                private static UIInventorySlotListItem FindDropTarget(PointerEventData eventData)
                {
                        var raycastObject = eventData.pointerCurrentRaycast.gameObject;
                        if (raycastObject != null)
                        {
                                var directSlot = raycastObject.GetComponentInParent<UIInventorySlotListItem>();
                                if (directSlot != null)
                                        return directSlot;
                        }

                        if (EventSystem.current == null)
                                return null;

                        _raycastResults.Clear();
                        EventSystem.current.RaycastAll(eventData, _raycastResults);
                        for (int i = 0; i < _raycastResults.Count; i++)
                        {
                                var resultSlot = _raycastResults[i].gameObject.GetComponentInParent<UIInventorySlotListItem>();
                                if (resultSlot != null)
                                {
                                        _raycastResults.Clear();
                                        return resultSlot;
                                }
                        }

                        _raycastResults.Clear();

                        if (_allSlots.Count > 0)
                        {
                                var camera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
                                var screenPosition = eventData.position;

                                for (int i = 0; i < _allSlots.Count; i++)
                                {
                                        var slot = _allSlots[i];
                                        if (slot == null || slot.Owner == null)
                                                continue;

                                        if (RectTransformUtility.RectangleContainsScreenPoint(slot.SlotRectTransform, screenPosition, camera))
                                        {
                                                return slot;
                                        }
                                }
                        }

                        return null;
                }

                private void TryRestoreHoverState(PointerEventData eventData)
                {
                        if (_owner == null || eventData == null)
                                return;

                        var ownerSlot = OwnerSlot;
                        if (ownerSlot == null)
                                return;

                        var camera = eventData.enterEventCamera != null ? eventData.enterEventCamera : eventData.pressEventCamera;
                        if (RectTransformUtility.RectangleContainsScreenPoint(SlotRectTransform, eventData.position, camera) == false)
                                return;

                        _isPointerOver = true;
                        if (IsPointerInsideTooltipRegion(eventData) == true)
                        {
                                _isPointerInsideTooltipRegion = true;
                                _owner.HandleSlotPointerEnter(ownerSlot, eventData);
                        }
                        else
                        {
                                _isPointerInsideTooltipRegion = false;
                        }
                }

                private RectTransform TooltipHotspot => _tooltipHotspot != null ? _tooltipHotspot : SlotRectTransform;

                private bool IsPointerInsideTooltipRegion(PointerEventData eventData)
                {
                        var hotspot = TooltipHotspot;
                        if (hotspot == null || eventData == null)
                                return false;

                        var camera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
                        return RectTransformUtility.RectangleContainsScreenPoint(hotspot, eventData.position, camera);
                }

                private void UpdateTooltipHoverState(UIListItem ownerSlot, PointerEventData eventData, bool allowMove)
                {
                        if (ownerSlot == null || eventData == null)
                                return;

                        bool isInsideRegion = IsPointerInsideTooltipRegion(eventData);
                        if (isInsideRegion == false)
                        {
                                if (_isPointerInsideTooltipRegion == true)
                                {
                                        _isPointerInsideTooltipRegion = false;
                                        _owner?.HandleSlotPointerExit(ownerSlot);
                                }
                                return;
                        }

                        if (_isPointerInsideTooltipRegion == false)
                        {
                                _isPointerInsideTooltipRegion = true;
                                _owner?.HandleSlotPointerEnter(ownerSlot, eventData);
                                return;
                        }

                        if (allowMove == true)
                        {
                                _owner?.HandleSlotPointerMove(ownerSlot, eventData);
                        }
                }
        }
}
