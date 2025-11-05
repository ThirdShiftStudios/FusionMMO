using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIProfessionItem : UIWidget, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
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
        private bool _hasProfession;
        private Professions.ProfessionIndex _professionIndex;
        private UIGameplayInventory _inventoryView;

        public void SetData(string professionCode, Professions.ProfessionSnapshot snapshot)
        {
            _professionCodeValue = professionCode;
            _snapshot = snapshot;

            if (Professions.TryGetIndex(professionCode, out int professionIndex) == true)
            {
                _hasProfession = true;
                _professionIndex = (Professions.ProfessionIndex)professionIndex;
            }
            else
            {
                _hasProfession = false;
                _professionIndex = default;
            }

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

                    if (_hasProfession == true)
                    {
                        ProfessionResourceDefinitions definitions = ProfessionResourceDefinitions.Instance;
                        if (definitions != null)
                        {
                            ProfessionResource resource = definitions.GetResource(_professionIndex);
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

            if (_hasProfession == false)
            {
                HideTooltip();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_hasProfession == false)
            {
                HideTooltip();
                return;
            }

            var inventoryView = GetInventoryView();
            if (inventoryView == null)
                return;

            inventoryView.ShowProfessionTooltip(_professionIndex, _professionCodeValue, _snapshot, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (_hasProfession == false)
                return;

            var inventoryView = GetInventoryView();
            inventoryView?.UpdateProfessionTooltipPosition(eventData.position);
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
            inventoryView?.HideProfessionTooltip();
        }
    }
}
