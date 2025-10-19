using System;
using TSS.Data;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TPSBR
{
    public abstract class StatDefinition : DataDefinition
    {
        [SerializeField]
        private string _displayName;
        [SerializeField]
        private Sprite _icon;
        [SerializeField]
        private string _code = "???";

        public override string Name => _displayName;
        public override Texture2D Icon => _icon != null ? _icon.texture : null;
        public Sprite IconSprite => _icon;
        public string Code => _code;

        public virtual float GetTotalHealth(int statLevel)
        {
            return 0f;
        }

        public virtual float GetMovementSpeedMultiplier(int statLevel)
        {
            return 0f;
        }

        internal void RuntimeInitialize(string displayName, string code)
        {
            _displayName = displayName;
            _code = NormalizeCode(code);
            _icon = null;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            string normalizedCode = NormalizeCode(_code);
            if (string.Equals(_code, normalizedCode, StringComparison.Ordinal) == false)
            {
                _code = normalizedCode;
                EditorUtility.SetDirty(this);
            }
        }
#endif

        protected static string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code) == true)
            {
                return "???";
            }

            string trimmed = code.Trim().ToUpperInvariant();
            return trimmed.Length <= 3 ? trimmed : trimmed.Substring(0, 3);
        }
    }

    [CreateAssetMenu(fileName = "IntelligenceDefinition", menuName = "TSS/Stats/Intelligence")]
    public sealed class IntelligenceDefinition : StatDefinition
    {
    }

    [CreateAssetMenu(fileName = "StrengthDefinition", menuName = "TSS/Stats/Strength")]
    public sealed class StrengthDefinition : StatDefinition
    {
        public override float GetTotalHealth(int statLevel)
        {
            return Mathf.Max(0, statLevel) * 10f;
        }
    }

    [CreateAssetMenu(fileName = "DexterityDefinition", menuName = "TSS/Stats/Dexterity")]
    public sealed class DexterityDefinition : StatDefinition
    {
        public override float GetMovementSpeedMultiplier(int statLevel)
        {
            return Mathf.Max(0, statLevel) * 0.01f;
        }
    }

    [CreateAssetMenu(fileName = "EnduranceDefinition", menuName = "TSS/Stats/Endurance")]
    public sealed class EnduranceDefinition : StatDefinition
    {
        public override float GetTotalHealth(int statLevel)
        {
            return Mathf.Max(0, statLevel) * 20f;
        }
    }

    [CreateAssetMenu(fileName = "LuckDefinition", menuName = "TSS/Stats/Luck")]
    public sealed class LuckDefinition : StatDefinition
    {
    }

    [CreateAssetMenu(fileName = "WillpowerDefinition", menuName = "TSS/Stats/Willpower")]
    public sealed class WillpowerDefinition : StatDefinition
    {
    }
}
