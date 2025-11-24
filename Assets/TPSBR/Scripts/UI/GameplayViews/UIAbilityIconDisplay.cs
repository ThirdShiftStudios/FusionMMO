using TPSBR.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public sealed class UIAbilityIconDisplay : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;
        [SerializeField]
        private Image _castOverlay;
        [SerializeField]
        private TextMeshProUGUI _label;

        private float _currentRotation;

        public AbilityDefinition Ability { get; private set; }

        public void SetAbility(AbilityDefinition ability, string label)
        {
            Ability = ability;

            if (_icon != null)
            {
                _icon.sprite = ability != null ? ability.Icon : null;
                _icon.enabled = _icon.sprite != null;
            }

            if (_label != null)
            {
                UIExtensions.SetTextSafe(_label, label);
                _label.gameObject.SetActive(string.IsNullOrEmpty(label) == false);
            }

            UpdateCastProgress(0f, 0f, Color.clear, 0f);
        }

        public void Clear()
        {
            SetAbility(null, string.Empty);
        }

        public void UpdateCastProgress(float normalizedProgress, float deltaTime, Color overlayColor, float rotationSpeed)
        {
            if (_castOverlay == null)
            {
                return;
            }

            normalizedProgress = Mathf.Clamp01(normalizedProgress);

            if (Ability == null || normalizedProgress <= 0f)
            {
                _castOverlay.fillAmount = 0f;
                _castOverlay.enabled = false;
                return;
            }

            _castOverlay.enabled = true;
            _castOverlay.fillAmount = normalizedProgress;
            _castOverlay.color = overlayColor;

            if (rotationSpeed != 0f)
            {
                _currentRotation += rotationSpeed * deltaTime;
                _castOverlay.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _currentRotation);
            }
        }
    }
}
