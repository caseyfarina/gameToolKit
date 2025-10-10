# PhysicsCharacterController - Tutorial & Usage Guide

This guide explains how to set up and use the PhysicsCharacterController component for creating physics-based character movement with capsule colliders.

---

## Component Overview

**PhysicsCharacterController** - Rigidbody-based character controller
- Physics-based movement using forces (not transform manipulation)
- Camera-relative movement controls
- Ground detection and slope handling
- Jump mechanics with grounded checking
- Animation integration support
- Unity Input System integration
- Event system for movement states

---

## IMPORTANT: Required Setup Steps

### ⚠️ Critical Requirements

For the character to move, you **MUST** have all of these:

1. **Rigidbody Component** - Add Component → Rigidbody
   - The script will auto-configure it, but it must exist first

2. **Capsule Collider Component** - Add Component → Capsule Collider
   - The script will auto-find it if on the same GameObject

3. **Input System Setup** - Your scene must have:
   - PlayerInput component on the character GameObject
   - Input Action Asset with "Move" and "Jump" actions configured
   - Actions must be set to "Send Messages" behavior

4. **Main Camera** - Scene must have a camera tagged as "MainCamera"

5. **Ground** - Objects with colliders on the Ground layer

**If movement isn't working, check these 5 things first!**

---

## Quick Start Guide

### Step 1: Create Character GameObject

1. Create Empty GameObject (name it "Player")
2. Add Component → **Rigidbody**
3. Add Component → **Capsule Collider**
4. Add Component → **PhysicsCharacterController**

### Step 2: Configure Input System

#### Option A: Using Existing Input Actions
1. Add Component → **Player Input**
2. Drag your Input Action Asset into the "Actions" field
3. Set Behavior to **"Send Messages"** (NOT "Invoke Unity Events")
4. Your Input Action Asset must have:
   - Action named **"Move"** (Vector2 type)
   - Action named **"Jump"** (Button type)

#### Option B: Create New Input Actions
1. Right-click in Project → Create → Input Actions
2. Name it "CharacterInputActions"
3. Create Action Map named "Player"
4. Add Action: **"Move"**
   - Action Type: Value
   - Control Type: Vector 2
   - Binding: WASD or Left Stick
5. Add Action: **"Jump"**
   - Action Type: Button
   - Binding: Space or South Button (A/Cross)
6. Save Asset
7. Add to Player Input component as described in Option A

### Step 3: Verify Ground Layer

1. Create a ground plane/cube
2. Set its layer to "Default" (layer 0) or create a "Ground" layer
3. On PhysicsCharacterController:
   - Set "Ground Layer" to match your ground objects

### Step 4: Add Camera (if not already in scene)

1. Ensure you have a Camera in the scene
2. Make sure it's tagged "MainCamera" (Inspector → Tag dropdown)

### Step 5: Test

Press Play and use WASD to move, Space to jump!

---

## Common Setup Problems & Solutions

### Problem: Character doesn't move at all

**Cause 1: Missing Rigidbody**
- ✅ Solution: Add Component → Rigidbody to the character

**Cause 2: Input System not configured**
- ✅ Solution: Check Player Input component exists
- ✅ Solution: Verify Behavior is set to "Send Messages"
- ✅ Solution: Ensure action names match exactly: "Move" and "Jump"

**Cause 3: Rigidbody is Kinematic**
- ✅ Solution: In Rigidbody component, uncheck "Is Kinematic"

**Cause 4: Constraints are wrong**
- ✅ Solution: Script auto-configures this, but verify:
  - Freeze Position: X, Y, Z should be UNCHECKED
  - Freeze Rotation: X, Y, Z should be CHECKED

**Cause 5: No Main Camera**
- ✅ Solution: Make sure scene has a camera tagged "MainCamera"

### Problem: Character falls through ground

**Cause 1: No collider on ground**
- ✅ Solution: Add Mesh Collider or Box Collider to ground objects

**Cause 2: Wrong Ground Layer**
- ✅ Solution: Check "Ground Layer" matches your ground objects' layer

**Cause 3: Capsule Collider missing**
- ✅ Solution: Add Capsule Collider to character

### Problem: Character moves but in wrong direction

**Cause: No Main Camera**
- ✅ Solution: Movement is camera-relative, needs MainCamera to exist

### Problem: Jump doesn't work

**Cause 1: Not grounded**
- ✅ Solution: Check ground detection distance in Inspector
- ✅ Solution: Increase "Ground Check Distance" to 0.2 or 0.3

**Cause 2: Jump action not configured**
- ✅ Solution: Verify Input Action named "Jump" exists and is bound to a key

**Cause 3: Jump Force too low**
- ✅ Solution: Increase "Jump Force" value (try 15-20)

---

## Parameter Reference

### Movement Settings

#### Move Force
- How much force is applied to move the character
- Default: 10
- Higher = faster acceleration
- Lower = slower, heavier feeling
- **TIP:** 5-15 works well for most characters

#### Max Velocity
- Maximum horizontal movement speed
- Default: 8
- Character cannot move faster than this
- **TIP:** 5-10 for walking, 15-20 for running

