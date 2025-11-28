using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class RideMountState : ClipState
    {
        private AnimationClip _defaultClip;
        private float _defaultSpeed;
        private bool _defaultLooping;

        public void ApplyDefinition(MountDefinition definition)
        {
            if (Node == null)
                return;

            if (definition != null && definition.RiderRideClip != null)
            {
                Node.Clip = definition.RiderRideClip;
                Node.Speed = definition.RiderRideClipSpeed;
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
            SetAnimationCategory(AnimationCategory.FullBody);
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
        }
    }
}
