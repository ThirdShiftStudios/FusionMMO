using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class RideMountState : ClipState
    {
        [SerializeField]
        private MountDefinition _mountDefinition;

        private AnimationClip _defaultClip;
        private float _defaultSpeed;
        private bool _defaultLooping;
        private MountDefinition _appliedDefinition;

        public MountDefinition MountDefinition => _mountDefinition;

        public void ApplyDefinition(MountDefinition definition)
        {
            if (Node == null)
                return;

            _appliedDefinition = definition != null ? definition : _mountDefinition;

            if (_appliedDefinition != null && _appliedDefinition.RiderRideClip != null)
            {
                Node.Clip = _appliedDefinition.RiderRideClip;
                Node.Speed = _appliedDefinition.RiderRideClipSpeed;
                Node.IsLooping = true;
            }
            else
            {
                Node.Clip = _defaultClip;
                Node.Speed = _defaultSpeed;
                Node.IsLooping = _defaultLooping;
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            ApplyDefinition(_appliedDefinition != null ? _appliedDefinition : _mountDefinition);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (Node != null)
            {
                _defaultClip = Node.Clip;
                _defaultSpeed = Node.Speed;
                _defaultLooping = Node.IsLooping;
            }

            _appliedDefinition = _mountDefinition;
        }
    }
}
