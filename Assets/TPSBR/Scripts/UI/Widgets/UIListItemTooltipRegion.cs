using UnityEngine;

namespace TPSBR.UI
{
    /// <summary>
    /// Designates a RectTransform that should control when slot-based tooltips are visible.
    /// </summary>
    public sealed class UIListItemTooltipRegion : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _tooltipArea;

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            RectTransform region = _tooltipArea != null ? _tooltipArea : transform as RectTransform;
            if (region == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(region, screenPoint, eventCamera);
        }
    }
}
