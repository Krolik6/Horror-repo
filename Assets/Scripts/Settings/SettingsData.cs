using System;
using System.Collections.Generic;
using UnityEngine;

namespace Horror.Settings
{
    public enum WindowMode
    {
        Fullscreen,
        Borderless,
        Windowed
    }

    public enum QualityPreset
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Every player setting in one plain class. It is saved as JSON in PlayerPrefs by GameSettings.
    /// To add a new setting: add a field here with its default value, then show it in SettingsMenu.
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        // Graphics
        [Tooltip("0 means the monitor's native resolution.")]
        public int resolutionWidth;
        public int resolutionHeight;
        public WindowMode windowMode = WindowMode.Borderless;
        public bool vSync = true;
        [Tooltip("0 means no limit.")]
        public int fpsLimit;
        [Tooltip("Horizontal field of view in degrees, like most PC games show it.")]
        public float fieldOfView = 90f;
        public QualityPreset quality = QualityPreset.High;

        // Controls
        public float mouseSensitivity = 1f;
        public float controllerSensitivity = 1f;
        public bool invertY;
        public List<KeyBindingEntry> keyBindings = new List<KeyBindingEntry>();

        // Audio (0..1). Saved now, hooked up to FMOD buses later.
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public float npcVolume = 1f;
    }

    [Serializable]
    public class KeyBindingEntry
    {
        public GameAction action;
        // Stored by name (e.g. "W", "LeftShift") so saves survive Input System updates.
        public string key;
    }

}
