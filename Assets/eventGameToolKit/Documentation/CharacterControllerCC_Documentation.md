# CharacterControllerCC - Complete Setup Guide

**Last Updated:** October 2025
**Unity Version:** Unity 6+
**Script Location:** `Assets/Scripts/Physics/CharacterControllers/CharacterControllerCC.cs`

---

## Overview

CharacterControllerCC is a kinematic character controller designed for third-person adventure games, action platformers, and character movement systems. It uses Unity's CharacterController component for smooth, physics-independent movement with support for slopes, moving platforms, dodge mechanics, and extensive animation integration.

### Key Features
- ✅ Smooth movement with acceleration/deceleration (Unity TPC style)
- ✅ Height-based jumping with anti-spam timeout
- ✅ Slope detection and automatic sliding on steep surfaces
- ✅ Moving platform support (tag/layer/both detection modes)
- ✅ Dodge mechanic with cooldown system
- ✅ Full animation integration with 5 animator parameters
- ✅ Extensive UnityEvent system for no-code interactions
- ✅ Debug visualization with scene gizmos

---

## 🚀 Quick Start (5 Minutes - For Overwhelmed Students!)

**Feeling overwhelmed by all the details below? Don't worry! Here's the absolute minimum to get a working character moving around.** You can always come back and add fancy features like dodge rolls, moving platforms, and animations later. Let's start simple:

### Your First Working Character (3 Easy Steps)

**Step 1: Add the Component**
1. Create an empty GameObject in your scene (Right-click in Hierarchy → Create Empty)
2. Name it "Player" and position it at (0, 1, 0) - just above the ground
3. In the Inspector, click **Add Component** and search for "CharacterControllerCC"
4. Unity automatically adds a CharacterController component for you ✨

**Step 2: Connect the Input**
1. With Player still selected, click **Add Component** again
2. Search for "PlayerInput" and add it
3. In the PlayerInput component:
   - Set **Actions** to `InputSystem_Actions` (find it in your Assets folder)
   - Set **Default Map** to "Player"
   - Set **Behavior** to "Send Messages"

**Step 3: Set the Ground Layer**
1. In the CharacterControllerCC component (scroll down if needed)
2. Find **Ground Layer** under "Jump Settings"
3. Click the dropdown and check the layer your ground uses (probably "Default" or "Ground")

**That's it! Press Play and use WASD to move, Space to jump!** 🎉

### It's Not Working? Try These:
- ✅ Make sure you have a ground object (like a plane or cube) under your character
- ✅ Make sure your Main Camera is in the scene and tagged "MainCamera"
- ✅ Check that the ground has a Collider component (not a trigger)

### What to Do Next (When You're Ready):
- 📖 **Want animations?** → See "Step 4: Setup Character Mesh/Model" below
- 🎮 **Want dodge rolls?** → See "Dodge Settings" in the Parameter Reference
- 🛤️ **Want moving platforms?** → See "Scenario 2: Platformer with Moving Platforms"
- 🎨 **Want to customize movement feel?** → See "Movement Settings" (change Move Speed, Jump Height, etc.)

**Remember:** You don't need to understand everything at once! Start with WASD movement, then add one feature at a time when you're comfortable. The full documentation below is here when you need it - not all at once. You've got this! 💪

---

## Required Components

### Automatically Added Components
When you add CharacterControllerCC to a GameObject, Unity will automatically add:
- **CharacterController** - Required for kinematic movement (tagged with `[RequireComponent]`)

### Components You Must Add
1. **PlayerInput** - For Unity Input System integration
2. **Animator** (optional) - If using animations
3. **Capsule Mesh or Character Model** (optional) - Visual representation

---

## Complete Setup Guide

### Step 1: Create Character GameObject

1. Create an empty GameObject in your scene
2. Name it "Player" or "Character"
3. Position it above the ground (e.g., Y = 1)
4. Add the **CharacterControllerCC** component
   - This automatically adds **CharacterController**

### Step 2: Configure CharacterController Component

The script auto-configures this, but verify these settings:

| Parameter | Recommended Value | Description |
|-----------|------------------|-------------|
| **Slope Limit** | 45° | Auto-set from `maxSlopeAngle` |
| **Skin Width** | 0.08 | Auto-set if < 0.01 |
| **Center** | (0, 1, 0) | Center of capsule |
| **Radius** | 0.5 | Capsule radius |
| **Height** | 2.0 | Capsule height |

### Step 3: Setup Input System

