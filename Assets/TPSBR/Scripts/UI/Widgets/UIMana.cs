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
        [SerializeField] private Image _manaProgress;
        [SerializeField] private Image _manaIcon;
        [SerializeField] private float _animationDuration = 0.2f;
        [SerializeField] private Color _depletedColor = Color.gray;

        private AgentMana _mana;
        private int _lastMana = int.MinValue;
        private int _lastTotalMana = int.MinValue;
        private float _lastProgress = -1f;

        private Color _initialTextColor = Color.white;
        private Color _initialIconColor = Color.white;

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

            if (_manaProgress != null && Mathf.Abs(progress - _lastProgress) > Mathf.Epsilon)
            {
                DOTween.Kill(_manaProgress);
                _manaProgress.DOFillAmount(progress, _animationDuration);
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
                _manaProgress.fillAmount = progress;
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

            if (_manaText != null)
            {
                _manaText.color = Color.Lerp(_depletedColor, _initialTextColor, t);
            }

            if (_manaIcon != null)
            {
                _manaIcon.color = Color.Lerp(_depletedColor, _initialIconColor, t);
            }
        }
    }
}
