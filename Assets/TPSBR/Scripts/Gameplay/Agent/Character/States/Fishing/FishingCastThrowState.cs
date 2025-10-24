using Fusion.Addons.AnimationController;
using UnityEngine;

namespace TPSBR
{
    public class FishingCastThrowState : ClipState
    {
        [SerializeField, Range(0f, 1f)]
        private float _launchNormalizedTime = 0.55f;

        private FishingPoleWeapon _weapon;
        private bool _lureLaunched;

        internal void SetActiveWeapon(FishingPoleWeapon weapon)
        {
            _weapon = weapon;
            _lureLaunched = false;
        }

        internal void ClearActiveWeapon(FishingPoleWeapon weapon)
        {
            if (_weapon == weapon)
            {
                _weapon = null;
            }

            _lureLaunched = false;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            _lureLaunched = false;
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            _lureLaunched = false;
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (_weapon == null || _lureLaunched == true)
                return;

            float triggerTime = Mathf.Clamp01(_launchNormalizedTime);

            if (IsFinished(triggerTime) == false)
                return;

            _lureLaunched = true;
            _weapon.LaunchLure();
        }
    }
}
