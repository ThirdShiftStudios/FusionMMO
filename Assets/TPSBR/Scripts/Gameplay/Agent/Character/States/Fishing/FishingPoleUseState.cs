using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class FishingPoleUseState : MixerState
    {
        [SerializeField] private FishingCastParentState _castState;
        [SerializeField] private FishingWaitingState _waiting;
        [SerializeField] private FishingFightingState _fighting;
        [SerializeField] private FishingCatchParentState _catch;

        [Header("Blending")]
        [SerializeField] private float _blendInDuration = 0.1f;
        [SerializeField] private float _blendOutDuration = 0.1f;

        private FishingPoleWeapon _activeWeapon;

        public bool StartCast(FishingPoleWeapon weapon)
        {
            if (weapon == null)
                return false;

            if (_castState == null)
                return false;

            if (_activeWeapon != null && _activeWeapon != weapon)
                return false;

            _activeWeapon = weapon;

            DeactivateSecondaryStates();

            if (IsActive(true) == false)
            {
                Activate(_blendInDuration);
            }

            if (_castState.Play(this, weapon) == false)
            {
                OnCastFinished(weapon);
                return false;
            }

            return true;
        }

        internal void OnCastFinished(FishingPoleWeapon weapon)
        {
            if (_activeWeapon != weapon)
                return;

            _activeWeapon = null;

            if (IsActive(true) == true)
            {
                Deactivate(_blendOutDuration);
            }
        }

        private void DeactivateSecondaryStates()
        {
            _waiting?.Deactivate(_blendOutDuration, true);
            _fighting?.Deactivate(_blendOutDuration, true);
            _catch?.Deactivate(_blendOutDuration, true);
        }
    }
}
