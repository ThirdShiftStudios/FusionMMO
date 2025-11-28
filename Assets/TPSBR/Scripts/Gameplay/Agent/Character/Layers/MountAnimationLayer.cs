namespace TPSBR
{
    using Fusion.Addons.AnimationController;
    using UnityEngine;

    public sealed class MountAnimationLayer : AnimationLayer
    {
        [SerializeField]
        private RideMountState _ride;

        private bool _isMounted;
        private MountDefinition _definition;

        public void SetMounted(bool isMounted, MountDefinition mountDefinition)
        {
            _definition = mountDefinition;
            _isMounted = isMounted;

            if (_ride == null)
                return;

            _ride.ApplyDefinition(_definition);

            if (isMounted == true)
            {
                _ride.Activate(0.1f);
            }
            else if (_ride.IsActive(true) == true)
            {
                _ride.Deactivate(0.15f);
            }
        }

        protected override void OnInitialize()
        {
            if (_ride != null)
            {
                _ride.ApplyDefinition(_definition);
            }
        }

        protected override void OnFixedUpdate()
        {
            if (_isMounted == false && _ride != null && _ride.IsActive(true) == true)
            {
                _ride.Deactivate(0.15f);
            }
        }
    }
}
