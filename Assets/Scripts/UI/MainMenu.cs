using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Horror.UI
{
    /// <summary>
    /// Main menu: Play, Settings, Credits, Quit. Switches between the three panels and handles
    /// "back" (Escape on keyboard, B / Circle on a gamepad).
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Tooltip("Scene loaded by Play. It must be in File > Build Profiles (Scene List).")]
        [SerializeField] private string gameSceneName = "SampleScene";

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private SettingsMenu settingsMenu;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button creditsBackButton;

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (settingsButton != null) settingsButton.onClick.AddListener(ShowSettings);
            if (creditsButton != null) creditsButton.onClick.AddListener(ShowCredits);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            if (creditsBackButton != null) creditsBackButton.onClick.AddListener(ShowMain);
            if (settingsMenu != null) settingsMenu.BackPressed += ShowMain;
        }

        private void Start()
        {
            // Coming back from gameplay the cursor may still be locked.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ShowMain();
        }

        private void Update()
        {
            if (mainPanel != null && mainPanel.activeSelf) return;
            if (settingsMenu != null && settingsMenu.IsBusy) return; // Escape is cancelling a key rebind

            bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                        || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
            if (back) ShowMain();
        }

        public void Play()
        {
            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError($"[MainMenu] Can't load scene '{gameSceneName}'. Add it to File > Build Profiles > Scene List, " +
                               "or change 'Game Scene Name' on the MainMenu object.");
                return;
            }
            SceneManager.LoadScene(gameSceneName);
        }

        public void ShowMain()
        {
            SetPanel(mainPanel);
            Select(playButton);
        }

        public void ShowSettings()
        {
            SetPanel(settingsPanel);
            if (settingsMenu != null) settingsMenu.SelectFirst();
        }

        public void ShowCredits()
        {
            SetPanel(creditsPanel);
            Select(creditsBackButton);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetPanel(GameObject active)
        {
            if (mainPanel != null) mainPanel.SetActive(mainPanel == active);
            if (settingsPanel != null) settingsPanel.SetActive(settingsPanel == active);
            if (creditsPanel != null) creditsPanel.SetActive(creditsPanel == active);
        }

        private static void Select(Selectable selectable)
        {
            if (EventSystem.current != null && selectable != null)
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
