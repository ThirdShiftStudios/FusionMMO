using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
        public class UIProfessionExperienceFeedItem : UIFeedItemBase
        {
                [SerializeField]
                private TextMeshProUGUI _amountText;
                [SerializeField]
                private TextMeshProUGUI _professionNameText;
                [SerializeField]
                private TextMeshProUGUI _levelText;
                [SerializeField]
                private Image _icon;
                [SerializeField]
                private Color _defaultColor = Color.white;
                [SerializeField]
                private Color _levelUpColor = new Color(1f, 0.8431373f, 0f);

                protected override void ApplyData(IFeedData data)
                {
                        if (data is ProfessionExperienceFeedData experienceData == false)
                                return;

                        Color targetColor = experienceData.LevelIncreased == true ? _levelUpColor : _defaultColor;

                        if (_amountText != null)
                        {
                                _amountText.text = $"+{experienceData.ExperienceAmount} XP";
                                _amountText.color = targetColor;
                        }

                        if (_professionNameText != null)
                        {
                                _professionNameText.text = experienceData.ProfessionName ?? string.Empty;
                        }

                        if (_levelText != null)
                        {
                                if (experienceData.NewLevel > 0)
                                {
                                        _levelText.gameObject.SetActive(true);
                                        _levelText.text = $"Lvl {experienceData.NewLevel}";
                                        _levelText.color = targetColor;
                                }
                                else
                                {
                                        _levelText.gameObject.SetActive(false);
                                }
                        }

                        if (_icon != null)
                        {
                                Sprite icon = null;

                                var definitions = ProfessionResourceDefinitions.Instance;
                                var resource     = definitions != null ? definitions.GetResource(experienceData.Profession) : null;

                                if (resource != null)
                                {
                                        icon = resource.Icon;
                                }

                                _icon.sprite = icon;
                                _icon.gameObject.SetActive(icon != null);
                        }
                }
        }
}
