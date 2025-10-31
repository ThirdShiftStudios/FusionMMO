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
        [SerializeField] private UISlider _levelProgress;
        [SerializeField] private TextMeshProUGUI _currentExperience;
        [SerializeField] private TextMeshProUGUI _nextLevelExperience;

        private int _professionCurrentExperience;
        private int _professionExperienceNextLevel;

        public void SetData(string professionCode, Professions.ProfessionSnapshot snapshot)
        {
            _professionCurrentExperience = snapshot.Experience;
            _professionExperienceNextLevel = snapshot.ExperienceToNextLevel;

            if (_professionCode != null)
            {
                _professionCode.SetTextSafe(professionCode);
            }

            if (_professionLevel != null)
            {
                int level = snapshot.Level;
                if (string.IsNullOrEmpty(professionCode) == true || level <= 0)
                {
                    _professionLevel.SetTextSafe(string.Empty);
                }
                else
                {
                    _professionLevel.SetTextSafe(level.ToString());
                }
            }

            if (_professionIcon != null)
            {
                float progress = snapshot.Progress;
                if (string.IsNullOrEmpty(professionCode) == true)
                {
                    _professionIcon.fillAmount = 0f;
                }
                else
                {
                    _professionIcon.fillAmount = Mathf.Clamp01(progress);
                }
            }

            if (_levelProgress != null)
            {
                float progress = 0f;
                if (_professionExperienceNextLevel > 0)
                {
                    progress = Mathf.Clamp01((float)_professionCurrentExperience / _professionExperienceNextLevel);
                }

                _levelProgress.SetValue(progress);
            }

            if (_currentExperience != null)
            {
                _currentExperience.SetTextSafe(_professionCurrentExperience.ToString());
            }

            if (_nextLevelExperience != null)
            {
                _nextLevelExperience.SetTextSafe(_professionExperienceNextLevel.ToString());
            }
        }
    }
}
