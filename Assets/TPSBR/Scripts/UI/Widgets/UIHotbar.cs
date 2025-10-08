using TPSBR;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIHotbar : UIWidget, IUIItemSlotOwner
    {
        [SerializeField]
        private RectTransform _dragLayer;

        private UIItemSlot[] _slots;
        private Inventory _inventory;
        private UIItemSlot _dragSource;
        private RectTransform _dragIcon;
        private Image _dragImage;
        private CanvasGroup _dragCanvasGroup;
        private Color _selectionColor = Color.white;
        private int _lastSelectedSlot = -1;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _slots = GetComponentsInChildren<UIItemSlot>(true);

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].InitializeSlot(this, i);
            }

            UpdateSelection(true);
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
                _inventory.HotbarSlotChanged -= OnHotbarSlotChanged;
            }

            _inventory = inventory;

            if (_inventory != null)
            {
                _inventory.HotbarSlotChanged += OnHotbarSlotChanged;

                for (int i = 0; i < _slots.Length; i++)
                {
                    var weapon = _inventory.GetWeapon(i + 1);
                    UpdateSlot(i, weapon);
                }
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

            UpdateSelection(true);
        }

        internal void SetSelectedColor(Color color)
        {
            if (_selectionColor == color)
                return;

            _selectionColor = color;
            UpdateSelection(true);
        }

        protected override void OnTick()
        {
            base.OnTick();

            UpdateSelection();
        }

        void IUIItemSlotOwner.BeginSlotDrag(UIItemSlot slot, PointerEventData eventData)
        {
            if (_inventory == null)
                return;

            if (slot == null || slot.HasItem == false)
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
            if (_inventory == null || target == null)
                return;

            if (source == null)
                return;

            if (ReferenceEquals(source.Owner, this) == true)
            {
                _inventory.RequestSwapHotbar(source.Index, target.Index);
            }
            else
            {
                _inventory.RequestAssignHotbar(source.Index, target.Index);
            }
        }

        void IUIItemSlotOwner.HandleSlotDropOutside(UIItemSlot slot, PointerEventData eventData)
        {
            if (_inventory == null || slot == null)
                return;

            _inventory.RequestDropHotbar(slot.Index);
        }

        private void OnHotbarSlotChanged(int index, Weapon weapon)
        {
            int slotIndex = index - 1;
            if (_slots == null)
                return;

            if (slotIndex < 0 || slotIndex >= _slots.Length)
                return;

            UpdateSlot(slotIndex, weapon);
        }

        private void UpdateSlot(int index, Weapon weapon)
        {
            if (_slots == null || index < 0 || index >= _slots.Length)
                return;

            if (weapon == null)
            {
                _slots[index].Clear();
                return;
            }

            _slots[index].SetItem(weapon.Icon, 1);
        }

        private void UpdateSelection(bool forceUpdate = false)
        {
            if (_slots == null || _slots.Length == 0)
                return;

            int selectedSlot = -1;

            if (_inventory != null)
            {
                int inventorySlot = _inventory.CurrentWeaponSlot;
                if (inventorySlot > 0)
                {
                    selectedSlot = inventorySlot - 1;
                }
            }

            if (forceUpdate == false && selectedSlot == _lastSelectedSlot)
                return;

            _lastSelectedSlot = selectedSlot;

            for (int i = 0; i < _slots.Length; i++)
            {
                bool isSelected = i == selectedSlot;
                _slots[i].SetSelected(isSelected, _selectionColor);
            }
        }

        private void EnsureDragVisual()
        {
            if (_dragIcon != null)
                return;

            var parent = _dragLayer != null ? _dragLayer : SceneUI.Canvas.transform as RectTransform;
            var dragObject = new GameObject("HotbarDrag", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
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
