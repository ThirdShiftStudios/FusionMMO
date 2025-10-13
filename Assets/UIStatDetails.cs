using TPSBR.UI;

namespace TPSBR
{
    public class UIStatDetails : UIWidget
    {
        private UIStatTotalItem[] _statTotalItems;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _statTotalItems = GetComponentsInChildren<UIStatTotalItem>();
        }
    }
}
