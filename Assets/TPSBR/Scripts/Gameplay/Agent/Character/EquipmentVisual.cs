namespace TPSBR
{
    using UnityEngine;
    using TSS.Data;

    public class EquipmentVisual : MonoBehaviour
    {
        [SerializeField]
        private ItemDefinition _itemDefinition;

        [SerializeField]
        private ESlotCategory _slotCategory = ESlotCategory.General;

        [SerializeField]
        private bool _defaultObject;

        public ItemDefinition ItemDefinition => _itemDefinition;
        public ESlotCategory SlotCategory => _slotCategory;
        public bool DefaultObject => _defaultObject;
    }
}
