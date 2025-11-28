namespace TPSBR
{
    using System;
    using UnityEngine;
    using TSS.Data;
    [CreateAssetMenu(fileName = "MountDefinition", menuName = "TPSBR/Mount Definition", order = 1000)]
    public sealed class MountDefinition : ItemDefinition
    {
        [SerializeField] private string _code;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private float _moveSpeed = 10f;
        [SerializeField] private float _turnSpeed = 240f;
        [SerializeField] private float _acceleration = 20f;
        [Header("Animation")]
        [SerializeField] private AnimationClip _riderRideClip;
        [SerializeField] private float _riderRideClipSpeed = 1f;
        [SerializeField] private AnimationClip _mountIdleClip;
        [SerializeField] private AnimationClip _mountMoveClip;
        [SerializeField] private float _mountMoveClipSpeed = 1f;

        public string Code => _code;
        public string DisplayName => _displayName.HasValue() ? _displayName : name;
        public string Description => _description;
        public float MoveSpeed => _moveSpeed;
        public float TurnSpeed => _turnSpeed;
        public float Acceleration => _acceleration;
        public AnimationClip RiderRideClip => _riderRideClip;
        public float RiderRideClipSpeed => _riderRideClipSpeed;
        public AnimationClip MountIdleClip => _mountIdleClip;
        public AnimationClip MountMoveClip => _mountMoveClip;
        public float MountMoveClipSpeed => _mountMoveClipSpeed;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _moveSpeed = Mathf.Max(0.0f, _moveSpeed);
            _turnSpeed = Mathf.Max(0.0f, _turnSpeed);
            _acceleration = Mathf.Max(0.0f, _acceleration);
            _riderRideClipSpeed = Mathf.Max(0.0f, _riderRideClipSpeed);
            _mountMoveClipSpeed = Mathf.Max(0.0f, _mountMoveClipSpeed);

            if (string.IsNullOrEmpty(_code) == true)
            {
                _code = Guid.NewGuid().ToString("N");
            }
        }
#endif
    }
}
