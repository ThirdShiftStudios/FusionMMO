using TPSBR;

namespace TPSBR.UI
{
    public sealed class UISlotMachineView : UIGamblingView
    {
        public const string ResourcePath = "UI/GameplayViews/UISlotMachineView";

        public SlotMachineNetworked SlotMachine { get; private set; }

        protected override void OnConfigured(GamblingMachine machine, Agent agent)
        {
            base.OnConfigured(machine, agent);

            SlotMachine = machine as SlotMachineNetworked;
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
