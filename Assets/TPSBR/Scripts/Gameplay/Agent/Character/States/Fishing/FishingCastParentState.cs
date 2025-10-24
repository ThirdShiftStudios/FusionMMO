using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class FishingCastParentState : MixerState
    {
        [SerializeField] private FishingCastBeginState _begin;
        [SerializeField] private FishingCastThrowState _throw;

        public bool IsBeginActive => _begin != null && _begin.IsActive(true);
        public bool IsThrowActive => _throw != null && _throw.IsActive(true);
        public bool IsThrowFinished => _throw != null && _throw.IsFinished(0.95f);

        public void PlayBegin(float blendDuration)
        {
            if (_begin == null)
                return;

            if (_throw != null && _throw.IsActive(true) == true)
            {
                _throw.Deactivate(blendDuration, true);
            }

            _begin.SetAnimationTime(0f);
            _begin.Activate(blendDuration);
            Activate(blendDuration);
        }

        public void PlayThrow(float blendDuration)
        {
            if (_throw == null)
                return;

            if (_begin != null && _begin.IsActive(true) == true)
            {
                _begin.Deactivate(blendDuration, true);
            }

            _throw.SetAnimationTime(0f);
            _throw.Activate(blendDuration);
            Activate(blendDuration);
        }

        public void Stop(float blendDuration)
        {
            if (_begin != null && _begin.IsActive(true) == true)
            {
                _begin.Deactivate(blendDuration, true);
            }

            if (_throw != null && _throw.IsActive(true) == true)
            {
                _throw.Deactivate(blendDuration, true);
            }

            if (IsActive(true) == true)
            {
                Deactivate(blendDuration);
            }
        }
    }
}
