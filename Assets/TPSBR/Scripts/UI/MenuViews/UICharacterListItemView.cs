using System.Globalization;
using TMPro;
using TSS.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
        public class UICharacterListItemView : MonoBehaviour
        {
                [SerializeField]
                private TextMeshProUGUI _title;
                [SerializeField]
                private TextMeshProUGUI _subtitle;
                [SerializeField]
                private Image _icon;
                [SerializeField]
                private float _textHorizontalPadding = 80f;
                [SerializeField]
                private float _textVerticalPadding = 10f;

                private const string LevelFormat = "Level {0}";

                private void Awake()
                {
                        EnsureUIElements();
                }

                public void SetCharacter(PlayerCharacterSaveData character)
                {
                        EnsureUIElements();

                        if (character == null)
                        {
                                if (_title != null)
                                {
                                        _title.text = string.Empty;
                                }

                                if (_subtitle != null)
                                {
                                        _subtitle.text = string.Empty;
                                }

                                if (_icon != null)
                                {
                                        _icon.enabled = false;
                                }

                                return;
                        }

                        int level = character.CharacterLevel > 0 ? character.CharacterLevel : 1;
                        if (_title != null)
                        {
                                _title.text = character.CharacterName ?? string.Empty;
                        }

                        var definition = CharacterDefinition.GetByStringCode(character.CharacterDefinitionCode);
                        if (_subtitle != null)
                        {
                                _subtitle.text = string.Format(CultureInfo.InvariantCulture, LevelFormat, level);
                        }

                        if (_icon != null)
                        {
                                var sprite = definition != null ? definition.Icon : null;
                                _icon.sprite = sprite;
                                _icon.enabled = sprite != null;
                        }
                }

                public void SetDefinition(CharacterDefinition definition)
                {
                        EnsureUIElements();

                        if (_title != null)
                        {
                                _title.text = definition != null ? definition.Name : string.Empty;
                        }

                        if (_subtitle != null)
                        {
                                _subtitle.text = definition != null ? definition.StringCode : string.Empty;
                        }

                        if (_icon != null)
                        {
                                var sprite = definition != null ? definition.Icon : null;
                                _icon.sprite = sprite;
                                _icon.enabled = sprite != null;
                        }
                }

                private void EnsureUIElements()
                {
                        if (_title == null || _subtitle == null)
                        {
                                var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
                                for (int i = 0; i < texts.Length; i++)
                                {
                                        var text = texts[i];
                                        if (text == null)
                                                continue;

                                        var lowerName = text.gameObject.name.ToLowerInvariant();

                                        if (_title == null && lowerName.Contains("name"))
                                        {
                                                _title = text;
                                                continue;
                                        }

                                        if (_subtitle == null && (lowerName.Contains("level") || lowerName.Contains("subtitle")))
                                        {
                                                _subtitle = text;
                                        }
                                }
                        }

                        if (_title != null && _subtitle != null)
                                return;

                        var rootRect = transform as RectTransform;
                        if (rootRect == null)
                                return;

                        RectTransform textContainer = null;

                        if (_title != null)
                        {
                                textContainer = _title.transform.parent as RectTransform;
                        }
                        else if (_subtitle != null)
                        {
                                textContainer = _subtitle.transform.parent as RectTransform;
                        }

                        if (textContainer == null)
                        {
                                textContainer = new GameObject("CharacterText", typeof(RectTransform)).GetComponent<RectTransform>();
                                textContainer.SetParent(rootRect, false);
                                textContainer.anchorMin = Vector2.zero;
                                textContainer.anchorMax = Vector2.one;
                                textContainer.offsetMin = new Vector2(_textHorizontalPadding, _textVerticalPadding);
                                textContainer.offsetMax = new Vector2(-_textVerticalPadding, -_textVerticalPadding);
                        }

                        if (_title == null)
                        {
                                _title = CreateLabel("CharacterName", textContainer, 24f, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
                        }

                        if (_subtitle == null)
                        {
                                _subtitle = CreateLabel("CharacterLevel", textContainer, 18f, FontStyles.Normal, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
                        }

                        LayoutRebuilder.MarkLayoutForRebuild(rootRect);
                }

                private static TextMeshProUGUI CreateLabel(string name, RectTransform parent, float fontSize, FontStyles style, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
                {
                        var go = new GameObject(name, typeof(RectTransform));
                        var rectTransform = go.GetComponent<RectTransform>();
                        rectTransform.SetParent(parent, false);
                        rectTransform.anchorMin = anchorMin;
                        rectTransform.anchorMax = anchorMax;
                        rectTransform.pivot = pivot;
                        rectTransform.offsetMin = Vector2.zero;
                        rectTransform.offsetMax = Vector2.zero;

                        var text = go.AddComponent<TextMeshProUGUI>();
                        text.fontSize = fontSize;
                        text.fontStyle = style;
                        text.alignment = TextAlignmentOptions.Left;
                        text.enableWordWrapping = false;
                        text.raycastTarget = false;
                        text.text = string.Empty;
                        text.color = Color.white;

                        return text;
                }
        }
}