1. Add **PlayerInput** component to the same GameObject
2. Configure PlayerInput:
   - **Actions**: Select `InputSystem_Actions` asset
   - **Default Map**: "Player"
   - **Behavior**: "Invoke Unity Events" or "Send Messages"
3. Required Input Actions in your InputActionAsset:
   - **Move** (Vector2) - WASD or left stick
   - **Jump** (Button) - Space or gamepad button
   - **Dodge** (Button) - Left Shift or gamepad button

**Input Callbacks:**
The script listens for these Input System callbacks:
- `OnMove(InputValue value)` - Movement input
- `OnJump(InputValue value)` - Jump input
- `OnDodge(InputValue value)` - Dodge input

### Step 4: Setup Character Mesh/Model (Optional)

If using a 3D character model with animations:

1. Create a child GameObject named "CharacterMesh"
2. Add your character model as a child of this GameObject
3. Add **Animator** component to the model GameObject
4. In CharacterControllerCC Inspector:
   - Assign **Character Mesh** → the GameObject with your model
   - Assign **Character Animator** → the Animator component
5. Setup Animator Controller with these parameters:

| Parameter Name | Type | Description |
|---------------|------|-------------|
| **Speed** | Float | Horizontal movement speed (0 = idle, 8 = running) |
| **Grounded** | Bool | True when character is on the ground |
| **VerticalVelocity** | Float | Upward/downward velocity (positive = rising, negative = falling) |
| **IsDodging** | Bool | True during dodge roll animation |
| **IsWalking** | Bool | True when moving on ground (combines `Speed > 0.1 && Grounded`) |

### 🎯 Understanding IsDodging and IsWalking Parameters

**IsDodging (Bool) - Interrupt Any Animation for Dodge:**

This parameter lets dodge animations **interrupt** whatever the character is doing:

```
Any State → Dodge (Condition: IsDodging == true)
├─ Priority: High (so it interrupts everything)
├─ Has Exit Time: ❌ (instant interrupt!)
└─ Transition Duration: 0.0s

Dodge Animation → Idle (Condition: IsDodging == false)
├─ Has Exit Time: ✅ (let dodge animation finish)
├─ Exit Time: 0.9 (90% through animation)
└─ Transition Duration: 0.15s
```

**Why use "Any State"?** Dodge can trigger from Idle, Walk, Run, Jump, Fall - any state!

**IsWalking (Bool) - Prevents Air-Walk Bug:**

This is a **convenience parameter** that prevents a common bug:

❌ **Bad Approach** (using Speed alone):
```
Idle → Walk (Condition: Speed > 0.1)
```
**Problem:** If you jump while moving forward, Speed is still > 0.1, so Walk animation plays in mid-air!

✅ **Good Approach** (using IsWalking):
```csharp
// CharacterControllerCC automatically calculates:
bool isWalking = speed > 0.1f && isGrounded;
```
```
Idle → Walk (Condition: IsWalking == true)
```
**Solution:** Walk animation only plays when grounded! No air-walking!

**Parameter Usage Summary:**

| Parameter | Use Case | Typical Transition |
|-----------|----------|-------------------|
| **Speed** | Drive Walk/Run blend tree | Walk Blend Tree (0.0 = Idle, 8.0 = Run) |
| **IsWalking** | Simple Idle ↔ Walk switch | `IsWalking == true` to start walking |
| **IsDodging** | Interrupt with dodge | `Any State → Dodge` when `IsDodging == true` |
| **Grounded** | Ground ↔ Air states | `Grounded == false` to enter Air Movement |
| **VerticalVelocity** | Drive Jump/Fall blend tree | Air Movement Blend Tree (-10 to +5) |
| **IdleTime** (Optional) | Emote/fidget after idle | `Idle → Emote` when `IdleTime > 5.0` |

### 🎯 Understanding IdleTime for Emote/Fidget Animations

The **IdleTime** parameter is an **optional** float that tracks how long the character has been standing still. This is perfect for playing fidget animations or emotes after being idle!

**How IdleTime Works:**
- Starts at `0.0` when character is moving, jumping, or dodging
- Counts up in seconds when character is truly idle (not moving, grounded, not dodging)
- Automatically resets to `0.0` when player moves again

**Setup Steps:**

1. **Enable in CharacterControllerCC:**
   - Set **Idle Time Before Emote** to a positive value (e.g., `5.0` for 5 seconds)
   - Set to `0` to disable this feature

