using TPSBR.UI;
using UnityEngine;
using UnityEditor;
namespace TPSBR
{
    public sealed class SlotMachineNetworked : GamblingMachine
    {
        private UISlotMachineView _slotMachineView;
        [SerializeField]
        private SlotMachine _slotMachine;
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
