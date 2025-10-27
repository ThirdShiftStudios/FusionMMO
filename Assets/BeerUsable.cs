using UnityEngine;

namespace TPSBR
{
    public class BeerUsable : Weapon
    {
        private bool _isDrinking;

        public override bool CanFire(bool keyDown)
        {
            return false;
        }

        public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
        {

        }

        public override WeaponUseRequest EvaluateUse(bool attackActivated, bool attackHeld, bool attackReleased)
        {
            if (_isDrinking == true)
            {
                return WeaponUseRequest.None;
            }

            if (attackActivated == false)
            {
                return WeaponUseRequest.None;
            }

            return WeaponUseRequest.CreateAnimation(WeaponUseAnimation.BeerDrink);
        }

        public override void OnUseStarted(in WeaponUseRequest request)
        {
            if (request.Animation == WeaponUseAnimation.BeerDrink)
            {
                _isDrinking = true;
            }
        }

        public override bool HandleAnimationRequest(UseLayer attackLayer, in WeaponUseRequest request)
        {
            if (request.Animation != WeaponUseAnimation.BeerDrink)
            {
                return base.HandleAnimationRequest(attackLayer, request);
            }

            if (attackLayer == null)
            {
                return false;
            }

            BeerUseState beerUseState = attackLayer.BeerUseState;

            if (beerUseState == null)
            {
                return false;
            }

            beerUseState.PlayDrink(this);

            return true;
        }

        internal void NotifyDrinkFinished()
        {
            _isDrinking = false;
        }
    }
}
