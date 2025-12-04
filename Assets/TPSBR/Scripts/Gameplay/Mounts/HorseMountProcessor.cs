namespace TPSBR
{
    using UnityEngine;
    using Fusion.Addons.KCC;

    public sealed class HorseMountProcessor : KCCProcessor, IPrepareData
    {
        [SerializeField] private float _maxGroundAngle = 60f;
        [SerializeField] private float _gravityMultiplier = 1f;

        private Vector3 _movementDirection;
        private float _kinematicSpeed;

        public override float GetPriority(KCC kcc) => BREnvironmentProcessor.DefaultPriority + 1;

        public void SetMovement(Vector3 movementDirection, float speed)
        {
            _movementDirection = movementDirection;
            _kinematicSpeed = speed;
        }

        public void ResetMovement()
        {
            _movementDirection = Vector3.zero;
            _kinematicSpeed = 0f;
        }

        public void Execute(PrepareData stage, KCC kcc, KCCData data)
        {
            data.MaxGroundAngle = _maxGroundAngle;
            data.Gravity = Physics.gravity * _gravityMultiplier;
            data.DynamicVelocity = Vector3.zero;

            Vector3 planarDirection = new Vector3(_movementDirection.x, 0f, _movementDirection.z);
            Vector3 normalizedDirection = planarDirection.sqrMagnitude > 0f ? planarDirection.normalized : Vector3.zero;

            data.InputDirection = planarDirection;
            data.KinematicDirection = normalizedDirection;
            data.KinematicSpeed = _kinematicSpeed;
            data.KinematicVelocity = normalizedDirection * _kinematicSpeed;
        }
    }
}
