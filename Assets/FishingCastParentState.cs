using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class FishingCastParentState : MixerState
    {
        [SerializeField] FishingCastBeginState _begin;
        [SerializeField] FishingCastThrowState _throw;
    }
}
