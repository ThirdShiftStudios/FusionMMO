using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class BeerUsable : Weapon
    {
        private bool _isDrinking;

        [Networked]
        private byte _beerStack { get; set; }

        public byte BeerStack => _beerStack;
        public bool HasBeerStack => _beerStack > 0;
        public bool IsBeerStackFull => _beerStack >= byte.MaxValue;

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

            if (HasBeerStack == false)
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

                if (HasStateAuthority == true && HasBeerStack == true)
                {
                    _beerStack--;
                }
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

        public void AddBeerStack(byte amount)
        {
            if (amount == 0)
                return;

            if (HasStateAuthority == false)
                return;

            if (IsBeerStackFull == true)
                return;

            int newStack = Mathf.Clamp(_beerStack + amount, 0, byte.MaxValue);
            _beerStack = (byte)newStack;
        }
    }
}
