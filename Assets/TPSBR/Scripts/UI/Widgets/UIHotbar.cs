using System;
using Fusion;
using TPSBR;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIHotbar : UIWidget, IUIListItemOwner
    {
        [SerializeField]
        private RectTransform _dragLayer;

        [SerializeField]
        private UIListItem[] _slots;
        private Inventory _inventory;
        private UIListItem _dragSource;
        private RectTransform _dragIcon;
        private Image _dragImage;
        private CanvasGroup _dragCanvasGroup;
        private Color _selectionColor = Color.white;
        private int _lastSelectedSlot = -1;
        [SerializeField]
        private Color _selectedSlotColor = Color.white;
        internal event Action<IInventoryItemDetails, NetworkString<_32>> ItemSelected;

        private int _selectedSlotIndex = -1;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_slots != null && _slots.Length > 0)
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] == null)
                        continue;

                    _slots[i].InitializeSlot(this, i);
                }
            }

            UpdateSelection(true);
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
                _inventory.HotbarSlotChanged -= OnHotbarSlotChanged;
            }

            _inventory = inventory;

            if (_inventory != null)
            {
                _inventory.HotbarSlotChanged += OnHotbarSlotChanged;

                if (_slots != null && _slots.Length > 0)
                {
                    for (int i = 0; i < _slots.Length; i++)
                    {
                        var weapon = _inventory.GetWeapon(i + 1);
                        UpdateSlot(i, weapon);
                    }
                }
            }
            else if (_slots != null && _slots.Length > 0)
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] == null)
                        continue;

                    _slots[i].Clear();
                }
            }

            SetDragVisible(false);
            _dragSource = null;
            ClearSelection();

            UpdateSelection(true);
            UpdateSelectionHighlight();
        }

        internal void SetSelectedColor(Color color)
        {
            if (_selectionColor == color)
                return;

            _selectionColor = color;
            UpdateSelection(true);
        }

        internal void SetSelectionHighlightColor(Color color)
        {
            if (_selectedSlotColor == color)
                return;

            _selectedSlotColor = color;
            UpdateSelectionHighlight();
        }

        protected override void OnTick()
        {
            base.OnTick();

            UpdateSelection();
        }

        void IUIListItemOwner.BeginSlotDrag(UIListItem slot, PointerEventData eventData)
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

        void IUIListItemOwner.UpdateSlotDrag(PointerEventData eventData)
        {
            if (_dragSource == null)
                return;

            UpdateDragPosition(eventData);
        }

        void IUIListItemOwner.EndSlotDrag(UIListItem slot, PointerEventData eventData)
        {
            if (_dragSource != slot)
                return;

            _dragSource = null;
            SetDragVisible(false);
        }

        void IUIListItemOwner.HandleSlotDrop(UIListItem source, UIListItem target)
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

        void IUIListItemOwner.HandleSlotDropOutside(UIListItem slot, PointerEventData eventData)
        {
            if (_inventory == null || slot == null)
                return;

            _inventory.RequestDropHotbar(slot.Index);
        }

        void IUIListItemOwner.HandleSlotSelected(UIListItem slot)
        {
            if (_inventory == null || slot == null)
            {
                ClearSelection();
                return;
            }

            var weapon = _inventory.GetWeapon(slot.Index + 1);

            if (weapon == null)
            {
                ClearSelection();
                return;
            }

            if (_selectedSlotIndex == slot.Index)
            {
                NotifySelectionChanged(weapon);
                return;
            }

            _selectedSlotIndex = slot.Index;
            UpdateSelectionHighlight();
            NotifySelectionChanged(weapon);
        }

        internal void ClearSelection(bool notify = true)
        {
            if (_selectedSlotIndex < 0)
            {
                if (notify == true)
                {
                    NotifySelectionChanged(null);
                }
                return;
            }

            _selectedSlotIndex = -1;
            UpdateSelectionHighlight();

            if (notify == true)
            {
                NotifySelectionChanged(null);
            }
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

            var slot = _slots[index];
            if (slot == null)
                return;

            if (weapon == null)
            {
                slot.Clear();
                if (_selectedSlotIndex == index)
                {
                    _selectedSlotIndex = -1;
                    UpdateSelectionHighlight();
                    NotifySelectionChanged(null);
                }
                return;
            }

            slot.SetItem(weapon.Icon, 1);

            if (_selectedSlotIndex == index)
            {
                NotifySelectionChanged(weapon);
            }
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
                var slot = _slots[i];
                if (slot == null)
                    continue;

                slot.SetSelected(isSelected, _selectionColor);
            }

            UpdateSelectionHighlight();
        }

        private void UpdateSelectionHighlight()
        {
            if (_slots == null)
                return;

            for (int i = 0; i < _slots.Length; i++)
            {
                bool isSelected = i == _selectedSlotIndex;
                var slot = _slots[i];
                if (slot == null)
                    continue;

                slot.SetSelectionHighlight(isSelected, _selectedSlotColor);
            }
        }

        private void NotifySelectionChanged(Weapon weapon)
        {
            NetworkString<_32> configurationHash = default;

            if (weapon != null)
            {
                configurationHash = weapon.ConfigurationHash;
            }

            ItemSelected?.Invoke(weapon, configurationHash);
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
