using Fusion.Addons.AnimationController;
using Fusion.Addons.KCC;
using UnityEngine;

namespace TPSBR
{
    public class FishingWaitingState : ClipState
    {
        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.1f;

        private FishingPoleWeapon _weapon;

        internal void SetActiveWeapon(FishingPoleWeapon weapon)
        {
            _weapon = weapon;
        }

        internal void ClearActiveWeapon(FishingPoleWeapon weapon)
        {
            if (_weapon == weapon)
            {
                _weapon = null;
            }
        }

        internal void Play(float blendDuration)
        {
            SetAnimationTime(0f);
            Activate(blendDuration);
        }

        internal void Stop(float blendDuration)
        {
            if (IsActive(true) == true)
            {
                Deactivate(blendDuration, true);
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            SuppressMovement();
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            SuppressMovement();
        }

        protected override void OnInterpolate()
        {
            base.OnInterpolate();
            SuppressMovement();
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            SuppressMovement();
        }

        private void SuppressMovement()
        {
            if (_weapon?.Character?.CharacterController is not KCC kcc)
                return;

            kcc.SetInputDirection(Vector3.zero);
            kcc.SetDynamicVelocity(Vector3.zero);
            kcc.SetKinematicVelocity(Vector3.zero);
            kcc.SetExternalVelocity(Vector3.zero);
            kcc.SetExternalAcceleration(Vector3.zero);
        }
    }
}
