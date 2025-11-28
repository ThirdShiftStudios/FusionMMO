namespace TPSBR
{
    using Fusion.Addons.AnimationController;

    public sealed class MountAnimationLayer : AnimationLayer
    {
        [SerializeField]
        private RideMountState _ride;

        private bool _isMounted;

        public void SetMounted(bool isMounted, MountDefinition mountDefinition)
        {
            _ = mountDefinition;
            _isMounted = isMounted;

            if (_ride == null)
                return;

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
                _ride.SetAnimationCategory(AnimationCategory.FullBody);
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
