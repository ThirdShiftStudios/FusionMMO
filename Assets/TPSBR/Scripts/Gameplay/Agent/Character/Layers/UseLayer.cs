using UnityEngine.Serialization;

namespace TPSBR
{
    using UnityEngine;
    using Fusion.Addons.AnimationController;

    public sealed class UseLayer : AnimationLayer
    {
        // PUBLIC MEMBERS

        public StaffUseState StaffAttack => staffAttack;
        public FishingPoleUseState FishingPoleUseState  => fishingPoleUse;
        public BeerUseState BeerUseState => beerUse;

        // PRIVATE MEMBERS

        [FormerlySerializedAs("_shoot")]
        [SerializeField]
        private StaffUseState staffAttack;
        [SerializeField]
        private FishingPoleUseState fishingPoleUse;
        [SerializeField]
        private BeerUseState beerUse;

        public bool TryHandleUse(Weapon weapon, in WeaponUseRequest request)
        {
            if (request.ShouldUse == false)
                return true;

            if (weapon == null)
                return false;

            return weapon.HandleAnimationRequest(this, request);
        }

        // AnimationLayer INTERFACE

        protected override void OnFixedUpdate()
        {

        }
    }
}
