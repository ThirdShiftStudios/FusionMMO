using UnityEngine;
using UnityEngine.EventSystems;

namespace TPSBR.UI
{
    public class UIItemSlot : UIWidget
    {
        private UIButton _button;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _button = GetComponent<UIButton>();
        }
    }
}
