using UnityEngine;
using UnityEngine.InputSystem;

namespace Abiogenesis3d
{
    public class ToggleEnabled : MonoBehaviour
    {
        public Behaviour behaviour;
        public Key toggleKey;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var keyControl = keyboard[toggleKey];
            if (keyControl != null && keyControl.wasPressedThisFrame)
                behaviour.enabled = !behaviour.enabled;
        }
    }
}
