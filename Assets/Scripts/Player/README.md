# First-person player

Heavy-feeling first-person movement and camera for the horror game, built on Unity's `CharacterController`.
Works with either the new Input System package or the old Input Manager, with no input asset to set up.

## Quick setup

1. In the Hierarchy, right-click and choose **Horror > First Person Player** (or use the **GameObject > Horror** menu).
   This creates the whole rig and moves your Main Camera into it.
2. Make sure there's a floor with a collider under it, and press Play.

If you'd rather build it by hand:

```
Player            CharacterController (radius 0.3, height 1.8, center 0/0.9/0)
                  PlayerInputReader, PlayerStamina, PlayerMovement, PlayerDebugHUD (optional)
└── CameraRoot    PlayerCamera            (local position 0, 1.68, 0)
    └── Main Camera                       (local position 0, 0, 0; near clip 0.05)
```

The Player's pivot is its feet. References between the scripts fill in automatically.

## Controls

| Action | Keyboard / mouse | Gamepad |
| --- | --- | --- |
| Move | WASD | Left stick |
| Look | Mouse | Right stick |
| Sprint | Left Shift (hold) | Left stick click |
| Jump | Space | A / Cross |
| Crouch (toggle) | C or Left Ctrl | B / Circle |
| Prone (toggle) | Z | Right stick click |
| Free / lock cursor | Esc / left click | |

Pressing jump while crouched or prone stands you up one level instead of jumping.
Holding sprint while crouched stands you up and starts running.

## How it behaves

- **Weight:** speed ramps up and down instead of snapping, there's very little air control, and you fall faster than you rise.
  Hard landings cost momentum and dip the camera.
- **Stamina:** sprinting drains it (18/s) and a jump costs 15. It starts coming back 1.25 s after you stop, faster while crouched or prone.
  Running it to zero leaves you exhausted: no sprinting or jumping, slightly slower walking, and heavier breathing until it climbs back to 35.
- **Stances:** standing, crouching and prone change height gradually. Before getting taller the script checks for room,
  so you can't stand up under a table or out of a vent. Prone limits how far up you can look and slows turning.
- **Camera:** smoothed look, headbob tied to distance walked, sway that trails fast mouse moves, a slight lean into strafes,
  a landing dip, and idle breathing that gets stronger as stamina runs out.

Every number is exposed in the Inspector with a tooltip. To make it feel heavier, lower `acceleration`, `deceleration`
and `lookSmoothing`; to make it snappier, raise them.

## Hooks for later

`PlayerMovement` raises events you can attach sound or AI to:

- `Footstep(stance, isSprinting)`: once per stride on the ground, for footstep audio or letting monsters hear you.
- `Landed(impactSpeed)`, `Jumped`, `StanceChanged(stance)`.

`PlayerStamina` raises `Changed(normalized)`, `ExhaustedStarted` and `ExhaustedEnded`, handy for a stamina bar or panting sounds.

## Notes

- `PlayerDebugHUD` draws a plain stamina bar and status line. Delete it once you have a real HUD.
- If the player has colliders on child objects, put them on their own layer and remove that layer from
  `PlayerMovement > Obstacle Mask` (the script already ignores its own colliders, so this is only needed for odd setups).
