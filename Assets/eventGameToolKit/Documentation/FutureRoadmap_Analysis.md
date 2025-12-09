# eventGameToolKit Future Roadmap Analysis

## Executive Summary

This report analyzes the eventGameToolKit codebase to plan expansion into **2D platformers**, **3D driving games**, and **turn-based templates**, while addressing the critical **UnityEvent prefab serialization limitation**.

---

## Table of Contents

1. [Current Architecture Assessment](#current-architecture-assessment)
2. [The Prefab UnityEvent Problem](#the-prefab-unityevent-problem)
3. [2D Platformer Implementation Plan](#2d-platformer-implementation-plan)
4. [3D Driving Game Implementation Plan](#3d-driving-game-implementation-plan)
5. [Turn-Based Template Implementation Plan](#turn-based-template-implementation-plan)
6. [Recommended Implementation Order](#recommended-implementation-order)
7. [Summary Tables](#summary-tables)

---

## Current Architecture Assessment

### Strengths of Existing Design

| Strength | Description |
|----------|-------------|
| **Event-Driven Core** | 100% UnityEvent-based - zero code required for basic games |
| **Modular Components** | Mix-and-match design with consistent naming conventions |
| **Manager Pattern** | Centralized coordination (State, Health, Timer, Checkpoint, Audio) |
| **Interface Decoupling** | ISpawnPointProvider pattern allows extensibility |
| **DOTween Integration** | Smooth animations without code |
| **XML Documentation** | 100% coverage enables documentation generation |

### Current Capabilities

The toolkit currently supports:
- 3D character movement (CharacterController-based)
- Physics-based enemies with AI chase behavior
- Platform mechanics (moving platforms, bumpers)
- Health, scoring, timer systems
- Checkpoint/respawn systems
- Dialogue and UI display
- Audio management with crossfading
- Puzzle switches and state machines

### Reusability Matrix

| Component Category | 2D Platformer | 3D Driving | Turn-Based |
|-------------------|---------------|------------|------------|
| **Managers** (State, Health, Timer, Audio, UI) | 100% | 100% | 100% |
| **Actions** (Spawn, Display, Dialogue, Sound) | 95% | 90% | 100% |
| **Input** (Key, Mouse, Trigger zones) | 80%* | 70%* | 90% |
| **Character Controllers** | 0%** | 0%** | 50%*** |
| **Physics** (Bumpers, Platforms) | 20%** | 10%** | N/A |

*Requires 2D/3D variants for collision detection
**Requires new implementations
***Turn-based doesn't need real-time movement

---

## The Prefab UnityEvent Problem

### The Core Issue

UnityEvents configured in the Inspector **do not serialize properly on prefabs** that are instantiated at runtime. When a student:

1. Creates a prefab with UnityEvent connections
2. Instantiates that prefab via `ActionSpawnObject` or `Instantiate()`
3. The spawned instance has **empty UnityEvents** - all connections are lost

This is a fundamental Unity limitation, not a bug in eventGameToolKit.

### Why This Matters

Students want to create:
- Enemies that damage the player on collision
- Collectibles that add to score when picked up
- Projectiles that trigger effects on impact
- Power-ups that modify player stats

Currently, the workaround is: **"Configure events only on permanent GameObjects, not spawned objects."**

### Oblique Strategies to Address This

#### Strategy 1: Tag-Based Auto-Discovery (Recommended)

**Concept**: Spawned objects use tags to find permanent managers and register themselves.

**Implementation Pattern**:
```csharp
// On spawned collectible prefab:
public class CollectibleItem : MonoBehaviour
{
    [SerializeField] private string scoreManagerTag = "ScoreManager";
    [SerializeField] private int scoreValue = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Find manager by tag and call method directly
            GameObject manager = GameObject.FindWithTag(scoreManagerTag);
            if (manager != null)
            {
                manager.GetComponent<GameCollectionManager>()?.AddScore(scoreValue);
            }
            Destroy(gameObject);
        }
    }
}
```

**Pros**:
- Zero Inspector configuration needed on prefab
- Works with spawned objects
- Students just set tag on manager once

**Cons**:
- Slightly less flexible than UnityEvents
- Requires knowing method names

**New Scripts Needed**:
- `CollectibleItem.cs` - Auto-finds score manager
- `DamageOnContact.cs` - Auto-finds health manager
- `PowerUpOnContact.cs` - Auto-modifies player stats

#### Strategy 2: ScriptableObject Event Channels

**Concept**: Create ScriptableObject assets that act as event buses. Both prefabs and scene objects reference the same SO asset.

**Implementation Pattern**:
```csharp
// ScriptableObject event channel
[CreateAssetMenu(menuName = "Events/Void Event Channel")]
public class VoidEventChannel : ScriptableObject
{
    public event System.Action OnEventRaised;

    public void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}

// On prefab - raises event
public class CollectibleBroadcaster : MonoBehaviour
{
    [SerializeField] private VoidEventChannel onCollectedChannel;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            onCollectedChannel?.RaiseEvent();
            Destroy(gameObject);
        }
    }
}

// On permanent manager - listens
public class ScoreListener : MonoBehaviour
{
    [SerializeField] private VoidEventChannel onCollectedChannel;
    [SerializeField] private int scorePerCollect = 10;

    private void OnEnable() => onCollectedChannel.OnEventRaised += AddScore;
    private void OnDisable() => onCollectedChannel.OnEventRaised -= AddScore;

    private void AddScore() => GetComponent<GameCollectionManager>()?.AddScore(scorePerCollect);
}
```

**Pros**:
- True decoupling - prefab and manager don't know about each other
- Reusable event channels
- Works across scenes

**Cons**:
- More complex mental model
- Requires creating SO assets in Project
- Two-step setup (prefab + listener)

#### Strategy 3: Singleton Manager Auto-Registration

**Concept**: Managers expose static references. Spawned objects find them automatically.

**Implementation Pattern**:
```csharp
// Manager exposes singleton
public class GameCollectionManager : MonoBehaviour
{
    public static GameCollectionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
}

// Spawned object uses singleton
public class Collectible : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameCollectionManager.Instance?.AddScore(scoreValue);
            Destroy(gameObject);
        }
    }
}
```

**Pros**:
- Simplest code pattern
- No tag/find overhead
- Fast access

**Cons**:
- Singleton pattern can be fragile
- Only one manager instance allowed
- Tighter coupling

#### Strategy 4: Collision Response Components

**Concept**: Create specialized components that handle common collision patterns without needing UnityEvents.

**New Components**:
```
CollisionDamage.cs     - Damages player on contact (configurable amount)
CollisionScore.cs      - Adds score on contact (configurable amount)
CollisionHealth.cs     - Heals player on contact
CollisionDestroy.cs    - Destroys self on contact
CollisionSpawn.cs      - Spawns prefab on contact
CollisionSound.cs      - Plays sound on contact
```

**Example Implementation**:
```csharp
/// <summary>
/// Adds score when player touches this object. Works on spawned prefabs.
/// </summary>
public class CollisionScore : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField] private int scoreAmount = 10;
    [SerializeField] private bool destroyOnCollect = true;

    [Header("Target")]
    [SerializeField] private string playerTag = "Player";

    [Header("Effects")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private GameObject collectEffect;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // Find score manager via interface or singleton
        var scoreManager = FindObjectOfType<GameCollectionManager>();
        scoreManager?.AddScore(scoreAmount);

        // Play effects
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        if (collectEffect != null)
            Instantiate(collectEffect, transform.position, Quaternion.identity);

        if (destroyOnCollect)
            Destroy(gameObject);
    }
}
```

**Pros**:
- No configuration needed - just add component
- All settings on the prefab itself
- Works perfectly with spawning

**Cons**:
- Less flexible than UnityEvents
- Fixed behavior patterns
- More components to learn

### Recommended Approach: Hybrid Strategy

Combine **Strategy 1 (Tag-Based)** and **Strategy 4 (Collision Response)** for the best balance:

1. **For common patterns** (damage, score, pickup): Use pre-built Collision* components
2. **For custom behavior**: Use tag-based manager discovery
3. **Keep UnityEvents** for permanent scene objects (they work perfectly there)

**New Component Set**:
```
Runtime/Collision/
├── CollisionScore.cs        - Add score on contact
├── CollisionDamage.cs       - Deal damage on contact
├── CollisionHeal.cs         - Restore health on contact
├── CollisionDestroy.cs      - Destroy self/other on contact
├── CollisionSpawnEffect.cs  - Spawn particle/prefab on contact
├── CollisionPlaySound.cs    - Play audio on contact
├── CollisionTeleport.cs     - Teleport to target on contact
└── CollisionEvent.cs        - Raise ScriptableObject event on contact
```

**Documentation Update**:
```markdown
## When to Use UnityEvents vs Collision Components

| Scenario | Recommended Approach |
|----------|---------------------|
| Permanent scene objects (buttons, doors, switches) | UnityEvents |
| Spawned collectibles (coins, gems, power-ups) | CollisionScore |
| Spawned enemies (damage player) | CollisionDamage |
| Projectiles (damage on hit) | CollisionDamage + CollisionDestroy |
| Spawned power-ups | CollisionHeal, custom scripts |
```

---

## 2D Platformer Implementation Plan

### Overview

2D platformers require different physics, collision detection, and camera systems than 3D games, but share the same game logic patterns.

### What Can Be Reused (No Changes)

| Component | Notes |
|-----------|-------|
| GameStateManager | Pause, victory, game over - identical |
| GameHealthManager | Health logic is dimension-agnostic |
| GameTimerManager | Time tracking - identical |
| GameCollectionManager | Score/collectibles - identical |
| GameAudioManager | Audio - identical |
| GameCheckpointManager | Position storage works (just ignore Z) |
| GameUIManager | UI is screen-space - identical |
| ActionDisplayText/Image | UI components - identical |
| ActionDialogueSequence | Dialogue - identical |
| ActionSpawnObject | Spawning works in 2D (just set Z=0) |
| ActionEventSequencer | Event timing - identical |
| PuzzleSwitch/Checker | Puzzle logic - identical |

### New Scripts Required

#### Core Movement

**CharacterController2D.cs** (~400 lines estimated)
```
Purpose: 2D player movement with platformer feel
Physics: Rigidbody2D + CapsuleCollider2D
Features:
- Horizontal movement with acceleration/deceleration
- Variable jump height (hold to jump higher)
- Coyote time (jump briefly after leaving platform)
- Jump buffering (queue jump before landing)
- Wall slide and wall jump (optional)
- One-way platforms (drop through)
- Ground detection via CircleCast2D
- Animation parameters (Speed, IsGrounded, IsJumping)
- ISpawnPointProvider integration

Events:
- onGrounded
- onJump
- onLanding
- onWallSlide (optional)
- onWallJump (optional)
```

**EnemyController2D.cs** (~300 lines estimated)
```
Purpose: 2D enemy AI with patrol and chase
Physics: Rigidbody2D
Features:
- Patrol between waypoints
- Edge detection (don't walk off cliffs)
- Player detection via raycast
- Chase behavior
- Sprite flipping for direction
- Animation support

Events:
- onPlayerDetected
- onPatrolWaypointReached
- onChaseStart
- onChaseEnd
```

#### Physics Components

**PhysicsBumper2D.cs** (~150 lines)
```
Purpose: Bounce pads for 2D
Physics: Trigger2D + AddForce2D
Features:
- Configurable bounce force
- Directional or normal-based bounce
- Cooldown and animation
```

**PhysicsPlatform2D.cs** (~200 lines)
```
Purpose: Moving platforms for 2D
Features:
- Waypoint-based movement
- Player sticking (parent transform or velocity matching)
- One-way platform option
- Animation curves for easing
```

#### Input Components

**InputTriggerZone2D.cs** (~60 lines)
```
Purpose: 2D trigger detection
Uses: OnTriggerEnter2D, OnTriggerExit2D
Events: onTriggerEnter, onTriggerStay, onTriggerExit
```

**InputCollisionEnter2D.cs** (~80 lines)
```
Purpose: 2D collision detection (non-trigger)
Uses: OnCollisionEnter2D, OnCollisionExit2D
Events: onCollisionEnter, onCollisionExit
```

### Camera Considerations

Cinemachine works for 2D with these settings:
- Virtual Camera with Framing Transposer
- Orthographic projection
- Confiner2D for level bounds

**No new camera scripts needed** - just documentation for 2D Cinemachine setup.

### Estimated New Code

| Script | Estimated Lines | Complexity |
|--------|-----------------|------------|
| CharacterController2D | 400 | High |
| EnemyController2D | 300 | Medium |
| PhysicsBumper2D | 150 | Low |
| PhysicsPlatform2D | 200 | Medium |
| InputTriggerZone2D | 60 | Low |
| InputCollisionEnter2D | 80 | Low |
| **Total** | **~1,190** | |

### Configuration Complexity

**Low** - All new components follow existing patterns:
- Same event structure
- Same Inspector organization
- Same naming conventions
- Familiar to students already using 3D toolkit

---

## 3D Driving Game Implementation Plan

### Overview

Driving games require fundamentally different physics (wheel-based or arcade) and control schemes, but can reuse all manager and UI systems.

### What Can Be Reused (No Changes)

| Component | Notes |
|-----------|-------|
| All Game Managers | State, health, timer, score, audio, checkpoint |
| All UI Components | Display text, images, dialogue |
| All Action Components | Spawning, effects, sounds |
| GameCameraManager | Camera switching works |
| InputKeyPress | Basic key detection |
| ActionEventSequencer | Event timing |

### New Scripts Required

#### Core Vehicle System

**VehicleController.cs** (~600 lines estimated)
```
Purpose: Arcade-style vehicle physics
Physics: Rigidbody with custom forces
Control Modes:
1. Arcade (recommended for students):
   - Direct steering angle control
   - Simplified acceleration/braking
   - Optional drift mechanic

2. Simulation (advanced):
   - WheelCollider-based
   - Realistic suspension
   - Tire friction curves

Features:
- Steering with configurable responsiveness
- Acceleration/braking curves
- Speed-based steering reduction (stability at high speed)
- Ground detection for jumps
- Drift/handbrake mechanic
- Nitro boost (optional)
- Damage on collision (optional)
- Animation hooks for wheels, steering wheel

Events:
- onAccelerate
- onBrake
- onDrift
- onNitro
- onCollision
- onGrounded
- onAirborne
```

**VehicleAI.cs** (~400 lines estimated)
```
Purpose: AI-controlled vehicles for racing
Features:
- Waypoint following
- Speed regulation based on corners
- Obstacle avoidance
- Rubber-banding (catch up to player)
- Collision recovery

Events:
- onWaypointReached
- onLapComplete
- onCollision
```

#### Supporting Components

**Checkpoint3D.cs** (~150 lines)
```
Purpose: Racing checkpoint/lap system
Features:
- Ordered checkpoint validation
- Lap counting
- Time recording per checkpoint
- Wrong-way detection

Events:
- onCheckpointReached
- onLapComplete
- onWrongWay
```

**RaceManager.cs** (~250 lines)
```
Purpose: Race state management
Features:
- Countdown start sequence
- Position tracking (1st, 2nd, 3rd...)
- Finish detection
- Time tracking

Events:
- onCountdownTick (3, 2, 1, GO!)
- onRaceStart
- onPositionChanged
- onRaceFinish
```

**SpeedBoostZone.cs** (~60 lines)
```
Purpose: Trigger zone that boosts vehicle speed
Features:
- Configurable boost amount and duration
- Visual/audio feedback

Events:
- onBoostActivated
```

#### Camera System

**VehicleCamera.cs** (~200 lines)
```
Purpose: Vehicle-specific camera behavior
Features:
- Third-person follow with lag
- Speed-based FOV adjustment
- Look-behind button
- Cockpit view option
- Smooth rotation following

Events:
- onCameraSwitch
```

Alternatively, configure Cinemachine with:
- Transposer for position follow
- Composer for rotation
- Custom extension for speed-based FOV

### Estimated New Code

| Script | Estimated Lines | Complexity |
|--------|-----------------|------------|
| VehicleController | 600 | High |
| VehicleAI | 400 | High |
| Checkpoint3D | 150 | Medium |
| RaceManager | 250 | Medium |
| SpeedBoostZone | 60 | Low |
| VehicleCamera | 200 | Medium |
| **Total** | **~1,660** | |

### Configuration Complexity

**Medium-High** - Vehicle physics requires tuning:
- Acceleration curves
- Steering sensitivity
- Drift behavior
- Weight distribution

Recommend providing **presets** (Arcade Kart, Realistic Car, Monster Truck) to reduce configuration burden.

---

## Turn-Based Template Implementation Plan

### Overview

Turn-based games are fundamentally different from real-time games. They don't need physics-based movement, but require state machines, action queues, and grid/positioning systems.

### What Can Be Reused (No Changes)

| Component | Notes |
|-----------|-------|
| GameStateManager | Pause, game over - useful |
| GameHealthManager | Health/damage - directly applicable |
| GameAudioManager | Audio - identical |
| GameUIManager | UI - identical |
| All Display Actions | Text, images, dialogue - core to turn-based |
| ActionEventSequencer | Event timing - very useful |
| PuzzleSwitch/Checker | State machines - directly applicable |

### Not Applicable (Skip)

| Component | Why |
|-----------|-----|
| CharacterControllerCC | No real-time movement |
| PhysicsEnemyController | No physics-based AI |
| PhysicsBumper/Platform | No physics puzzles |
| Timer components | Turn-based, not time-based |

### New Scripts Required

#### Core Turn System

**TurnManager.cs** (~300 lines estimated)
```
Purpose: Controls turn flow
Features:
- Turn order (player, enemy, player, enemy...)
- Initiative/speed-based ordering (optional)
- Turn phases (Start, Action, End)
- Skip turn option
- Wait for animation completion

Events:
- onTurnStart (who's turn)
- onTurnEnd
- onPlayerTurn
- onEnemyTurn
- onRoundComplete
```

**ActionQueue.cs** (~200 lines estimated)
```
Purpose: Queue and execute turn actions
Features:
- Queue multiple actions
- Execute sequentially with timing
- Cancel/interrupt support
- Animation integration

Events:
- onActionQueued
- onActionExecuted
- onQueueComplete
```

#### Unit System

**TurnBasedUnit.cs** (~250 lines estimated)
```
Purpose: Base class for player/enemy units
Features:
- Stats (HP, Attack, Defense, Speed)
- Action points per turn
- Status effects (poison, stun, buff)
- Death/removal handling
- Animation triggers

Events:
- onDamageReceived
- onHealed
- onStatusApplied
- onStatusRemoved
- onDeath
```

**UnitAction.cs** (ScriptableObject, ~150 lines)
```
Purpose: Define actions units can take
Features:
- Action name and description
- Action cost (AP)
- Target type (self, enemy, ally, area)
- Damage/heal amount
- Status effects to apply
- Animation to play
- Sound to play
```

#### Grid System (Optional)

**GridManager.cs** (~350 lines estimated)
```
Purpose: Tile-based positioning
Features:
- Grid creation (width x height)
- Tile types (walkable, blocked, hazard)
- Pathfinding (A* or simple)
- Range visualization
- Tile selection

Events:
- onTileSelected
- onTileHovered
- onPathCalculated
```

**GridUnit.cs** (~150 lines estimated)
```
Purpose: Unit positioning on grid
Features:
- Grid coordinates
- Movement range
- Attack range
- Facing direction
- Move animation

Events:
- onMoved
- onFacingChanged
```

#### UI Components

**TurnOrderDisplay.cs** (~100 lines)
```
Purpose: Show turn order visually
Features:
- Portrait/icon queue
- Current turn highlight
- Preview next N turns
```

**ActionMenu.cs** (~150 lines)
```
Purpose: Player action selection
Features:
- Button-based action list
- Action cost display
- Target selection mode
- Cancel button

Events:
- onActionSelected
- onTargetSelected
- onCancelled
```

### Estimated New Code

| Script | Estimated Lines | Complexity |
|--------|-----------------|------------|
| TurnManager | 300 | High |
| ActionQueue | 200 | Medium |
| TurnBasedUnit | 250 | Medium |
| UnitAction (SO) | 150 | Low |
| GridManager | 350 | High |
| GridUnit | 150 | Medium |
| TurnOrderDisplay | 100 | Low |
| ActionMenu | 150 | Medium |
| **Total** | **~1,650** | |

### Configuration Complexity

**Medium** - Turn-based requires:
- Defining actions (ScriptableObjects)
- Setting up unit stats
- Grid layout (if using grid)
- Turn order rules

Recommend providing **example units and actions** as templates.

---

## Recommended Implementation Order

### Phase 1: Foundation (Address Prefab Problem)
**Priority: Critical**
**Effort: 1-2 weeks**

1. Create Collision Response Components:
   - CollisionScore
   - CollisionDamage
   - CollisionHeal
   - CollisionDestroy
   - CollisionSpawnEffect
   - CollisionPlaySound

2. Update documentation explaining when to use UnityEvents vs Collision components

3. Create example prefabs demonstrating patterns

### Phase 2: 2D Platformer Support
**Priority: High**
**Effort: 2-3 weeks**

1. CharacterController2D (core movement)
2. EnemyController2D (AI)
3. InputTriggerZone2D and InputCollisionEnter2D
4. PhysicsBumper2D and PhysicsPlatform2D
5. 2D example scene
6. Documentation and quick-start guide

### Phase 3: Turn-Based Template
**Priority: Medium**
**Effort: 3-4 weeks**

1. TurnManager and ActionQueue
2. TurnBasedUnit and UnitAction (ScriptableObject)
3. UI components (TurnOrderDisplay, ActionMenu)
4. GridManager and GridUnit (optional but valuable)
5. Example turn-based scene
6. Documentation and quick-start guide

### Phase 4: 3D Driving Games
**Priority: Lower**
**Effort: 3-4 weeks**

1. VehicleController (arcade mode first)
2. VehicleCamera or Cinemachine configuration
3. Checkpoint3D and RaceManager
4. VehicleAI (optional)
5. Example racing scene
6. Documentation and quick-start guide

---

## Summary Tables

### New Scripts by Category

| Category | 2D Platformer | 3D Driving | Turn-Based | Collision |
|----------|---------------|------------|------------|-----------|
| Character/Vehicle | 2 | 2 | - | - |
| Physics | 2 | 1 | - | - |
| Input | 2 | - | - | - |
| Game Logic | - | 2 | 4 | - |
| UI | - | - | 2 | - |
| Grid | - | - | 2 | - |
| Collision | - | - | - | 6 |
| **Total New Scripts** | **6** | **5** | **8** | **6** |

### Estimated Lines of Code

| Template | New Lines | Reused Lines | % Reused |
|----------|-----------|--------------|----------|
| 2D Platformer | ~1,190 | ~8,000+ | 87% |
| 3D Driving | ~1,660 | ~8,000+ | 83% |
| Turn-Based | ~1,650 | ~5,000+ | 75% |
| Collision Components | ~600 | N/A | N/A |

### Configuration Complexity Comparison

| Aspect | Current 3D | 2D Platformer | 3D Driving | Turn-Based |
|--------|------------|---------------|------------|------------|
| Character Setup | Medium | Medium | High | Low |
| Physics Tuning | Low | Low | High | None |
| Event Wiring | High | High | Medium | Medium |
| Asset Creation | Low | Low | Medium | High* |

*Turn-based requires creating UnitAction ScriptableObjects

### Student Learning Curve

| Template | New Concepts | Difficulty |
|----------|--------------|------------|
| 2D Platformer | 2D colliders, sprites | Easy transition |
| 3D Driving | Vehicle physics, racing | Medium |
| Turn-Based | State machines, actions | Medium-High |

---

## Appendix: Prefab-Friendly Component Examples

### CollisionScore.cs (Full Implementation)

```csharp
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Adds score when player collides with this object. Works on spawned prefabs without Inspector event wiring.
/// Common use: Coins, gems, collectibles, or any pickup that should add to player's score.
/// </summary>
public class CollisionScore : MonoBehaviour
{
    [Header("Score Settings")]
    [Tooltip("Amount of score to add when collected")]
    [SerializeField] private int scoreAmount = 10;

    [Tooltip("Destroy this object after collection")]
    [SerializeField] private bool destroyOnCollect = true;

    [Header("Detection")]
    [Tooltip("Tag of the object that can collect this (usually 'Player')")]
    [SerializeField] private string collectorTag = "Player";

    [Tooltip("Use trigger detection (requires IsTrigger on collider)")]
    [SerializeField] private bool useTrigger = true;

    [Header("Effects")]
    [Tooltip("Sound to play when collected")]
    [SerializeField] private AudioClip collectSound;

    [Tooltip("Effect prefab to spawn when collected (particles, etc.)")]
    [SerializeField] private GameObject collectEffect;

    [Header("Events (Optional)")]
    /// <summary>
    /// Fires when this object is collected. Works on scene objects, NOT spawned prefabs.
    /// For spawned prefabs, the score is added automatically without needing this event.
    /// </summary>
    public UnityEvent onCollected;

    private bool isCollected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (useTrigger) HandleCollection(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!useTrigger) HandleCollection(collision.gameObject);
    }

    private void HandleCollection(GameObject collector)
    {
        if (isCollected) return;
        if (!collector.CompareTag(collectorTag)) return;

        isCollected = true;

        // Find and update score manager
        var scoreManager = FindObjectOfType<GameCollectionManager>();
        if (scoreManager != null)
        {
            scoreManager.AddScore(scoreAmount);
        }
        else
        {
            Debug.LogWarning($"CollisionScore: No GameCollectionManager found in scene!", this);
        }

        // Play sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        // Spawn effect
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Fire event (only works for scene objects, not spawned prefabs)
        onCollected?.Invoke();

        // Destroy if configured
        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sets the score amount at runtime
    /// </summary>
    public void SetScoreAmount(int amount)
    {
        scoreAmount = amount;
    }
}
```

### CollisionDamage.cs (Full Implementation)

```csharp
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Deals damage to player when they collide with this object. Works on spawned prefabs.
/// Common use: Enemy projectiles, hazards, spikes, lava, or any damaging obstacle.
/// </summary>
public class CollisionDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Amount of damage to deal")]
    [SerializeField] private int damageAmount = 10;

    [Tooltip("Destroy this object after dealing damage")]
    [SerializeField] private bool destroyOnDamage = false;

    [Tooltip("Cooldown between damage applications (0 = damage once only)")]
    [SerializeField] private float damageCooldown = 0f;

    [Header("Detection")]
    [Tooltip("Tag of the object to damage (usually 'Player')")]
    [SerializeField] private string targetTag = "Player";

    [Tooltip("Use trigger detection (requires IsTrigger on collider)")]
    [SerializeField] private bool useTrigger = true;

    [Header("Effects")]
    [Tooltip("Sound to play when damage is dealt")]
    [SerializeField] private AudioClip damageSound;

    [Tooltip("Effect prefab to spawn when damage is dealt")]
    [SerializeField] private GameObject damageEffect;

    [Header("Events (Optional)")]
    /// <summary>
    /// Fires when damage is dealt. Works on scene objects, NOT spawned prefabs.
    /// </summary>
    public UnityEvent onDamageDealt;

    private float lastDamageTime = -Mathf.Infinity;

    private void OnTriggerEnter(Collider other)
    {
        if (useTrigger) HandleDamage(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        if (useTrigger && damageCooldown > 0) HandleDamage(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!useTrigger) HandleDamage(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!useTrigger && damageCooldown > 0) HandleDamage(collision.gameObject);
    }

    private void HandleDamage(GameObject target)
    {
        if (!target.CompareTag(targetTag)) return;

        // Check cooldown
        if (damageCooldown > 0 && Time.time < lastDamageTime + damageCooldown) return;
        lastDamageTime = Time.time;

        // Find health manager on target or in scene
        var healthManager = target.GetComponent<GameHealthManager>();
        if (healthManager == null)
        {
            healthManager = FindObjectOfType<GameHealthManager>();
        }

        if (healthManager != null)
        {
            healthManager.TakeDamage(damageAmount);
        }
        else
        {
            Debug.LogWarning($"CollisionDamage: No GameHealthManager found!", this);
        }

        // Play sound
        if (damageSound != null)
        {
            AudioSource.PlayClipAtPoint(damageSound, transform.position);
        }

        // Spawn effect
        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }

        // Fire event
        onDamageDealt?.Invoke();

        // Destroy if configured
        if (destroyOnDamage)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sets the damage amount at runtime
    /// </summary>
    public void SetDamageAmount(int amount)
    {
        damageAmount = amount;
    }
}
```

---

## Conclusion

The eventGameToolKit has an excellent foundation for expansion. The modular, event-driven architecture means that **75-90% of existing code can be reused** across all three target genres.

**Key Recommendations**:

1. **Address the prefab problem first** - Collision components will unlock spawned object functionality
2. **Start with 2D platformers** - Smallest learning curve, highest demand
3. **Add turn-based next** - Unique value proposition, complements real-time templates
4. **Add driving last** - More niche, higher complexity

The estimated total new code across all templates is approximately **5,100 lines**, compared to the current ~15,000 lines in the toolkit. This represents significant value from a relatively modest investment.
