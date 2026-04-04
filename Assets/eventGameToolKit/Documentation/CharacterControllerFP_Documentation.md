# CharacterControllerFP - Complete Setup Guide

**Last Updated:** February 2026
**Unity Version:** Unity 6+
**Script Location:** `Assets/eventGameToolKit/Runtime/CharacterControllers/Player/CharacterControllerFP.cs`

---

## Overview

CharacterControllerFP is a kinematic first-person character controller designed for FPS games, walking simulators, exploration games, and horror games. It uses Unity's CharacterController component for smooth, physics-independent movement with mouse/gamepad look, slope detection, moving platform support, and cursor management.

### Key Features
- Mouse and gamepad look with separate sensitivity settings
- Smooth movement with acceleration/deceleration
- Height-based jumping with anti-spam timeout
- Slope detection and automatic sliding on steep surfaces
- Moving platform support (tag/layer/both detection modes)
- Sprint functionality
- Cursor lock/unlock management with events
- Animator integration with 5 parameters
- Extensive UnityEvent system for no-code interactions
- Debug visualization with scene gizmos
- Spawn point provider support (checkpoint integration)

---

## Quick Start (5 Minutes)

**Just want a working first-person controller? Here's the fastest way:**

### Manual Setup (3 Steps)

**Step 1: Create the Player**
1. Create an empty GameObject, name it "Player", position at (0, 1, 0)
2. Add **CharacterControllerFP** component (auto-adds CharacterController and PlayerInput)

**Step 2: Create the Camera**
1. Create a child GameObject under Player, name it "PlayerCamera"
2. Add a **Camera** component
3. Set **Near Clip Plane** to `0.1` (prevents seeing inside objects)
4. Set **Field of View** to `70`
5. Position at (0, 1.6, 0) — eye height
6. Tag it as **MainCamera**
7. On CharacterControllerFP, drag PlayerCamera into the **Player Camera** field

**Step 3: Connect Input**
1. On the **PlayerInput** component:
   - Set **Actions** to `InputSystem_Actions`
   - Set **Default Map** to "Player"
   - Set **Behavior** to "Send Messages"

**Press Play!** WASD moves, mouse looks, Space jumps.

### It's Not Working?
- Make sure you have a ground object with a Collider (not trigger)
- Make sure **Ground Layer** includes your ground's layer
- Remove or disable any other camera tagged "MainCamera"
- Check the Console for error messages

---

## Required Components

### Automatically Added
When you add CharacterControllerFP, Unity automatically adds:
- **CharacterController** — Required for kinematic movement
- **PlayerInput** — Required for Input System integration

### You Must Create
1. **Camera** (child GameObject) — For first-person view
2. **Ground** — Something to stand on with a Collider

### Optional
- **Animator** — For first-person arm/weapon animations
- **AudioListener** — Usually on the camera (remove from other cameras)

---

## Complete Setup Guide

### Step 1: Create Player GameObject

1. Create an empty GameObject in your scene
2. Name it "Player"
3. Position it at (0, 1, 0) — above the ground
4. Add **CharacterControllerFP** component

### Step 2: Configure CharacterController

The script auto-configures slope limit and skin width, but verify:

| Parameter | Recommended | Description |
|-----------|-------------|-------------|
| **Slope Limit** | 45 | Auto-set from maxSlopeAngle |
| **Skin Width** | 0.08 | Auto-set if < 0.01 |
| **Center** | (0, 1, 0) | Center of capsule |
| **Radius** | 0.5 | Capsule radius |
| **Height** | 2.0 | Capsule height |

### Step 3: Create Camera

1. Right-click the Player → Create Empty, name it "PlayerCamera"
2. Add **Camera** component
3. Set **Near Clip Plane** to `0.1`
4. Set **Field of View** to `70` (adjust to taste; 60-90 is typical)
5. Set **local position** to `(0, 1.6, 0)` — simulates eye height
6. Tag as **MainCamera**
7. Add **AudioListener** (remove from any other object)
8. On CharacterControllerFP, drag this into the **Player Camera** field

### Step 4: Configure PlayerInput

1. Set **Actions** to your InputSystem_Actions asset
2. Set **Default Map** to "Player"
3. Set **Behavior** to "Send Messages"

### Step 5: Create Ground

1. Create a Plane or Cube
2. Scale it up (e.g., Plane at scale 3,1,3)
3. Make sure it has a **Collider** (not trigger)
4. Set its layer to match the controller's **Ground Layer**

---

## Hierarchy Structure

