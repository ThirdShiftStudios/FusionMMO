namespace TPSBR
{
    using Fusion.Addons.KCC;
    using UnityEngine;

    public sealed class HorseMountProcessor : KCCProcessor, IPrepareData
    {
        [SerializeField] private MountDefinition _definition;
        [SerializeField] private MountAnimator _animator;
        [SerializeField] private Vector3 _gravity = default;
        [SerializeField] private float _upGravityMultiplier = 1f;
        [SerializeField] private float _downGravityMultiplier = 1f;

        [Header("Kinematic")]
        [SerializeField] private float _kinematicGroundAcceleration = 50f;
        [SerializeField] private float _kinematicGroundFriction = 35f;
        [SerializeField] private float _kinematicAirAcceleration = 5f;
        [SerializeField] private float _kinematicAirFriction = 2f;

        [Header("Dynamic")]
        [SerializeField] private float _dynamicGroundFriction = 20f;
        [SerializeField] private float _dynamicAirFriction = 2f;

        private float _currentSpeed;

        public void Configure(MountDefinition definition, MountAnimator animator)
        {
            _definition = definition;
            _animator = animator;
        }

        public void ResetState()
        {
            _currentSpeed = 0f;
        }

        public override float GetPriority(KCC kcc) => 0f;

        public void Execute(PrepareData stage, KCC kcc, KCCData data)
        {
            if (_definition == null)
                return;

            Vector3 gravity = _gravity != default ? _gravity : Physics.gravity;
            KCCData fixedData = kcc.FixedData;
            Vector3 inputDirection = kcc.IsInFixedUpdate == true ? data.InputDirection : fixedData.InputDirection;

            if (kcc.IsInFixedUpdate == true)
            {
                float fixedDeltaTime = fixedData.DeltaTime;
                float targetSpeed = Mathf.Clamp01(inputDirection.magnitude) * _definition.MoveSpeed;
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _definition.Acceleration * fixedDeltaTime);
            }

            Vector3 kinematicDirection = Vector3.zero;
            if (_currentSpeed > float.Epsilon && inputDirection.sqrMagnitude > float.Epsilon)
            {
                kinematicDirection = inputDirection.normalized;
            }

            Vector3 appliedGravity = gravity * (fixedData.RealVelocity.y > 0.0f ? _upGravityMultiplier : _downGravityMultiplier);
            Vector3 dynamicVelocity = fixedData.DynamicVelocity;
            bool   finalized       = false;

            bool applyGravity = fixedData.IsGrounded == false || (fixedData.IsSteppingUp == false && (fixedData.IsSnappingToGround == true || fixedData.GroundDistance > 0.001f));
            if (applyGravity == true)
            {
                dynamicVelocity += appliedGravity * fixedData.DeltaTime;
            }

            if (dynamicVelocity.IsZero() == false)
            {
                if (dynamicVelocity.IsAlmostZero(0.001f) == true)
                {
                    dynamicVelocity = default;
                }
                else if (fixedData.IsGrounded == true)
                {
                    Vector3 frictionAxis = Vector3.one;
                    if (fixedData.GroundDistance > 0.001f || fixedData.IsSnappingToGround == true)
                    {
                        frictionAxis.y = 0f;
                    }

                    dynamicVelocity += KCCPhysicsUtility.GetFriction(dynamicVelocity, dynamicVelocity, frictionAxis, fixedData.GroundNormal, fixedData.KinematicSpeed, true, 0.0f, 0.0f, _dynamicGroundFriction, fixedData.DeltaTime);
                }
                else
                {
                    dynamicVelocity += KCCPhysicsUtility.GetFriction(dynamicVelocity, dynamicVelocity, new Vector3(1.0f, 0.0f, 1.0f), fixedData.KinematicSpeed, true, 0.0f, 0.0f, _dynamicAirFriction, fixedData.DeltaTime);
                }
            }

            data.DynamicVelocity = dynamicVelocity;

            Vector3 kinematicVelocity = fixedData.KinematicVelocity;

            if (fixedData.IsGrounded == true)
            {
                if (kinematicDirection.IsAlmostZero(0.0001f) == false && KCCPhysicsUtility.ProjectOnGround(fixedData.GroundNormal, kinematicDirection, out Vector3 projectedDirection) == true)
                {
                    data.KinematicTangent = projectedDirection.normalized;
                }
                else
                {
                    data.KinematicTangent = fixedData.GroundTangent;
                }

                if (kinematicVelocity.IsAlmostZero() == false && KCCPhysicsUtility.ProjectOnGround(fixedData.GroundNormal, kinematicVelocity, out Vector3 projectedKinematicVelocity) == true)
                {
                    kinematicVelocity = projectedKinematicVelocity.normalized * kinematicVelocity.magnitude;
                }

                if (kinematicDirection.IsAlmostZero() == true)
                {
                    data.KinematicVelocity = kinematicVelocity + KCCPhysicsUtility.GetFriction(kinematicVelocity, kinematicVelocity, Vector3.one, fixedData.GroundNormal, _currentSpeed, true, 0.0f, 0.0f, _kinematicGroundFriction, fixedData.DeltaTime);
                    FinalizeData(data, appliedGravity, kinematicDirection);
                    finalized = true;
                }
            }
            else
            {
                if (kinematicDirection.IsAlmostZero() == false)
                {
                    data.KinematicTangent = kinematicDirection.normalized;
                }
                else
                {
                    data.KinematicTangent = data.TransformDirection;
                }

                if (kinematicDirection.IsAlmostZero() == true)
                {
                    data.KinematicVelocity = kinematicVelocity + KCCPhysicsUtility.GetFriction(kinematicVelocity, kinematicVelocity, new Vector3(1.0f, 0.0f, 1.0f), _currentSpeed, true, 0.0f, 0.0f, _kinematicAirFriction, fixedData.DeltaTime);
                    FinalizeData(data, appliedGravity, kinematicDirection);
                    finalized = true;
                }
            }

            if (finalized == true)
            {
                UpdateAnimator();
                return;
            }

            Vector3 moveDirection = kinematicVelocity.IsZero() == false ? kinematicVelocity : data.KinematicTangent;
            Vector3 acceleration;
            Vector3 friction;

            if (fixedData.IsGrounded == true)
            {
                acceleration = KCCPhysicsUtility.GetAcceleration(kinematicVelocity, data.KinematicTangent, Vector3.one, _currentSpeed, false, kinematicDirection.magnitude, 0.0f, _kinematicGroundAcceleration, 0.0f, fixedData.DeltaTime);
                friction     = KCCPhysicsUtility.GetFriction(kinematicVelocity, moveDirection, Vector3.one, fixedData.GroundNormal, _currentSpeed, false, 0.0f, 0.0f, _kinematicGroundFriction, fixedData.DeltaTime);
            }
            else
            {
                acceleration = KCCPhysicsUtility.GetAcceleration(kinematicVelocity, data.KinematicTangent, Vector3.one, _currentSpeed, false, kinematicDirection.magnitude, 0.0f, _kinematicAirAcceleration, 0.0f, fixedData.DeltaTime);
                friction     = KCCPhysicsUtility.GetFriction(kinematicVelocity, moveDirection, new Vector3(1.0f, 0.0f, 1.0f), _currentSpeed, false, 0.0f, 0.0f, _kinematicAirFriction, fixedData.DeltaTime);
            }

            kinematicVelocity = KCCPhysicsUtility.CombineAccelerationAndFriction(kinematicVelocity, acceleration, friction);

            if (kinematicVelocity.sqrMagnitude > _currentSpeed * _currentSpeed)
            {
                kinematicVelocity = kinematicVelocity / Vector3.Magnitude(kinematicVelocity) * _currentSpeed;
            }

            data.KinematicVelocity = kinematicVelocity;

            FinalizeData(data, appliedGravity, kinematicDirection);

            UpdateAnimator();
        }

        private void FinalizeData(KCCData data, Vector3 appliedGravity, Vector3 kinematicDirection)
        {
            data.Gravity = appliedGravity;
            data.KinematicDirection = kinematicDirection;
            data.KinematicSpeed = _currentSpeed;
        }

        private void UpdateAnimator()
        {
            if (_animator == null)
                return;

            float normalizedSpeed = _definition.MoveSpeed > 0f ? _currentSpeed / _definition.MoveSpeed : 0f;
            _animator.SetMoveInput(normalizedSpeed);
        }
    }
}