2. **Add parameter in Animator:**
   - Create a Float parameter called **"IdleTime"**

3. **Create emote transition:**
   ```
   Idle → Emote (Condition: IdleTime > 5.0)
   ├─ Has Exit Time: ❌
   └─ Transition Duration: 0.2s

   Emote → Idle (Condition: IdleTime < 0.1)  ← Resets when moving
   ├─ Has Exit Time: ✅ (let emote finish)
   ├─ Exit Time: 0.95
   └─ Transition Duration: 0.3s
   ```

**Example Use Cases:**
- **Fidget Animation**: Character looks around, adjusts clothing at 3 seconds idle
- **Stretching**: Character stretches or yawns at 8 seconds idle
- **Boredom**: Character sits down, checks phone at 10 seconds idle
- **Multiple Emotes**: Chain multiple emotes with different thresholds (5s, 10s, 15s)

**Advanced Pattern - Multiple Emotes:**
```
Idle
├─ → Fidget_1   (IdleTime > 3.0 && IdleTime < 6.0)
├─ → Fidget_2   (IdleTime > 6.0 && IdleTime < 10.0)
└─ → Sit_Down   (IdleTime > 10.0)

Each emote → Idle (IdleTime < 0.1)
```

This creates a progression of idle behaviors the longer the player waits!

### 🎯 Understanding VerticalVelocity for Airborne Animations

The **VerticalVelocity** parameter is the **Y component** of the character's velocity (from `controller.velocity.y`). This float value is crucial for creating smooth, realistic airborne animations.

**How VerticalVelocity Works:**
- **Positive values** = Character moving upward (jumping, launched up)
  - Example: `+5.0` at start of jump, decreasing as gravity slows you down
- **Zero** = Peak of jump (brief moment before falling starts)
- **Negative values** = Character falling downward
  - Example: `-10.0` while falling, increasing to terminal velocity `-50.0`

**Best Practice - Use Blend Trees for Smooth Air Animations:**

Instead of simple state transitions, create a **1D Blend Tree** for your airborne movement:

```
Air Movement (1D Blend Tree)
Parameter: VerticalVelocity
├─ Jump Start Animation    (Threshold: +3.0 to +6.0)
├─ Jump Apex Animation      (Threshold: -0.5 to +0.5)
├─ Fall Slow Animation      (Threshold: -3.0 to -0.5)
└─ Fall Fast Animation      (Threshold: -10.0 to -3.0)
```

This creates **smooth blending** as velocity changes, avoiding sudden animation pops!

**Example Animator State Setup:**
```
Idle (default)
├─ Transition to Walk (Condition: IsWalking == true)
│  └─ Has Exit Time: ❌
│  └─ Transition Duration: 0.1s
│
Walk
├─ Transition to Idle (Condition: IsWalking == false)
│  └─ Has Exit Time: ❌
│  └─ Transition Duration: 0.1s
│
├─ Transition to Jump (Condition: Grounded == false, VerticalVelocity > 0.1)
│  └─ Has Exit Time: ❌
│  └─ Transition Duration: 0.0s
│
Jump
├─ Transition to Fall (Condition: VerticalVelocity < -0.1)
│  └─ Has Exit Time: ❌
│  └─ Transition Duration: 0.1s
│
Fall
├─ Transition to Land (Condition: Grounded == true)
│  └─ Has Exit Time: ❌
│  └─ Transition Duration: 0.1s
│
Dodge
├─ Transition to Idle (Condition: IsDodging == false)
│  └─ Has Exit Time: ✅ (Exit Time: 0.9)
│  └─ Transition Duration: 0.15s
```

### 🎓 Advanced: Blend Tree Setup for Airborne Animations

**Why Use Blend Trees for VerticalVelocity?**

❌ **Simple Transitions** (basic):
- Jump animation plays at +5.0 velocity
- Fall animation plays at -5.0 velocity
- **Problem**: Sudden animation "pop" when switching between them

✅ **1D Blend Tree** (professional):
- Jump smoothly blends into fall as velocity changes from +5.0 → 0.0 → -5.0
- **Result**: Natural, fluid airborne movement like AAA games

**How to Create Air Movement Blend Tree:**

1. In Animator window, create new State called "Air Movement"
2. Right-click state → "Create new BlendTree in State"
3. Double-click to enter blend tree
4. In Inspector:
   - **Blend Type**: 1D
   - **Parameter**: VerticalVelocity

