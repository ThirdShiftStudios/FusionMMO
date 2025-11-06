using UnityEngine;

namespace TSS.Data
{
    [CreateAssetMenu(fileName = "PipeDefinition", menuName = "TSS/Data Definitions/Pipe")]
    public class PipeDefinition : ItemDefinition
    {
        public override ESlotCategory SlotCategory => ESlotCategory.Pipe;
    }
}
