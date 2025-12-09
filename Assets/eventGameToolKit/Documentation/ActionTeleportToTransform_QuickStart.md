# ActionTeleportToTransform Quick Start Guide

**Create portal systems and instant teleportation with loop prevention**

Get a working teleporter in 3 minutes, then explore two-way portals, visual effects, and runtime control!

---

## Table of Contents

1. [3-Minute Quick Start](#3-minute-quick-start)
2. [How It Works](#how-it-works)
3. [Two-Way Portals](#two-way-portals)
4. [Visual Effects](#visual-effects)
5. [Runtime Control](#runtime-control)
6. [Common Scenarios](#common-scenarios)
7. [Troubleshooting](#troubleshooting)

---

## 3-Minute Quick Start

**Goal:** Create a simple teleporter that moves the player to a new location.

### Step 1: Create the Destination (30 seconds)

1. Create an empty GameObject (`GameObject > Create Empty`)
2. Name it "TeleportExit"
3. Position it where you want the player to appear

### Step 2: Create the Teleporter (1 minute)

1. Create a Cube (`GameObject > 3D Object > Cube`)
2. Name it "TeleportPad"
3. Add a **Box Collider** and check **Is Trigger**
4. Click **Add Component** and add **InputTriggerZone**
5. Click **Add Component** and add **ActionTeleportToTransform**

### Step 3: Wire It Up (1 minute)

1. In **ActionTeleportToTransform**:
   - Drag "TeleportExit" into the **Destination** field

2. In **InputTriggerZone**:
   - Set **Trigger Object Tag** to "Player"
   - In **On Trigger Enter** event:
     - Click **+** to add event
     - Drag the "TeleportPad" GameObject
     - Select: **ActionTeleportToTransform > TeleportPlayer**

### Step 4: Test It (30 seconds)

1. Press **Play** in Unity
2. Walk into the teleport pad
3. Player instantly appears at the exit!

---

## How It Works

**ActionTeleportToTransform** teleports objects to a destination Transform. It:

- Works with **CharacterControllerCC** (uses built-in TeleportTo method)
- Falls back to **CharacterController** or **Rigidbody** handling
- Includes **teleport immunity** to prevent infinite loops in two-way portals
- Supports **visual effects** at departure and arrival

### Key Features

| Feature | Description |
|---------|-------------|
| **Destination** | Any Transform in the scene |
| **Immunity System** | Prevents teleport loops (default 0.5s) |
| **Cooldown** | Prevents rapid re-triggering |
| **Visual Effects** | Spawn particles at departure/arrival |
| **Events** | React to teleport start/complete/fail |

---

## Two-Way Portals

Create portals that work in both directions without infinite loops!

### The Problem

Without protection, stepping into Portal A teleports you to Portal B, which immediately teleports you back to Portal A, forever.

### The Solution

**Teleport Immunity** - After teleporting, the player can't be teleported again for a brief period (default 0.5 seconds).

### Setup

**Portal A:**
```
PortalA (GameObject)
├── BoxCollider (isTrigger: true)
├── InputTriggerZone
│   └── onTriggerEnter → PortalA_Teleport.TeleportPlayer()
└── ActionTeleportToTransform (PortalA_Teleport)
    └── Destination: PortalB_Exit

PortalA_Exit (empty child of PortalB area)
```

**Portal B:**
```
PortalB (GameObject)
├── BoxCollider (isTrigger: true)
├── InputTriggerZone
│   └── onTriggerEnter → PortalB_Teleport.TeleportPlayer()
└── ActionTeleportToTransform (PortalB_Teleport)
    └── Destination: PortalA_Exit

PortalB_Exit (empty child of PortalA area)
```

**Result:** Player enters Portal A → teleports to Portal B Exit → has immunity → Portal B trigger is ignored → player walks away safely.

### Adjusting Immunity Duration

In the Inspector, adjust **Teleport Immunity Duration**:
- **0.5s** (default) - Quick recovery, responsive feel
- **1.0s** - More forgiving, prevents accidental re-entry
- **0.0s** - No immunity (use for one-way portals only!)

---

## Visual Effects

Add particles or effects when teleporting.

### Departure Effect

Spawns at the player's **original position** before teleporting.

1. Create a particle effect prefab (explosion, swirl, etc.)
2. Drag it into **Departure Effect** field

### Arrival Effect

Spawns at the **destination** when player arrives.

1. Create a particle effect prefab (flash, sparkles, etc.)
2. Drag it into **Arrival Effect** field

### Effect Duration

Set **Effect Duration** to control how long effects stay before being destroyed (default: 2 seconds).

### Example Setup

```
ActionTeleportToTransform
├── Departure Effect: "PortalSwirl" prefab
├── Arrival Effect: "PortalFlash" prefab
└── Effect Duration: 2
```

---

## Runtime Control

### Public Methods

| Method | Description |
|--------|-------------|
| `TeleportPlayer()` | Teleport with configured delay |
| `TeleportPlayerImmediate()` | Teleport instantly (ignores delay) |
| `TeleportObject(GameObject)` | Teleport any GameObject |
| `SetDestination(Transform)` | Change destination at runtime |
| `SetTeleportDelay(float)` | Change delay at runtime |
| `ClearPlayerImmunity()` | Remove immunity early |

### Events

| Event | Description |
|-------|-------------|
| `onTeleportStarted` | Fires when teleport begins (before delay) |
| `onTeleportCompleted` | Fires when player arrives |
| `onTeleportFailed` | Fires if no player or destination found |
| `onTeleportedToPosition(Vector3)` | Fires with destination position |

### Properties

| Property | Description |
|----------|-------------|
| `IsTeleporting` | True during teleport process |
| `IsOnCooldown` | True if cooldown is active |
| `IsPlayerImmune()` | True if player has teleport immunity |
| `Destination` | Current destination Transform |

---

## Common Scenarios

### Scenario 1: Simple One-Way Teleporter

**Goal:** Teleport player to a specific location.

**Setup:**
1. Create trigger zone with InputTriggerZone
2. Add ActionTeleportToTransform
3. Set Destination to target Transform
4. Wire: `onTriggerEnter → TeleportPlayer()`

---

### Scenario 2: Key-Activated Teleporter

**Goal:** Press a key to teleport while standing on a pad.

**Setup:**
1. Create teleport pad with InputTriggerZone
2. Add ActionTeleportToTransform to pad
3. Add InputKeyPress component
4. Wire: `onKeyPressed → TeleportPlayer()`

**Tip:** Use InputTriggerZone's `onTriggerStay` to only allow teleport while on pad.

---

### Scenario 3: Delayed Teleport with Effects

**Goal:** Show charging effect, then teleport after delay.

**Setup:**
```
ActionTeleportToTransform:
├── Teleport Delay: 1.5
├── Departure Effect: "ChargingParticles"
├── Arrival Effect: "ArrivalFlash"
```

Wire: `onTeleportStarted → PlayChargingSound()`

---


### Scenario 5: Teleporter with UI Feedback

**Goal:** Show "Teleporting..." text during teleport.

**Setup:**
```
onTeleportStarted → UIText.SetText("Teleporting...")
onTeleportCompleted → UIText.SetText("")
```

---

### Scenario 6: Checkpoint + Teleporter Combo

**Goal:** Teleport to last checkpoint.

**Setup:**
1. Use GameCheckpointManager for checkpoints
2. On death trigger: Use ActionRespawnPlayer instead
3. Or: Get checkpoint position and call `SetDestination()` + `TeleportPlayer()`

---

## Inspector Reference

### Destination
- **Destination** - Transform to teleport player to
- **Use Destination Rotation** - Apply destination's rotation to player (default: true)

### Player Detection
- **Player Tag** - Tag to find player if not assigned (default: "Player")
- **Player Object** - Optional direct reference to player

### Teleport Settings
- **Teleport Delay** - Wait time before teleporting (default: 0)
- **Reset Velocity** - Zero out player velocity after teleport (default: true)
- **Teleport Cooldown** - Time before this teleporter can be used again (default: 0.5s)
- **Teleport Immunity Duration** - Time player is immune to all teleports after teleporting (default: 0.5s)

### Visual Effects
- **Departure Effect** - Prefab to spawn at origin
- **Arrival Effect** - Prefab to spawn at destination
- **Effect Duration** - How long effects last (default: 2s)

### Events
- **onTeleportStarted** - Teleport begins
- **onTeleportCompleted** - Player arrives
- **onTeleportFailed** - Teleport failed
- **onTeleportedToPosition(Vector3)** - Includes destination position

---

## Troubleshooting

### "Player doesn't teleport"

**Check:**
- Is **Destination** assigned?
- Is player tagged correctly? (default: "Player")
- Is player in the trigger zone?
- Is teleporter on cooldown? Check `IsOnCooldown` property

---

### "Player teleports back and forth infinitely"

**Check:**
- Is **Teleport Immunity Duration** greater than 0?
- Default is 0.5 seconds - increase if loops still occur
- Make sure exits are positioned outside the opposing trigger zone

---

### "Teleport feels laggy"

**Check:**
- Is **Teleport Delay** set to 0 for instant teleport?
- Use `TeleportPlayerImmediate()` to bypass delay entirely

---

### "Effects don't appear"

**Check:**
- Are effect prefabs assigned?
- Do prefabs have particle systems set to **Play On Awake**?
- Is **Effect Duration** long enough to see the effect?

---

### "Player rotation is wrong after teleport"

**Check:**
- Is **Use Destination Rotation** enabled?
- Is the destination Transform rotated correctly in the scene?

---

### "Teleporter works once then stops"

**Check:**
- Is **Teleport Cooldown** too long?
- Is immunity preventing re-use? Try `ClearPlayerImmunity()` if needed

---

## Quick Reference

### Minimum Setup
```
1. Create destination Transform
2. Add InputTriggerZone + ActionTeleportToTransform to trigger
3. Set Destination field
4. Wire: onTriggerEnter → TeleportPlayer()
```

### Two-Way Portal Checklist
```
☐ Both portals have ActionTeleportToTransform
☐ Each portal's Destination is the OTHER portal's exit
☐ Exits are positioned OUTSIDE the opposing trigger
☐ Teleport Immunity Duration > 0 (default 0.5s)
```

### Common Method Calls

**Teleporting:**
- `TeleportPlayer()` - Standard teleport
- `TeleportPlayerImmediate()` - Skip delay
- `TeleportObject(myObject)` - Teleport any object

**Configuration:**
- `SetDestination(newTransform)` - Change target
- `SetTeleportDelay(1.5f)` - Change delay
- `ClearPlayerImmunity()` - Allow immediate re-teleport

---

## You're Ready!

Start with the **3-Minute Quick Start**, get a basic teleporter working, then explore:

1. Set up two-way portals with immunity
2. Add visual effects for polish
3. Wire events for UI feedback
4. Combine with checkpoints for respawn systems

**Simple destination-based teleportation with built-in loop prevention!**
