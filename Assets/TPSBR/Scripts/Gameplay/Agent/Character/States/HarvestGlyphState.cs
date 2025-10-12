using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public sealed class HarvestGlyphState : ClipState
    {
        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.1f;

        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        public bool Play()
        {
            if (_isPlaying == true)
                return false;

            _isPlaying = true;

            SetAnimationTime(0.0f);
            Activate(_blendInDuration);

            return true;
        }

        public void Stop()
        {
            if (_isPlaying == false)
                return;

            _isPlaying = false;

            Deactivate(_blendOutDuration);
        }
    }
}
