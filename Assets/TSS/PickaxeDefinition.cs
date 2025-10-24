using TPSBR;
using TSS.Data;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class PickaxeDefinition : ItemDefinition
    {
        [SerializeField]
        private Pickaxe _pickaxePrefab;
        public Pickaxe PickaxePrefab => _pickaxePrefab;
        public override ushort MaxStack => 1;
        public override ESlotCategory SlotCategory => ESlotCategory.Pickaxe;
    }
}
