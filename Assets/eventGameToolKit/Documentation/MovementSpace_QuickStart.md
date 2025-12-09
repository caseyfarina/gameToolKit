# Movement Space Quick Start Guide

**Control how player input directions are interpreted**

Choose between camera-relative controls, world-space movement, custom reference transforms, or classic tank controls!

---

## Table of Contents

1. [Overview](#overview)
2. [Movement Space Options](#movement-space-options)
3. [Setup Guide](#setup-guide)
4. [Use Cases](#use-cases)
5. [Runtime Control](#runtime-control)
6. [Troubleshooting](#troubleshooting)

---

## Overview

The **Movement Space** setting on `CharacterControllerCC` determines how the player's input (WASD/joystick) translates into world movement. By default, movement is relative to the camera - pressing "forward" moves toward where the camera is looking.

**Location:** CharacterControllerCC > Movement Space section in the Inspector

---

## Movement Space Options

| Mode | Description | Best For |
|------|-------------|----------|
| **CameraRelative** | Movement relative to main camera (default) | Third-person games, action games |
| **WorldSpace** | Movement relative to world axes (Up = +Z) | Top-down games, fixed camera angles |
| **TransformRelative** | Movement relative to a custom Transform | Side-scrollers, custom camera rigs |
| **TankControls** | Left/right rotates, forward/back moves | Classic survival horror, vehicle controls |

---

## Setup Guide

### CameraRelative (Default)

No setup required - this is the default behavior.

- Pressing **W/Up** moves toward where the camera is looking
- Pressing **A/Left** moves to the camera's left
- Character automatically rotates to face movement direction

### WorldSpace

1. Select your player GameObject
2. Find **CharacterControllerCC** component
3. Set **Movement Space** to **WorldSpace**

- Pressing **W/Up** always moves toward +Z (world forward)
- Pressing **A/Left** always moves toward -X (world left)
- Character rotates to face movement direction

### TransformRelative

1. Create an empty GameObject to use as your reference (e.g., "MovementReference")
2. Position and rotate it as desired
3. Select your player GameObject
4. Set **Movement Space** to **TransformRelative**
5. Drag your reference GameObject into the **Movement Reference** field

- Movement is relative to the reference Transform's orientation
- Useful for fixed camera angles or side-scrolling sections
- If no reference is assigned, falls back to WorldSpace

### TankControls

1. Select your player GameObject
2. Set **Movement Space** to **TankControls**
3. Adjust **Tank Turn Speed** (default: 180 degrees/second)

- Pressing **A/D** or **Left/Right** rotates the character in place
- Pressing **W/S** or **Up/Down** moves forward/backward
- Character does NOT auto-rotate toward movement

---

## Use Cases

### Third-Person Adventure (CameraRelative)
```
Movement Space: CameraRelative
```
Standard third-person controls where "forward" is always toward the camera's view direction. Most intuitive for action games.

### Top-Down Shooter (WorldSpace)
```
Movement Space: WorldSpace
```
With a fixed overhead camera, world-space controls ensure consistent movement regardless of camera angle. "Up" always moves up on screen.

### Side-Scroller Section (TransformRelative)
```
Movement Space: TransformRelative
Movement Reference: [Side-view camera or empty GameObject facing +X]
```
Lock movement to a 2D plane by using a reference Transform aligned with your side-view. Player moves left/right relative to that reference.

### Classic Survival Horror (TankControls)
```
Movement Space: TankControls
Tank Turn Speed: 120
```
Resident Evil-style controls where the player rotates in place and moves forward/backward. Creates tension through deliberate movement.

---

## Runtime Control

Change movement space during gameplay via UnityEvents or scripts:

### Public Methods

| Method | Description |
|--------|-------------|
| `SetMovementSpace(MovementSpace)` | Change the movement mode |
| `SetMovementReference(Transform)` | Set the reference for TransformRelative mode |
| `SetTankTurnSpeed(float)` | Adjust turn speed for TankControls (degrees/sec) |

### Example: Switch to Tank Controls via Trigger

1. Add **InputTriggerZone** to a trigger collider
2. In **On Trigger Enter** event:
   - Drag your player GameObject
   - Select: **CharacterControllerCC > SetMovementSpace**
   - Set parameter to **TankControls**

### Properties

| Property | Description |
|----------|-------------|
| `CurrentMovementSpace` | Get the current movement space mode |
| `MovementReference` | Get the current reference Transform |

---

## Troubleshooting

### Character moves in wrong direction
- **CameraRelative:** Ensure your main camera is tagged "MainCamera"
- **TransformRelative:** Check that your reference Transform is assigned and oriented correctly
- **WorldSpace:** Remember +Z is forward, +X is right in Unity

### Character doesn't rotate in TankControls
This is expected! In TankControls mode, left/right input rotates the character instead of moving sideways. The character only faces the direction it's moving (forward/back).

### TransformRelative not working
If **Movement Reference** is null, the system falls back to WorldSpace. Make sure to assign a Transform to the field.

### Movement feels sluggish in TankControls
Increase **Tank Turn Speed** (try 180-360 degrees/second for responsive turning).

---

## Quick Reference

```
CharacterControllerCC
├── Movement Space
│   ├── Movement Space: [CameraRelative/WorldSpace/TransformRelative/TankControls]
│   ├── Movement Reference: [Transform] (for TransformRelative)
│   └── Tank Turn Speed: 180 (for TankControls)
```

**Default Values:**
- Movement Space: CameraRelative
- Tank Turn Speed: 180 degrees/second
