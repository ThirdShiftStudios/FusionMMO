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

                public void SetCharacter(PlayerCharacterSaveData character)
                {
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

                        if (_title != null)
                        {
                                _title.text = character.CharacterName ?? string.Empty;
                        }

                        var definition = CharacterDefinition.GetByStringCode(character.CharacterDefinitionCode);
                        if (_subtitle != null)
                        {
                                _subtitle.text = definition != null ? definition.Name : string.Empty;
                        }

                        if (_icon != null)
                        {
                                var sprite = definition != null ? definition.IconSprite : null;
                                _icon.sprite = sprite;
                                _icon.enabled = sprite != null;
                        }
                }

                public void SetDefinition(CharacterDefinition definition)
                {
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
                                var sprite = definition != null ? definition.IconSprite : null;
                                _icon.sprite = sprite;
                                _icon.enabled = sprite != null;
                        }
                }
        }
}
