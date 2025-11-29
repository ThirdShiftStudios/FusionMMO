namespace TPSBR
{
    using Fusion.Addons.AnimationController;
    using UnityEngine;

    public sealed class MountAnimationLayer : AnimationLayer
    {
        [SerializeField]
        private RideMountState[] _rideStates;

        private RideMountState _activeRide;
        private bool _isMounted;
        private MountDefinition _definition;

        public void SetMounted(bool isMounted, MountDefinition mountDefinition)
        {
            _definition = mountDefinition;
            _isMounted = isMounted;

            RideMountState previousRide = _activeRide;
            _activeRide = GetRideState(mountDefinition);

            if (previousRide != null && previousRide != _activeRide && previousRide.IsActive(true) == true)
            {
                previousRide.Deactivate(0.15f);
            }

            if (_activeRide == null)
                return;

            _activeRide.ApplyDefinition(_definition);

            if (isMounted == true)
            {
                _activeRide.Activate(0.1f);
            }
            else if (_activeRide.IsActive(true) == true)
            {
                _activeRide.Deactivate(0.15f);
            }
        }

        protected override void OnInitialize()
        {
            _activeRide = GetRideState(_definition);
            _activeRide?.ApplyDefinition(_definition);
        }

        protected override void OnFixedUpdate()
        {
            if (_isMounted == false && _activeRide != null && _activeRide.IsActive(true) == true)
            {
                _activeRide.Deactivate(0.15f);
            }
        }

        private RideMountState GetRideState(MountDefinition mountDefinition)
        {
            if (_rideStates == null || _rideStates.Length == 0)
                return null;

            if (mountDefinition != null)
            {
                for (int i = 0; i < _rideStates.Length; ++i)
                {
                    if (_rideStates[i] != null && _rideStates[i].MountDefinition == mountDefinition)
                    {
                        return _rideStates[i];
                    }
                }
            }

            return _rideStates[0];
        }
    }
}
