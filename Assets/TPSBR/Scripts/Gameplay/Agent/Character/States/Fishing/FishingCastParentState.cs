using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class FishingCastParentState : MixerState
    {
        [SerializeField] private FishingCastBeginState _begin;
        [SerializeField] private FishingCastThrowState _throw;

        [Header("Blending")]
        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.1f;

        private FishingPoleUseState _owner;
        private FishingPoleWeapon _weapon;
        private bool _isThrowing;

        public bool Play(FishingPoleUseState owner, FishingPoleWeapon weapon)
        {
            if (owner == null || weapon == null)
                return false;

            if (_begin == null)
                return false;

            _owner = owner;
            _weapon = weapon;
            _isThrowing = false;

            _begin.SetAnimationTime(0.0f);
            _begin.Activate(_blendInDuration);

            if (_throw != null)
            {
                _throw.Deactivate(0.0f, true);
            }

            Activate(_blendInDuration);

            return true;
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (_weapon == null || _owner == null)
                return;

            if (_isThrowing == false)
            {
                if (_weapon.TryConsumeCancelRequest() == true)
                {
                    CancelCast();
                    return;
                }

                if (_weapon.TryConsumePrimaryRelease() == true)
                {
                    StartThrow();
                    return;
                }

                if (_begin != null && _begin.IsFinished() == true)
                {
                    _begin.SetAnimationTime(0.0f);
                }
            }
            else
            {
                if (_weapon.TryConsumeCancelRequest() == true)
                {
                    CancelCast();
                    return;
                }

                if (_throw == null || _throw.IsFinished() == true)
                {
                    CompleteCast();
                }
            }
        }

        private void StartThrow()
        {
            _isThrowing = true;

            _begin?.Deactivate(_blendOutDuration, true);

            if (_throw != null)
            {
                _throw.SetAnimationTime(0.0f);
                _throw.Activate(_blendInDuration);
            }
            else
            {
                CompleteCast();
            }
        }

        private void CancelCast()
        {
            _begin?.Deactivate(_blendOutDuration, true);
            _throw?.Deactivate(_blendOutDuration, true);
            FinishCast();
        }

        private void CompleteCast()
        {
            _throw?.Deactivate(_blendOutDuration, true);
            FinishCast();
        }

        private void FinishCast()
        {
            if (IsActive(true) == true)
            {
                Deactivate(_blendOutDuration);
            }

            _weapon?.NotifyCastFinished();
            _owner?.OnCastFinished(_weapon);

            _weapon = null;
            _owner = null;
            _isThrowing = false;
        }
    }
}