5. Add motions (click **+** button):
   ```
   Motion 1: Jump_Ascend     | Threshold: +4.0  (shooting upward)
   Motion 2: Jump_Peak       | Threshold:  0.0  (apex of jump)
   Motion 3: Fall_Start      | Threshold: -2.0  (beginning to fall)
   Motion 4: Fall_Fast       | Threshold: -8.0  (falling rapidly)
   ```

6. Create transitions:
   ```
   Grounded → Air Movement (Condition: Grounded == false)
   Air Movement → Grounded (Condition: Grounded == true)
   ```

**What Happens:**
- At VerticalVelocity = +4.0: 100% Jump_Ascend animation
- At VerticalVelocity = +2.0: 50% Jump_Ascend, 50% Jump_Peak (blending!)
- At VerticalVelocity = 0.0: 100% Jump_Peak animation
- At VerticalVelocity = -5.0: 50% Fall_Start, 50% Fall_Fast (smooth!)

This technique is used in Unity's Third Person Controller starter assets and professional games!

### Step 5: Setup Layers

Create these layers in your project (Edit > Project Settings > Tags and Layers):

1. **Ground** - For walkable surfaces
2. **MovingPlatform** (optional) - For moving platform detection

In CharacterControllerCC Inspector:
- **Ground Layer**: Select "Ground" layer
- **Platform Layer**: Select "MovingPlatform" layer (if using Layer or Both detection mode)

### Step 6: Tag Moving Platforms (If Using Tag Detection)

If using `PlatformDetectionMode.Tag` or `PlatformDetectionMode.Both`:

