using UnityEngine;

namespace TSS.Data
{
    [CreateAssetMenu(fileName = "WizardHatDefinition", menuName = "TSS/Data Definitions/Wizard Hat")]
    public class WizardHatDefinition : ItemDefinition
    {
        public override ESlotCategory SlotCategory => ESlotCategory.Head;
    }
}
