using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TPSBR.UI
{
        public sealed class UIListItem : UIInventorySlotListItem
        {
        }

        public sealed class UICharacterListItem : UIListItemBase<MonoBehaviour>
        {
                [SerializeField]
                private TextMeshProUGUI _characterName;
                [SerializeField]
                private TextMeshProUGUI _characterLevel;

                public string CharacterName
                {
                        get => _characterName != null ? _characterName.text : string.Empty;
                        set
                        {
                                if (_characterName != null)
                                {
                                        _characterName.text = value;
                                }
                        }
                }

                public string CharacterLevel
                {
                        get => _characterLevel != null ? _characterLevel.text : string.Empty;
                        set
                        {
                                if (_characterLevel != null)
                                {
                                        _characterLevel.text = value;
                                }
                        }
                }
        }

        public class UIInventorySlotListItem : UIListItemBase<MonoBehaviour>, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
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
                private Sprite _iconSprite;
                private int _quantity;
                private bool _isDragging;

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

                        _allSlots.Remove(this);

                        base.OnDeinitialize();
                }

                protected override void OnDestroy()
                {
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

                // EVENT HANDLERS

                public void OnBeginDrag(PointerEventData eventData)
                {
                        if (_owner == null)
                                return;

                        if (HasItem == false)
                                return;

                        _isDragging = true;
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
                                return;
                        }

                        if (ReferenceEquals(targetSlot, this))
                                return;

                        var targetOwnerSlot = targetSlot as UIListItem;
                        if (targetOwnerSlot == null)
                                return;

                        var targetOwner = targetSlot.Owner;
                        targetOwner?.HandleSlotDrop(ownerSlot, targetOwnerSlot);
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
        }

        public abstract class UIListItemBase<T> : UIWidget where T : MonoBehaviour
        {
                // PUBLIC MEMBERS

                public int  ID { get; set; }
                public T    Content => _content;
                public bool IsSelectable => _button != null;
                public bool IsSelected { get { return _isSelected; } set { SetIsSelected(value); } }
                public bool IsInteractable
                {
                        get => _button != null && _button.interactable;
                        set
                        {
                                if (_button != null)
                                {
                                        _button.interactable = value;
                                }
                        }
                }

                public Action<int> Clicked;

                protected RectTransform CachedRectTransform => _rectTransform != null ? _rectTransform : (_rectTransform = transform as RectTransform);
                protected UIButton ButtonWrapper => _buttonWrapper;
                protected Image BackgroundImage => _backgroundImage;
                protected Image SelectionBorderImage => _selectionBorderImage;

                // PRIVATE MEMBERS

                [SerializeField]
                private Button _button;
                [SerializeField]
                private Animator _animator;
                [SerializeField]
                private T _content;
                [SerializeField]
                private string _selectedAnimatorParameter = "IsSelected";
                [SerializeField]
                private CanvasGroup _selectedGroup;
                [SerializeField]
                private CanvasGroup _deselectedGroup;
                [SerializeField]
                private Image _backgroundImage;
                [SerializeField]
                private Image _selectionBorderImage;

                private UIButton _buttonWrapper;
                private bool _isSelected;
                private Color _defaultBackgroundColor;
                private bool _defaultBackgroundColorCached;
                private Color _defaultSelectionBorderColor;
                private bool _defaultSelectionBorderColorCached;
                private RectTransform _rectTransform;

                // MONOBEHAVIOR

                protected override void OnInitialize()
                {
                        base.OnInitialize();
                        RuntimeInitialize();
                }

                private void Awake()
                {
                        RuntimeInitialize();
                }

                protected override void OnDeinitialize()
                {
                        Clicked = null;

                        if (_button != null)
                        {
                                _button.onClick.RemoveListener(OnClick);
                        }

                        base.OnDeinitialize();
                }

                // INTERNAL METHODS

                internal void SetSelected(bool selected, Color selectedColor)
                {
                        if (_backgroundImage == null)
                                return;

                        CacheDefaultBackgroundColor();

                        _backgroundImage.color = selected ? selectedColor : _defaultBackgroundColor;
                }

                internal void SetSelectionHighlight(bool selected, Color selectedColor)
                {
                        if (_selectionBorderImage == null)
                                return;

                        CacheDefaultSelectionBorderColor();

                        _selectionBorderImage.color = selected ? selectedColor : _defaultSelectionBorderColor;
                }

                // PROTECTED METHODS

                protected virtual void RuntimeInitialize()
                {
                        SetIsSelected(false, true);

                        if (_button != null)
                        {
                                _button.onClick.RemoveListener(OnClick);
                                _button.onClick.AddListener(OnClick);
                        }

                        if (_buttonWrapper == null)
                        {
                                _buttonWrapper = GetComponent<UIButton>();
                        }

                        if (_button != null && _button.transition == Selectable.Transition.Animation)
                        {
                                _animator = _button.animator;
                        }

                        CacheDefaultBackgroundColor();
                        CacheDefaultSelectionBorderColor();
                }

                protected void SetIsSelected(bool value, bool force = false)
                {
                        if (_isSelected == value && force == false)
                                return;

                        _isSelected = value;

                        _selectedGroup?.SetVisibility(value);
                        _deselectedGroup?.SetVisibility(value == false);

                        UpdateAnimator();
                }

                protected void CacheDefaultBackgroundColor()
                {
                        if (_backgroundImage == null || _defaultBackgroundColorCached == true)
                                return;

                        _defaultBackgroundColor = _backgroundImage.color;
                        _defaultBackgroundColorCached = true;
                }

                protected void CacheDefaultSelectionBorderColor()
                {
                        if (_selectionBorderImage == null || _defaultSelectionBorderColorCached == true)
                                return;

                        _defaultSelectionBorderColor = _selectionBorderImage.color;
                        _defaultSelectionBorderColorCached = true;
                }

                // PRIVATE METHODS

                private void OnClick()
                {
                        Clicked?.Invoke(ID);
                }

                private void UpdateAnimator()
                {
                        if (_animator == null)
                                return;

                        if (_selectedAnimatorParameter.HasValue() == false)
                                return;

                        _animator.SetBool(_selectedAnimatorParameter, _isSelected);
                }
        }
}
