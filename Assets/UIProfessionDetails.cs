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
                SetProfessions(null);
            }
        }

        public void SetProfessions(IReadOnlyList<Professions.ProfessionSnapshot> professions)
        {
            if (_professionTotalItems == null)
            {
                return;
            }

            int professionCount = Mathf.Min(_professionTotalItems.Length, Professions.Count);

            for (int index = 0; index < professionCount; ++index)
            {
                UIProfessionItem professionItem = _professionTotalItems[index];
                if (professionItem == null)
                {
                    continue;
                }

                string professionCode = Professions.GetCode(index);
                Professions.ProfessionSnapshot snapshot = Professions.ProfessionSnapshot.Empty;

                if (professions != null && index < professions.Count)
                {
                    snapshot = professions[index];
                }

                professionItem.SetData(professionCode, snapshot);
            }

            for (int index = professionCount; index < _professionTotalItems.Length; ++index)
            {
                UIProfessionItem professionItem = _professionTotalItems[index];
                if (professionItem == null)
                {
                    continue;
                }

                professionItem.SetData(string.Empty, Professions.ProfessionSnapshot.Empty);
            }
        }
    }
}