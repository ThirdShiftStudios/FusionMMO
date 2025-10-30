using System.Collections.Generic;
using TPSBR.UI;
using UnityEngine;

namespace TPSBR
{
    public class UIStatDetails : UIWidget
    {
        private UIStatTotalItem[] _statTotalItems;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _statTotalItems = GetComponentsInChildren<UIStatTotalItem>();

            if (_statTotalItems != null && _statTotalItems.Length > 0)
            {
                SetStats(null);
            }
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
    }
}
