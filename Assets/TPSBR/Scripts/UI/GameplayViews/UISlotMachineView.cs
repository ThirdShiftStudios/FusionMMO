using TPSBR;

namespace TPSBR.UI
{
    public sealed class UISlotMachineView : UIGamblingView
    {
        public SlotMachine SlotMachine { get; private set; }

        protected override void OnConfigured(GamblingMachine machine, Agent agent)
        {
            base.OnConfigured(machine, agent);

            SlotMachine = machine as SlotMachine;
        }

        protected override void OnCleared(GamblingMachine machine, Agent agent)
        {
            base.OnCleared(machine, agent);

            if (SlotMachine == machine)
            {
                SlotMachine = null;
            }
        }
    }
}
