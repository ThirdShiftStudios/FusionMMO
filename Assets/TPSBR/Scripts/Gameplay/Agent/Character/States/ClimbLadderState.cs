using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public sealed class ClimbLadderState : MixerState
    {
        [SerializeField]
        private ClipState _start;

        [SerializeField]
        private ClipState _climb;

        [SerializeField]
        private ClipState _idle;

        [SerializeField]
        private ClipState _exit;

        [SerializeField]
        private float _blendDuration = 0.15f;

        private bool _isActive;
        private bool _isExiting;

        public bool IsRunning => _isActive && _isExiting == false;

        public void BeginClimb()
        {
            _isActive = true;
            _isExiting = false;

            Activate(_blendDuration);

            _exit?.Deactivate(0f, true);
            _idle?.Deactivate(0f, true);
            _climb?.Deactivate(0f, true);

            if (_start != null)
            {
                _start.SetAnimationTime(0f);
                _start.Activate(_blendDuration);
            }
            else
            {
                ActivateClimb(0f);
            }
        }

        public bool UpdateClimb(float normalizedProgress, bool isMoving)
        {
            if (_isActive == false)
            {
                return false;
            }

            normalizedProgress = Mathf.Clamp01(normalizedProgress);

            if (_climb != null)
            {
                _climb.SetAnimationTime(normalizedProgress);
            }

            if (_idle != null)
            {
                _idle.SetAnimationTime(normalizedProgress);
            }

            if (_isExiting == true)
            {
                if (_exit == null || _exit.IsFinished(1f, true) == true)
                {
                    _exit?.Deactivate(_blendDuration, true);
                    Deactivate(_blendDuration);
                    _isActive = false;
                    _isExiting = false;
                    return false;
                }

                return true;
            }

            if (_start != null && _start.IsActive(true) == true)
            {
                if (_start.IsFinished(1f, true) == true)
                {
                    _start.Deactivate(_blendDuration, true);
                    ActivateClimb(normalizedProgress);
                }

                return true;
            }

            if (isMoving == true)
            {
                if (_climb != null && _climb.IsActive(true) == false)
                {
                    ActivateClimb(normalizedProgress);
                }

                if (_idle != null && _idle.IsActive(true) == true)
                {
                    _idle.Deactivate(_blendDuration, true);
                }
            }
            else
            {
                if (_climb != null && _climb.IsActive(true) == true)
                {
                    _climb.Deactivate(_blendDuration, true);
                }

                if (_idle != null && _idle.IsActive(true) == false)
                {
                    _idle.Activate(_blendDuration);
                }
            }

            return true;
        }

        public void EndClimb()
        {
            if (_isActive == false || _isExiting == true)
            {
                return;
            }

            _isExiting = true;

            _start?.Deactivate(_blendDuration, true);
            _climb?.Deactivate(_blendDuration, true);
            _idle?.Deactivate(_blendDuration, true);

            if (_exit != null)
            {
                _exit.SetAnimationTime(0f);
                _exit.Activate(_blendDuration);
            }
            else
            {
                Deactivate(_blendDuration);
                _isActive = false;
                _isExiting = false;
            }
        }

        public void CancelClimb()
        {
            _isActive = false;
            _isExiting = false;

            _start?.Deactivate(0.05f, true);
            _climb?.Deactivate(0.05f, true);
            _idle?.Deactivate(0.05f, true);
            _exit?.Deactivate(0.05f, true);

            if (IsActive(true) == true)
            {
                Deactivate(0.05f);
            }
        }

        private void ActivateClimb(float normalizedProgress)
        {
            if (_climb == null)
            {
                return;
            }

            _climb.SetAnimationTime(normalizedProgress);
            _climb.Activate(_blendDuration);
        }
    }
}
