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

        public string Code => _code;
        public string DisplayName => _displayName.HasValue() ? _displayName : name;
        public string Description => _description;
        public float MoveSpeed => _moveSpeed;
        public float TurnSpeed => _turnSpeed;
        public float Acceleration => _acceleration;
        public override ESlotCategory SlotCategory => ESlotCategory.Mount;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _moveSpeed = Mathf.Max(0.0f, _moveSpeed);
            _turnSpeed = Mathf.Max(0.0f, _turnSpeed);
            _acceleration = Mathf.Max(0.0f, _acceleration);

            if (string.IsNullOrEmpty(_code) == true)
            {
                _code = Guid.NewGuid().ToString("N");
            }
        }
#endif
    }
}
