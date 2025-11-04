using System.Collections.Generic;
using UnityEngine;

namespace TPSBR.UI
{
    public class UIBuffsWidget : UIWidget
    {
        [SerializeField] private UIBuffsList _buffsList;

        private readonly List<BuffData> _buffer = new List<BuffData>(BuffSystem.MaxBuffSlots);
        private BuffSystem _buffSystem;

        public void SetAgent(Agent agent)
        {
            _buffSystem = agent != null ? agent.BuffSystem : null;
            Refresh();
        }

        public void UpdateBuffs(Agent agent)
        {
            BuffSystem targetSystem = agent != null ? agent.BuffSystem : null;
            if (_buffSystem != targetSystem)
            {
                _buffSystem = targetSystem;
            }

            Refresh();
        }

        public void Clear()
        {
            _buffer.Clear();
            _buffSystem = null;
            _buffsList?.Clear();
        }

        private void Refresh()
        {
            if (_buffsList == null)
                return;

            if (_buffSystem == null)
            {
                _buffsList.Clear();
                return;
            }

            _buffSystem.GetActiveBuffs(_buffer);
            _buffsList.Display(_buffer);
        }
    }
}
