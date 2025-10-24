using TPSBR;
using TSS.Data;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class WoodAxeDefinition : ItemDefinition
    {
        [SerializeField]
        private WoodAxe _woodAxePrefab;
        public WoodAxe WoodAxePrefab => _woodAxePrefab;
        public override ushort MaxStack => 1;
        public override ESlotCategory SlotCategory => ESlotCategory.WoodAxe;
    }
}
