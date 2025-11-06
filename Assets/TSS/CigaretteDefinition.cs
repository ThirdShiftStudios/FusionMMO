using TSS.Data;
using TPSBR.Abilities;
using Unity.Template.CompetitiveActionMultiplayer;
using UnityEngine;

namespace TPSBR
{
    public class CigaretteDefinition : WeaponDefinition
    {
        [SerializeField, Min(1)] private ushort _maxStack = 20;

        public override ESlotCategory SlotCategory => ESlotCategory.Consumable;
        public override ushort MaxStack => _maxStack;
    }
}
