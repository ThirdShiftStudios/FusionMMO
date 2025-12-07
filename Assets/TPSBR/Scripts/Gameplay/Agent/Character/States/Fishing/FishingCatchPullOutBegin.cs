using Fusion.Addons.AnimationController;
using Fusion.Addons.KCC;
using UnityEngine;

namespace TPSBR
{
    public class FishingCatchPullOutBegin : ClipState
    {
        public event System.Action ClipFinished;

        [SerializeField] private Transform _fishTransform;

        private FishingPoleWeapon _weapon;
        private bool _fishAttached;

        internal void SetActiveWeapon(FishingPoleWeapon weapon)
        {
            _weapon = weapon;

            if (weapon != null && IsActive(true) == true)
            {
                EnsureWeaponReference(attachFish: true);
            }
        }

        internal void ClearActiveWeapon(FishingPoleWeapon weapon)
        {
            if (_weapon == weapon)
            {
                _weapon = null;
                _fishAttached = false;
            }
        }

        protected override void OnClipFinished()
        {
            base.OnClipFinished();
            ClipFinished?.Invoke();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            _fishAttached = false;
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
            _fishAttached = false;
        }

        private void SuppressMovement()
        {
            EnsureWeaponReference(attachFish: _fishAttached == false);

            if (_weapon?.Character?.CharacterController is not KCC kcc)
                return;

            kcc.SetInputDirection(Vector3.zero);
            kcc.SetDynamicVelocity(Vector3.zero);
            kcc.SetKinematicVelocity(Vector3.zero);
            kcc.SetExternalVelocity(Vector3.zero);
            kcc.SetExternalAcceleration(Vector3.zero);
        }

        private void EnsureWeaponReference(bool attachFish = false)
        {
            if (_weapon != null)
            {
                if (attachFish == true && _fishTransform != null)
                {
                    _weapon.AttachFishToCatchTransform(_fishTransform);
                    _fishAttached = true;
                }

                return;
            }

            Character character = GetComponentInParent<Character>();
            Agent agent = character != null ? character.Agent : null;
            Inventory inventory = agent != null ? agent.Inventory : null;
            FishingPoleWeapon resolvedWeapon = inventory != null ? inventory.CurrentWeapon as FishingPoleWeapon : null;

            if (resolvedWeapon == null)
                return;

            _weapon = resolvedWeapon;

            if (attachFish == true && _fishTransform != null)
            {
                _weapon.AttachFishToCatchTransform(_fishTransform);
                _fishAttached = true;
            }
        }
    }
}
