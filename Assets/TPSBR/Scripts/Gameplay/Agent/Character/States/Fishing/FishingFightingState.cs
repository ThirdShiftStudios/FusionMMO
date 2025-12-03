using Fusion.Addons.AnimationController;
using Fusion.Addons.KCC;
using UnityEngine;

namespace TPSBR
{
    public class FishingFightingState : ClipState
    {
        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.1f;

        private FishingPoleWeapon _weapon;

        internal void SetActiveWeapon(FishingPoleWeapon weapon)
        {
            _weapon = weapon;
        }

        internal void ClearActiveWeapon(FishingPoleWeapon weapon)
        {
            if (_weapon == weapon)
            {
                _weapon = null;
            }
        }

        internal void Play(float blendDuration)
        {
            SetAnimationTime(0f);
            Activate(blendDuration > 0f ? blendDuration : _blendInDuration);
        }

        internal void Stop(float blendDuration)
        {
            if (IsActive(true) == true)
            {
                Deactivate(blendDuration > 0f ? blendDuration : _blendOutDuration, true);
            }
        }
    }
}
