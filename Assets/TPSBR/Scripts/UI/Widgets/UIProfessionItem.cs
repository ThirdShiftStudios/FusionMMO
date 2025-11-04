using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIProfessionItem : UIWidget, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler, UnityEngine.EventSystems.IPointerMoveHandler
    {

        [SerializeField] private TextMeshProUGUI _professionCode;
        [SerializeField] private TextMeshProUGUI _professionLevel;
        [SerializeField] private Image _professionIcon;
        [SerializeField] private UISlider _levelProgress;
        [SerializeField] private TextMeshProUGUI _currentExperience;

        private int _professionCurrentExperience;
        private int _professionExperienceNextLevel;
        private string _professionCodeValue;
        private Professions.ProfessionSnapshot _snapshot;
        private UIGameplayInventory _inventoryView;
        private bool _hasData;

        public void SetData(string professionCode, Professions.ProfessionSnapshot snapshot)
        {
            _professionCodeValue = professionCode;
            _snapshot = snapshot;
            _hasData = string.IsNullOrWhiteSpace(professionCode) == false;

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
                Sprite icon = null;
                float progress = 0f;

                if (string.IsNullOrEmpty(professionCode) == false)
                {
                    progress = Mathf.Clamp01(snapshot.Progress);

                    if (Professions.TryGetIndex(professionCode, out int professionIndex) == true)
                    {
                        ProfessionResourceDefinitions definitions = ProfessionResourceDefinitions.Instance;
                        if (definitions != null)
                        {
                            ProfessionResource resource = definitions.GetResource((Professions.ProfessionIndex)professionIndex);
                            icon = resource != null ? resource.Icon : null;
                        }
                    }
                }

                _professionIcon.fillAmount = progress;

                if (_professionIcon.sprite != icon)
                {
                    _professionIcon.sprite = icon;
                }

                _professionIcon.enabled = icon != null;
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
                _currentExperience.SetTextSafe($"{_professionCurrentExperience.ToString()} / {_professionExperienceNextLevel.ToString()}");
            }
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

            _inventoryView?.ShowProfessionTooltip(_professionCodeValue, _snapshot, eventData.position);
        }

        void UnityEngine.EventSystems.IPointerExitHandler.OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_inventoryView == null)
                return;

            _inventoryView.HideProfessionTooltip();
        }

        void UnityEngine.EventSystems.IPointerMoveHandler.OnPointerMove(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_hasData == false || eventData == null)
                return;

            _inventoryView?.UpdateProfessionTooltipPosition(eventData.position);
        }
    }
}
