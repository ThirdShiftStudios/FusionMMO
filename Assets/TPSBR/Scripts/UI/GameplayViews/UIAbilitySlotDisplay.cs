using TMPro;
using UnityEngine;
using UnityEngine.UI;

using TPSBR.Abilities;

namespace TPSBR.UI
{
    public sealed class UIAbilitySlotDisplay : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;
        [SerializeField]
        private Image _castOverlay;
        [SerializeField]
        private TextMeshProUGUI _controlLabel;
        [SerializeField]
        private float _overlayRotationSpeed = 180f;

        private AbilityDefinition _ability;
        private float _castDuration;
        private float _castElapsed;
        private bool _isCasting;

        public AbilityDefinition Ability => _ability;

        public void SetControlLabel(string label)
        {
            UIExtensions.SetTextSafe(_controlLabel, label);
        }

        public void SetAbility(AbilityDefinition ability)
        {
            _ability = ability;

            if (_icon != null)
            {
                _icon.sprite = ability != null ? ability.Icon : null;
                _icon.enabled = _icon.sprite != null;
            }

            StopCast();
        }

        public void StartCast(float castTime)
        {
            if (_ability == null)
            {
                return;
            }

            _castDuration = Mathf.Max(Mathf.Epsilon, castTime);
            _castElapsed = 0f;
            _isCasting = true;

            UpdateOverlay(0f);
        }

        public void StopCast()
        {
            _isCasting = false;
            _castDuration = 0f;
            _castElapsed = 0f;
            UpdateOverlay(0f);
        }

        public void Tick(float deltaTime)
        {
            if (_castOverlay != null && _castOverlay.gameObject.activeInHierarchy == true)
            {
                _castOverlay.rectTransform.Rotate(Vector3.forward, -_overlayRotationSpeed * deltaTime);
            }

            if (_isCasting == false)
            {
                return;
            }

            _castElapsed += deltaTime;

            float progress = Mathf.Clamp01(_castElapsed / _castDuration);
            UpdateOverlay(progress);

            if (progress >= 1f)
            {
                _isCasting = false;
            }
        }

        private void UpdateOverlay(float progress)
        {
            if (_castOverlay == null)
            {
                return;
            }

            bool shouldShow = _ability != null && (_isCasting == true || progress > 0f);
            _castOverlay.enabled = shouldShow;
            _castOverlay.fillAmount = shouldShow ? progress : 0f;
        }
    }
}
