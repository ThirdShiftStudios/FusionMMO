using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIProfessionItem : UIWidget
    {
        
        [SerializeField] private TextMeshProUGUI _professionCode;
        [SerializeField] private TextMeshProUGUI _professionLevel;
        [SerializeField] private Image _professionIcon;

        private int _professtionCurrentExperience;
        private int _professtionExperienceNextLevel;
        
        public void SetData(string statCode, int level, int currentExperience, int nextLevelExpereince)
        {
            _professtionCurrentExperience = currentExperience;
            _professtionExperienceNextLevel = nextLevelExpereince;
            _professionCode.SetTextSafe(statCode);
            _professionLevel.SetTextSafe(level.ToString());
        }
    }
}
