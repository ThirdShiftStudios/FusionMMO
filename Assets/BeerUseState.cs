using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class BeerUseState : MixerState
    {
        [SerializeField] private BeerDrinkState _drinkState;
        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.15f;

        private BeerUsable _activeWeapon;

        public void PlayDrink(BeerUsable weapon)
        {
            if (weapon == null)
            {
                return;
            }

            if (_activeWeapon != null && _activeWeapon != weapon)
            {
                return;
            }

            _activeWeapon = weapon;

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

            if (_activeWeapon == null)
            {
                return;
            }

            if (_drinkState == null)
            {
                Finish();
                return;
            }

            Character character = _activeWeapon.Character;
            Agent agent = character != null ? character.Agent : null;
            Inventory inventory = agent != null ? agent.Inventory : null;

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

            _activeWeapon?.NotifyDrinkFinished();
            _activeWeapon = null;
        }
    }
}
