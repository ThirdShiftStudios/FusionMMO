using TPSBR;
using TSS.Data;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class FishingPoleDefinition : WeaponDefinition
    {
        public FishingPoleWeapon FishingPolePrefab => WeaponPrefab as FishingPoleWeapon;

        public override ESlotCategory SlotCategory => ESlotCategory.FishingPole;
    }
}
