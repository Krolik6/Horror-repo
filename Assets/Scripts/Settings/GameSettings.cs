using System;
using UnityEngine;

namespace Horror.Settings
{
    /// <summary>
    /// The one place every script reads player settings from.
    ///
    ///   GameSettings.Current.mouseSensitivity   read a value
    ///   GameSettings.Changed += MyMethod;       get told when the player changes something
    ///
    /// Settings are loaded and applied automatically when the game starts (before the first scene),
    /// so this works in any scene without putting anything in it.
    /// </summary>
    public static class GameSettings
    {
        private const string PrefsKey = "Horror.Settings.v1";

        private static SettingsData current;
        private static bool hasSavedSettings;

        /// <summary>Raised after settings are applied. Scripts that use a setting (camera FOV, look speed) listen to this.</summary>
        public static event Action Changed;

        public static SettingsData Current
        {
            get
            {
                if (current == null) Load();
                return current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitOnGameStart()
        {
            // Reset statics in case Domain Reload is turned off in the editor.
            Changed = null;
            current = null;
            Load();
            // Don't touch the window on the very first launch: keep whatever Player Settings chose.
            Apply(applyDisplay: hasSavedSettings);
        }

        public static void Load()
        {
            hasSavedSettings = PlayerPrefs.HasKey(PrefsKey);
            current = null;

            if (hasSavedSettings)
            {
                try
                {
                    current = JsonUtility.FromJson<SettingsData>(PlayerPrefs.GetString(PrefsKey));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Settings] Saved settings were unreadable, using defaults. {e.Message}");
                }
            }

            if (current == null) current = new SettingsData();
            if (current.keyBindings == null) current.keyBindings = new System.Collections.Generic.List<KeyBindingEntry>();
        }

        public static void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
            hasSavedSettings = true;
        }

        /// <summary>Pushes the current values into Unity (screen, quality, frame rate, audio) and raises Changed.</summary>
        public static void Apply(bool applyDisplay = true)
        {
            SettingsData s = Current;

            if (applyDisplay) ApplyDisplay(s);
            GraphicsQuality.Apply(s.quality);

            // VSync wins over the FPS limit when it's on; that's how Unity (and the GPU) work.
            QualitySettings.vSyncCount = s.vSync ? 1 : 0;
            Application.targetFrameRate = s.fpsLimit > 0 ? s.fpsLimit : -1;

            AudioVolumes.Apply(s);

            Changed?.Invoke();
        }

        public static void ResetToDefaults()
        {
            current = new SettingsData();
            Apply();
            Save();
        }

        public static FullScreenMode ToFullScreenMode(WindowMode mode)
        {
            switch (mode)
            {
                case WindowMode.Fullscreen:
#if UNITY_STANDALONE_WIN
                    return FullScreenMode.ExclusiveFullScreen;
#else
                    // Exclusive fullscreen only exists on Windows; elsewhere this is the closest match.
                    return FullScreenMode.FullScreenWindow;
#endif
                case WindowMode.Windowed:
                    return FullScreenMode.Windowed;
                default:
                    return FullScreenMode.FullScreenWindow;
            }
        }

        private static void ApplyDisplay(SettingsData s)
        {
            int width = s.resolutionWidth;
            int height = s.resolutionHeight;
            if (width <= 0 || height <= 0)
            {
                Resolution native = Screen.currentResolution;
                width = native.width;
                height = native.height;
            }

            FullScreenMode mode = ToFullScreenMode(s.windowMode);

            // Skip if nothing changed, so moving an unrelated slider never makes the window flicker.
            if (width == Screen.width && height == Screen.height && mode == Screen.fullScreenMode)
                return;

            // Note: in the Unity editor this does nothing; test resolution and window mode in a build.
            Screen.SetResolution(width, height, mode);
        }
    }
}
