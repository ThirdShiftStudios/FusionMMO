using System;
using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
        public class OpenChestState : MixerState
        {
                [SerializeField] private ClipState _startOpenChest;
                [SerializeField] private ClipState _endOpenChest;
                [SerializeField] private float     _blendInDuration  = 0.1f;
                [SerializeField] private float     _blendOutDuration = 0.1f;
                [SerializeField, Range(0.0f, 1.0f)] private float _completionThreshold = 0.98f;

                private float _currentCompletionThreshold;

                private Action _onOpenTriggered;
                private Action _onFinished;
                private bool   _isPlaying;
                private bool   _startCompleted;
                private bool   _endActivated;

                public bool IsPlaying => _isPlaying;

                public bool Play(Action onOpenTriggered, Action onFinished, float openNormalizedTime = -1.0f)
                {
                        if (_startOpenChest == null)
                                return false;

                        if (_isPlaying == true)
                                return false;

                        _onOpenTriggered            = onOpenTriggered;
                        _onFinished                 = onFinished;
                        _isPlaying                  = true;
                        _startCompleted             = false;
                        _endActivated               = false;
                        _currentCompletionThreshold = Mathf.Clamp01(openNormalizedTime < 0.0f ? _completionThreshold : openNormalizedTime);

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

                        _startOpenChest?.Deactivate(_blendOutDuration, true);
                        _endOpenChest?.Deactivate(_blendOutDuration, true);
                        Deactivate(_blendOutDuration);

                        _onOpenTriggered = null;
                        InvokeFinished();
                }

                protected override void OnFixedUpdate()
                {
                        base.OnFixedUpdate();

                        if (_isPlaying == false)
                                return;

                        if (_startCompleted == false)
                        {
                                if (_startOpenChest == null || HasReachedStartThreshold() == true)
                                {
                                        _startCompleted = true;

                                        _onOpenTriggered?.Invoke();
                                        _onOpenTriggered = null;

                                        if (_endOpenChest != null)
                                        {
                                                _endActivated = true;
                                                _endOpenChest.SetAnimationTime(0.0f);
                                                _endOpenChest.Activate(_blendInDuration);
                                        }
                                        else
                                        {
                                                Complete();
                                        }
                                }

                                return;
                        }

                        if (_endActivated == true)
                        {
                                if (_endOpenChest == null || _endOpenChest.IsFinished(_completionThreshold) == true)
                                {
                                        Complete();
                                }

                                return;
                        }

                        Complete();
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

                private bool HasReachedStartThreshold()
                {
                        if (_startOpenChest == null)
                                return true;

                        if (_startOpenChest.AnimationTime >= _currentCompletionThreshold)
                                return true;

                        return _startOpenChest.IsFinished();
                }
        }
}
