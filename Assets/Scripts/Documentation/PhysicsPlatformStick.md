# PhysicsPlatformStick - Tutorial & Usage Guide

This guide explains how to make characters stick to moving platforms without using parenting.

---

## Component Overview

**PhysicsPlatformStick** - Attach characters to moving platforms using transform offsets
- Works with PhysicsCharacterController and other Rigidbody controllers
- No parenting required (avoids rotation/scale issues)
- Supports both translating and rotating platforms
- Character can move freely while on platform
- Capsule collider support for accurate detection

---

## How It Works

The script uses a **transform offset** approach (industry standard):

1. **Detects platform** - Raycasts downward to find platforms by layer and tag
2. **Tracks platform movement** - Stores position/rotation delta each frame
3. **Applies offset** - Moves character by the platform's delta after physics calculations
4. **Preserves character movement** - Character can walk/jump while platform carries them

This approach avoids physics conflicts and works reliably with all character controllers.

---

## Layer Setup (Required)

**Create Separate Layers:**

1. Open **Edit → Project Settings → Tags and Layers**
2. Create these layers:
   - Layer 6: **Ground** (static surfaces)
   - Layer 7: **MovingPlatform** (moving platforms only)

**Why separate layers?**
- Performance: Only checks moving objects
- Clarity: Clear distinction between static and dynamic
- Control: Can enable/disable platform sticking independently

---

## Quick Start Guide

### Step 1: Setup Layers

**Create Tag:**
1. Edit → Project Settings → Tags and Layers
2. Add Tag: **"movingPlatform"**

**Create Layer:**
1. Same menu, add Layer 7: **"MovingPlatform"**

### Step 2: Setup Moving Platform

1. Create a platform GameObject (Cube, custom mesh, etc.)
2. Set Layer to **MovingPlatform**
3. Add Tag **"movingPlatform"**
4. Add collider (Box Collider, Mesh Collider, etc.)
5. Move it using:
   - Transform manipulation in Update
   - Animation
   - Waypoint system
   - Physics (Rigidbody Kinematic)

### Step 3: Setup Character

