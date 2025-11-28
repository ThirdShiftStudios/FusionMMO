namespace TPSBR
{
    using Fusion.Addons.AnimationController;
    using UnityEngine;

    public sealed class MountLocomotionLayer : AnimationLayer
    {
        [SerializeField] private ClipState _idle;
        [SerializeField] private ClipState _move;
        [SerializeField] private float _blendDuration = 0.1f;
        [SerializeField] private float _speedLerp = 8f;
        [SerializeField] private float _moveThreshold = 0.05f;

        private float _targetSpeed;
        private float _currentSpeed;
        private bool _isMounted;
        private AnimationClip _defaultIdleClip;
        private AnimationClip _defaultMoveClip;
        private float _defaultMoveSpeed = 1f;
        private float _activeMoveSpeed = 1f;
        private bool _defaultIdleLooping = true;
        private bool _defaultMoveLooping = true;

        public void ApplyDefinition(MountDefinition definition)
        {
            _activeMoveSpeed = definition != null ? definition.MountMoveClipSpeed : _defaultMoveSpeed;

            SetClip(_idle, definition?.MountIdleClip, 1f, ref _defaultIdleClip, ref _defaultIdleLooping);
            SetClip(_move, definition?.MountMoveClip, _activeMoveSpeed, ref _defaultMoveClip, ref _defaultMoveLooping);
        }

        public void SetMounted(bool mounted)
        {
            _isMounted = mounted;

            if (_isMounted == false)
            {
                _targetSpeed = 0f;
            }
        }

        public void SetMoveInput(float normalizedSpeed)
        {
            _targetSpeed = Mathf.Clamp01(normalizedSpeed);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_idle != null && _idle.Node != null)
            {
                _defaultIdleClip = _idle.Node.Clip;
                _defaultIdleLooping = _idle.Node.IsLooping;
            }

            if (_move != null && _move.Node != null)
            {
                _defaultMoveClip = _move.Node.Clip;
                _defaultMoveSpeed = _move.Node.Speed;
                _activeMoveSpeed = _defaultMoveSpeed;
                _defaultMoveLooping = _move.Node.IsLooping;
            }
        }

        protected override void OnFixedUpdate()
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, _targetSpeed, Controller.DeltaTime * _speedLerp);

            bool isMoving = _currentSpeed > _moveThreshold && _isMounted == true;

            if (isMoving == true)
            {
                if (_move != null && _move.Node != null)
                {
                    _move.Node.Speed = Mathf.Max(0.01f, _currentSpeed * _activeMoveSpeed);
                    _move.Activate(_blendDuration);
                }

                _idle?.Deactivate(_blendDuration, true);
            }
            else
            {
                if (_idle != null)
                {
                    _idle.Activate(_blendDuration);
                }

                _move?.Deactivate(_blendDuration, true);
            }
        }

        private static void SetClip(ClipState state, AnimationClip clip, float speed, ref AnimationClip defaultClip, ref bool defaultLooping)
        {
            if (state == null || state.Node == null)
                return;

            if (clip != null)
            {
                state.Node.Clip = clip;
                state.Node.Speed = speed;
                state.Node.IsLooping = true;
            }
            else
            {
                state.Node.Clip = defaultClip;
                state.Node.Speed = speed;
                state.Node.IsLooping = defaultLooping;
            }
        }
    }
}
