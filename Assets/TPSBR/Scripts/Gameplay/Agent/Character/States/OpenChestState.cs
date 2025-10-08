using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class OpenChestState : MixerState
    {
        [SerializeField] private ClipState _startOpenChest;
        [SerializeField] private ClipState _endOpenChest;
    }
}