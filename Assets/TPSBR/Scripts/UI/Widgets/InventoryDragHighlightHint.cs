using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    [DisallowMultipleComponent]
    public sealed class InventoryDragHighlightHint : MonoBehaviour
    {
        [SerializeField]
        private Image _highlightImage;

        [SerializeField]
        private Color _highlightColor = Color.white;

        [SerializeField]
        private bool _disableImageWhenHidden = true;

        private Color _originalColor;
        private bool _originalColorCached;

        private void Awake()
        {
            CacheOriginalColor();
            ApplyVisibility(false);
        }

        public void ShowHighlight(Color? overrideColor = null)
        {
            CacheOriginalColor();

            if (_highlightImage != null)
            {
                _highlightImage.color = overrideColor ?? _highlightColor;
            }

            ApplyVisibility(true);
        }

        public void HideHighlight()
        {
            if (_highlightImage != null && _originalColorCached == true)
            {
                _highlightImage.color = _originalColor;
            }

            ApplyVisibility(false);
        }

        private void CacheOriginalColor()
        {
            if (_highlightImage == null || _originalColorCached == true)
                return;

            _originalColor = _highlightImage.color;
            _originalColorCached = true;
        }

        private void ApplyVisibility(bool visible)
        {
            if (_highlightImage == null)
                return;

            if (_disableImageWhenHidden == true)
            {
                _highlightImage.enabled = visible;
            }
            else
            {
                _highlightImage.color = visible ? _highlightImage.color : new Color(_highlightImage.color.r, _highlightImage.color.g, _highlightImage.color.b, 0f);
            }
        }
    }
}
