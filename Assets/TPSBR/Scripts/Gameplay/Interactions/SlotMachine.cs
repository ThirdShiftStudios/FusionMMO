using TPSBR.UI;

namespace TPSBR
{
    public sealed class SlotMachine : GamblingMachine
    {
        private UISlotMachineView _slotMachineView;

        protected override UIGamblingView ResolveView()
        {
            if (_slotMachineView == null && Context != null && Context.UI != null)
            {
                _slotMachineView = Context.UI.Get<UISlotMachineView>();
            }

            return _slotMachineView;
        }
    }
}
