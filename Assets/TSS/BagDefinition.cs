using UnityEngine;

namespace TSS.Data
{
    [CreateAssetMenu(fileName = "BagDefinition", menuName = "TSS/Data Definitions/Bag")]
    public class BagDefinition : ItemDefinition
    {
        [SerializeField]
        private int _slots = 4;

        public int Slots => _slots;
        public override ESlotCategory SlotCategory => ESlotCategory.Bag;
    }
}
