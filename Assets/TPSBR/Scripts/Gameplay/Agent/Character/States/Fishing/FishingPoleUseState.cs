using Fusion.Addons.AnimationController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public class FishingPoleUseState : MixerState
    {
        [SerializeField] FishingCastParentState _castState;
        [SerializeField] FishingWaitingState _waiting;
        [SerializeField] FishingFightingState _fighting;
        [SerializeField] FishingCatchParentState _catch;
    }
}
