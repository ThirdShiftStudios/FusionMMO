using Fusion.Addons.AnimationController;
using Fusion.Addons.KCC;
using UnityEngine;

namespace TPSBR
{
    public class FishingCatchPullOutLoop : ClipState
    {
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
            EnsureWeaponReference();

            if (_weapon?.Character?.CharacterController is not KCC kcc)
                return;

            kcc.SetInputDirection(Vector3.zero);
            kcc.SetDynamicVelocity(Vector3.zero);
            kcc.SetKinematicVelocity(Vector3.zero);
            kcc.SetExternalVelocity(Vector3.zero);
            kcc.SetExternalAcceleration(Vector3.zero);
        }

        private void EnsureWeaponReference()
        {
            if (_weapon != null)
            {
                return;
            }

            Character character = GetComponentInParent<Character>();
            Agent agent = character != null ? character.Agent : null;
            Inventory inventory = agent != null ? agent.Inventory : null;
            FishingPoleWeapon resolvedWeapon = inventory != null ? inventory.CurrentWeapon as FishingPoleWeapon : null;

            if (resolvedWeapon == null)
                return;

            _weapon = resolvedWeapon;
        }
    }
}
