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
        [Tooltip("Icon displayed in UI when referencing this ability.")]
        private Sprite _icon;

        [SerializeField]
        [Tooltip("Unique string identifier used when referencing this ability across systems.")]
        private string _stringCode;

        public override string Name => _displayName;
        public override Sprite Icon => _icon;
        public string StringCode => _stringCode;

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
        }
#endif
    }
}
