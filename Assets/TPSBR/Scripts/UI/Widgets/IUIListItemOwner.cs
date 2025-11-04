using UnityEngine.EventSystems;

namespace TPSBR.UI
{
    internal interface IUIListItemOwner
    {
        void BeginSlotDrag(UIListItem slot, PointerEventData eventData);
        void UpdateSlotDrag(PointerEventData eventData);
        void EndSlotDrag(UIListItem slot, PointerEventData eventData);
        void HandleSlotDrop(UIListItem source, UIListItem target);
        void HandleSlotDropOutside(UIListItem slot, PointerEventData eventData);
        void HandleSlotSelected(UIListItem slot);
        void HandleSlotPointerEnter(UIListItem slot, PointerEventData eventData);
        void HandleSlotPointerExit(UIListItem slot);
        void HandleSlotPointerMove(UIListItem slot, PointerEventData eventData);
    }
}
