using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIItemSlot : UIWidget, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private static readonly Dictionary<int, UIItemSlot> _lastDropTargets = new Dictionary<int, UIItemSlot>();
        private static readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

        private IUIItemSlotOwner _owner;
        private UIButton _button;
        private CanvasGroup _canvasGroup;
        private Image _iconImage;
        private TextMeshProUGUI _quantityLabel;
        private Sprite _iconSprite;
        private int _quantity;
        private bool _isDragging;

        public int Index { get; private set; } = -1;
        public RectTransform SlotRectTransform => RectTransform;
        internal Sprite IconSprite => _iconSprite;
        internal int Quantity => _quantity;
        internal bool HasItem => _iconSprite != null && _quantity > 0;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _button = GetComponent<UIButton>();
            EnsureCanvasGroup();
        }

        protected override void OnDeinitialize()
        {
            _owner = null;
            Index = -1;
            base.OnDeinitialize();
        }

        internal void InitializeSlot(IUIItemSlotOwner owner, int index)
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

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_owner == null)
                return;

            if (HasItem == false)
                return;

            _isDragging = true;
            EnsureCanvasGroup();
            _canvasGroup.alpha = 0.35f;
            _canvasGroup.blocksRaycasts = false;

            _owner.BeginSlotDrag(this, eventData);
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
            EnsureCanvasGroup();
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            _owner.EndSlotDrag(this, eventData);

            if (eventData == null)
                return;

            if (TryConsumeDropTarget(eventData.pointerId))
                return;

            var targetSlot = FindDropTarget(eventData);
            if (targetSlot == null || targetSlot == this)
                return;

            var targetOwner = targetSlot.Owner;
            targetOwner?.HandleSlotDrop(this, targetSlot);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_owner == null)
                return;

            var sourceSlot = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<UIItemSlot>() : null;
            if (sourceSlot == null || sourceSlot == this)
                return;

            CacheDropTarget(eventData.pointerId, this);
            _owner.HandleSlotDrop(sourceSlot, this);
        }

        internal IUIItemSlotOwner Owner => _owner;

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

        private static void CacheDropTarget(int pointerId, UIItemSlot slot)
        {
            if (slot == null)
                return;

            _lastDropTargets[pointerId] = slot;
        }

        private static bool TryConsumeDropTarget(int pointerId)
        {
            if (_lastDropTargets.Remove(pointerId))
                return true;

            return false;
        }

        private static UIItemSlot FindDropTarget(PointerEventData eventData)
        {
            var raycastObject = eventData.pointerCurrentRaycast.gameObject;
            if (raycastObject != null)
            {
                var directSlot = raycastObject.GetComponentInParent<UIItemSlot>();
                if (directSlot != null)
                    return directSlot;
            }

            if (EventSystem.current == null)
                return null;

            _raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _raycastResults);
            for (int i = 0; i < _raycastResults.Count; i++)
            {
                var resultSlot = _raycastResults[i].gameObject.GetComponentInParent<UIItemSlot>();
                if (resultSlot != null)
                {
                    _raycastResults.Clear();
                    return resultSlot;
                }
            }

            _raycastResults.Clear();
            return null;
        }
    }
}
