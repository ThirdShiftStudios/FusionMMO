using System.Collections.Generic;
using TPSBR.UI;
using UnityEngine;

namespace TPSBR
{
    public class UIProfessionDetails : UIWidget
    {
        private UIProfessionItem[] _professionTotalItems;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _professionTotalItems = GetComponentsInChildren<UIProfessionItem>();

            if (_professionTotalItems != null && _professionTotalItems.Length > 0)
            {
                SetStats(null);
            }
        }

        public void SetStats(IReadOnlyList<int> statValues)
        {
            if (_professionTotalItems == null)
            {
                return;
            }

            int statCount = Mathf.Min(_professionTotalItems.Length, Stats.Count);

            for (int index = 0; index < statCount; ++index)
            {
                UIProfessionItem professionItem = _professionTotalItems[index];
                if (professionItem == null)
                {
                    continue;
                }

                string professionCode = Stats.GetCode(index);
                int statValue = 0;

                if (statValues != null && index < statValues.Count)
                {
                    statValue = statValues[index];
                }

                professionItem.SetData(professionCode, statValue, -1,-1);
            }

            for (int index = statCount; index < _professionTotalItems.Length; ++index)
            {
                UIProfessionItem statItem = _professionTotalItems[index];
                if (statItem == null)
                {
                    continue;
                }

                statItem.SetData(string.Empty, 0,-1,-1);
            }
        }
    }
}