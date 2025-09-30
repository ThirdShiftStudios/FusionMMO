using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;


namespace Gamekit3D
{
    public class InteractOnButton : InteractOnTrigger
    {

        public Key button = Key.X;
        public UnityEvent OnButtonPress;

        bool canExecuteButtons = false;

        protected override void ExecuteOnEnter(Collider other)
        {
            canExecuteButtons = true;
        }

        protected override void ExecuteOnExit(Collider other)
        {
            canExecuteButtons = false;
        }

        void Update()
        {
            if (!canExecuteButtons)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var keyControl = keyboard[button];
            if (keyControl != null && keyControl.wasPressedThisFrame)
            {
                OnButtonPress.Invoke();
            }
        }

    }
}
