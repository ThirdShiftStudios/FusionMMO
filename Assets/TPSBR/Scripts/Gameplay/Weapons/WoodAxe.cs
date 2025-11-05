using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public class WoodAxe : Tool
    {
        [SerializeField]
        private WoodAxeDefinition _woodAxeDefinition;

        public WoodAxeDefinition Definition => _woodAxeDefinition;

        protected override bool ShouldHideWhenUnequipped => true;

        protected override string GetDefaultDisplayName()
        {
            if (_woodAxeDefinition != null)
            {
                return _woodAxeDefinition.Name;
            }

            return gameObject.name;
        }

        protected override Sprite GetIcon()
        {
            return _woodAxeDefinition != null ? _woodAxeDefinition.Icon : null;
        }
    }
}