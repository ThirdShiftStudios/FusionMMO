using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class CigaretteUseState : MixerState
    {
        [SerializeField] private CigaretteSmokeState _smokeState;
        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.15f;

        private IConsumableUse _activeConsumable;
        private Weapon _activeWeapon;

        public void PlaySmoke(IConsumableUse consumable)
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

            if (_smokeState != null)
            {
                _smokeState.SetAnimationTime(0f);
                _smokeState.Activate(_blendInDuration);
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

            if (_smokeState == null)
            {
                Finish();
                return;
            }

            Character character = _activeConsumable.Character;
            Agent agent = character != null ? character.Agent : null;
            Inventory inventory = agent != null ? agent.Inventory : null;

            if (inventory != null && inventory.CurrentWeapon != _activeWeapon)
            {
                Finish();
                return;
            }

            if (_smokeState.IsFinished(0.99f) == true || _smokeState.IsActive(true) == false)
            {
                Finish();
            }
        }

        private void Finish()
        {
            if (_smokeState != null && _smokeState.IsActive(true) == true)
            {
                _smokeState.Deactivate(_blendOutDuration);
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
