using TPSBR;

namespace TPSBR.UI
{
    public abstract class UIGamblingView : UICloseView
    {
        public GamblingMachine Machine { get; private set; }
        public Agent Agent { get; private set; }

        internal void Configure(GamblingMachine machine, Agent agent)
        {
            Machine = machine;
            Agent = agent;
            OnConfigured(machine, agent);
        }

        internal void ClearConfiguration(GamblingMachine machine)
        {
            if (Machine != machine)
                return;

            OnCleared(machine, Agent);

            Machine = null;
            Agent = null;
        }

        protected virtual void OnConfigured(GamblingMachine machine, Agent agent)
        {
            _ = machine;
            _ = agent;
        }

        protected virtual void OnCleared(GamblingMachine machine, Agent agent)
        {
            _ = machine;
            _ = agent;
        }
    }
}
