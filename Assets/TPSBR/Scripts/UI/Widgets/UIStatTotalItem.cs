using TMPro;
using TPSBR.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TPSBR
{
    public class UIStatTotalItem : UIWidget, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] private TextMeshProUGUI _statCode;
        [SerializeField] private TextMeshProUGUI _statValue;

        private string _currentCode;
        private int _currentValue;
        private bool _hasValidStat;
        private Stats.StatIndex _currentStatIndex;
        private UIGameplayInventory _inventoryView;

        public void SetData(string statCode, int statValue)
        {
            _currentCode = statCode;
            _currentValue = statValue;

            if (Stats.TryGetIndex(statCode, out int statIndex) == true)
            {
                _hasValidStat = true;
                _currentStatIndex = (Stats.StatIndex)statIndex;
            }
            else
            {
                _hasValidStat = false;
                _currentStatIndex = default;
            }

            _statCode.SetTextSafe(statCode);
            _statValue.SetTextSafe(statValue.ToString());

            if (_hasValidStat == false)
            {
                HideTooltip();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_hasValidStat == false)
            {
                HideTooltip();
                return;
            }

            var inventoryView = GetInventoryView();
            if (inventoryView == null)
                return;

            inventoryView.ShowStatTooltip(_currentStatIndex, _currentCode, _currentValue, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (_hasValidStat == false)
                return;

            var inventoryView = GetInventoryView();
            inventoryView?.UpdateStatTooltipPosition(eventData.position);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            CacheInventoryView();
        }

        protected override void OnVisible()
        {
            base.OnVisible();
            CacheInventoryView();
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            HideTooltip();
        }

        private UIGameplayInventory GetInventoryView()
        {
            if (_inventoryView == null)
            {
                CacheInventoryView();
            }

            return _inventoryView;
        }

        private void CacheInventoryView()
        {
            _inventoryView = SceneUI != null ? SceneUI.Get<UIGameplayInventory>() : null;
        }

        private void HideTooltip()
        {
            var inventoryView = GetInventoryView();
            inventoryView?.HideStatTooltip();
        }
    }
}
