using UnityEngine;

namespace TPSBR
{
    public class FishingPoleWeapon : Weapon
    {
        public override bool CanFire(bool keyDown)
        {
            return false;
        }

        public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
        {
            // Fishing pole currently has no firing behaviour. Override when casting logic is implemented.
        }

        public override WeaponUseRequest EvaluateUse(bool attackActivated, bool attackHeld, bool attackReleased)
        {
            return WeaponUseRequest.None;
        }
    }
}
