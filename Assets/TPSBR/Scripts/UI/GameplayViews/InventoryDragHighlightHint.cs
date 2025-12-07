using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class InventoryDragHighlightHint : MonoBehaviour
    {
        [SerializeField]
        private Graphic _highlightGraphic;

        [SerializeField]
        private Color _highlightColor = Color.white;

        private Color _defaultColor;
        private bool _defaultColorCached;

        private void Awake()
        {
            CacheDefaultColor();
        }

        private void OnEnable()
        {
            ApplyHighlight(false, _highlightColor);
        }

        internal void ApplyHighlight(bool active)
        {
            ApplyHighlight(active, _highlightColor);
        }

        internal void ApplyHighlight(bool active, Color colorOverride)
        {
            if (_highlightGraphic == null)
                return;

            CacheDefaultColor();

            _highlightGraphic.color = active ? colorOverride : _defaultColor;
        }

        private void CacheDefaultColor()
        {
            if (_highlightGraphic == null || _defaultColorCached == true)
                return;

            _defaultColor = _highlightGraphic.color;
            _defaultColorCached = true;
        }
    }
}
