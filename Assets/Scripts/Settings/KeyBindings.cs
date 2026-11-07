using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Horror.Settings
{
    /// <summary>Everything the player can rebind. Add a new action here and it shows up in the Controls tab after rebuilding the menu.</summary>
    public enum GameAction
    {
        MoveForward,
        MoveBackward,
        MoveLeft,
        MoveRight,
        Sprint,
        Jump,
        Crouch,
        Prone,
        Interact
    }

    /// <summary>
    /// Rebindable keyboard keys, saved with the rest of the settings.
    ///
    /// In gameplay code, use these instead of hard-coded keys:
    ///   KeyBindings.IsPressed(GameAction.Sprint)
    ///   KeyBindings.WasPressedThisFrame(GameAction.Jump)
    /// </summary>
    public static class KeyBindings
    {
        public static readonly IReadOnlyDictionary<GameAction, Key> Defaults = new Dictionary<GameAction, Key>
        {
            { GameAction.MoveForward, Key.W },
            { GameAction.MoveBackward, Key.S },
            { GameAction.MoveLeft, Key.A },
            { GameAction.MoveRight, Key.D },
            { GameAction.Sprint, Key.LeftShift },
            { GameAction.Jump, Key.Space },
            { GameAction.Crouch, Key.C },
            { GameAction.Prone, Key.Z },
            { GameAction.Interact, Key.E },
        };

        public static GameAction[] AllActions => (GameAction[])Enum.GetValues(typeof(GameAction));

        public static Key Get(GameAction action)
        {
            foreach (KeyBindingEntry entry in GameSettings.Current.keyBindings)
            {
                if (entry.action == action && Enum.TryParse(entry.key, out Key key) && key != Key.None)
                    return key;
            }
            return Defaults.TryGetValue(action, out Key fallback) ? fallback : Key.None;
        }

        /// <summary>Binds a key. If another action already used that key, the two swap so nothing is left doubled up.</summary>
        public static void Set(GameAction action, Key key)
        {
            Key previous = Get(action);
            foreach (GameAction other in AllActions)
            {
                if (other != action && Get(other) == key)
                    Store(other, previous);
            }
            Store(action, key);
        }

        public static void ResetToDefaults()
        {
            GameSettings.Current.keyBindings.Clear();
        }

        public static bool IsPressed(GameAction action)
        {
            KeyControl control = GetControl(action);
            return control != null && control.isPressed;
        }

        public static bool WasPressedThisFrame(GameAction action)
        {
            KeyControl control = GetControl(action);
            return control != null && control.wasPressedThisFrame;
        }

        /// <summary>Name to show the player, following their keyboard layout when possible (e.g. "Z" on AZERTY).</summary>
        public static string GetKeyDisplayName(Key key)
        {
            if (key == Key.None) return "-";
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                string name = keyboard[key].displayName;
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return key.ToString();
        }

        /// <summary>"MoveForward" becomes "Move Forward".</summary>
        public static string GetActionDisplayName(GameAction action)
        {
            string raw = action.ToString();
            var builder = new System.Text.StringBuilder(raw.Length + 4);
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && char.IsUpper(raw[i])) builder.Append(' ');
                builder.Append(raw[i]);
            }
            return builder.ToString();
        }

        private static KeyControl GetControl(GameAction action)
        {
            Keyboard keyboard = Keyboard.current;
            Key key = Get(action);
            if (keyboard == null || key == Key.None) return null;
            return keyboard[key];
        }

        private static void Store(GameAction action, Key key)
        {
            List<KeyBindingEntry> list = GameSettings.Current.keyBindings;
            foreach (KeyBindingEntry entry in list)
            {
                if (entry.action == action)
                {
                    entry.key = key.ToString();
                    return;
                }
            }
            list.Add(new KeyBindingEntry { action = action, key = key.ToString() });
        }
    }
}
