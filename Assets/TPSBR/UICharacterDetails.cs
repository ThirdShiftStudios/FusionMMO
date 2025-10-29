using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TPSBR
{
    public class UICharacterDetails : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _characterName;
        [SerializeField] private TextMeshProUGUI _characterClass;
        [SerializeField] private TextMeshProUGUI _characterLevel;

        public void SetData(PlayerData playerData)
        {
            _characterName.SetTextSafe(playerData.CharacterName);
            _characterClass.SetTextSafe(playerData.GetCharacterClassName());
            _characterLevel.SetTextSafe(playerData.Level.ToString());
        }
    }
}
