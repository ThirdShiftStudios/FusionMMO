using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIStamina : UIBehaviour
    {
        [SerializeField] private TextMeshProUGUI _staminaText;
        [SerializeField] private TextMeshProUGUI _maxStaminaText;
        [SerializeField] private Image _staminaProgress;
        [SerializeField] private Image _staminaIcon;
        [SerializeField] private float _animationDuration = 0.2f;
        [SerializeField] private Color _exhaustedColor = Color.gray;

        private AgentStamina _stamina;
        private int _lastStamina = int.MinValue;
        private int _lastTotalStamina = int.MinValue;
        private float _lastProgress = -1f;

        private Color _initialTextColor = Color.white;
        private Color _initialIconColor = Color.white;

        protected void Awake()
        {
            if (_staminaText != null)
            {
                _initialTextColor = _staminaText.color;
            }

            if (_staminaIcon != null)
            {
                _initialIconColor = _staminaIcon.color;
            }
        }

        public void UpdateStamina(AgentStamina stamina)
        {
            if (_stamina != stamina)
            {
                _stamina = stamina;
                _lastStamina = int.MinValue;
                _lastTotalStamina = int.MinValue;
                _lastProgress = -1f;
            }

            if (stamina == null)
            {
                ApplyVisuals(0, 0, 0f);
                return;
            }

            float totalValue = Mathf.Max(0f, stamina.TotalStamina);
            float currentValue = Mathf.Clamp(stamina.CurrentStamina, 0f, totalValue);

            int currentStamina = Mathf.RoundToInt(currentValue);
            int totalStamina = Mathf.RoundToInt(totalValue);
            float progress = totalValue > 0f ? Mathf.Clamp01(currentValue / totalValue) : 0f;

            if (_staminaProgress != null && Mathf.Abs(progress - _lastProgress) > Mathf.Epsilon)
            {
                DOTween.Kill(_staminaProgress);
                _staminaProgress.DOFillAmount(progress, _animationDuration);
                _lastProgress = progress;
            }

            if (_staminaText != null && currentStamina != _lastStamina)
            {
                _staminaText.text = currentStamina.ToString();
                _lastStamina = currentStamina;
            }

            if (_maxStaminaText != null && totalStamina != _lastTotalStamina)
            {
                _maxStaminaText.text = totalStamina > 0 ? totalStamina.ToString() : string.Empty;
                _lastTotalStamina = totalStamina;
            }

            UpdateColors(progress);
        }

        private void ApplyVisuals(int currentStamina, int totalStamina, float progress)
        {
            if (_staminaProgress != null)
            {
                DOTween.Kill(_staminaProgress);
                _staminaProgress.fillAmount = progress;
            }

            if (_staminaText != null)
            {
                _staminaText.text = currentStamina.ToString();
            }

            if (_maxStaminaText != null)
            {
                _maxStaminaText.text = totalStamina > 0 ? totalStamina.ToString() : string.Empty;
            }

            UpdateColors(progress);

            _lastStamina = currentStamina;
            _lastTotalStamina = totalStamina;
            _lastProgress = progress;
        }

        private void UpdateColors(float progress)
        {
            float t = Mathf.Clamp01(progress);

            if (_staminaText != null)
            {
                _staminaText.color = Color.Lerp(_exhaustedColor, _initialTextColor, t);
            }

            if (_staminaIcon != null)
            {
                _staminaIcon.color = Color.Lerp(_exhaustedColor, _initialIconColor, t);
            }
        }
    }
}
