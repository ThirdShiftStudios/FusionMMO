using System.Collections;
using System.Collections.Generic;
using TMPro;
using TPSBR.UI;
using UnityEngine;

namespace TPSBR
{
    public class UIStatTotalItem : UIWidget
    {
        [SerializeField] private TextMeshProUGUI _statCode;
        [SerializeField] private TextMeshProUGUI _statValue;

        public void SetData(string statCode, int statValue)
        {
            _statCode.SetTextSafe(statCode);
            _statValue.SetTextSafe(statValue.ToString());
        }
    }
}
