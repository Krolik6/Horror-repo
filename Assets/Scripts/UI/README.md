# Main menu and settings

## First time setup (one click)

1. Open the project in Unity and wait for scripts to compile.
2. In the top menu bar click **Horror > Build Main Menu Scene**.
3. That creates `Assets/Scenes/MainMenu.unity`, opens it, and makes it the first scene of the build.
4. Press Play.

**Play** loads `SampleScene`. To load a different scene, select the **Canvas** object and change
*Game Scene Name* on the **Main Menu** component. The scene must be in *File > Build Profiles > Scene List*.

Running the menu item again rebuilds the scene from scratch (it asks first), so restyle the scene
by hand only once you're happy with the layout.

## What's where

| File | What it does |
| --- | --- |
| `Settings/SettingsData.cs` | Every setting and its default value. |
| `Settings/GameSettings.cs` | Loads, saves (PlayerPrefs) and applies settings. Runs automatically when the game starts. |
| `Settings/GraphicsQuality.cs` | Low / Medium / High presets. Edit the numbers here. |
| `Settings/KeyBindings.cs` | Rebindable keys and their defaults. |
| `Settings/AudioVolumes.cs` | Volume placeholder, with the FMOD code ready to uncomment. |
| `Settings/CameraFovFromSettings.cs` | Add to the player's Camera so it follows the FOV setting. |
| `UI/MainMenu.cs` | Play / Settings / Credits / Quit and the Back key. |
| `UI/SettingsMenu.cs` | Connects the settings screen's sliders, dropdowns and checkboxes to the settings. |
| `UI/KeyRebindRow.cs` | One "click and press a key" row in the Controls tab. |
| `Editor/MainMenuSceneBuilder.cs` | The **Horror > Build Main Menu Scene** menu item. |

## Using the settings in gameplay code

```csharp
using Horror.Settings;

float look = mouseDelta * GameSettings.Current.mouseSensitivity;
if (GameSettings.Current.invertY) look.y = -look.y;

if (KeyBindings.WasPressedThisFrame(GameAction.Jump)) Jump();
bool sprinting = KeyBindings.IsPressed(GameAction.Sprint);
```

## Good to know

- Resolution and window mode only change in a **built game**. In the editor the Game view ignores them.
- The FPS limit is greyed out while VSync is on, because VSync caps the frame rate itself.
- FOV is horizontal (90 = the usual PC default) and converted for Unity's camera automatically.
- Saved settings live in PlayerPrefs. To wipe them: *Edit > Clear All PlayerPrefs*.
