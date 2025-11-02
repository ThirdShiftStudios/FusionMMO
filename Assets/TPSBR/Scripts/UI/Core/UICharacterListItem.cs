using TMPro;
using UnityEngine;

namespace TPSBR.UI
{
    public sealed class UICharacterListItem : UIListItemBase<MonoBehaviour>
        {
                [SerializeField]
                private TextMeshProUGUI _characterName;
                [SerializeField]
                private TextMeshProUGUI _characterLevel;

                public string CharacterName
                {
                        get => _characterName != null ? _characterName.text : string.Empty;
                        set
                        {
                                if (_characterName != null)
                                {
                                        _characterName.text = value;
                                }
                        }
                }

                public string CharacterLevel
                {
                        get => _characterLevel != null ? _characterLevel.text : string.Empty;
                        set
                        {
                                if (_characterLevel != null)
                                {
                                        _characterLevel.text = value;
                                }
                        }
                }
        }
}
