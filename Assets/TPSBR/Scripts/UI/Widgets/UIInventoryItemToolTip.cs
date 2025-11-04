using Fusion;
using TMPro;
using TSS.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIInventoryItemToolTip : UIWidget
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private float _screenPadding = 16f;
        [SerializeField] private Vector2 _cursorOffset = new Vector2(24f, 24f);

        private RectTransform _rectTransform;
        private bool _isVisible;
        private Color _defaultTitleColor;
        private bool _defaultTitleColorSet;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _rectTransform = transform as RectTransform;
            if (_rectTransform != null)
            {
                _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            CacheDefaultTitleColor();
            SetVisible(false, true);
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            SetVisible(false, true);
        }

        public void Show(IInventoryItemDetails details, ItemDefinition definition, NetworkString<_32> configurationHash, Vector2 screenPosition)
        {
            if (details == null)
            {
                Hide();
                return;
            }

            string displayName = details.GetDisplayName(configurationHash);
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = details.DisplayName;
            }

            string description = details.GetDescription(configurationHash);
            if (string.IsNullOrEmpty(description))
            {
                description = details.GetDescription();
            }

            ApplyTitleColor(definition ?? ResolveDefinition(details));
            ShowInternal(displayName, description, screenPosition);
        }

        public void Show(IInventoryItemDetails details, NetworkString<_32> configurationHash, Vector2 screenPosition)
        {
            Show(details, ResolveDefinition(details), configurationHash, screenPosition);
        }

        public void Show(ItemDefinition definition, string title, string description, Vector2 screenPosition)
        {
            ApplyTitleColor(definition);
            ShowInternal(title, description, screenPosition);
        }

        public void Show(string title, string description, Vector2 screenPosition)
        {
            ApplyDefaultTitleColor();
            ShowInternal(title, description, screenPosition);
        }

        public void UpdatePosition(Vector2 screenPosition)
        {
            if (_rectTransform == null || SceneUI?.Canvas == null)
                return;

            var canvas = SceneUI.Canvas;
            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint) == false)
                return;

            Vector2 size = _rectTransform.rect.size;
            Rect pixelRect = canvas.pixelRect;
            float padding = Mathf.Max(0f, _screenPadding);

            int horizontalDirection = 1;
            if (screenPosition.x + size.x + padding > pixelRect.xMax && screenPosition.x - size.x - padding >= pixelRect.xMin)
            {
                horizontalDirection = -1;
            }
            else if (screenPosition.x - size.x - padding < pixelRect.xMin && screenPosition.x + size.x + padding > pixelRect.xMax)
            {
                horizontalDirection = screenPosition.x <= pixelRect.center.x ? 1 : -1;
            }
            else if (screenPosition.x - size.x - padding < pixelRect.xMin)
            {
                horizontalDirection = 1;
            }

            int verticalDirection = 1;
            if (screenPosition.y + size.y + padding > pixelRect.yMax && screenPosition.y - size.y - padding >= pixelRect.yMin)
            {
                verticalDirection = -1;
            }
            else if (screenPosition.y - size.y - padding < pixelRect.yMin && screenPosition.y + size.y + padding > pixelRect.yMax)
            {
                verticalDirection = screenPosition.y <= pixelRect.center.y ? 1 : -1;
            }
            else if (screenPosition.y - size.y - padding < pixelRect.yMin)
            {
                verticalDirection = 1;
            }

            Vector2 pivot = new Vector2(horizontalDirection >= 0 ? 0f : 1f, verticalDirection >= 0 ? 0f : 1f);
            _rectTransform.pivot = pivot;

            float offsetX = Mathf.Abs(_cursorOffset.x);
            float offsetY = Mathf.Abs(_cursorOffset.y);

            Vector2 anchoredPosition = localPoint;
            anchoredPosition.x += horizontalDirection >= 0 ? offsetX : -offsetX;
            anchoredPosition.y += verticalDirection >= 0 ? offsetY : -offsetY;

            Rect rect = canvasRect.rect;

            float minX = rect.xMin + padding + size.x * pivot.x;
            float maxX = rect.xMax - padding - size.x * (1f - pivot.x);
            float minY = rect.yMin + padding + size.y * pivot.y;
            float maxY = rect.yMax - padding - size.y * (1f - pivot.y);

            if (minX > maxX)
            {
                float centerX = (minX + maxX) * 0.5f;
                minX = maxX = centerX;
            }

            if (minY > maxY)
            {
                float centerY = (minY + maxY) * 0.5f;
                minY = maxY = centerY;
            }

            anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
            anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, minY, maxY);

            _rectTransform.anchoredPosition = anchoredPosition;
        }

        public void Hide()
        {
            ApplyDefaultTitleColor();
            SetVisible(false);
        }

        private void SetVisible(bool visible, bool immediate = false)
        {
            if (_canvasGroup == null)
                return;

            if (_isVisible == visible && immediate == false)
                return;

            _isVisible = visible;
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void ShowInternal(string title, string description, Vector2 screenPosition)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(description))
            {
                Hide();
                return;
            }

            if (_titleLabel != null)
            {
                _titleLabel.text = title ?? string.Empty;
            }

            if (_descriptionLabel != null)
            {
                _descriptionLabel.text = description ?? string.Empty;
                _descriptionLabel.gameObject.SetActive(string.IsNullOrEmpty(_descriptionLabel.text) == false);
            }

            if (_rectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
            }

            UpdatePosition(screenPosition);
            SetVisible(true);
        }

        private void CacheDefaultTitleColor()
        {
            if (_defaultTitleColorSet == true || _titleLabel == null)
            {
                return;
            }

            _defaultTitleColor = _titleLabel.color;
            _defaultTitleColorSet = true;
        }

        private void ApplyDefaultTitleColor()
        {
            CacheDefaultTitleColor();

            if (_titleLabel != null && _defaultTitleColorSet == true)
            {
                _titleLabel.color = _defaultTitleColor;
            }
        }

        private void ApplyTitleColor(ItemDefinition definition)
        {
            if (definition == null)
            {
                ApplyDefaultTitleColor();
                return;
            }

            var rarityResources = ItemRarityResourcesDefinition.Instance;
            if (rarityResources != null && rarityResources.TryGetPrimaryColor(definition.ItemRarity, out Color color))
            {
                if (_titleLabel != null)
                {
                    _titleLabel.color = color;
                }
            }
            else
            {
                ApplyDefaultTitleColor();
            }
        }

        private ItemDefinition ResolveDefinition(IInventoryItemDetails details)
        {
            switch (details)
            {
                case Weapon weapon when weapon.Definition != null:
                    return weapon.Definition;
                case Pickaxe pickaxe when pickaxe.Definition != null:
                    return pickaxe.Definition;
                case WoodAxe woodAxe when woodAxe.Definition != null:
                    return woodAxe.Definition;
                case ItemDefinition itemDefinition:
                    return itemDefinition;
                default:
                    return null;
            }
        }
    }
}
