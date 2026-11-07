using System;
using Horror.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace Horror.UI
{
    /// <summary>
    /// One line in the Controls tab: "Jump  [Space]". Click the button, press a key, done.
    /// Escape cancels.
    /// </summary>
    public class KeyRebindRow : MonoBehaviour
    {
        [SerializeField] private GameAction action;
        [SerializeField] private Text actionLabel;
        [SerializeField] private Button keyButton;
        [SerializeField] private Text keyLabel;

        /// <summary>The row currently waiting for a key press, if any.</summary>
        public static KeyRebindRow Listening { get; private set; }

        /// <summary>True while waiting for a key, and on the frame it finished, so Escape doesn't also close the menu.</summary>
        public static bool IsBusy => Listening != null || Time.frameCount == lastFinishedFrame;

        /// <summary>Raised whenever any binding changes, so every row can refresh (keys can swap between rows).</summary>
        public static event Action BindingsChanged;

        private static int lastFinishedFrame = -1;
        private int startedFrame;
        private bool navigationWasEnabled;

        public GameAction BoundAction => action;

        public static void NotifyBindingsChanged() => BindingsChanged?.Invoke();

        private void Awake()
        {
            if (keyButton != null) keyButton.onClick.AddListener(StartListening);
            if (actionLabel != null) actionLabel.text = KeyBindings.GetActionDisplayName(action);
        }

        private void OnEnable()
        {
            BindingsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            BindingsChanged -= Refresh;
            if (Listening == this) Finish();
        }

        public void Refresh()
        {
            if (keyLabel == null) return;
            keyLabel.text = Listening == this
                ? "Press a key... (Esc to cancel)"
                : KeyBindings.GetKeyDisplayName(KeyBindings.Get(action));
        }

        private void StartListening()
        {
            if (Listening != null) Listening.Finish();

            Listening = this;
            // Ignore the frame we started on, otherwise pressing Enter on the button would bind Enter.
            startedFrame = Time.frameCount;

            // Stop WASD/arrows from moving the menu selection while we wait for a key.
            if (EventSystem.current != null)
            {
                navigationWasEnabled = EventSystem.current.sendNavigationEvents;
                EventSystem.current.sendNavigationEvents = false;
            }
            Refresh();
        }

        private void Update()
        {
            if (Listening != this || Time.frameCount == startedFrame) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Finish();
                return;
            }

            foreach (KeyControl control in keyboard.allKeys)
            {
                if (control == null || control.keyCode == Key.None || !control.wasPressedThisFrame)
                    continue;

                KeyBindings.Set(action, control.keyCode);
                Finish();
                return;
            }
        }

        private void Finish()
        {
            Listening = null;
            lastFinishedFrame = Time.frameCount;
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = navigationWasEnabled;
            NotifyBindingsChanged();
        }
    }
}
