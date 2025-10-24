using Fusion.Addons.AnimationController;
using UnityEngine;
namespace TPSBR
{
    public class FishingCatchParentState : MixerState
    {
        [SerializeField] FishingCatchPullOutBegin _begin;
        [SerializeField] FishingCatchPullOutLoop _loop;
        [SerializeField] FishingCatchPullOutEnd _end;
    }
}
