using UnityEngine;
using UnityEngine.InputSystem;

namespace Abiogenesis3d
{
    public partial class Utils
    {
        public static Vector3 GetWorldPointAtHeight(Camera cam, Vector3 point, float height, Vector3 defaultValue = default)
        {
            Plane plane = new Plane(Vector3.down, height);

            Ray ray = cam.ScreenPointToRay(point);

            if (plane.Raycast(ray, out float rayDistance))
                return ray.GetPoint(rayDistance);

            return defaultValue;
        }

        public static Vector3 GetWorldMousePosition(Camera cam, float height, Vector3 defaultValue = default)
        {
            // TODO: extract mousePosition handling: FreezeCursorOnRightMouse.mousePosition
            return GetWorldPointAtHeight(cam, GetMousePosition(), height, defaultValue);
        }

        public static Vector3 VectorToCamSpace(Camera cam, Vector3 input)
        {
            Vector3 camRight = cam.transform.right;
            Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;

            return input.z * camForward + input.x * camRight;
        }

        static Vector3 GetMousePosition()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return Vector3.zero;

            var position = mouse.position.ReadValue();
            return new Vector3(position.x, position.y, 0f);
        }

    }
}