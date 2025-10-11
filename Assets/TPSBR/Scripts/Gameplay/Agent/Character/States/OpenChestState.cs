using System;
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

        private float  _currentCompletionThreshold;
        private Action _onOpenTriggered;
        private Action _onFinished;

        private bool _isPlaying;
        private bool _startCompleted;
        private bool _endActivated;

        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// Starts the sequence. If openNormalizedTime &lt; 0, uses _completionThreshold; else uses the provided 0..1 value.
        /// </summary>
        public bool Play(Action onOpenTriggered, Action onFinished, float openNormalizedTime = -1.0f)
        {
            if (_startOpenChest == null)
                return false;

            if (_isPlaying)
                return false;

            _onOpenTriggered            = onOpenTriggered;
            _onFinished                 = onFinished;
            _isPlaying                  = true;
            _startCompleted             = false;
            _endActivated               = false;
            _currentCompletionThreshold = 0.5f;

            // Reset clips
            _startOpenChest.SetAnimationTime(0.0f);
            _startOpenChest.Activate(_blendInDuration);

            if (_endOpenChest != null)
            {
                _endOpenChest.SetAnimationTime(0.0f);
                _endOpenChest.Deactivate(0.0f, true);
            }

            // Bring this mixer online
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

            _startOpenChest?.Deactivate(_blendOutDuration, true);
            _endOpenChest?.Deactivate(_blendOutDuration, true);
            Deactivate(_blendOutDuration);

            _onOpenTriggered = null;
            InvokeFinished();
        }

        /// <summary>
        /// FixedUpdate-driven flow, as requested.
        /// </summary>
        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (_isPlaying == false)
                return;

            // Phase 1: wait until the start clip reaches the threshold
            if (_startCompleted == false)
            {
                if (HasReachedStartThreshold())
                {
                    _startCompleted = true;

                    // Fire the gameplay trigger exactly at the threshold
                    _onOpenTriggered?.Invoke();
                    _onOpenTriggered = null;

                    if (_endOpenChest != null)
                    {
                        // Clean handoff: fade start out while bringing end in
                        _startOpenChest?.Deactivate(_blendOutDuration, true);

                        _endActivated = true;
                        _endOpenChest.SetAnimationTime(0.0f);
                        _endOpenChest.Activate(_blendInDuration);
                    }
                    else
                    {
                        
                    }
                    Complete();
                }
            }
        }

        private void Complete()
        {
            if (_isPlaying == false)
                return;

            _isPlaying      = false;
            _startCompleted = false;
            _endActivated   = false;

            _startOpenChest?.Deactivate(_blendOutDuration, true);
            _endOpenChest?.Deactivate(_blendOutDuration, true);
            Deactivate(_blendOutDuration);

            InvokeFinished();
        }

        private void InvokeFinished()
        {
            Action finished = _onFinished;
            _onFinished      = null;
            _onOpenTriggered = null;

            finished?.Invoke();
        }

        /// <summary>
        /// Strict threshold check: flips true when visible animation time crosses the threshold.
        /// No fallback to IsFinished(threshold) to avoid late triggers.
        /// </summary>
        private bool HasReachedStartThreshold()
        {
            return _startOpenChest.IsFinished(_currentCompletionThreshold);
            if (_startOpenChest == null)
                return true;

            float t = Mathf.Max(_startOpenChest.AnimationTime, _startOpenChest.InterpolatedAnimationTime);
            return t >= _currentCompletionThreshold;
        }
    }
}
