// This script is used in the demo scene to allow on-click interactions with the Shatter Stone ore nodes.
// It supports both Unity's old Input Manager and the new Input System.

using UnityEngine;

#if ENABLE_INPUT_SYSTEM && UNITY_INPUT_SYSTEM_EXISTS
using UnityEngine.InputSystem;
#endif

namespace ShatterStone
{
    public class DemoClickInteraction : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float maxDistance = 100f;

        [Tooltip("Tag used to identify ore nodes in the scene.")]
        [SerializeField] private string oreNodeTag = "OreNode";
        [SerializeField] private string orePickupTag = "OrePickup";

        private void Update()
        {
            // Check input based on the input system available
            bool inputPressed = false;
            Vector2 pointerPos = default;

#if ENABLE_INPUT_SYSTEM && UNITY_INPUT_SYSTEM_EXISTS
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                inputPressed = true;
                pointerPos = Mouse.current.position.ReadValue();
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                inputPressed = true;
                pointerPos = Input.mousePosition;
            }
#endif

            if (!inputPressed) return;

            Ray ray = mainCamera.ScreenPointToRay(pointerPos);

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                if (hit.collider.CompareTag(oreNodeTag))
                {
                    var oreNode = hit.collider.GetComponent<OreNode>();
                    oreNode?.Interact();
                }

                if (hit.collider.CompareTag(orePickupTag))
                {
                    var orePickup = hit.collider.GetComponent<PickupController>();
                    orePickup?.CollectItem();
                }
            }
        }
    }
}
