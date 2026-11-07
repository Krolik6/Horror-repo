using System.Collections.Generic;
using System.IO;
using Horror.Settings;
using Horror.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Horror.EditorTools
{
    /// <summary>
    /// Menu item "Horror > Build Main Menu Scene". Creates Assets/Scenes/MainMenu.unity with a working
    /// main menu and tabbed settings screen, wires every reference, and adds the scene to the build
    /// as the first scene. The look is deliberately plain: it's a functional base to restyle later.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameSceneName = "SampleScene";

        private static readonly Color BackgroundColor = new Color(0.03f, 0.03f, 0.04f, 1f);
        private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.09f, 0.97f);
        private static readonly Color ButtonColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        private static readonly Color TextColor = new Color(0.9f, 0.88f, 0.85f, 1f);
        private static readonly Color DimTextColor = new Color(0.6f, 0.58f, 0.55f, 1f);

        private const int RowHeight = 50;
        private const int LabelWidth = 380;
        private const int FontSize = 26;

        private static DefaultControls.Resources resources;
        private static Font font;

        [MenuItem("Horror/Build Main Menu Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Build Main Menu Scene",
                    $"{ScenePath} already exists. Replace it? Changes you made to that scene will be lost.",
                    "Replace", "Cancel"))
                return;

            LoadResources();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();
            Transform canvas = CreateCanvas();

            Stretch(NewImage("Background", canvas, BackgroundColor).rectTransform);

            var menuRoot = canvas.gameObject;
            MainMenu mainMenu = menuRoot.AddComponent<MainMenu>();

            // ---------- main panel ----------
            GameObject mainPanel = NewObject("MainPanel", canvas);
            Stretch((RectTransform)mainPanel.transform);
            var mainLayout = mainPanel.AddComponent<VerticalLayoutGroup>();
            ConfigureVertical(mainLayout, 22, TextAnchor.MiddleCenter, expandWidth: false);

            Text title = NewText("Title", mainPanel.transform, "HORROR", 96, TextAnchor.MiddleCenter);
            SetSize(title.gameObject, 1200, 180);

            Button play = NewButton("PlayButton", mainPanel.transform, "Play", 420, 72);
            Button settings = NewButton("SettingsButton", mainPanel.transform, "Settings", 420, 72);
            Button credits = NewButton("CreditsButton", mainPanel.transform, "Credits", 420, 72);
            Button quit = NewButton("QuitButton", mainPanel.transform, "Quit", 420, 72);

            // ---------- settings panel ----------
            GameObject settingsPanel = BuildSettingsPanel(canvas, out SettingsMenu settingsMenu);

            // ---------- credits panel ----------
            GameObject creditsPanel = NewPanel("CreditsPanel", canvas, 1000, 700);
            var creditsLayout = creditsPanel.AddComponent<VerticalLayoutGroup>();
            ConfigureVertical(creditsLayout, 20, TextAnchor.UpperCenter, expandWidth: true);
            creditsLayout.padding = new RectOffset(40, 40, 30, 30);
            SetSize(NewText("Title", creditsPanel.transform, "Credits", 48, TextAnchor.MiddleCenter).gameObject, -1, 80);
            Text creditsBody = NewText("CreditsText", creditsPanel.transform,
                "Game design & programming\nYour Name\n\nArt\nYour Name\n\nSound\nYour Name\n\n(Edit this text on the CreditsText object.)",
                FontSize, TextAnchor.UpperCenter);
            creditsBody.GetComponent<LayoutElement>().flexibleHeight = 1;
            Button creditsBack = NewButton("BackButton", creditsPanel.transform, "Back", -1, 64);

            // ---------- wiring ----------
            SetString(mainMenu, "gameSceneName", GameSceneName);
            SetRef(mainMenu, "mainPanel", mainPanel);
            SetRef(mainMenu, "settingsPanel", settingsPanel);
            SetRef(mainMenu, "creditsPanel", creditsPanel);
            SetRef(mainMenu, "settingsMenu", settingsMenu);
            SetRef(mainMenu, "playButton", play);
            SetRef(mainMenu, "settingsButton", settings);
            SetRef(mainMenu, "creditsButton", credits);
            SetRef(mainMenu, "quitButton", quit);
            SetRef(mainMenu, "creditsBackButton", creditsBack);

            settingsPanel.SetActive(false);
            creditsPanel.SetActive(false);

            SetLayerRecursively(canvas.gameObject, LayerMask.NameToLayer("UI"));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildFirst(ScenePath);
            Selection.activeGameObject = menuRoot;

            Debug.Log($"[Horror] Main menu built at {ScenePath} and added to the build as the first scene. " +
                      $"Play loads '{GameSceneName}' (change it on the Canvas > MainMenu component).");
        }

        // =====================================================================
        // Settings
        // =====================================================================

        private static GameObject BuildSettingsPanel(Transform canvas, out SettingsMenu menu)
        {
            GameObject panel = NewPanel("SettingsPanel", canvas, 1400, 920);
            menu = panel.AddComponent<SettingsMenu>();

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            ConfigureVertical(layout, 16, TextAnchor.UpperCenter, expandWidth: true);
            layout.padding = new RectOffset(36, 36, 24, 28);

            SetSize(NewText("Title", panel.transform, "Settings", 48, TextAnchor.MiddleCenter).gameObject, -1, 70);

            // Tab bar
            GameObject tabBar = NewObject("Tabs", panel.transform);
            SetSize(tabBar, -1, 64);
            var tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 10;
            tabLayout.childControlWidth = tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = tabLayout.childForceExpandHeight = true;

            string[] tabNames = { "Graphics", "Controls", "Audio" };
            var tabButtons = new Button[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++)
                tabButtons[i] = NewButton(tabNames[i] + "Tab", tabBar.transform, tabNames[i], -1, 64);

            // Pages area
            GameObject pages = NewObject("Pages", panel.transform);
            pages.AddComponent<LayoutElement>().flexibleHeight = 1;

            Transform graphics = NewScrollPage("GraphicsPage", pages.transform, out GameObject graphicsPage);
            Transform controls = NewScrollPage("ControlsPage", pages.transform, out GameObject controlsPage);
            Transform audio = NewScrollPage("AudioPage", pages.transform, out GameObject audioPage);

            // ----- Graphics -----
            SetRef(menu, "resolutionDropdown", DropdownRow(graphics, "Resolution"));
            SetRef(menu, "windowModeDropdown", DropdownRow(graphics, "Window Mode"));
            SetRef(menu, "vSyncToggle", ToggleRow(graphics, "VSync"));
            SliderRow(graphics, "FPS Limit", out Slider fps, out Text fpsValue);
            SetRef(menu, "fpsLimitSlider", fps);
            SetRef(menu, "fpsLimitValue", fpsValue);
            SliderRow(graphics, "Field of View", out Slider fov, out Text fovValue);
            SetRef(menu, "fovSlider", fov);
            SetRef(menu, "fovValue", fovValue);
            SetRef(menu, "qualityDropdown", DropdownRow(graphics, "Graphics Quality"));

            // ----- Controls -----
            SliderRow(controls, "Mouse Sensitivity", out Slider mouse, out Text mouseValue);
            SetRef(menu, "mouseSensitivitySlider", mouse);
            SetRef(menu, "mouseSensitivityValue", mouseValue);
            SliderRow(controls, "Controller Sensitivity", out Slider pad, out Text padValue);
            SetRef(menu, "controllerSensitivitySlider", pad);
            SetRef(menu, "controllerSensitivityValue", padValue);
            SetRef(menu, "invertYToggle", ToggleRow(controls, "Invert Look Y"));

            Header(controls, "Key Bindings (click, then press a key)");
            foreach (GameAction action in KeyBindings.AllActions)
                RebindRow(controls, action);

            Transform resetKeysRow = Row(controls, "");
            SetRef(menu, "resetKeysButton", NewButton("ResetKeysButton", resetKeysRow, "Reset Key Bindings", 360, 44));

            // ----- Audio -----
            Text note = NewText("Note", audio,
                "Sound isn't hooked up yet. These volumes are saved and will drive the FMOD buses " +
                "(Master, Music, SFX, NPC) once FMOD is added. Master already controls Unity audio.",
                22, TextAnchor.MiddleLeft);
            note.color = DimTextColor;
            SetSize(note.gameObject, -1, 80);

            string[] volumeNames = { "Master", "Music", "SFX", "NPC" };
            string[] volumeFields = { "master", "music", "sfx", "npc" };
            for (int i = 0; i < volumeNames.Length; i++)
            {
                SliderRow(audio, volumeNames[i] + " Volume", out Slider s, out Text v);
                SetRef(menu, volumeFields[i] + "VolumeSlider", s);
                SetRef(menu, volumeFields[i] + "VolumeValue", v);
            }

            // Bottom bar
            GameObject bottom = NewObject("BottomBar", panel.transform);
            SetSize(bottom, -1, 64);
            var bottomLayout = bottom.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.spacing = 20;
            bottomLayout.childAlignment = TextAnchor.MiddleCenter;
            bottomLayout.childControlWidth = bottomLayout.childControlHeight = true;
            bottomLayout.childForceExpandWidth = false;
            bottomLayout.childForceExpandHeight = true;

            Button resetAll = NewButton("ResetAllButton", bottom.transform, "Reset to Defaults", 360, 64);
            NewObject("Spacer", bottom.transform).AddComponent<LayoutElement>().flexibleWidth = 1;
            Button back = NewButton("BackButton", bottom.transform, "Back", 300, 64);

            SetArray(menu, "tabButtons", tabButtons);
            SetArray(menu, "tabPages", new Object[] { graphicsPage, controlsPage, audioPage });
            SetRef(menu, "resetAllButton", resetAll);
            SetRef(menu, "backButton", back);

            controlsPage.SetActive(false);
            audioPage.SetActive(false);
            return panel;
        }

        // =====================================================================
        // Rows
        // =====================================================================

        private static Transform Row(Transform parent, string label)
        {
            GameObject row = NewObject(string.IsNullOrEmpty(label) ? "Row" : label + " Row", parent);
            SetSize(row, -1, RowHeight);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            Text text = NewText("Label", row.transform, label, FontSize, TextAnchor.MiddleLeft);
            SetSize(text.gameObject, LabelWidth, RowHeight);
            return row.transform;
        }

        private static void Header(Transform parent, string label)
        {
            Text text = NewText(label, parent, label, 30, TextAnchor.LowerLeft);
            text.fontStyle = FontStyle.Bold;
            SetSize(text.gameObject, -1, 70);
        }

        private static void SliderRow(Transform parent, string label, out Slider slider, out Text value)
        {
            Transform row = Row(parent, label);

            GameObject go = DefaultControls.CreateSlider(resources);
            go.name = "Slider";
            go.transform.SetParent(row, false);
            LayoutElement le = SetSize(go, -1, 24);
            le.flexibleWidth = 1;
            le.minWidth = 300;
            slider = go.GetComponent<Slider>();

            value = NewText("Value", row, "0", FontSize, TextAnchor.MiddleRight);
            SetSize(value.gameObject, 170, RowHeight);
        }

        private static Toggle ToggleRow(Transform parent, string label)
        {
            Transform row = Row(parent, label);

            GameObject go = DefaultControls.CreateToggle(resources);
            go.name = "Toggle";
            go.transform.SetParent(row, false);
            SetSize(go, 40, 40);

            // Make the box bigger and drop the default "Toggle" caption.
            var background = (RectTransform)go.transform.Find("Background");
            if (background != null)
            {
                background.anchorMin = Vector2.zero;
                background.anchorMax = Vector2.one;
                background.offsetMin = background.offsetMax = Vector2.zero;
                background.anchoredPosition = Vector2.zero;
            }
            Text caption = go.GetComponentInChildren<Text>();
            if (caption != null) Object.DestroyImmediate(caption.gameObject);

            return go.GetComponent<Toggle>();
        }

        private static Dropdown DropdownRow(Transform parent, string label)
        {
            Transform row = Row(parent, label);

            GameObject go = DefaultControls.CreateDropdown(resources);
            go.name = "Dropdown";
            go.transform.SetParent(row, false);
            SetSize(go, 480, 44);

            Dropdown dropdown = go.GetComponent<Dropdown>();
            dropdown.ClearOptions();

            // Default dropdown text is tiny at 1080p; scale it up.
            if (dropdown.captionText != null) StyleText(dropdown.captionText, 24);
            if (dropdown.itemText != null) StyleText(dropdown.itemText, 24);

            RectTransform template = dropdown.template;
            if (template != null)
            {
                template.sizeDelta = new Vector2(template.sizeDelta.x, 320);
                var content = (RectTransform)template.Find("Viewport/Content");
                if (content != null)
                {
                    content.sizeDelta = new Vector2(content.sizeDelta.x, 44);
                    var item = (RectTransform)content.Find("Item");
                    if (item != null) item.sizeDelta = new Vector2(item.sizeDelta.x, 44);
                }
            }
            return dropdown;
        }

        private static void RebindRow(Transform parent, GameAction action)
        {
            string name = KeyBindings.GetActionDisplayName(action);
            Transform row = Row(parent, name);
            Text label = row.Find("Label").GetComponent<Text>();

            Button button = NewButton("KeyButton", row, KeyBindings.Defaults[action].ToString(), 420, 44);
            Text keyText = button.GetComponentInChildren<Text>();

            KeyRebindRow rebind = row.gameObject.AddComponent<KeyRebindRow>();
            SetEnum(rebind, "action", (int)action);
            SetRef(rebind, "actionLabel", label);
            SetRef(rebind, "keyButton", button);
            SetRef(rebind, "keyLabel", keyText);
        }

        // =====================================================================
        // Basic building blocks
        // =====================================================================

        private static void LoadResources()
        {
            resources = new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
            };
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            go.AddComponent<AudioListener>();
        }

        private static void CreateEventSystem()
        {
            // The project uses the new Input System only, so the UI needs the Input System UI module.
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static Transform CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go.transform;
        }

        private static GameObject NewObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject NewPanel(string name, Transform parent, float width, float height)
        {
            Image image = NewImage(name, parent, PanelColor);
            RectTransform rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            return image.gameObject;
        }

        private static Image NewImage(string name, Transform parent, Color color)
        {
            GameObject go = NewObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text NewText(string name, Transform parent, string content, int size, TextAnchor alignment)
        {
            GameObject go = NewObject(name, parent);
            Text text = go.AddComponent<Text>();
            text.text = content;
            text.alignment = alignment;
            StyleText(text, size);
            go.AddComponent<LayoutElement>();
            return text;
        }

        private static void StyleText(Text text, int size)
        {
            text.font = font;
            text.fontSize = size;
            text.color = TextColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static Button NewButton(string name, Transform parent, string label, float width, float height)
        {
            GameObject go = DefaultControls.CreateButton(resources);
            go.name = name;
            go.transform.SetParent(parent, false);
            SetSize(go, width, height);

            go.GetComponent<Image>().color = ButtonColor;
            ColorBlock colors = go.GetComponent<Button>().colors;
            colors.highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            colors.selectedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            colors.colorMultiplier = 1.5f;
            go.GetComponent<Button>().colors = colors;

            Text text = go.GetComponentInChildren<Text>();
            if (text == null)
            {
                text = NewText("Text", go.transform, label, FontSize, TextAnchor.MiddleCenter);
                Stretch(text.rectTransform);
            }
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            StyleText(text, FontSize);
            return go.GetComponent<Button>();
        }

        /// <summary>A scroll view that fills its parent; returns the Content transform to add rows to.</summary>
        private static Transform NewScrollPage(string name, Transform parent, out GameObject page)
        {
            page = DefaultControls.CreateScrollView(resources);
            page.name = name;
            page.transform.SetParent(parent, false);
            Stretch((RectTransform)page.transform);
            page.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

            ScrollRect scroll = page.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            if (scroll.horizontalScrollbar != null)
            {
                Object.DestroyImmediate(scroll.horizontalScrollbar.gameObject);
                scroll.horizontalScrollbar = null;
            }
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            RectTransform content = scroll.content;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureVertical(layout, 8, TextAnchor.UpperLeft, expandWidth: true);
            layout.padding = new RectOffset(24, 24, 16, 16);

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return content;
        }

        private static void ConfigureVertical(VerticalLayoutGroup layout, float spacing, TextAnchor alignment, bool expandWidth)
        {
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = false;
        }

        /// <summary>Sets preferred size for layout groups. Pass -1 to leave a dimension alone.</summary>
        private static LayoutElement SetSize(GameObject go, float width, float height)
        {
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            if (width >= 0) le.preferredWidth = width;
            if (height >= 0)
            {
                le.preferredHeight = height;
                le.minHeight = height;
            }
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(width >= 0 ? width : rt.sizeDelta.x, height >= 0 ? height : rt.sizeDelta.y);
            return le;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            if (layer < 0) return;
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private static void AddSceneToBuildFirst(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------- serialized field wiring ----------

        private static SerializedProperty Find(SerializedObject so, string field)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) Debug.LogError($"[Horror] Field '{field}' not found on {so.targetObject.GetType().Name}.");
            return p;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string field, int index)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Find(so, field);
            if (p == null) return;
            p.enumValueIndex = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
