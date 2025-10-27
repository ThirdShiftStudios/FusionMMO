using Fusion.Addons.AnimationController;
using Fusion.Addons.KCC;
using UnityEngine;

namespace TPSBR
{
    public class FishingCatchPullOutLoop : ClipState
    {
        [SerializeField] private Transform _fishTransform;
        private FishingPoleWeapon _weapon;
        private bool _hasAwardedCatch;

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
            _weapon?.AttachFishToCatchTransform(_fishTransform);
            TryAwardCatch();
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
            _hasAwardedCatch = false;
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

        private void TryAwardCatch()
        {
            if (_hasAwardedCatch == true)
            {
                return;
            }

            if (_weapon == null || _weapon.HasStateAuthority == false)
            {
                return;
            }

            FishDefinition definition = _weapon.ActiveFishDefinition;
            if (definition == null)
            {
                return;
            }

            Agent agent = _weapon.Character?.Agent;
            Inventory inventory = agent?.Inventory;

            if (inventory == null)
            {
                return;
            }

            byte remainder = inventory.AddItem(definition, 1);

            if (remainder < 1)
            {
                _hasAwardedCatch = true;
            }
        }
    }
}
