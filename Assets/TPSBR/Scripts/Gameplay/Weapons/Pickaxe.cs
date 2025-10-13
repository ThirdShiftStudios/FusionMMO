using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public class Pickaxe : Tool
    {
        [SerializeField]
        private PickaxeDefinition _pickaxeDefinition;

        public PickaxeDefinition Definition => _pickaxeDefinition;

        protected override string GetDefaultDisplayName()
        {
            if (_pickaxeDefinition != null)
            {
                return _pickaxeDefinition.Name;
            }

            return gameObject.name;
        }

        protected override Sprite GetIcon()
        {
            return _pickaxeDefinition != null ? _pickaxeDefinition.IconSprite : null;
        }
    }
}
