using Fusion.Addons.KCC;
using System.Collections.Generic;

namespace TPSBR
{
    public sealed class LocationTriggerProxy : NetworkTRSPProcessor
    {
        private readonly List<LocationBehavior> _listeners = new List<LocationBehavior>();

        public void AddListener(LocationBehavior behavior)
        {
            if (behavior == null)
            {
                return;
            }

            if (_listeners.Contains(behavior) == false)
            {
                _listeners.Add(behavior);
            }
        }

        public void RemoveListener(LocationBehavior behavior)
        {
            if (behavior == null)
            {
                return;
            }

            _listeners.Remove(behavior);
        }

        public override void OnEnter(KCC kcc, KCCData data)
        {
            if (kcc == null || _listeners.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _listeners.Count; ++i)
            {
                _listeners[i]?.HandleAgentEntered(kcc);
            }
        }

        public override void OnExit(KCC kcc, KCCData data)
        {
        }
    }
}
