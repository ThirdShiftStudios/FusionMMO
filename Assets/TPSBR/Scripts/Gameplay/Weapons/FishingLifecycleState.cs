using System;

namespace TPSBR
{
    [Serializable]
    public enum FishingLifecycleState
    {
        Inactive,
        Ready,
        Casting,
        LureInFlight,
        Waiting,
        Fighting,
    }
}