1. Select your moving platform GameObject
2. Set Tag to "movingPlatform" (or customize in CharacterControllerCC's **Platform Tag** field)

---

## Hierarchy Structure

### Recommended Hierarchy

```
Player (CharacterControllerCC, CharacterController, PlayerInput)
└── CharacterMesh (optional - for visual model)
    └── YourCharacterModel (MeshRenderer, Animator)
        ├── Armature/Skeleton
        └── Mesh
```

### Camera Setup

For camera-relative movement to work, ensure you have a **Main Camera** in your scene:
- Camera tagged as "MainCamera"
- Positioned to view the character (e.g., third-person view)

**Cinemachine Setup (Recommended):**
```
CM vcam1 (CinemachineVirtualCamera)
├─ Follow: Player (Transform)
├─ Look At: Player (Transform)
└─ Body: 3rd Person Follow
```

---

## Parameter Reference

### Movement Settings

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Move Speed** | float | 8 | Base movement speed in m/s when walking |
| **Max Velocity** | float | 8 | Maximum horizontal speed (prevents infinite acceleration) |
| **Speed Change Rate** | float | 10 | How fast the character accelerates and decelerates (higher = snappier) |
| **Air Control Factor** | float | 0.5 | Movement control while in air (0.5 = 50% of ground speed) |

**Usage Tips:**
- `Move Speed` = Comfortable walking speed (5-8 for realistic, 10-15 for arcade)
- `Speed Change Rate` = 10-15 for responsive, 5-8 for heavy/realistic
- `Air Control Factor` = 0.3-0.5 for realistic, 0.8-1.0 for arcade platformers

---

### Jump Settings

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Jump Height** | float | 1.2 | Maximum jump height in meters (uses physics formula) |
| **Jump Timeout** | float | 0.5 | Cooldown between jumps to prevent spam (seconds) |
| **Ground Check Distance** | float | 0.1 | How far down to check for ground (should be small) |
| **Ground Layer** | LayerMask | Default | Which layers count as ground |

**Height Formula:**
Jump velocity is calculated using `v = √(height × -2 × gravity)`, making jump height intuitive:
- `1.0` = Jump 1 meter high
- `1.5` = Jump 1.5 meters high
- `2.0` = Jump 2 meters high

**Usage Tips:**
- Set `Jump Height` to match level design (0.8-1.2 for realistic, 2-4 for platformers)
- `Jump Timeout` = 0.5 prevents spam jumping, 0.0 allows instant consecutive jumps
- `Ground Check Distance` should be small (0.1-0.2), just enough to detect ground

---

### Dodge Settings

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Dodge Distance** | float | 5 | How far the character travels during dodge (meters) |
| **Dodge Speed** | float | 20 | How fast the dodge movement is (m/s) |
| **Dodge Cooldown** | float | 1 | Time before dodge can be used again (seconds) |
| **Allow Air Dodge** | bool | false | Can the character dodge while airborne? |

**Dodge Mechanics:**
- Dodge direction = movement input direction (or facing if no input)
- Blocked on steep slopes to prevent climbing walls
- Horizontal velocity is overridden during dodge, vertical velocity preserved

**Usage Tips:**
- Combat game: `Dodge Distance = 3-5`, `Cooldown = 0.8-1.2`, `Allow Air Dodge = false`
- Platformer: `Dodge Distance = 5-8`, `Cooldown = 0.5`, `Allow Air Dodge = true`

---

### Character Settings

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Rotation Smooth Time** | float | 0.12 | How quickly character turns to face movement direction (seconds) |
| **Controller** | CharacterController | Auto | Reference to CharacterController component (auto-assigned) |

**Rotation Behavior:**
- Uses `SmoothDampAngle` for natural, smooth rotation
- Only rotates during active input (prevents spinning)
- `0.05` = Very snappy rotation
- `0.12` = Unity standard (recommended)
- `0.25` = Slow, heavy rotation

---

### Gravity Settings

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Gravity** | float | -20 | Downward acceleration force (negative = down) |
| **Terminal Velocity** | float | -50 | Maximum falling speed (prevents infinite fall acceleration) |
| **Ground Stick Force** | float | -1.5 | Small downward force when grounded (keeps character stuck to ground) |

**Gravity Explanation:**
- Applied every `FixedUpdate` when not grounded: `velocity.y += gravity * Time.fixedDeltaTime`
- Real-world gravity ≈ -9.8 m/s², games often use -15 to -25 for better feel
- `Ground Stick Force` ensures character doesn't "hop" when walking downhill

**Usage Tips:**
- Realistic: `Gravity = -15`, `Terminal Velocity = -40`
- Standard: `Gravity = -20`, `Terminal Velocity = -50` (default)
- Floaty/Moon: `Gravity = -8`, `Terminal Velocity = -20`

---

### Slope Settings

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Max Slope Angle** | float | 45 | Maximum walkable slope in degrees (steeper = slide) |
| **Slope Check Distance** | float | 1 | How far ahead to check for slopes (meters) |
| **Slope Slide Speed** | float | 5 | How fast character slides down steep slopes (m/s) |

**Slope System:**
- Detects slope angle via SphereCast surface normal
- Blocks upward movement on slopes > `Max Slope Angle`
- Automatically slides down steep slopes with `Slope Slide Speed`
- Fires `onSteepSlope` event when encountering unclimbable slope

**Usage Tips:**
- Realistic game: `Max Slope Angle = 35-45°`
- Arcade platformer: `Max Slope Angle = 50-60°`
- Set `Slope Slide Speed` to 5-8 for gradual slide, 10-15 for fast slide

---

### Platform Settings

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Platform Detection Mode** | Enum | Tag | How to detect moving platforms (Tag, Layer, or Both) |
| **Platform Layer** | LayerMask | None | Which layer moving platforms are on (used for Layer/Both modes) |
| **Platform Tag** | string | "movingPlatform" | Tag name for moving platforms (used for Tag/Both modes) |
| **Apply Vertical Movement** | bool | true | Should character move vertically with platform (elevators)? |

**Detection Modes:**

| Mode | Raycast Layer | Tag Check | Use Case |
|------|--------------|-----------|----------|
| **Tag** | Ground Layer | ✅ Required | Easiest for students - just tag platforms |
| **Layer** | Platform Layer | ❌ Ignored | Separate layer for organization |
| **Both** | Platform Layer | ✅ Required | Most restrictive - requires both tag AND layer |

**How It Works:**
1. Raycasts downward to detect platform beneath character
2. Tracks platform position/rotation each frame
3. Calculates delta movement since last frame
4. Applies platform movement to character via `controller.Move()`

**Usage Tips:**
- **Tag Mode** (Recommended for students): Set platforms to "movingPlatform" tag, use Ground layer
- **Layer Mode**: Create separate "MovingPlatform" layer, no tagging required
- Disable `Apply Vertical Movement` for horizontal-only platforms (no elevator effect)

---

### Animation

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Character Animator** | Animator | null | Reference to Animator component (for animation control) |
| **Character Mesh** | Transform | null | Reference to character model root (script auto-finds Animator here) |

**Auto-Assignment:**
If you assign `Character Mesh` but leave `Character Animator` empty, the script will automatically search for an Animator component on the mesh's children.

---

### UnityEvents (Output Events)

All UnityEvents can be wired in the Inspector for no-code interactions.

| Event Name | When It Fires |
|------------|---------------|
| **onGrounded** | Every frame while character is touching the ground |
| **onJump** | When jump is initiated (upward velocity applied) |
| **onLanding** | When character lands on ground after being airborne |
| **onStartMoving** | When character begins horizontal movement |
| **onStopMoving** | When character stops moving horizontally |
| **onSteepSlope** | When character encounters slope > `Max Slope Angle` |
| **onDodge** | When dodge movement begins |
| **onDodgeCooldownReady** | When dodge cooldown finishes and dodge is ready again |

**Event Usage Examples:**

```
onJump → Play jump sound effect
onLanding → Spawn dust particle effect
onDodge → Play dodge animation, trigger i-frames
onSteepSlope → Display "Too steep!" UI message
onStartMoving → Start footstep audio loop
onStopMoving → Stop footstep audio loop
```

---

## Public Methods (Callable from UnityEvents)

These methods can be called from other scripts or UnityEvents to dynamically change controller behavior:

| Method | Parameter | Description |
|--------|-----------|-------------|
| `SetMoveSpeed(float)` | newSpeed | Changes movement speed at runtime |
| `SetJumpHeight(float)` | newHeight | Changes jump height in meters |
| `SetJumpTimeout(float)` | newTimeout | Changes jump cooldown duration |
| `SetSpeedChangeRate(float)` | newRate | Changes acceleration/deceleration rate |
| `SetMaxVelocity(float)` | newMax | Changes maximum movement speed cap |
| `SetRotationSmoothTime(float)` | newSmoothTime | Changes rotation smoothness |
| `SetDodgeDistance(float)` | newDistance | Changes dodge travel distance |
| `SetDodgeSpeed(float)` | newSpeed | Changes dodge movement speed |
| `SetDodgeCooldown(float)` | newCooldown | Changes dodge cooldown time |
| `SetGravity(float)` | newGravity | Changes gravity force (negative = down) |
| `SetTerminalVelocity(float)` | newTerminalVelocity | Changes max falling speed |
| `SetSlopeSlideSpeed(float)` | newSlideSpeed | Changes slope sliding speed |

**Example Usage:**
```
Pickup Power-Up → CharacterControllerCC.SetMoveSpeed(12) // Speed boost
Enter Water Zone → CharacterControllerCC.SetGravity(-5) // Reduced gravity
Collect Double Jump → CharacterControllerCC.SetJumpHeight(2.5) // Higher jump
```

---

## Public Properties (Read-Only)

Access character state from other scripts:

| Property | Type | Description |
|----------|------|-------------|
| `IsGrounded` | bool | Is character currently on the ground? |
| `IsMoving` | bool | Is character currently moving horizontally? |
| `IsOnSteepSlope` | bool | Is character on an unclimbable slope? |
| `IsDodging` | bool | Is character currently performing dodge? |
| `IsOnPlatform` | bool | Is character standing on a moving platform? |
| `CurrentPlatform` | Transform | Reference to current platform (null if not on platform) |
| `DodgeCooldownRemaining` | float | Seconds remaining until dodge is ready (0 = ready) |
| `CurrentSpeed` | float | Current horizontal movement speed |

**Example Usage:**
```csharp
// Check if player can perform action
if (characterController.IsGrounded && !characterController.IsDodging)
{
    // Allow attack
}

// Display dodge cooldown UI
float cooldownPercent = characterController.DodgeCooldownRemaining / dodgeCooldown;
cooldownBar.fillAmount = 1.0f - cooldownPercent;
```

---

## Scene Gizmos (Debug Visualization)

When the GameObject is selected in the Scene view, you'll see:

| Gizmo | Color | What It Shows |
|-------|-------|---------------|
| **Ground Check Sphere** | Green/Red | Grounded detection range (green = grounded, red = airborne) |
| **Platform Detection Ray** | Yellow/Green | Downward ray for platform detection (green = on platform) |
| **Slope Check Ray** | Yellow/Red | Forward ray for slope detection (red = steep slope ahead) |
| **Slope Normal** | Blue | Surface normal direction of current ground |
| **Slope Slide Direction** | Red | Direction character will slide on steep slope |
| **Dodge Direction** | Cyan | Direction and distance of active dodge |
| **Platform Delta** | Magenta | Platform movement applied to character (scaled 10× for visibility) |

---

## Common Setup Scenarios

### Scenario 1: Basic Third-Person Controller

**Goal:** Simple WASD movement with camera-relative controls

**Setup:**
1. Add CharacterControllerCC to character GameObject
2. Add PlayerInput, configure InputActions for Move + Jump
3. Set `Move Speed = 6`, `Jump Height = 1.2`
4. Set `Ground Layer = Ground`
5. Position Main Camera behind character at 45° angle

**Result:** Character moves relative to camera direction, responsive controls

---

### Scenario 2: Platformer with Moving Platforms

**Goal:** Character sticks to moving platforms and elevators

**Setup:**
1. Complete Basic Setup (above)
2. Create moving platform GameObjects
3. Tag platforms with "movingPlatform"
4. Set platforms to "Ground" layer
5. In CharacterControllerCC:
   - `Platform Detection Mode = Tag`
   - `Platform Tag = movingPlatform`
   - `Apply Vertical Movement = true` (for elevators)

**Result:** Character follows platform movement automatically

---

### Scenario 3: Combat Game with Dodge Roll

**Goal:** Add dodge mechanic for combat gameplay

**Setup:**
1. Complete Basic Setup
2. Configure InputActions to include Dodge action (Left Shift)
3. In CharacterControllerCC:
   - `Dodge Distance = 4`
   - `Dodge Speed = 18`
   - `Dodge Cooldown = 1.0`
   - `Allow Air Dodge = false`
4. Wire `onDodge` event to play dodge animation
5. Wire `onDodgeCooldownReady` event to update UI

**Result:** Dodge roll mechanic with visual feedback

---

### Scenario 4: Slope-Based Level Design

**Goal:** Create walkable slopes with steep walls

**Setup:**
1. Complete Basic Setup
2. Create terrain/level geometry
3. In CharacterControllerCC:
   - `Max Slope Angle = 45` (stairs/ramps OK)
   - `Slope Slide Speed = 6` (gentle slide)
4. Test with different slope angles:
   - 0-45° = Walkable
   - 46-90° = Character slides down

**Result:** Natural slope-based movement restrictions

---

## Troubleshooting

### Character Won't Move
**Symptoms:** Input works but character doesn't respond
**Solutions:**
- ✅ Check PlayerInput component is added and configured
- ✅ Verify InputActions asset is assigned
- ✅ Ensure `Move` action exists and is mapped to WASD/stick
- ✅ Check Main Camera exists and is tagged "MainCamera"
- ✅ Verify `Move Speed` > 0

### Character Doesn't Jump
**Symptoms:** Jump input fires but character stays grounded
**Solutions:**
- ✅ Check `Ground Layer` matches your ground objects' layer
- ✅ Ensure `Jump Height` > 0 and `Gravity` < 0 (negative)
- ✅ Verify ground objects have colliders
- ✅ Check `Jump Timeout` isn't blocking spam jumps

### Character Falls Through Ground
**Symptoms:** Character phases through floor
**Solutions:**
- ✅ Ensure ground has Collider component
- ✅ Verify `Ground Layer` includes ground objects' layer
- ✅ Check CharacterController `Skin Width` isn't too large (use 0.08)
- ✅ Ground colliders must not be triggers

### Character Doesn't Stick to Moving Platforms
**Symptoms:** Platform slides out from under character
**Solutions:**
- ✅ Check `Platform Detection Mode` matches your setup:
  - **Tag mode**: Platform must have correct tag
  - **Layer mode**: Platform must be on correct layer
  - **Both mode**: Platform needs both tag AND layer
- ✅ Verify platform raycast hits the platform (check gizmo visualization)
- ✅ Ensure platform is on `Ground Layer` if using Tag mode
- ✅ Check platform has collider (not trigger)

### Character Can Climb Steep Walls
**Symptoms:** Character walks up 80° walls
**Solutions:**
- ✅ Verify `Max Slope Angle` is set correctly (default 45°)
- ✅ Ensure wall objects are on `Ground Layer`
- ✅ Check wall colliders are properly configured
- ✅ Look at blue slope normal gizmo - should point away from wall

### Animator Parameters Not Working
**Symptoms:** "Parameter does not exist" errors
**Solutions:**
- ✅ Create these parameters in Animator Controller:
  - `Speed` (Float)
  - `Grounded` (Bool)
  - `VerticalVelocity` (Float)
  - `IsDodging` (Bool)
  - `IsWalking` (Bool)
- ✅ Parameter names are case-sensitive (must match exactly)
- ✅ Script has safety checks - missing parameters won't cause crashes

### Character Spins Randomly
**Symptoms:** Character rotates when standing still
**Solutions:**
- ✅ This was fixed in latest version - rotation only occurs during active input
- ✅ Check input deadzone in InputActions (set to 0.2 for stick drift)
- ✅ Verify `Rotation Smooth Time` is reasonable (0.05-0.3)

---

## Best Practices

### Performance
- ✅ Use StringToHash for animator parameters (automatic in script)
- ✅ Ground checking uses SphereCast + CharacterController.isGrounded (efficient hybrid)
- ✅ Platform detection only raycasts when needed
- ✅ Gizmos only render when GameObject is selected

### Level Design
- 🎯 Design slopes within `Max Slope Angle` for walkable areas
- 🎯 Use steeper slopes for visual-only terrain (character will slide)
- 🎯 Place moving platforms on consistent layer/tag for easy detection
- 🎯 Test jump height early to ensure level is navigable

### Animation
- 🎨 Use blend trees for movement (Idle → Walk → Run based on `Speed`)
- 🎨 Set transition "Has Exit Time" based on animation type:
  - Movement transitions: ❌ (instant response)
  - Attack/dodge: ✅ (let animation complete)
- 🎨 Use `VerticalVelocity` to differentiate jump (positive) from fall (negative)

### Events
- 📢 Use `onGrounded` sparingly (fires every frame) - prefer `onLanding` for one-time events
- 📢 Wire `onDodge` to disable damage collision for dodge i-frames
- 📢 Use `onSteepSlope` to guide players away from unclimbable areas

---

## Technical Notes

### Physics Update Timing
- Movement calculations occur in `FixedUpdate` for physics consistency
- Visual updates (rotation, animation) occur in `Update` for smooth rendering
- Platform movement uses `FixedUpdate` time steps for accurate delta tracking

### Grounded Detection Algorithm
1. If `velocity.y > 0.1`, force not grounded (prevents mid-jump grounding)
2. Perform SphereCast downward from capsule bottom
3. Check `CharacterController.isGrounded` as backup
4. Combine both checks for reliability
5. Extract surface normal from SphereCast hit for slope calculation

### Moving Platform System
- Uses **non-parenting approach** - character not parented to platform
- Tracks platform position/rotation delta each frame
- Applies delta movement via `controller.Move()` in platform's local space
- Handles both translation and rotation correctly
- 2-frame stabilization delay after landing to prevent jitter

### Slope Sliding Physics
- Calculates slide direction: `Vector3.ProjectOnPlane(Vector3.down, slopeNormal)`
- Overrides horizontal velocity during slide
- Blocks upward movement input on steep slopes
- Preserves slope normal from SphereCast for accurate physics

---

## Related Scripts

**Recommended Companion Scripts:**
- `GameCameraManager.cs` - Camera switching system
- `GameHealthManager.cs` - Health system integration
- `GameCheckpointManager.cs` - Checkpoint/respawn system
- `InputTriggerZone.cs` - Trigger zone detection
- `ActionDisplayText.cs` - UI feedback for events

**Alternative Controllers:**
- `PhysicsCharacterController.cs` - Rigidbody-based character (physics-driven)
- `PhysicsPlayerController.cs` - Ball-based player (different gameplay feel)

---

## Version History

### October 2025 - Unity TPC Improvements
- ✅ Added height-based jump with physics formula
- ✅ Added jump timeout anti-spam system
- ✅ Added smooth acceleration/deceleration (Unity TPC style)
- ✅ Changed rotation to SmoothDampAngle for better feel
- ✅ Added StringToHash optimization for animator parameters
- ✅ Fixed grounded detection with robust SphereCast + surface normal
- ✅ Fixed slope sliding to prevent wall-sticking
- ✅ Improved platform detection with configurable Tag/Layer/Both modes
- ✅ Added safety checks for missing animator parameters

**Lines of Code:** ~940 lines (includes comments, gizmos, and documentation)
**Complexity:** Medium-High (educational toolkit - well-commented)

---

## License & Credits

**Part of:** Unity Educational Toolkit for "Animation and Interactivity" class
**Design Philosophy:** UnityEvent-driven, no-code student interactions
**Inspiration:** Unity Starter Assets ThirdPersonController + custom educational features

---

**Questions or Issues?**
Check the script's inline comments or use Unity's Scene Gizmos to debug behavior visually.
