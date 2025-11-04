using System.Collections;
using System.Collections.Generic;
using TMPro;
using TPSBR.UI;
using UnityEngine;

namespace TPSBR
{
    public class UIStatTotalItem : UIWidget, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler, UnityEngine.EventSystems.IPointerMoveHandler
    {
        [SerializeField] private TextMeshProUGUI _statCode;
        [SerializeField] private TextMeshProUGUI _statValue;

        private string _currentCode;
        private int _currentValue;
        private bool _hasData;
        private UIGameplayInventory _inventoryView;

        public void SetData(string statCode, int statValue)
        {
            _currentCode = statCode;
            _currentValue = statValue;
            _hasData = string.IsNullOrWhiteSpace(statCode) == false;

            _statCode.SetTextSafe(statCode);
            _statValue.SetTextSafe(statValue.ToString());
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _inventoryView = GetComponentInParent<UIGameplayInventory>();
        }

        void UnityEngine.EventSystems.IPointerEnterHandler.OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_hasData == false || eventData == null)
                return;

            if (_inventoryView == null)
            {
                _inventoryView = GetComponentInParent<UIGameplayInventory>();
            }

            _inventoryView?.ShowStatTooltip(_currentCode, _currentValue, eventData.position);
        }

        void UnityEngine.EventSystems.IPointerExitHandler.OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_inventoryView == null)
                return;

            _inventoryView.HideStatTooltip();
        }

        void UnityEngine.EventSystems.IPointerMoveHandler.OnPointerMove(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_hasData == false || eventData == null)
                return;

            _inventoryView?.UpdateStatTooltipPosition(eventData.position);
        }
    }
}
