using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class BeerUsable : Weapon
    {
        [SerializeField] private byte _startingBeerStack = 3;
        [SerializeField] private byte _maxBeerStack = 6;

        [Networked]
        private byte _beerStack { get; set; }

        private bool _isDrinking;

        public byte BeerStack => _beerStack;
        public byte MaxBeerStack => _maxBeerStack;
        public bool IsStackFull => _maxBeerStack > 0 && _beerStack >= _maxBeerStack;

        public override void Spawned()
        {
            base.Spawned();

            if (HasStateAuthority == true)
            {
                _beerStack = ClampToMax(_startingBeerStack);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _beerStack = 0;
            _isDrinking = false;

            base.Despawned(runner, hasState);
        }

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

                if (HasStateAuthority == true && _beerStack > 0)
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

        public bool TryAddBeerStack(byte amount)
        {
            if (amount == 0 || HasStateAuthority == false)
            {
                return false;
            }

            if (_maxBeerStack > 0 && _beerStack >= _maxBeerStack)
            {
                return false;
            }

            int targetValue = _beerStack + amount;

            if (_maxBeerStack > 0)
            {
                targetValue = Mathf.Min(targetValue, _maxBeerStack);
            }
            else
            {
                targetValue = Mathf.Min(targetValue, byte.MaxValue);
            }

            byte clampedValue = (byte)Mathf.Clamp(targetValue, 0, byte.MaxValue);

            if (clampedValue == _beerStack)
            {
                return false;
            }

            _beerStack = clampedValue;
            return true;
        }

        private byte ClampToMax(byte value)
        {
            if (_maxBeerStack > 0)
            {
                return (byte)Mathf.Clamp(value, 0, _maxBeerStack);
            }

            return value;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxBeerStack = (byte)Mathf.Clamp(_maxBeerStack, 0, byte.MaxValue);
            _startingBeerStack = ClampToMax(_startingBeerStack);
        }
#endif
    }
}
