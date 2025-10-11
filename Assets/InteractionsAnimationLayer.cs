using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class InteractionsAnimationLayer : AnimationLayer
    {
        [SerializeField]
        private OpenChestState    _openChest;

        public OpenChestState OpenChest => _openChest;
    }
}
