using System;
using System.Collections.Generic;
using Horror.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Horror.UI
{
    /// <summary>
    /// The tabbed settings screen (Graphics / Controls / Audio). It only connects UI controls to
    /// GameSettings: every change is applied straight away and saved when the menu closes.
    ///
    /// The menu built by "Horror > Build Main Menu Scene" has everything wired up. If you restyle it,
    /// keep the controls and just drag them into the matching fields here. Any field left empty
    /// is skipped, so you can remove a setting from the UI without touching code.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        // Slider ranges live here so the UI and the code can't disagree.
        public const int FpsMin = 30;
        public const int FpsMax = 240;
        public const int FpsUnlimitedValue = FpsMax + 10; // the last notch on the slider means "no limit"
        public const float FovMin = 60f;
        public const float FovMax = 120f;
        public const float SensitivityMin = 0.1f;
        public const float SensitivityMax = 5f;

        [Header("Tabs (same order in both lists)")]
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private GameObject[] tabPages;
        [SerializeField] private Color activeTabColor = new Color(0.45f, 0.08f, 0.08f);
        [SerializeField] private Color inactiveTabColor = new Color(0.18f, 0.18f, 0.18f);

        [Header("Graphics")]
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Dropdown windowModeDropdown;
        [SerializeField] private Toggle vSyncToggle;
        [SerializeField] private Slider fpsLimitSlider;
        [SerializeField] private Text fpsLimitValue;
        [SerializeField] private Slider fovSlider;
        [SerializeField] private Text fovValue;
        [SerializeField] private Dropdown qualityDropdown;

        [Header("Controls")]
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private Text mouseSensitivityValue;
        [SerializeField] private Slider controllerSensitivitySlider;
        [SerializeField] private Text controllerSensitivityValue;
        [SerializeField] private Toggle invertYToggle;
        [SerializeField] private Button resetKeysButton;

        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Text masterVolumeValue;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Text musicVolumeValue;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text sfxVolumeValue;
        [SerializeField] private Slider npcVolumeSlider;
        [SerializeField] private Text npcVolumeValue;

        [Header("Bottom bar")]
        [SerializeField] private Button resetAllButton;
        [SerializeField] private Button backButton;

        /// <summary>The Back button was pressed. MainMenu (or a pause menu) listens to this to close the screen.</summary>
        public event Action BackPressed;

        /// <summary>True while a key rebind is waiting for input; menus shouldn't treat Escape as "back" then.</summary>
        public bool IsBusy => KeyRebindRow.IsBusy;

        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private int currentTab;
        private bool initialized;

        private static SettingsData S => GameSettings.Current;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            RefreshAll();
            ShowTab(currentTab);
        }

        private void OnDisable()
        {
            // Saving once on close instead of on every slider tick.
            GameSettings.Save();
        }

        public void ShowTab(int index)
        {
            if (tabPages == null || tabPages.Length == 0) return;
            currentTab = Mathf.Clamp(index, 0, tabPages.Length - 1);

            for (int i = 0; i < tabPages.Length; i++)
            {
                if (tabPages[i] != null) tabPages[i].SetActive(i == currentTab);
                if (tabButtons != null && i < tabButtons.Length && tabButtons[i] != null && tabButtons[i].image != null)
                    tabButtons[i].image.color = i == currentTab ? activeTabColor : inactiveTabColor;
            }
        }

        /// <summary>Selects the first tab button, so gamepad / keyboard navigation has somewhere to start.</summary>
        public void SelectFirst()
        {
            if (EventSystem.current != null && tabButtons != null && tabButtons.Length > currentTab && tabButtons[currentTab] != null)
                EventSystem.current.SetSelectedGameObject(tabButtons[currentTab].gameObject);
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;

            // Tabs
            if (tabButtons != null)
            {
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    int index = i; // captured copy for the lambda
                    if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => ShowTab(index));
                }
            }

            // Graphics
            FillDropdown(windowModeDropdown, new[] { "Fullscreen", "Borderless window", "Windowed" });
            FillDropdown(qualityDropdown, new[] { "Low", "Medium", "High" });
            BuildResolutionList();

            OnDropdown(resolutionDropdown, i =>
            {
                if (i < 0 || i >= resolutions.Count) return;
                S.resolutionWidth = resolutions[i].x;
                S.resolutionHeight = resolutions[i].y;
            });
            OnDropdown(windowModeDropdown, i => S.windowMode = (WindowMode)i);
            OnDropdown(qualityDropdown, i => S.quality = (QualityPreset)i);
            OnToggle(vSyncToggle, on => { S.vSync = on; UpdateFpsInteractable(); });

            SetupSlider(fpsLimitSlider, FpsMin, FpsUnlimitedValue, true);
            OnSlider(fpsLimitSlider, v =>
            {
                S.fpsLimit = v >= FpsUnlimitedValue ? 0 : Mathf.RoundToInt(v);
                SetText(fpsLimitValue, FpsLabel());
            });

            SetupSlider(fovSlider, FovMin, FovMax, true);
            OnSlider(fovSlider, v => { S.fieldOfView = v; SetText(fovValue, $"{v:0}°"); });

            // Controls
            SetupSlider(mouseSensitivitySlider, SensitivityMin, SensitivityMax, false);
            OnSlider(mouseSensitivitySlider, v => { S.mouseSensitivity = v; SetText(mouseSensitivityValue, $"{v:0.00}x"); });
            SetupSlider(controllerSensitivitySlider, SensitivityMin, SensitivityMax, false);
            OnSlider(controllerSensitivitySlider, v => { S.controllerSensitivity = v; SetText(controllerSensitivityValue, $"{v:0.00}x"); });
            OnToggle(invertYToggle, on => S.invertY = on);

            if (resetKeysButton != null)
            {
                resetKeysButton.onClick.AddListener(() =>
                {
                    KeyBindings.ResetToDefaults();
                    KeyRebindRow.NotifyBindingsChanged();
                    GameSettings.Save();
                });
            }

            // Audio
            SetupVolume(masterVolumeSlider, masterVolumeValue, v => S.masterVolume = v);
            SetupVolume(musicVolumeSlider, musicVolumeValue, v => S.musicVolume = v);
            SetupVolume(sfxVolumeSlider, sfxVolumeValue, v => S.sfxVolume = v);
            SetupVolume(npcVolumeSlider, npcVolumeValue, v => S.npcVolume = v);

            // Bottom bar
            if (resetAllButton != null)
            {
                resetAllButton.onClick.AddListener(() =>
                {
                    GameSettings.ResetToDefaults();
                    RefreshAll();
                    KeyRebindRow.NotifyBindingsChanged();
                });
            }
            if (backButton != null) backButton.onClick.AddListener(() => BackPressed?.Invoke());
        }

        /// <summary>Moves every control to the saved value without triggering its change handler.</summary>
        public void RefreshAll()
        {
            SettingsData s = S;

            if (resolutionDropdown != null)
                resolutionDropdown.SetValueWithoutNotify(FindResolutionIndex(s));
            if (windowModeDropdown != null) windowModeDropdown.SetValueWithoutNotify((int)s.windowMode);
            if (qualityDropdown != null) qualityDropdown.SetValueWithoutNotify((int)s.quality);
            if (vSyncToggle != null) vSyncToggle.SetIsOnWithoutNotify(s.vSync);

            if (fpsLimitSlider != null) fpsLimitSlider.SetValueWithoutNotify(s.fpsLimit > 0 ? s.fpsLimit : FpsUnlimitedValue);
            SetText(fpsLimitValue, FpsLabel());
            UpdateFpsInteractable();

            if (fovSlider != null) fovSlider.SetValueWithoutNotify(s.fieldOfView);
            SetText(fovValue, $"{s.fieldOfView:0}°");

            if (mouseSensitivitySlider != null) mouseSensitivitySlider.SetValueWithoutNotify(s.mouseSensitivity);
            SetText(mouseSensitivityValue, $"{s.mouseSensitivity:0.00}x");
            if (controllerSensitivitySlider != null) controllerSensitivitySlider.SetValueWithoutNotify(s.controllerSensitivity);
            SetText(controllerSensitivityValue, $"{s.controllerSensitivity:0.00}x");
            if (invertYToggle != null) invertYToggle.SetIsOnWithoutNotify(s.invertY);

            RefreshVolume(masterVolumeSlider, masterVolumeValue, s.masterVolume);
            RefreshVolume(musicVolumeSlider, musicVolumeValue, s.musicVolume);
            RefreshVolume(sfxVolumeSlider, sfxVolumeValue, s.sfxVolume);
            RefreshVolume(npcVolumeSlider, npcVolumeValue, s.npcVolume);
        }

        // ---------- resolution ----------

        private void BuildResolutionList()
        {
            resolutions.Clear();
            foreach (Resolution r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (!resolutions.Contains(size)) resolutions.Add(size); // the same size appears once per refresh rate
            }

            // Make sure the current size is in the list (e.g. the editor's Game view).
            var current = new Vector2Int(Screen.width, Screen.height);
            if (!resolutions.Contains(current)) resolutions.Add(current);

            resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            var labels = new List<string>(resolutions.Count);
            foreach (Vector2Int size in resolutions) labels.Add($"{size.x} x {size.y}");
            FillDropdown(resolutionDropdown, labels.ToArray());
        }

        private int FindResolutionIndex(SettingsData s)
        {
            Vector2Int wanted = s.resolutionWidth > 0 && s.resolutionHeight > 0
                ? new Vector2Int(s.resolutionWidth, s.resolutionHeight)
                : new Vector2Int(Screen.width, Screen.height);

            int index = resolutions.IndexOf(wanted);
            return index >= 0 ? index : resolutions.Count - 1;
        }

        // ---------- helpers ----------

        private string FpsLabel() => S.fpsLimit > 0 ? S.fpsLimit.ToString() : "Unlimited";

        private void UpdateFpsInteractable()
        {
            // The FPS limit does nothing while VSync is on, so grey it out to avoid confusion.
            if (fpsLimitSlider != null) fpsLimitSlider.interactable = !S.vSync;
            if (fpsLimitValue != null && S.vSync) fpsLimitValue.text = "VSync on";
            else SetText(fpsLimitValue, FpsLabel());
        }

        private void SetupVolume(Slider slider, Text label, Action<float> write)
        {
            SetupSlider(slider, 0f, 100f, true);
            OnSlider(slider, v =>
            {
                write(v / 100f);
                SetText(label, $"{v:0}%");
            });
        }

        private static void RefreshVolume(Slider slider, Text label, float value01)
        {
            if (slider != null) slider.SetValueWithoutNotify(value01 * 100f);
            SetText(label, $"{value01 * 100f:0}%");
        }

        private static void SetupSlider(Slider slider, float min, float max, bool wholeNumbers)
        {
            if (slider == null) return;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
        }

        private static void FillDropdown(Dropdown dropdown, string[] options)
        {
            if (dropdown == null) return;
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
        }

        // Each change handler writes into the settings, then applies them right away.
        private static void OnSlider(Slider slider, Action<float> write)
        {
            if (slider != null) slider.onValueChanged.AddListener(v => { write(v); GameSettings.Apply(); });
        }

        private static void OnToggle(Toggle toggle, Action<bool> write)
        {
            if (toggle != null) toggle.onValueChanged.AddListener(v => { write(v); GameSettings.Apply(); });
        }

        private static void OnDropdown(Dropdown dropdown, Action<int> write)
        {
            if (dropdown != null) dropdown.onValueChanged.AddListener(i => { write(i); GameSettings.Apply(); });
        }

        private static void SetText(Text text, string value)
        {
            if (text != null) text.text = value;
        }
    }
}
