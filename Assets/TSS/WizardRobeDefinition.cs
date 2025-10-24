using UnityEngine;

namespace TSS.Data
{
    [CreateAssetMenu(fileName = "WizardRobeDefinition", menuName = "TSS/Data Definitions/Wizard Robe")]
    public class WizardRobeDefinition : ItemDefinition
    {
        public override ESlotCategory SlotCategory => ESlotCategory.UpperBody;
    }
}
