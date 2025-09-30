using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gamekit3D
{
    public class StartScreenSpriteOffsetter : MonoBehaviour {

        public float spriteOffset;
        Vector3 initialPosition;
        Vector3 newPosition;

        private void Start()
        {
            initialPosition = transform.position;
        }

        void Update ()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 mousePosition = mouse.position.ReadValue();
            transform.position = new Vector3(
                initialPosition.x + spriteOffset * mousePosition.x,
                initialPosition.y + spriteOffset * mousePosition.y,
                initialPosition.z);
        }
    }
}