using System;
using TSS.Data;
using UnityEngine;

namespace TPSBR.Abilities
{
    public abstract class AbilityDefinition : DataDefinition
    {
        [SerializeField]
        [Tooltip("Human readable name displayed to players.")]
        private string _displayName;

        [SerializeField]
        [Tooltip("Rich text description displayed to players when inspecting the ability.")]
        [TextArea(3, 10)]
        private string _abilityDescription;

        [SerializeField]
        [Tooltip("Icon displayed in UI when referencing this ability.")]
        private Sprite _icon;

        [SerializeField]
        [Tooltip("Unique string identifier used when referencing this ability across systems.")]
        private string _stringCode;

        [SerializeField]
        [Tooltip("Base cast time for this ability in seconds.")]
        [Min(0f)]
        private float _baseCastTime = 1f;

        public override string Name => _displayName;
        public override Sprite Icon => _icon;
        public string AbilityDescription => _abilityDescription;
        public string StringCode => _stringCode;
        public float BaseCastTime => _baseCastTime;

        public bool IsStringCode(string value, StringComparison comparison = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(_stringCode) == true)
            {
                return false;
            }

            return string.Equals(_stringCode, value, comparison);
        }

        protected void SetStringCode(string value)
        {
            _stringCode = string.IsNullOrWhiteSpace(value) == true ? string.Empty : value.Trim();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetStringCode(_stringCode);
            _baseCastTime = Mathf.Max(0f, _baseCastTime);
        }
#endif
    }
}
