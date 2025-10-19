using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.UI
{
    public class UICharacterDetailsView : UIWidget
    {
        [SerializeField] private UICharacterDetails characterDetails;
        [SerializeField] private UIStatDetails _statDetails;
        [SerializeField] private UIProfessionDetails _professionDetails;

        public void UpdateCharacterDetails(PlayerData playerData)
        {
            characterDetails.SetData(playerData);
        }

        public void UpdateStats(Stats stats)
        {
            //_statDetails.SetStats(stats);
        }

        public void UpdateProfessions(Professions professions)
        {
            //_professionDetails.SetProfessions(professions);
        }
    }
}
