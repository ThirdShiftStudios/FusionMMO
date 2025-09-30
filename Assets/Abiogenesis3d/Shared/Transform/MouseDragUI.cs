using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Abiogenesis3d
{
    public class MouseDragUI : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        const int DragMouseButton = 0;
        Vector2 lastMousePosition;
        RectTransform rectTransform;

        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsDragButtonPressed()) return;

            lastMousePosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragButtonPressed()) return;

            Vector2 currentMousePosition = eventData.position;

            currentMousePosition.x = Mathf.Clamp(currentMousePosition.x, 0, Screen.width);
            currentMousePosition.y = Mathf.Clamp(currentMousePosition.y, 0, Screen.height);

            Vector2 diff = currentMousePosition - lastMousePosition;

            rectTransform.position += (Vector3)diff;
            lastMousePosition = currentMousePosition;
        }
        static bool IsDragButtonPressed()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return false;

            return DragMouseButton switch
            {
                0 => mouse.leftButton.isPressed,
                1 => mouse.rightButton.isPressed,
                2 => mouse.middleButton.isPressed,
                _ => false
            };
        }
    }
}