#### Air Control Factor
- How much control you have while airborne (0-1)
- Default: 0.5 (half control in air)
- 0 = no air control (realistic)
- 1 = full air control (arcade feel)

### Jump Settings

#### Jump Force
- How much upward force when jumping
- Default: 12
- Higher = jumps higher
- **TIP:** 10-15 for normal jump, 20+ for super jump

#### Ground Check Distance
- How far below capsule to check for ground
- Default: 0.1
- Increase if jump detection is unreliable
- **WARNING:** Too high and you'll "ground" while in air

#### Ground Layer
- Which layers count as ground
- Default: 1 (Default layer)
- Set to match your ground objects
- **TIP:** Create a "Ground" layer for better control

### Character Settings

#### Rotation Speed
- How fast character rotates to face movement direction
- Default: 10
- Higher = instant rotation
- Lower = smooth turning
- **TIP:** 5-10 for realistic, 20+ for snappy

#### Capsule Collider
- Reference to the CapsuleCollider component
- **Leave empty** - script auto-finds it
- Only set manually if collider is on child object

### Slope Settings

#### Max Slope Angle
- Maximum slope angle character can walk on (degrees)
- Default: 45
- Above this angle, character cannot climb
- Triggers onSteepSlope event

#### Slope Check Distance
- How far to raycast for slope detection
- Default: 1
- Should be slightly longer than capsule height

### Animation

#### Character Animator
- Reference to Animator component (optional)
- **Leave empty if no animator**
- Script will auto-find if on child object
- Updates these parameters:
  - `Speed` (float) - horizontal velocity magnitude
  - `IsGrounded` (bool) - is character on ground
  - `VerticalVelocity` (float) - Y velocity
  - `IsWalking` (bool) - speed > 0.1 and grounded

#### Character Mesh
- Reference to child object with visual mesh/animator
- Used to auto-find Animator component
- **Leave empty if animator is on same GameObject**

### Events

#### onGrounded
- Fires every frame while grounded
- **USE CASE:** Continuous ground effects

#### onJump
- Fires when jump is executed
- **USE CASE:** Jump sound effect, particle burst

#### onLanding
- Fires once when landing after being airborne
- **USE CASE:** Landing sound, dust particle, camera shake

#### onStartMoving
- Fires when character starts moving from standstill
- **USE CASE:** Footstep sounds, movement dust

#### onStopMoving
- Fires when character stops moving
- **USE CASE:** Stop footstep loop, idle animation trigger

#### onSteepSlope
- Fires when character enters slope too steep to climb
- **USE CASE:** Slide down animation, warning sound

---

## Public Methods (For UnityEvents & Scripting)

#### SetMoveForce(float)
- Changes move force at runtime
- **USE CASE:** Power-ups, speed buffs

#### SetJumpForce(float)
- Changes jump force at runtime
- **USE CASE:** Double jump, jump power-ups

#### SetMaxVelocity(float)
- Changes max velocity at runtime
- **USE CASE:** Speed zones, slow effects

### Public Properties (Read-Only)

#### IsGrounded
- Returns true if character is on ground
- **USE CASE:** Check before allowing actions

#### IsMoving
- Returns true if character is moving
- **USE CASE:** Toggle animations, effects

#### IsOnSteepSlope
- Returns true if on slope too steep to climb
- **USE CASE:** Trigger slide animations

#### CurrentSpeed
- Returns current horizontal speed (magnitude)
- **USE CASE:** Speed-based effects, UI display

---

## Layer Setup (Recommended)

For best results, create separate layers for ground and moving platforms:

**Unity Layers (Edit → Project Settings → Tags and Layers):**
- Layer 6: **Ground** (static walkable surfaces)
- Layer 7: **MovingPlatform** (moving/rotating platforms only)

**Component Configuration:**
```
PhysicsCharacterController:
  Ground Layer: Ground + MovingPlatform (select both)

PhysicsPlatformStick (if using):
  Platform Layer: MovingPlatform (only this layer)
```

**GameObject Layer Assignment:**
- Static floors, terrain, ramps → Layer: **Ground**
- Moving platforms, elevators → Layer: **MovingPlatform**, Tag: **movingPlatform**

This separation provides better performance and clearer debugging.

---

## Complete Setup Example

### Scenario: Basic Third-Person Character

**Step 1: Character Setup**
```
GameObject "Player"
├── Rigidbody (auto-configured by script)
├── Capsule Collider
│   └── Height: 2
│   └── Radius: 0.5
│   └── Center: (0, 1, 0)
├── PhysicsCharacterController
│   └── Move Force: 10
│   └── Max Velocity: 8
│   └── Jump Force: 12
│   └── Ground Layer: Ground + MovingPlatform
└── Player Input
    └── Actions: CharacterInputActions
    └── Behavior: Send Messages
```

**Step 2: Create Child Mesh**
```
GameObject "Player"
└── GameObject "CharacterMesh"
    ├── MeshFilter (your character model)
    ├── MeshRenderer
    └── Animator (optional)
```

