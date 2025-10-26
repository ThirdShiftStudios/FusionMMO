using Fusion.Addons.AnimationController;
using System;
using UnityEngine;
namespace TPSBR
{
    public class FishingCatchParentState : MixerState
    {
        [SerializeField] FishingCatchPullOutBegin _begin;
        [SerializeField] FishingCatchPullOutLoop _loop;
        [SerializeField] FishingCatchPullOutEnd _end;

        public bool IsBeginActive => _begin != null && _begin.IsActive(true);
        public bool IsLoopActive => _loop != null && _loop.IsActive(true);
        public bool IsEndActive => _end != null && _end.IsActive(true);
        public bool HasCompletedSequence => _hasCompletedSequence;

        private FishingPoleWeapon _activeWeapon;
        private float _blendDuration = 0.1f;
        private bool _hasCompletedSequence;

        private void Awake()
        {
            if (_begin != null)
            {
                _begin.ClipFinished += OnBeginClipFinished;
            }

            if (_end != null)
            {
                _end.ClipFinished += OnEndClipFinished;
            }
        }

        private void OnDestroy()
        {
            if (_begin != null)
            {
                _begin.ClipFinished -= OnBeginClipFinished;
            }

            if (_end != null)
            {
                _end.ClipFinished -= OnEndClipFinished;
            }
        }

        internal void SetActiveWeapon(FishingPoleWeapon weapon)
        {
            _activeWeapon = weapon;
            _begin?.SetActiveWeapon(weapon);
            _loop?.SetActiveWeapon(weapon);
            _end?.SetActiveWeapon(weapon);
        }

        internal void ClearActiveWeapon(FishingPoleWeapon weapon)
        {
            if (_activeWeapon == weapon)
            {
                _activeWeapon = null;
            }

            _begin?.ClearActiveWeapon(weapon);
            _loop?.ClearActiveWeapon(weapon);
            _end?.ClearActiveWeapon(weapon);
        }

        internal void StartCatch(float blendDuration)
        {
            _blendDuration = blendDuration > 0f ? blendDuration : 0.1f;
            _hasCompletedSequence = false;

            if (_loop != null && _loop.IsActive(true) == true)
            {
                _loop.Deactivate(_blendDuration, true);
            }

            if (_end != null && _end.IsActive(true) == true)
            {
                _end.Deactivate(_blendDuration, true);
            }

            if (_begin != null)
            {
                _begin.SetAnimationTime(0f);
                _begin.Activate(_blendDuration);
            }
            else
            {
                ActivateLoop();
            }

            Activate(_blendDuration);
        }

        internal void PlayEnd(float blendDuration)
        {
            float transitionBlend = blendDuration > 0f ? blendDuration : _blendDuration;
            _hasCompletedSequence = false;

            if (_end != null && _end.IsActive(true) == true)
            {
                return;
            }

            if (_loop != null && _loop.IsActive(true) == true)
            {
                _loop.Deactivate(transitionBlend, true);
            }

            if (_end != null)
            {
                _end.SetAnimationTime(0f);
                _end.Activate(transitionBlend);
            }
            else
            {
                _hasCompletedSequence = true;
            }

            Activate(transitionBlend);
        }

        internal void Stop(float blendDuration)
        {
            if (_begin != null && _begin.IsActive(true) == true)
            {
                _begin.Deactivate(blendDuration, true);
            }

            if (_loop != null && _loop.IsActive(true) == true)
            {
                _loop.Deactivate(blendDuration, true);
            }

            if (_end != null && _end.IsActive(true) == true)
            {
                _end.Deactivate(blendDuration, true);
            }

            _hasCompletedSequence = false;

            if (IsActive(true) == true)
            {
                Deactivate(blendDuration);
            }
        }

        private void OnBeginClipFinished()
        {
            ActivateLoop();
        }

        private void OnEndClipFinished()
        {
            _hasCompletedSequence = true;
        }

        private void ActivateLoop()
        {
            if (_loop == null)
            {
                return;
            }

            _loop.SetAnimationTime(0f);
            _loop.Activate(_blendDuration);

            if (_begin != null && _begin.IsActive(true) == true)
            {
                _begin.Deactivate(_blendDuration, true);
            }
        }
    }
}
