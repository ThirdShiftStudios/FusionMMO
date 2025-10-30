using System.Collections.Generic;
using TPSBR.UI;
using UnityEngine;

namespace TPSBR
{
    public class UIStatDetails : UIWidget
    {
        private UIStatTotalItem[] _statTotalItems;
        private RectTransform _rectTransform;
        private Vector2 _originalAnchorMin;
        private Vector2 _originalAnchorMax;
        private Vector2 _originalPivot;
        private bool _anchorStateCached;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            CacheAnchorState();

            _statTotalItems = GetComponentsInChildren<UIStatTotalItem>();

            if (_statTotalItems != null && _statTotalItems.Length > 0)
            {
                SetStats(null);
            }
        }

        protected override void OnVisible()
        {
            base.OnVisible();

            RestoreAnchorState();
        }

        protected override void OnTick()
        {
            base.OnTick();

            RestoreAnchorState();
        }

        public void SetStats(IReadOnlyList<int> statValues)
        {
            if (_statTotalItems == null)
            {
                return;
            }

            int statCount = Mathf.Min(_statTotalItems.Length, Stats.Count);

            for (int index = 0; index < statCount; ++index)
            {
                UIStatTotalItem statItem = _statTotalItems[index];
                if (statItem == null)
                {
                    continue;
                }

                string statCode = Stats.GetCode(index);
                int statValue = 0;

                if (statValues != null && index < statValues.Count)
                {
                    statValue = statValues[index];
                }

                statItem.SetData(statCode, statValue);
            }

            for (int index = statCount; index < _statTotalItems.Length; ++index)
            {
                UIStatTotalItem statItem = _statTotalItems[index];
                if (statItem == null)
                {
                    continue;
                }

                statItem.SetData(string.Empty, 0);
            }
        }

        private void CacheAnchorState()
        {
            if (_anchorStateCached == true)
            {
                return;
            }

            _rectTransform = RectTransform;

            if (_rectTransform == null)
            {
                return;
            }

            _originalAnchorMin = _rectTransform.anchorMin;
            _originalAnchorMax = _rectTransform.anchorMax;
            _originalPivot = _rectTransform.pivot;

            _anchorStateCached = true;
        }

        private void RestoreAnchorState()
        {
            if (_anchorStateCached == false)
            {
                CacheAnchorState();
            }

            if (_rectTransform == null)
            {
                return;
            }

            if (_rectTransform.anchorMin != _originalAnchorMin)
            {
                _rectTransform.anchorMin = _originalAnchorMin;
            }

            if (_rectTransform.anchorMax != _originalAnchorMax)
            {
                _rectTransform.anchorMax = _originalAnchorMax;
            }

            if (_rectTransform.pivot != _originalPivot)
            {
                _rectTransform.pivot = _originalPivot;
            }
        }
    }
}
