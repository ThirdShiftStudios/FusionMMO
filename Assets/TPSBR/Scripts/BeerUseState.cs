using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public interface IConsumableUse
    {
        Weapon OwnerWeapon { get; }
        Character Character { get; }
        void NotifyUseFinished();
    }

    public class BeerUseState : MixerState
    {
        [SerializeField] private BeerDrinkState _drinkState;
        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.15f;

        private IConsumableUse _activeConsumable;
        private Weapon _activeWeapon;

        public void PlayDrink(IConsumableUse consumable)
        {
            if (consumable == null)
            {
                return;
            }

            if (_activeConsumable != null && _activeConsumable != consumable)
            {
                return;
            }

            _activeConsumable = consumable;
            _activeWeapon = consumable.OwnerWeapon;

            if (_drinkState != null)
            {
                _drinkState.SetAnimationTime(0f);
                _drinkState.Activate(_blendInDuration);
            }

            Activate(_blendInDuration);
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (_activeConsumable == null || _activeWeapon == null)
            {
                return;
            }

            if (_drinkState == null)
            {
                Finish();
                return;
            }

            Character character = _activeConsumable.Character;
            Agent agent = character != null ? character.Agent : null;
            Inventory inventory = agent != null ? agent.Inventory : null;

            if (_activeConsumable is BeerUsable beerUsable)
            {
                beerUsable.NotifyDrinkProgress(_drinkState.AnimationTime, _drinkState.BuffApplyNormalizedTime);
            }

            if (inventory != null && inventory.CurrentWeapon != _activeWeapon)
            {
                Finish();
                return;
            }

            if (_drinkState.IsFinished(0.99f) == true || _drinkState.IsActive(true) == false)
            {
                Finish();
            }
        }

        private void Finish()
        {
            if (_drinkState != null && _drinkState.IsActive(true) == true)
            {
                _drinkState.Deactivate(_blendOutDuration);
            }

            if (IsActive(true) == true)
            {
                Deactivate(_blendOutDuration);
            }

            _activeConsumable?.NotifyUseFinished();
            _activeConsumable = null;
            _activeWeapon = null;
        }
    }
}
