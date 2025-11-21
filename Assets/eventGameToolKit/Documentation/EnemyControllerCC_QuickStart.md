# EnemyControllerCC Quick Start Guide

**Feeling overwhelmed?** Don't worry! This guide will get you up and running with an AI enemy in just 5 minutes. 🎮

## What is EnemyControllerCC?

EnemyControllerCC is an educational AI component that creates enemies that can:
- **Chase the player** when they get close
- **Patrol between waypoints** when idle
- **Return to their starting position** when chase ends
- **Jump over obstacles** with configurable behaviors
- **Work on moving platforms** automatically

Perfect for action games, platformers, stealth games, and any project needing basic enemy AI.

---

## 5-Minute Setup (Basic Chase Enemy)

### Step 1: Create Your Enemy (30 seconds)

1. Create a **Capsule** GameObject (`GameObject > 3D Object > Capsule`)
2. Name it "Enemy"
3. Tag it as "Enemy" (or create this tag if needed)
4. Position it in your scene (recommended: 10-15 units from player)

### Step 2: Add the Component (30 seconds)

1. Select your Enemy GameObject
2. Click **Add Component**
3. Search for **"EnemyControllerCC"**
4. The component automatically adds a **CharacterController** (required)

**That's it!** You now have a basic chase enemy. The enemy will automatically detect and chase any GameObject tagged "Player" within 10 units.

### Step 3: Test It (1 minute)

1. Press **Play**
2. Move your player GameObject near the enemy
3. Watch the enemy chase you!
4. Move far away (beyond ~12 units) and the enemy returns to its starting position

**Troubleshooting:**
- ❌ Enemy doesn't chase? Check that your player is tagged "Player"
- ❌ Enemy falls through floor? Add a ground with a collider and set Ground Layer in inspector
- ❌ Enemy moves too fast/slow? Adjust **Move Speed** (default: 8)

---

## Common Scenarios (5 Minutes Each)

### Scenario 1: Patrolling Enemy

**Goal:** Enemy patrols between two points, chases player if detected, then returns to patrol.

**Setup:**
1. Create an empty GameObject named "Waypoint"
2. Position it where you want the enemy to patrol to
3. Select your Enemy
4. In EnemyControllerCC inspector:
   - ✅ Enable **Enable Patrol**
   - Drag "Waypoint" into **Patrol Waypoint** field
   - Set **Min Patrol Speed** to `2` (minimum speed)
   - Set **Max Patrol Speed** to `4` (maximum speed)
   - Set **Min Patrol Pause** to `0.5` (minimum pause at waypoints)
   - Set **Max Patrol Pause** to `2` (maximum pause at waypoints)

**How It Works:**
- Enemy patrols: Origin → Waypoint → Origin → Waypoint (repeats)
- **Speed is randomized** on spawn between min/max for variety
- **Pause duration is randomized** each time enemy reaches a waypoint
- If player enters **Detection Range**, enemy stops patrolling and chases
- When player leaves **Leash Range**, enemy returns to origin and resumes patrol

**Why Randomization?**
- Creates natural variation when duplicating enemies
- Multiple enemies won't move in perfect sync
- Easier to create variety without manual tweaking each enemy

**Visual Gizmos (Scene View):**
All gizmo colors are now documented in the Inspector header sections!
- 🟡🔴 Yellow/Red sphere = Detection range (Yellow idle, Red when chasing)
- 🟠 Orange sphere = Leash range (when enemy gives up chase)
- 🔵 Blue sphere = Stop distance (how close to get to player)
- 🟣 Magenta cross = Origin position (starting point)
- 🟢 Green line = Return path (when returning to origin)
- 🩵 Cyan cross = Patrol waypoint
- ⚪ White line = Patrol path between origin and waypoint
- 🟡 Yellow line = Current patrol target (where enemy is heading)
- 🟢🔴 Green/Red sphere (bottom) = Ground check (Green = grounded, Red = airborne)

---

### Scenario 2: Jumping Enemy (Platformer)

**Goal:** Enemy that jumps randomly or when hitting obstacles during chase.

**Setup:**
1. Select your Enemy
2. In **Jump Settings** section:
   - Change **Jump Mode** to `CollisionBased` (jumps when hitting walls)
   - Set **Jump Force** to `15` (adjust for your scene scale)
   - Set **Jump Cooldown** to `1` (prevents spam jumping)

**Jump Modes Explained:**
- `NoJumping` - Enemy never jumps (default)
- `RandomJumpDuringChase` - Jumps randomly only when chasing player (tactical)
- `RandomJumpAlways` - Jumps randomly at all times: patrol, chase, return, idle (chaotic)
- `CollisionJump` - Jumps only when hitting obstacles (smart navigation)
- `CombinedJump` - Both random and collision-based (aggressive)

**Tip:** Use `CollisionJump` for realistic enemies that navigate obstacles naturally, or `RandomJumpAlways` for unpredictable platformer enemies.

---

### Scenario 3: Aggressive Melee Enemy

**Goal:** Enemy that runs right up to the player and stops (for melee attacks).

**Setup:**
1. Select your Enemy
2. In **Chase Behavior** section:
   - Set **Move Speed** to `10` (faster pursuit)
   - ✅ Enable **Pause When Player Close**
   - Set **Stop Distance** to `2` (stops 2 units away)
3. Wire up **onReachedPlayer** UnityEvent:
   - Add your attack animation/logic here
   - Example: Call an `AttackPlayer()` method on your enemy script

**How It Works:**
- Enemy chases at full speed until within 2 units
- Stops and fires `onReachedPlayer` event
- You trigger attack animations/damage in response to event

**Extension Idea:** Create an "EnemyAttack" script that listens to `onReachedPlayer` and plays an attack animation every 2 seconds.

---

