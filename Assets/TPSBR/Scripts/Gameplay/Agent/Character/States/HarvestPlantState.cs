using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public sealed class HarvestPlantState : ClipState
    {
        [SerializeField] private AnimationClip _harvestPlant;
        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.1f;

        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        public bool Play()
        {
            if (_isPlaying == true)
                return false;

            _isPlaying = true;

            if (Node != null)
            {
                Node.IsLooping = true;
            }
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

        protected override void OnActivate()
        {
            base.OnActivate();
            if (Node != null)
            {
                Node.IsLooping = true;
            }
        }

        private void Awake()
        {
            AssignClip();
        }

        private void OnValidate()
        {
            AssignClip();
        }

        private void AssignClip()
        {
            if (Node != null && _harvestPlant != null)
            {
                Node.Clip = _harvestPlant;
                Node.IsLooping = true;
            }
        }
    }
}
