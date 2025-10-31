using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIMana : UIBehaviour
    {
        [SerializeField] private TextMeshProUGUI _manaText;
        [SerializeField] private TextMeshProUGUI _maxManaText;
        [SerializeField] private UISlider _manaProgress;
        [SerializeField] private Image _manaIcon;
        [SerializeField] private float _animationDuration = 0.2f;
        [SerializeField] private Color _depletedColor = Color.gray;

        private AgentMana _mana;
        private int _lastMana = int.MinValue;
        private int _lastTotalMana = int.MinValue;
        private float _lastProgress = -1f;

        private Color _initialTextColor = Color.white;
        private Color _initialIconColor = Color.white;
        private Color _initialSliderColor = Color.white;

        protected void Awake()
        {
            if (_manaText != null)
            {
                _initialTextColor = _manaText.color;
            }

            if (_manaIcon != null)
            {
                _initialIconColor = _manaIcon.color;
            }

            if (_manaProgress != null && _manaProgress.fillRect != null)
            {
                var image = _manaProgress.fillRect.GetComponent<Image>();
                if (image != null)
                {
                    _initialSliderColor = image.color;
                }
            }
        }

        public void UpdateMana(AgentMana mana)
        {
            if (_mana != mana)
            {
                _mana = mana;
                _lastMana = int.MinValue;
                _lastTotalMana = int.MinValue;
                _lastProgress = -1f;
            }

            if (mana == null)
            {
                ApplyVisuals(0, 0, 0f);
                return;
            }

            float totalValue = Mathf.Max(0f, mana.TotalMana);
            float currentValue = Mathf.Clamp(mana.CurrentMana, 0f, totalValue);

            int currentMana = Mathf.RoundToInt(currentValue);
            int totalMana = Mathf.RoundToInt(totalValue);
            float progress = totalValue > 0f ? Mathf.Clamp01(currentValue / totalValue) : 0f;

            if (Mathf.Abs(progress - _lastProgress) > Mathf.Epsilon)
            {
                if (_manaProgress != null)
                {
                    DOTween.Kill(_manaProgress);
                    _manaProgress.DOValue(progress, _animationDuration);
                }

                _lastProgress = progress;
            }

            if (_manaText != null && currentMana != _lastMana)
            {
                _manaText.text = currentMana.ToString();
                _lastMana = currentMana;
            }

            if (_maxManaText != null && totalMana != _lastTotalMana)
            {
                _maxManaText.text = totalMana > 0 ? totalMana.ToString() : string.Empty;
                _lastTotalMana = totalMana;
            }

            UpdateColors(progress);
        }

        private void ApplyVisuals(int currentMana, int totalMana, float progress)
        {
            if (_manaProgress != null)
            {
                DOTween.Kill(_manaProgress);
                _manaProgress.SetValue(progress);
            }

            if (_manaText != null)
            {
                _manaText.text = currentMana.ToString();
            }

            if (_maxManaText != null)
            {
                _maxManaText.text = totalMana > 0 ? totalMana.ToString() : string.Empty;
            }

            UpdateColors(progress);

            _lastMana = currentMana;
            _lastTotalMana = totalMana;
            _lastProgress = progress;
        }

        private void UpdateColors(float progress)
        {
            float t = Mathf.Clamp01(progress);
            Color textColor = Color.Lerp(_depletedColor, _initialTextColor, t);
            Color iconColor = Color.Lerp(_depletedColor, _initialIconColor, t);
            Color sliderColor = Color.Lerp(_depletedColor, _initialSliderColor, t);

            if (_manaText != null)
            {
                _manaText.color = textColor;
            }

            if (_manaIcon != null)
            {
                _manaIcon.color = iconColor;
            }

            if (_manaProgress != null)
            {
                UpdateSliderFillColor(_manaProgress, sliderColor);
            }
        }

        private void UpdateSliderFillColor(UISlider slider, Color color)
        {
            if (slider == null)
            {
                return;
            }

            if (slider.fillRect != null)
            {
                var image = slider.fillRect.GetComponent<Image>();
                if (image != null)
                {
                    image.color = color;
                }
            }
        }
    }
}
