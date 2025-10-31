using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
	public class UIHealth : UIBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private TextMeshProUGUI _healthText;
		[SerializeField]
                private UISlider        _healthProgress;
		[SerializeField]
		private TextMeshProUGUI _maxHealthText;
		[SerializeField]
		private Image           _healthIcon;
		[SerializeField]
		private Image           _healthIcon2;
		[SerializeField]
		private float           _healthAnimationDuration = 0.2f;
		[SerializeField]
		private Color[]         _healthColors;
		[SerializeField]
		private Animation       _criticalAnimation;
		[SerializeField]
		private float           _criticalThreshold = 0.2f;

		[SerializeField]
		private Image           _shieldIcon;
		[SerializeField]
		private TextMeshProUGUI _shieldText;
		[SerializeField]
		private Color           _shieldInactiveColor = Color.gray;
		[SerializeField]
                private UISlider        _shieldProgress;
		[SerializeField]
		private TextMeshProUGUI _maxShieldText;

		private int _lastHealth = -1;
		private int _lastMaxHealth = -1;

		private int _lastShield = -1;
		private int _lastMaxShield = -1;

		private Health _health;
		private Color  _shieldColor;

		// PUBLIC METHODS

		public void UpdateHealth(Health health)
		{
			if (_health != health)
			{
				_health        = health;
				_lastHealth    = -1;
				_lastMaxHealth = -1;
				_lastShield    = -1;
				_lastMaxShield = -1;
			}

			// HEALTH

			int currentHealth = Mathf.RoundToInt(health.CurrentHealth);

			if (currentHealth == 0 && health.CurrentHealth > 0)
			{
				// Do not show zero if not necessary
				currentHealth = 1;
			}

			int maxHealth = (int)health.MaxHealth;

                        if (currentHealth != _lastHealth)
                        {
                                float progress = health.MaxHealth > 0f ? currentHealth / health.MaxHealth : 0f;

                                if (_healthProgress != null)
                                {
                                        DOTween.Kill(_healthProgress);

                                        _healthProgress.DOValue(progress, _healthAnimationDuration);
                                }

                                if (_healthText != null)
                                {
                                        _healthText.text = currentHealth.ToString();
                                }

                                UpdateHealthColor(health);
                                ShowCriticalAnimation(progress);

                                _lastHealth = currentHealth;
                        }

			if (maxHealth != _lastMaxHealth)
			{
				if (_maxHealthText != null)
				{
					_maxHealthText.text = $"{maxHealth}";
				}

				_lastMaxHealth = maxHealth;
			}

			// SHIELD

			int currentShield = (int)health.CurrentShield;
			int maxShield = (int)health.MaxShield;

                        if (currentShield != _lastShield)
                        {
                                if (_shieldProgress != null)
                                {
                                        DOTween.Kill(_shieldProgress);

                                        float progress = health.MaxShield > 0f ? currentShield / health.MaxShield : 0f;
                                        _shieldProgress.DOValue(progress, _healthAnimationDuration);
                                }

                                if (_shieldText != null)
                                {
                                        _shieldText.text = currentShield.ToString();
                                        _shieldText.color = currentShield > 0 ? _shieldColor : _shieldInactiveColor;
                                }

                                if (_shieldIcon != null)
                                {
                                        _shieldIcon.color = currentShield > 0 ? _shieldColor : _shieldInactiveColor;
                                }

                                SetSliderFillColor(_shieldProgress, currentShield > 0 ? _shieldColor : _shieldInactiveColor);

                                UpdateHealthColor(health);

                                _lastShield = currentShield;
                        }

			if (maxShield != _lastMaxShield)
			{
                                if (_maxShieldText != null)
                                {
                                        _maxShieldText.text = $"{maxShield}";
                                }

				_lastMaxShield = maxShield;
			}
		}

		// MONOBEHAVIOUR

                protected void Awake()
                {
                        _shieldColor = _shieldIcon != null ? _shieldIcon.color : Color.white;
                }

                protected void OnEnable()
                {
                        if (_criticalAnimation != null)
                        {
                                _criticalAnimation.SampleStart();
                        }
		}

		// PRIVATE MEMBERS

		private void UpdateHealthColor(Health health)
		{
			var healthColor = GetHealthColor(health.CurrentHealth / health.MaxHealth);
                        if (_healthText != null)
                        {
                                _healthText.color = healthColor;
                        }

                        if (_healthIcon != null)
                        {
                                _healthIcon.color = healthColor;
                        }

                        if (_healthIcon2 != null)
                        {
                                _healthIcon2.color = healthColor;
                        }

                        SetSliderFillColor(_healthProgress, healthColor);
                }

		private Color GetHealthColor(float healthProgress)
		{
			float preciseIndex = MathUtility.Map(0f, 1f, 0f, _healthColors.Length - 1, healthProgress);

			int fromIndex = (int)preciseIndex;
			int toIndex = Mathf.Clamp(fromIndex + 1, 0, _healthColors.Length - 1);

			return Color.Lerp(_healthColors[fromIndex], _healthColors[toIndex], preciseIndex - fromIndex);
		}

		private void ShowCriticalAnimation(float healthProgress)
		{
                        if (_criticalAnimation == null)
                                return;

                        if (healthProgress > _criticalThreshold)
                        {
                                if (_criticalAnimation.isPlaying == true)
                                {
                                        _criticalAnimation.SampleStart();
                                }

				return;
			}

			if (_criticalAnimation.isPlaying == false)
			{
                                _criticalAnimation.Play();
                        }
                }

                private void SetSliderFillColor(UISlider slider, Color color)
                {
                        if (slider == null)
                                return;

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