**Step 3: Link References**
```
PhysicsCharacterController:
- Character Mesh: Drag "CharacterMesh" GameObject here
- Character Animator: Leave empty (auto-finds)
- Capsule Collider: Leave empty (auto-finds)
```

**Step 4: Ground Setup**
```
GameObject "Ground"
├── Mesh Collider (or Box Collider)
└── Layer: Default (or custom Ground layer)
```

**Step 5: Input Actions**
```
Input Action Asset "CharacterInputActions"
└── Action Map "Player"
    ├── Move (Vector2): WASD/Left Stick
    └── Jump (Button): Space/South Button
```

**Done!** Press Play and use WASD + Space.

---

## Animation Setup (Optional)

### Animator Controller Setup

Create an Animator Controller with these parameters:
- `Speed` (Float)
- `IsGrounded` (Bool)
- `VerticalVelocity` (Float)
- `IsWalking` (Bool)

**Example Transitions:**
- Idle → Walk: `IsWalking == true`
- Walk → Idle: `IsWalking == false`
- Any State → Jump: `IsGrounded == false && VerticalVelocity > 0`
- Jump → Fall: `VerticalVelocity < 0`
- Fall → Land: `IsGrounded == true`

The script automatically updates these parameters every frame.

---

## Integration with Other Systems

### With GameAudioManager
```
PhysicsCharacterController.onJump → GameAudioManager.PlaySFX("jump")
PhysicsCharacterController.onLanding → GameAudioManager.PlaySFX("land")
PhysicsCharacterController.onStartMoving → GameAudioManager.PlayMusic("footsteps")
```

### With Particle Effects
```
PhysicsCharacterController.onLanding → ParticleSystem.Play() (dust cloud)
PhysicsCharacterController.onJump → ParticleSystem.Play() (jump burst)
```

### With Cinemachine Camera
```
PhysicsCharacterController.onLanding → CinemachineImpulseSource.GenerateImpulse() (camera shake)
```

### With Power-Ups
```
Trigger Zone enters:
→ PhysicsCharacterController.SetMoveForce(20) (speed boost)
→ PhysicsCharacterController.SetJumpForce(20) (super jump)
```

---

## Debugging Tips

### Visualize Ground Check (Play Mode Only)
1. Select character in Hierarchy
2. Look at Scene view
3. Green sphere = grounded
4. Red sphere = not grounded
5. Yellow ray = slope check
6. Blue ray = slope normal

### Enable Gizmos
Make sure Gizmos are enabled in Scene view (top right of Scene window)

### Check Console for Errors
- Missing Rigidbody warning
- Missing Camera warning
- Input System errors

### Test Checklist
- [ ] Rigidbody exists and is NOT kinematic
- [ ] Capsule Collider exists
- [ ] Player Input component exists with "Send Messages" behavior
- [ ] Input Actions have "Move" and "Jump" actions
- [ ] Main Camera exists and is tagged "MainCamera"
- [ ] Ground has collider
- [ ] Ground Layer matches ground objects

---

## Advanced: Rigidbody Auto-Configuration

The script automatically configures these Rigidbody settings on Start:

- **Freeze Rotation:** All axes (prevents physics rotation)
- **Collision Detection:** Continuous (prevents tunneling)
- **Interpolation:** Interpolate (smooth camera following)
- **Constraints:** Freeze Rotation X, Y, Z

You don't need to manually set these - the script handles it!

---

## Troubleshooting: Movement Direction

### Problem: Character moves in wrong direction relative to camera

**Check:**
1. Is camera tagged "MainCamera"?
2. Is camera in scene and active?
3. Check console for "Camera.main is null" errors

The character uses **camera-relative movement**:
- W = forward relative to camera
- S = backward relative to camera
- A/D = strafe left/right relative to camera

Without a main camera, movement defaults to world-space (may feel wrong).

---

## Performance Notes

- Uses FixedUpdate for physics calculations (proper physics timing)
- Efficient ground checking with sphere cast
- Minimal allocations per frame
- Can handle many character controllers simultaneously

---

## Comparison to Other Controllers

### vs Transform-based Controller
- ✅ Realistic physics interactions
- ✅ Affected by forces (explosions, wind)
- ✅ Proper collision response
- ❌ Slightly less precise control

### vs CharacterController Component
- ✅ Full physics integration
- ✅ Rigidbody velocity/forces work
- ✅ Better for physics-based games
- ❌ Requires more setup

### vs Ball Controller
- ✅ Upright character (not rolling)
- ✅ Animation support
- ✅ Directional facing
- ❌ More complex setup

---

## Student Workflow Summary

For students creating their first physics character:

1. Create Empty GameObject
2. Add Rigidbody + Capsule Collider + PhysicsCharacterController
3. Add Player Input component with "Send Messages" behavior
4. Create/assign Input Action Asset with "Move" and "Jump"
5. Ensure ground has colliders and camera is tagged "MainCamera"
6. Press Play and use WASD + Space
7. Adjust Move Force and Jump Force to taste
8. Wire events to audio/effects for polish

**No code required!** Everything connects via Inspector.

---

This component is designed to provide physics-based character control for students without requiring any code. The setup is more involved than transform-based controllers but provides realistic physics interactions and better game feel.
