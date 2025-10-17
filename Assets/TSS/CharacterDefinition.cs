using System;
using System.Collections.Generic;
using UnityEngine;

namespace TSS.Data
{
    public abstract class CharacterDefinition : DataDefinition
    {
        [SerializeField]
        private string displayName;
        [SerializeField]
        private Sprite icon;
        [SerializeField]
        private string stringCode;

        [NonSerialized]
        private static CharacterDefinition[] _cachedDefinitions;
        [NonSerialized]
        private static Dictionary<string, CharacterDefinition> _definitionsByCode;

        public override string Name => displayName;
        public override Texture2D Icon => icon != null ? icon.texture : null;
        public Sprite IconSprite => icon;
        public string StringCode => stringCode;

        public static IReadOnlyList<CharacterDefinition> GetAll()
        {
            EnsureCache();
            return _cachedDefinitions;
        }

        public static CharacterDefinition GetByStringCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return null;
            }

            EnsureCache();

            if (_definitionsByCode.TryGetValue(code, out var definition) == true)
            {
                return definition;
            }

            return null;
        }

        public static void RefreshCache()
        {
            _cachedDefinitions = null;
            _definitionsByCode = null;
            EnsureCache();
        }

        private static void EnsureCache()
        {
            if (_cachedDefinitions != null && _definitionsByCode != null)
            {
                return;
            }

            var definitions = Resources.LoadAll<CharacterDefinition>(string.Empty);

            _cachedDefinitions = definitions ?? Array.Empty<CharacterDefinition>();
            _definitionsByCode = new Dictionary<string, CharacterDefinition>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _cachedDefinitions.Length; i++)
            {
                var definition = _cachedDefinitions[i];
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(definition.stringCode) == true)
                {
                    continue;
                }

                _definitionsByCode[definition.stringCode] = definition;
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (string.IsNullOrEmpty(stringCode) == true)
            {
                stringCode = name;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