```
Player                         (CharacterControllerFP + CharacterController + PlayerInput)
└── PlayerCamera               (Camera + AudioListener, local pos 0, 1.6, 0)
    └── [Optional: Arms/Weapon model]
```

---

## Camera Setup

### Basic Setup (Camera as Child)
The simplest approach: make the Camera a child of the Player. CharacterControllerFP rotates the camera's local X rotation for pitch and the Player's Y rotation for yaw.

### Cinemachine POV Camera (Advanced)
For smooth camera effects, you can use Cinemachine instead:

1. Create a **CinemachineCamera** in the scene
2. Set its **Body** to "Do Nothing"
3. Set its **Aim** to "POV"
4. Set **Follow** and **Look At** to the Player
5. Adjust POV settings for sensitivity
6. On CharacterControllerFP, leave **Player Camera** empty (Cinemachine handles it)

**Note:** When using Cinemachine POV, you handle look rotation through Cinemachine, not through CharacterControllerFP's built-in look system. You may want to set mouseSensitivity and gamepadSensitivity to 0 to prevent double-rotation.

---

## Parameter Reference

### Movement Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Move Speed** | 6 | Base movement speed (units/second) |
| **Max Velocity** | 6 | Maximum horizontal speed cap |
| **Speed Change Rate** | 10 | Acceleration and deceleration rate |
| **Air Control Factor** | 0.5 | Movement control while airborne (0-1) |

### Sprint Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Enable Sprint** | false | Toggle sprint functionality |
| **Sprint Speed Multiplier** | 1.5 | Speed multiplier when sprinting |

### Look Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Mouse Sensitivity** | 2 | Mouse look speed (no Time.deltaTime — already frame-independent) |
| **Gamepad Sensitivity** | 100 | Gamepad stick look speed (uses Time.deltaTime) |
| **Vertical Look Limit** | 80 | Max pitch angle in degrees (30-90) |
| **Invert Y** | false | Push up to look down |

### Cursor Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Lock Cursor On Start** | true | Lock and hide cursor when game starts |
| **Cursor Toggle Key** | Escape | Key to toggle cursor lock/unlock |

### Jump Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Jump Height** | 1.2 | Jump height in meters |
| **Jump Timeout** | 0.5 | Cooldown between jumps in seconds |
| **Ground Check Distance** | 0.1 | Distance below character to check for ground |

### Ground Detection

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Ground Layer** | Everything | Layer(s) detected as ground |

### Gravity Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Gravity** | -20 | Gravity acceleration (negative = down) |
| **Terminal Velocity** | -50 | Maximum fall speed |
| **Ground Stick Force** | -1.5 | Downward force when grounded |

### Slope Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Max Slope Angle** | 45 | Maximum walkable slope angle |
| **Slope Check Distance** | 1 | Forward raycast distance for slope detection |
| **Slope Slide Speed** | 5 | Slide speed on steep slopes |

### Platform Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Platform Detection Mode** | Tag | Detect by Tag, Layer, or Both |
| **Platform Layer** | None | Layer mask for platform detection |
| **Platform Tag** | Untagged | Tag for platform detection |
| **Apply Vertical Movement** | true | Follow platform vertical movement |

### Spawn Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Use Spawn Point Providers** | true | Check for ISpawnPointProvider on Awake |

### Camera

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Player Camera** | null (auto-finds) | Camera Transform for look rotation |

### Animation

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Character Animator** | null | Optional Animator for FP animations |

---

## Animator Parameters

If you assign an Animator, the controller will automatically drive these parameters (if they exist in your Animator Controller):

| Parameter | Type | Description |
|-----------|------|-------------|
| **Speed** | Float | Current horizontal speed |
| **Grounded** | Bool | Whether character is on ground (updates on change only) |
| **VerticalVelocity** | Float | Current vertical velocity |
| **IsWalking** | Bool | True when moving on ground |
| **IsSprinting** | Bool | True when sprinting on ground |

**Important:** The controller uses `HasParameter()` checks — it won't crash if your Animator is missing parameters. Add only the ones you need.

---

## UnityEvents Reference

| Event | Type | When It Fires |
|-------|------|---------------|
| **onGrounded** | UnityEvent | Every frame while grounded |
| **onJump** | UnityEvent | When jump is initiated |
| **onLanding** | UnityEvent | When landing after being airborne |
| **onStartMoving** | UnityEvent | When character starts moving from standstill |
| **onStopMoving** | UnityEvent | When character stops moving |
| **onSteepSlope** | UnityEvent | When on a slope steeper than maxSlopeAngle |
| **onTeleport** | UnityEvent\<Vector3\> | When teleported (passes destination) |
| **onSpawnPointUsed** | UnityEvent\<Vector3\> | When spawning at a checkpoint (passes position) |
| **onCursorLockChanged** | UnityEvent\<bool\> | When cursor lock state changes (true = locked) |

