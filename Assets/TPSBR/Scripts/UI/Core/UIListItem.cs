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
