using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Horror.Player
{
    /// <summary>
    /// Reads keyboard, mouse and gamepad input from whichever input system the project has enabled
    /// (new Input System package or the legacy Input Manager). No input action asset is needed.
    /// Runs before the other player scripts so they always see this frame's input.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        [Tooltip("Lock and hide the cursor when the game starts. Escape releases it, clicking locks it again.")]
        [SerializeField] private bool lockCursor = true;

        [Tooltip("Gamepad right stick look speed. With the default camera sensitivity, 75 is about 150 degrees per second.")]
        [SerializeField] private float gamepadLookScale = 75f;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool CrouchPressed { get; private set; }
        public bool PronePressed { get; private set; }

        private void Start()
        {
            if (lockCursor)
                SetCursorLocked(true);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            ReadInputSystem();
#elif ENABLE_LEGACY_INPUT_MANAGER
            ReadLegacyInput();
#endif
            // Ignore look input while the cursor is free so the view doesn't spin in menus.
            if (lockCursor && Cursor.lockState != CursorLockMode.Locked)
                Look = Vector2.zero;
        }

#if ENABLE_INPUT_SYSTEM
        private void ReadInputSystem()
        {
            Vector2 move = Vector2.zero;
            Vector2 look = Vector2.zero;
            bool sprint = false, jump = false, crouch = false, prone = false;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                move.x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
                move.y = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
                sprint = kb.leftShiftKey.isPressed;
                jump = kb.spaceKey.wasPressedThisFrame;
                crouch = kb.cKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame;
                prone = kb.zKey.wasPressedThisFrame;

                if (lockCursor && kb.escapeKey.wasPressedThisFrame)
                    SetCursorLocked(false);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                // Scaled to roughly match the legacy "Mouse X/Y" axes so sensitivity feels the same.
                look += mouse.delta.ReadValue() * 0.1f;

                if (lockCursor && mouse.leftButton.wasPressedThisFrame)
                    SetCursorLocked(true);
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                move += pad.leftStick.ReadValue();
                look += pad.rightStick.ReadValue() * gamepadLookScale * Time.deltaTime;
                sprint |= pad.leftStickButton.isPressed;
                jump |= pad.buttonSouth.wasPressedThisFrame;
                crouch |= pad.buttonEast.wasPressedThisFrame;
                prone |= pad.rightStickButton.wasPressedThisFrame;
            }

            Move = Vector2.ClampMagnitude(move, 1f);
            Look = look;
            SprintHeld = sprint;
            JumpPressed = jump;
            CrouchPressed = crouch;
            PronePressed = prone;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        private void ReadLegacyInput()
        {
            Move = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
            Look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            SprintHeld = Input.GetKey(KeyCode.LeftShift);
            JumpPressed = Input.GetKeyDown(KeyCode.Space);
            CrouchPressed = Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl);
            PronePressed = Input.GetKeyDown(KeyCode.Z);

            if (lockCursor && Input.GetKeyDown(KeyCode.Escape))
                SetCursorLocked(false);
            if (lockCursor && Input.GetMouseButtonDown(0))
                SetCursorLocked(true);
        }
#endif

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