### Scenario 4: Leashed Enemy (Tower Defense)

**Goal:** Enemy that won't chase too far from its starting position.

**Setup:**
1. Select your Enemy
2. In **Player Detection** section:
   - Set **Detection Range** to `8` (starts chasing at 8 units)
   - Set **Leash Range** to `15` (gives up chase at 15 units)
3. In **Return Behavior** section:
   - ✅ Enable **Return To Origin** (should be default)
   - Set **Return Speed** to `6` (walks back steadily)

**Use Cases:**
- Tower defense guards that protect a specific area
- Patrolling sentries that won't abandon their post
- Boss phases where enemy stays in arena center

**Gizmo Visualization:**
(All colors documented in Inspector headers)
- 🟡🔴 Yellow/Red sphere (8 units) = Detection range
- 🟠 Orange sphere (15 units) = Leash range
- 🟣 Magenta cross = Origin position (where enemy returns)
- 🟢🔴 Green/Red sphere (bottom) = Ground check

---

## Essential Parameters Reference

### Movement Settings
- **Move Speed** (default: 8) - How fast enemy chases player
- **Max Velocity** (default: 15) - Speed cap to prevent physics explosions
- **Enable Air Control** (default: off) - Allow movement while airborne

### Player Detection
- **Detection Range** (default: 10) - Distance to start chasing
- **Leash Range** (default: 0 = auto) - Distance to give up chase (auto = detection + 2)
- **Player Tag** (default: "Player") - Tag to search for

### Chase Behavior
- **Stop Distance** (default: 2) - How close to get before stopping
- **Pause When Player Close** (default: on) - Stop moving when within stop distance
- **Rotation Speed** (default: 10) - How fast enemy turns to face player

### Patrol Behavior (Optional)
- **Enable Patrol** (default: off) - Turn on patrolling
- **Patrol Waypoint** - Transform to patrol to
- **Min Patrol Speed** (default: 2) - Minimum movement speed (randomized on spawn)
- **Max Patrol Speed** (default: 4) - Maximum movement speed (randomized on spawn)
- **Min Patrol Pause** (default: 0.5) - Minimum pause at waypoints (randomized each arrival)
- **Max Patrol Pause** (default: 2) - Maximum pause at waypoints (randomized each arrival)

### Ground Detection
- **Ground Check Distance** (default: 0.2) - How far below to check for ground
- **Ground Layer** - Which layers count as ground (usually "Default")

---

## UnityEvents (No-Code Connections)

Connect these events in the Inspector to trigger actions without coding:

### Chase Events
- **onPlayerDetected** - Player enters detection range (play alert sound)
- **onChaseStart** - Enemy starts actively chasing (change animation)
- **onReachedPlayer** - Got within stop distance (trigger attack)
- **onPlayerLost** - Player left leash range (play confused animation)

### Patrol Events
- **onReachedWaypoint** - Reached patrol waypoint (play idle animation)
- **onReachedOriginDuringPatrol** - Back to starting point (look around)

### Return Events
- **onReturnStart** - Started returning to origin (play retreat sound)
- **onReturnComplete** - Arrived back at origin (resume idle state)

### Physics Events
- **onJump** - Enemy jumped (play jump sound)
- **onLanding** - Landed on ground (play thud sound)
- **onMoveApplied** - Movement applied each frame (play footsteps)

---

## Common Issues & Solutions

### ❌ Enemy doesn't move at all
- ✅ Check that **Ground Layer** includes your floor
- ✅ Make sure floor has a **Collider** component
- ✅ Verify **CharacterController** component is present (added automatically)

### ❌ Enemy chases but never stops
- ✅ Enable **Pause When Player Close**
- ✅ Set **Stop Distance** to a value > 0 (try 2)

### ❌ Enemy gets stuck on walls
- ✅ Change **Jump Mode** to `CollisionBased` or `Both`
- ✅ Increase **Jump Force** (try 15-20)
- ✅ Check that walls have colliders

### ❌ Patrol doesn't work (enemy stands still)
- ✅ Verify **Enable Patrol** is checked
- ✅ Assign a **Patrol Waypoint** (empty GameObject)
- ✅ Check waypoint isn't at same position as origin
- ✅ Make sure enemy is grounded (needs floor collider)

### ❌ Enemy falls through moving platforms
- ✅ Set **Platform Layer** to match your platform's layer
- ✅ OR tag platforms with **"movingPlatform"** (default tag)
- ✅ Enable **Apply Vertical Movement**

### ❌ Enemy keeps sliding after stopping
- ✅ This is normal CharacterController behavior
- ✅ Lower **Move Speed** slightly
- ✅ Increase **Rotation Speed** for tighter turns

---

## Next Steps

**You're ready to create AI enemies!** 🎉

Try combining scenarios:
- Patrolling + Jumping = Platformer guard
- Aggressive + Leashed = Boss arena fight
- Patrol + Events = Cutscene-triggered chase

**Pro Tip:** Use the **Scene View Gizmos** to visualize detection ranges. Select your enemy and watch the colored spheres and lines update in real-time. All gizmo colors are now documented directly in the Inspector header sections!

**Gizmo Features:**
- 📍 **Ground Check Now Visible!** - Green/Red sphere at character's feet shows grounded state
- 🎨 **Inspector Color Legend** - Every header section shows which gizmo colors it uses
- 🔍 **Edit Mode Preview** - Most gizmos visible even when not playing (yellow ground check)
- 📊 **Complete Color Legend** - Check the code comments in `OnDrawGizmosSelected()` for full list

**Need more control?** Check the full `EnemyControllerCC` documentation for advanced features like animator integration, custom jump timing, and platform detection modes.

**Remember:** All parameters can be changed at **runtime** using the public methods. Hook them up to UnityEvents for dynamic difficulty scaling!
