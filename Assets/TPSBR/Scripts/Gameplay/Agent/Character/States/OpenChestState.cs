using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class OpenChestState : MixerState
    {
        [Header("Clips")]
        [SerializeField] private ClipState _startOpenChest;
        [SerializeField] private ClipState _endOpenChest;

        [Header("Blending")]
        [SerializeField] private float _blendInDuration  = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.1f;

        private bool  _isPlaying;
        private bool  _startCompleted;
        private bool  _endActivated;
        private bool  _openTriggered;
        private float _openTriggerNormalizedTime = 0.5f;

        public bool IsPlaying => _isPlaying;

        public bool Play(float openNormalizedTime = -1.0f)
        {
            if (_startOpenChest == null)
                return false;

            if (_isPlaying == true)
                return false;

            _isPlaying                 = true;
            _startCompleted            = false;
            _endActivated              = false;
            _openTriggered             = false;
            _openTriggerNormalizedTime = openNormalizedTime >= 0.0f ? Mathf.Clamp01(openNormalizedTime) : 0.5f;

            _startOpenChest.SetAnimationTime(0.0f);
            _startOpenChest.Activate(_blendInDuration);

            if (_endOpenChest != null)
            {
                _endOpenChest.SetAnimationTime(0.0f);
                _endOpenChest.Deactivate(0.0f, true);
            }

            Activate(_blendInDuration);

            return true;
        }

        public void Cancel()
        {
            if (_isPlaying == false)
                return;

            _isPlaying      = false;
            _startCompleted = false;
            _endActivated   = false;
            _openTriggered  = false;

            _startOpenChest?.Deactivate(_blendOutDuration, true);
            _endOpenChest?.Deactivate(_blendOutDuration, true);
            Deactivate(_blendOutDuration);
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (_isPlaying == false)
                return;

            if (_startCompleted == false)
            {
                if (_startOpenChest == null || _startOpenChest.IsFinished())
                {
                    _startCompleted = true;

                    if (_endOpenChest != null)
                    {
                        _startOpenChest?.Deactivate(_blendOutDuration, true);

                        _endActivated = true;
                        _endOpenChest.SetAnimationTime(0.0f);
                        _endOpenChest.Activate(_blendInDuration);
                    }
                    else
                    {
                        Complete();
                    }
                }
            }
            else if (_endActivated == true)
            {
                if (_endOpenChest == null || _endOpenChest.IsFinished())
                {
                    Complete();
                }
            }
            else
            {
                Complete();
            }
        }

        public bool TryConsumeOpenTrigger()
        {
            if (_isPlaying == false)
                return false;

            if (_openTriggered == true)
                return false;

            if (_startOpenChest == null)
                return false;

            if (_startOpenChest.IsFinished(_openTriggerNormalizedTime) == false)
                return false;

            _openTriggered = true;
            return true;
        }

        private void Complete()
        {
            if (_isPlaying == false)
                return;

            _isPlaying      = false;
            _startCompleted = false;
            _endActivated   = false;
            _openTriggered  = false;

            _startOpenChest?.Deactivate(_blendOutDuration, true);
            _endOpenChest?.Deactivate(_blendOutDuration, true);
            Deactivate(_blendOutDuration);
        }
    }
}
