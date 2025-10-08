using UnityEngine.EventSystems;

namespace TPSBR.UI
{
    internal interface IUIItemSlotOwner
    {
        void BeginSlotDrag(UIItemSlot slot, PointerEventData eventData);
        void UpdateSlotDrag(PointerEventData eventData);
        void EndSlotDrag(UIItemSlot slot, PointerEventData eventData);
        void HandleSlotDrop(UIItemSlot source, UIItemSlot target);
        void HandleSlotDropOutside(UIItemSlot slot, PointerEventData eventData);
    }
}
