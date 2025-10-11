using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class InteractionsAnimationLayer : AnimationLayer
    {
        [SerializeField]
        private OpenChestState _openChest;

        public OpenChestState OpenChest
        {
            get
            {
                if (_openChest == null)
                {
                    FindState(out _openChest, true);
                }

                return _openChest;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_openChest == null)
            {
                FindState(out _openChest, true);
            }
        }
#endif
    }
}