### Example Event Wiring
- **onJump** → ActionPlaySound.PlayOneShot (jump sound)
- **onLanding** → ActionPlaySound.PlayOneShot (landing sound)
- **onCursorLockChanged** → GameStateManager.SetPaused (pause when cursor unlocked)
- **onStartMoving** → Enable footstep audio
- **onStopMoving** → Disable footstep audio

---

## Public Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| **TeleportTo** | `TeleportTo(Vector3 position)` | Teleport to position, keep rotation |
| **TeleportTo** | `TeleportTo(Vector3 position, Quaternion rotation)` | Teleport to position and rotation |
| **LockCursor** | `LockCursor()` | Lock and hide cursor |
| **UnlockCursor** | `UnlockCursor()` | Unlock and show cursor |
| **ToggleCursor** | `ToggleCursor()` | Toggle cursor lock state |
| **SetMoveSpeed** | `SetMoveSpeed(float)` | Change movement speed |
| **SetJumpHeight** | `SetJumpHeight(float)` | Change jump height |
| **SetJumpTimeout** | `SetJumpTimeout(float)` | Change jump cooldown |
| **SetSpeedChangeRate** | `SetSpeedChangeRate(float)` | Change acceleration |
| **SetMaxVelocity** | `SetMaxVelocity(float)` | Change max speed |
| **SetMouseSensitivity** | `SetMouseSensitivity(float)` | Change mouse sensitivity |
| **SetGamepadSensitivity** | `SetGamepadSensitivity(float)` | Change gamepad sensitivity |
| **SetInvertY** | `SetInvertY(bool)` | Toggle Y-axis inversion |
| **SetGravity** | `SetGravity(float)` | Change gravity |
| **SetTerminalVelocity** | `SetTerminalVelocity(float)` | Change max fall speed |
| **SetSlopeSlideSpeed** | `SetSlopeSlideSpeed(float)` | Change slope slide speed |
| **ResetVelocity** | `ResetVelocity()` | Zero out all velocity |

---

## Public Properties (Read-Only)

| Property | Type | Description |
|----------|------|-------------|
| **IsGrounded** | bool | Whether character is on ground |
| **IsMoving** | bool | Whether character is moving horizontally |
| **IsOnSteepSlope** | bool | Whether on steep slope |
| **IsOnPlatform** | bool | Whether on a moving platform |
| **CurrentPlatform** | Transform | Current platform (or null) |
| **CurrentSpeed** | float | Current horizontal speed |
| **IsSprinting** | bool | Whether sprinting |
| **Velocity** | Vector3 | Full velocity vector |
| **IsCursorLocked** | bool | Whether cursor is locked |
| **CameraPitch** | float | Current camera pitch angle |

---

## Gizmos

When the Player is selected in the Scene view, these gizmos are drawn:

| Gizmo | Color | Description |
|-------|-------|-------------|
| Wire Sphere (bottom) | Green/Red | Ground check radius (green = grounded) |
| Ray (down) | Green/Yellow | Platform detection ray |
| Ray (from center) | Blue | Slope normal direction |
| Ray (from center) | Red | Slide direction on steep slopes |
| Ray (from camera) | Cyan | Camera look direction |

---

## Common Scenarios

### Scenario 1: Walking Simulator

A calm exploration game with no jumping:

1. Set **Jump Height** to `0`
2. Set **Move Speed** to `3` (slow, contemplative pace)
3. Set **Enable Sprint** to `false`
4. Set **Mouse Sensitivity** to `1.5` (gentle rotation)
5. Wire **onStartMoving** → Start footstep audio
6. Wire **onStopMoving** → Stop footstep audio

### Scenario 2: FPS with Sprint

A fast-paced shooter:

1. Set **Move Speed** to `8`
2. Set **Enable Sprint** to `true`
3. Set **Sprint Speed Multiplier** to `2.0`
4. Set **Jump Height** to `1.5`
5. Set **Mouse Sensitivity** to `3`
6. Wire **onJump** → Play jump sound
7. Wire **onLanding** → Play landing sound

### Scenario 3: Exploration with Platforms

A puzzle-exploration game with moving platforms:

1. Set **Platform Detection Mode** to `Tag`
2. Set **Platform Tag** to "Platform" (create this tag)
3. Tag your moving platforms with "Platform"
4. Set **Apply Vertical Movement** to `true`
5. Add PhysicsPlatformAnimator to platforms for movement

### Scenario 4: Menu / Cursor Toggle