1. Select your character GameObject
2. Add Component → **PhysicsPlatformStick**
3. Configure:
   - **Ground Check Distance:** 0.3 (or match your character's ground check)
   - **Platform Layer:** Select "MovingPlatform" layer only
   - **Platform Tag:** "movingPlatform"
   - **Apply Vertical Movement:** true (for elevators), false (for horizontal only)

### Step 4: Configure Character's Ground Layer

On your **PhysicsCharacterController**:
- Set **Ground Layer** to: Ground + MovingPlatform (both layers)
- This allows walking on both static ground AND platforms

### Step 5: Test

Press Play and walk onto the platform. Character should move with it!

---

## Parameter Reference

### Platform Detection

#### Ground Check Distance
- How far down to raycast for platform detection
- Default: 0.3
- Should match character's ground detection distance
- Too short: Won't detect platform reliably
- Too long: May stick when jumping

#### Platform Layer
- Which layer to check for platforms
- **Set to:** MovingPlatform layer ONLY
- Do NOT include static ground layers (performance)

#### Platform Tag
- Tag required on platform GameObjects
- Default: "movingPlatform"
- Platforms must have this tag to be detected
- Prevents sticking to non-platform objects on same layer

### Movement Settings

#### Apply Vertical Movement
- Should character follow platform's vertical movement?
- **true** (default) - Full 3D platform movement (elevators, diagonal platforms)
- **false** - Horizontal only (carousel, conveyor belt)
- Vertical movement still allows jumping/falling naturally

### Capsule Settings (Optional)

#### Capsule Collider
- Reference to character's CapsuleCollider
- **Leave empty** - Script auto-finds it
- Only set manually if capsule is on child object
- Used to raycast from capsule bottom (more accurate)

---

## Public Properties (Read-Only)

#### IsOnPlatform
- Returns true if currently on a moving platform
- **USE CASE:** Disable certain abilities on platforms, UI indicators

#### CurrentPlatform
- Returns Transform of current platform (or null)
- **USE CASE:** Get platform information, check platform properties

#### PlatformPositionDelta
- Returns the position delta applied this frame
- **USE CASE:** Advanced usage, custom platform effects

---

## Common Patterns

### Pattern: Elevator Platform

**Setup:**
```
Platform GameObject:
  - Layer: MovingPlatform
  - Tag: movingPlatform
  - Box Collider
  - Move via: Transform.Translate or Animation

Character:
  - PhysicsPlatformStick:
    - Ground Check Distance: 0.3
    - Platform Layer: MovingPlatform
    - Apply Vertical Movement: true ✓
```

Character rises/descends with elevator.

---

### Pattern: Conveyor Belt (Horizontal Only)

**Setup:**
```
Platform GameObject:
  - Layer: MovingPlatform
  - Tag: movingPlatform
  - Move horizontally only

Character:
  - PhysicsPlatformStick:
    - Apply Vertical Movement: false ✗
```

Character moves horizontally with belt but can jump normally.

---

### Pattern: Rotating Platform

**Setup:**
```
Platform GameObject:
  - Layer: MovingPlatform
  - Tag: movingPlatform
  - Rotates around Y axis (Transform.Rotate)

Character:
  - PhysicsPlatformStick:
    - Apply Vertical Movement: true ✓
```

Character rotates with platform, stays in position relative to pivot.

---

### Pattern: Diagonal Moving Platform

**Setup:**
```
Platform GameObject:
  - Layer: MovingPlatform
  - Tag: movingPlatform
  - Moves diagonally up/forward

Character:
  - PhysicsPlatformStick:
    - Apply Vertical Movement: true ✓
```

Character follows full 3D movement path.

---

## Integration with PhysicsCharacterController

**Recommended Setup:**
```
Character GameObject:
  ├── Rigidbody (auto-configured)
  ├── Capsule Collider
  ├── PhysicsCharacterController
  │   └── Ground Layer: Ground + MovingPlatform
  └── PhysicsPlatformStick
      └── Platform Layer: MovingPlatform only
```

**How they work together:**
1. PhysicsCharacterController detects ground (both static and platform)
2. PhysicsPlatformStick detects if ground is a moving platform
3. Character can walk/jump normally
4. Platform movement is added on top of character movement
5. No conflicts or fighting between scripts

---

## Troubleshooting

### Character doesn't stick to platform

**Check 1: Layer and Tag**
- Platform Layer is **MovingPlatform**?
- Platform Tag is **"movingPlatform"**?
- PhysicsPlatformStick Platform Layer set to **MovingPlatform**?

**Check 2: Ground Check Distance**
- Is character close enough to platform?
- Try increasing Ground Check Distance to 0.5

**Check 3: Collider on Platform**
- Platform must have a collider (Box, Mesh, etc.)
- Collider must NOT be trigger

**Check 4: Gizmos (Scene View)**
- Select character in Hierarchy
- Look for yellow/green dotted line in Scene view (during Play)
- Green = on platform, Yellow = not on platform

### Character falls through platform

**Cause: Platform has no collider or is trigger**
- ✅ Solution: Add Box Collider to platform, uncheck "Is Trigger"

**Cause: Platform layer not in character's Ground Layer**
- ✅ Solution: PhysicsCharacterController Ground Layer should include MovingPlatform

### Character sticks when jumping off platform

**Cause: Ground Check Distance too high**
- ✅ Solution: Reduce Ground Check Distance to 0.3 or lower
- Should match character controller's ground detection

### Platform rotation doesn't work

**Cause: Platform only moves position, not rotation**
- ✅ Solution: Rotate platform using Transform.Rotate or Animation
- Script automatically handles rotation

### Character jitters on platform

**Cause 1: Platform moving in Update instead of FixedUpdate**
- ✅ Solution: Move platform in FixedUpdate for physics consistency

**Cause 2: Both scripts fighting**
- ✅ Solution: Check you don't have multiple platform stick scripts
- Only one PhysicsPlatformStick per character

---

## Advanced: Creating Moving Platforms

### Method 1: Transform Animation (Simplest)

```csharp
// On platform GameObject
void Update()
{
    // Horizontal movement
    transform.position += Vector3.right * 2f * Time.deltaTime;

    // Or rotation
    transform.Rotate(Vector3.up, 30f * Time.deltaTime);
}
```

### Method 2: Animation Component

1. Create Animation Clip
2. Animate Transform Position/Rotation
3. Add Animator to platform
4. Play animation

### Method 3: Waypoint System

Use existing waypoint scripts to move platform between points.

### Method 4: Physics (Advanced)

1. Add Rigidbody to platform
2. Set to **Kinematic**
3. Move using `rb.MovePosition()` in FixedUpdate

---

## Performance Notes

- **Efficient Detection:** Only raycasts when needed, only checks MovingPlatform layer
- **No Physics Overhead:** Uses transform manipulation, not forces
- **Capsule Optimization:** Auto-finds capsule once at start
- **Single Raycast:** One downward raycast per frame when active

Can support many characters on many platforms simultaneously.

---

## Comparison to Other Methods

### vs Parenting (NOT RECOMMENDED)
- ❌ Parenting: Scale issues, rotation complexity, physics conflicts
- ✅ PhysicsPlatformStick: Clean, no side effects, works with rotation

### vs Velocity Matching
- ❌ Velocity: Fights with character controller forces, imprecise
- ✅ PhysicsPlatformStick: No conflicts, precise positioning

### vs CharacterController.Move
- ⚠️ Unity's CharacterController: Built-in but not physics-based
- ✅ PhysicsPlatformStick: Works with Rigidbody physics systems

---

## Common Student Mistakes

### Mistake 1: Forgetting Platform Tag
- ❌ **WRONG:** Platform has layer but no "movingPlatform" tag
- ✅ **RIGHT:** Platform needs BOTH layer AND tag

### Mistake 2: Using Same Layer for All Ground
- ❌ **WRONG:** Everything uses "Ground" layer
- ✅ **RIGHT:** Static ground = Ground layer, Moving platforms = MovingPlatform layer

### Mistake 3: Platform Layer Set Wrong
- ❌ **WRONG:** PhysicsPlatformStick checking "Ground" layer
- ✅ **RIGHT:** Only check "MovingPlatform" layer

### Mistake 4: No Collider on Platform
- ❌ **WRONG:** Platform is just a mesh, no collider
- ✅ **RIGHT:** Platform needs Box Collider or Mesh Collider

### Mistake 5: Moving Platform in Update
- ❌ **WRONG:** Moving platform in Update() causes jitter
- ✅ **RIGHT:** Move in FixedUpdate() or use Animation

---

## Student Workflow Summary

For students creating their first moving platform:

1. Create platform GameObject (Cube)
2. Set Layer to "MovingPlatform"
3. Add Tag "movingPlatform"
4. Add Box Collider
5. Add simple movement script or Animation
6. Add PhysicsPlatformStick to character
7. Set Platform Layer to "MovingPlatform"
8. Test by walking onto platform

**No parenting required!** Everything works through physics detection.

---

## Example: Simple Moving Platform Script

For students to use on platforms:

```csharp
using UnityEngine;

public class SimplePlatformMove : MonoBehaviour
{
    public Vector3 moveDirection = Vector3.right;
    public float speed = 2f;
    public float distance = 5f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void FixedUpdate()
    {
        // Ping-pong movement
        float offset = Mathf.PingPong(Time.time * speed, distance);
        transform.position = startPosition + moveDirection * offset;
    }
}
```

Add this to platform, character automatically sticks!

---

This component is designed to provide reliable platform sticking for students without requiring parenting or complex physics setup. The transform offset approach is the industry standard used in professional games.
