using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public sealed class FishingCastThrowState : ClipState
    {
        [SerializeField]
        private float _blendInDuration = 0.1f;

        [SerializeField]
        private float _blendOutDuration = 0.1f;

        private bool _isPlaying;

        public bool Play()
        {
            SetAnimationTime(0f);
            Activate(_blendInDuration);
            _isPlaying = true;
            return true;
        }

        public void Stop()
        {
            if (_isPlaying == false)
                return;

            _isPlaying = false;
            Deactivate(_blendOutDuration);
        }

        public bool IsPlaying => _isPlaying;

        protected override void OnClipFinished()
        {
            base.OnClipFinished();
            _isPlaying = false;
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            _isPlaying = false;
        }
    }
}