Toggling between gameplay and a menu:

1. Set **Lock Cursor On Start** to `true`
2. Set **Cursor Toggle Key** to `Escape`
3. Wire **onCursorLockChanged** to show/hide your pause menu
4. Or call `LockCursor()` / `UnlockCursor()` from your menu buttons

---

## Troubleshooting

### Character falls through the ground
- Check that your ground has a **Collider** component (not set to trigger)
- Check that **Ground Layer** includes your ground's layer
- Check that the CharacterController's **Center** is correct (usually 0, 1, 0)

### Camera doesn't rotate / look doesn't work
- Make sure **Player Camera** field is assigned (or Camera.main exists)
- Check that `InputSystem_Actions` has a **Look** action mapped to Mouse Delta / Right Stick
- Make sure **Behavior** on PlayerInput is "Send Messages"
- If using Cinemachine, set sensitivities to 0 and let Cinemachine handle look

### Character moves in wrong direction
- Movement is always relative to the character's forward direction
- Make sure the Player's forward (blue arrow) points the correct way
- The camera should be a **child** of the Player for correct alignment

### Cursor won't unlock
- Press the **Cursor Toggle Key** (default: Escape)
- Or call `UnlockCursor()` from a UnityEvent or script

### Jittery camera movement
- Look rotation runs in Update() (not FixedUpdate) for smoothness
- If using Cinemachine, ensure only one system controls the camera
- Check for multiple cameras or conflicting rotation scripts

### Can't jump
- Check **Ground Layer** matches your ground's layer
- Check you're not on a steep slope (slopes > maxSlopeAngle block jumping)
- Try increasing **Ground Check Distance** slightly
- Check the **Jump Timeout** — there's a cooldown between jumps

### Sprint doesn't work
- Make sure **Enable Sprint** is checked
- InputSystem_Actions needs a **Sprint** action (Left Shift / gamepad button)
- Sprint only works while grounded

### Gamepad look is too slow / too fast
- Adjust **Gamepad Sensitivity** (default 100)
- Mouse and gamepad have separate sensitivity values
- Gamepad sensitivity uses Time.deltaTime; mouse does not (mouse delta is already frame-independent)

---

## Best Practices

### Camera Setup
- Always set near clip plane to `0.1` or lower for first-person
- Field of View between 60-90 is typical (70 is a good default)
- Put the camera at eye height: local Y of 1.5-1.7

### Performance
- The controller uses SphereCast for ground detection — this is efficient
- Animator parameter checks use HasParameter() to prevent errors
- Look handling runs in Update() for responsiveness

### Level Design
- Keep corridors at least 2 units wide (player radius is 0.5)
- Place ground at Y=0 with player starting at Y=1
- Use ramps (angled cubes) for elevation changes under maxSlopeAngle

### Events
- Use onCursorLockChanged to sync with pause menus
- Use onLanding for footstep sounds and screen effects
- Use onSteepSlope to warn players about impassable terrain

---

## Technical Notes

### Look Sensitivity
Mouse input from the Input System is already a delta (pixels moved since last frame), so it does not need Time.deltaTime. Gamepad stick input is a continuous value, so it is multiplied by Time.deltaTime for frame-rate independence.

### Ground Detection
Uses the same dual-detection system as CharacterControllerCC:
1. `controller.isGrounded` (CharacterController's built-in check)
2. SphereCast downward from the bottom of the capsule

Both are OR'd together: `isGrounded = controllerGrounded || sphereHit`

### Spawn Point System
On Awake(), the controller searches for any MonoBehaviour implementing ISpawnPointProvider (e.g., GameCheckpointManager). If found with a valid spawn point, the controller teleports there BEFORE physics runs, preventing visual flickering.

### Platform System
Same implementation as CharacterControllerCC — tracks platform position/rotation deltas each frame and applies them to the character. Supports horizontal following, vertical following, and rotation following.

---

## Related Scripts

| Script | Description |
|--------|-------------|
| **CharacterControllerCC** | Third-person version with dodge, rotation smoothing |
| **GameCheckpointManager** | Implements ISpawnPointProvider for checkpoint persistence |
| **InputCheckpointZone** | Trigger zone that saves checkpoint data |
| **ISpawnPointProvider** | Interface for spawn point systems |
| **GameStateManager** | Pause management (wire to onCursorLockChanged) |
| **ActionPlaySound** | Sound effects (wire to onJump, onLanding) |
| **PhysicsPlatformAnimator** | Moving platforms that work with this controller |
| **lockMouseCursorToDisplay** | Legacy cursor lock utility (CharacterControllerFP has this built-in) |
