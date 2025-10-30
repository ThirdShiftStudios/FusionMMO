using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class BeerUsable : Weapon
    {
        [Networked]
        private byte _beerStack { get; set; }

        private bool _isDrinking;

        public byte BeerStack => _beerStack;

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

            if (_beerStack == 0)
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

            if (HasStateAuthority == true && _beerStack > 0)
            {
                _beerStack--;
            }
        }

        public void AddBeerStack(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (HasStateAuthority == false)
            {
                return;
            }

            int newValue = Mathf.Clamp(_beerStack + amount, 0, byte.MaxValue);
            _beerStack = (byte)newValue;
        }
    }
}
