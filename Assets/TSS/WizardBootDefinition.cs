using UnityEngine;

namespace TSS.Data
{
    [CreateAssetMenu(fileName = "WizardBootDefinition", menuName = "TSS/Data Definitions/Wizard Boot")]
    public class WizardBootDefinition : ItemDefinition
    {
        public override ESlotCategory SlotCategory => ESlotCategory.LowerBody;
    }
}
