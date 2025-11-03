# Example Generators Reference

Complete guide to all one-click example scene generators in the Event Game Toolkit.

## Quick Start

All examples are accessible via **Tools > Examples** menu in Unity Editor. Each generator creates a fully functional scene with pre-configured components, UI annotations, and proper event wiring.

---

## Available Examples (13 Total)

### 1. AutoSpawner Example
**Menu**: Tools > Examples > Generate AutoSpawner Example

- **Demonstrates**: ActionAutoSpawner
- **What It Creates**: Automatic spawner that continuously generates falling objects
- **Features**:
  - Random spawning intervals (min/max timing)
  - Multiple prefab selection with random choice
  - Positional variance using insideUnitSphere
  - Continuous automatic spawning
- **Use Cases**: Enemy spawners, collectible generation, particle-like effects, endless runners
- **Controls**: Watch objects spawn automatically

---

### 2. CharacterController Example ✨
**Menu**: Tools > Examples > Generate CharacterController Example

- **Demonstrates**: CharacterControllerCC (Unity 6 Third Person Controller style)
- **What It Creates**: 3D character with WASD movement and jump using the Cameron model
- **Features**:
  - Modern CharacterController-based movement
  - Camera-relative WASD controls
  - Height-based jump mechanics with timeout
  - Smooth rotation and acceleration/deceleration
  - Grounded detection and animator integration
- **Use Cases**: Third-person games, platformers, adventure games, exploration games
- **Controls**: WASD to move, Space to jump

---

### 3. CheckpointSystem Example
**Menu**: Tools > Examples > Generate CheckpointSystem Example

- **Demonstrates**: GameCheckpointManager
- **What It Creates**: Checkpoints that save player position with death/respawn system
- **Features**:
  - Position saving at checkpoints
  - Respawn on death (falling into death zone)
  - Checkpoint activation via trigger zones
  - Platform-based level with multiple checkpoints
  - DontDestroyOnLoad persistence
- **Use Cases**: Platformers, racing games, checkpoint-based progression, death/respawn systems
- **Controls**: WASD to move, touch pink checkpoints to save, fall off to respawn

---

### 4. CollectionSystem Example
**Menu**: Tools > Examples > Generate CollectionSystem Example

- **Demonstrates**: GameCollectionManager
- **What It Creates**: Collectible items that increase score when player touches them
- **Features**:
  - Score tracking and incrementation
  - Threshold events at 5 and 10 items
  - UI score display with TextMeshPro
  - Collectible destruction on pickup
  - Visual feedback on collection
- **Use Cases**: Coin collection, pickup systems, score-based games, collectathons
- **Controls**: WASD to move, roll into pink spheres to collect

---

### 5. DisplayTextImage Example
**Menu**: Tools > Examples > Generate DisplayText & DisplayImage Example

- **Demonstrates**: ActionDisplayText + ActionDisplayImage
- **What It Creates**: Dialogue sequence between two characters (blue and red noodle) with restart button
- **Features**:
  - Text typewriter effects with configurable speed
  - Image fade and scale animations (DOTween-based)
  - Sequential dialogue playback via ActionEventSequencer
  - Restart button to replay sequence
  - Character portrait positioning (left/right)
- **Use Cases**: Visual novels, cutscenes, dialogue systems, tutorials, story games
- **Controls**: Watch dialogue play automatically, click "Restart Dialogue" to replay

---

### 6. EnemyController Example ✨
**Menu**: Tools > Examples > Generate EnemyController Example

- **Demonstrates**: PhysicsEnemyController
- **What It Creates**: AI enemies that chase the player ball with different behaviors
- **Features**:
  - Player detection within range
  - Chase AI using physics forces
  - Two enemy types: walker (no jump) and jumper (collision-based jumping)
  - Optional sprint mechanics (distance-based)
  - Idle time tracking for animations
  - Grounded state tracking
- **Use Cases**: Enemy AI, chase sequences, patrol systems, boss encounters, competitive racing
- **Controls**: WASD to move, enemies chase you automatically

---

### 7. EventSequencer Example
**Menu**: Tools > Examples > Generate EventSequencer Example

- **Demonstrates**: ActionEventSequencer
- **What It Creates**: Looping 5-second sequence with rotating cube and bouncing ball
- **Features**:
  - Timed event triggering at specific timestamps
  - Multiple events in parallel
  - Sequence looping (continuous playback)
  - Animation coordination
  - Event naming for organization
- **Use Cases**: Cutscenes, animation sequences, timed puzzles, rhythm games, synchronized events
- **Controls**: Watch the sequence loop automatically

---

### 8. HealthSystem Example
**Menu**: Tools > Examples > Generate HealthSystem Example

- **Demonstrates**: GameHealthManager
- **What It Creates**: Damage zones and health bar showing player health
- **Features**:
  - Health tracking (0-100)
  - Damage dealing via trigger zones (10 damage per second)
  - UI health bar with fill visualization
  - Threshold events at 50% and 25% health
  - Death state management
- **Use Cases**: Combat systems, survival games, damage zones, health mechanics, boss fights
- **Controls**: WASD to move, avoid pink damage zones (standing in them depletes health)

---

### 9. InventorySystem Example
**Menu**: Tools > Examples > Generate InventorySystem Example

- **Demonstrates**: GameInventorySlot
- **What It Creates**: Collectible items that fill inventory with capacity limits
- **Features**:
  - Multiple inventory slots with individual capacities
  - Overflow detection and events
  - UI inventory display with TextMeshPro
  - Different item types (keys and gems)
  - Full/empty state management
- **Use Cases**: Item collection, key systems, resource management, limited inventory
- **Controls**: WASD to move, roll into items to collect them

---

### 10. PhysicsBumper Example
**Menu**: Tools > Examples > Generate PhysicsBumper Example

- **Demonstrates**: PhysicsBumper
- **What It Creates**: Bouncy bumper pads that launch ball with visual and physics feedback
- **Features**:
  - Force application (radial or collision normal)
  - DOTween scale animations with AnimationCurve support
  - Material emission effects (glowing on hit)
  - Cooldown system to prevent spam
  - Per-axis scale animation control
- **Use Cases**: Pinball mechanics, bounce pads, launch pads, physics puzzles, bumper cars
- **Controls**: WASD to move, roll into pink bumpers to get launched

---

### 11. PuzzleSystem Example
**Menu**: Tools > Examples > Generate PuzzleSystem Example

- **Demonstrates**: ActionPuzzleRequirement (PuzzleSwitch + PuzzleSwitchChecker)
- **What It Creates**: Combination lock puzzle with 3 switches that must be set correctly
- **Features**:
  - Multi-target requirement checking
  - Puzzle logic (all switches must be ON)
  - Visual feedback (switches turn green when correct)
  - Success detection with events
  - Switch interaction via proximity
- **Use Cases**: Lock puzzles, combination systems, sequential triggers, escape rooms
- **Controls**: WASD to move, roll into switches to toggle them, solve the puzzle to open the door

---

### 12. TimerSystem Example
**Menu**: Tools > Examples > Generate TimerSystem Example

- **Demonstrates**: GameTimerManager
- **What It Creates**: Countdown timer with visual feedback and threshold events
- **Features**:
  - Countdown mode (counts down from 30 seconds)
  - Threshold events at 20s, 10s, and 5s
  - Time formatting (MM:SS display)
  - UI integration with TextMeshPro
  - Pause/resume integration with GameStateManager
- **Use Cases**: Timed challenges, speedruns, countdown timers, time-based events, escape sequences
- **Controls**: Watch timer countdown automatically, events trigger at thresholds

---

### 13. TriggerZone Example
**Menu**: Tools > Examples > Generate TriggerZone Example

- **Demonstrates**: InputTriggerZone
- **What It Creates**: Various trigger zones with enter, exit, and stay events
- **Features**:
  - Collision detection by tag
  - Three event types: Enter, Exit, Stay
  - Stay interval configuration (continuous damage)
  - Visual zone markers (semi-transparent)
  - Multiple zones with different behaviors
- **Use Cases**: Damage zones, goal zones, checkpoint triggers, area detection, safe zones
- **Controls**: WASD to move, roll into different colored zones to trigger events

---

## Common Features Across All Examples

### Automatic Setup
✅ **One-Click Generation**: All examples accessible via Tools > Examples menu
✅ **Pre-Configured Components**: All settings configured to optimal values
✅ **Event Wiring**: All UnityEvents wired via SerializedProperty (persistent)
✅ **UI Annotations**: TextMeshPro instructions displayed on-screen
✅ **EventSystem**: Automatically creates EventSystem for UI button interaction

### Visual Consistency
✅ **Materials**: Uses pink/blue materials from Assets/Materials/
✅ **Camera Setup**: Positions camera for optimal viewing of each example
✅ **Annotations**: Clear on-screen text explaining controls and purpose
✅ **Color Coding**: Pink = interactive elements, Blue = player, Gray = environment

### Play-Ready
✅ **Immediate Playback**: All examples work instantly in Play mode
✅ **No Additional Setup**: No manual configuration required
✅ **Self-Contained**: Each example is fully independent
✅ **Undo Support**: All creation operations registered with Unity's undo system

---

## Helper Scripts

### ExamplePlayerBallFactory.cs
**Purpose**: Creates consistent player balls for examples that need a controllable player.

**What It Creates**:
- Sphere with blue material
- Rigidbody with proper physics settings
- Player tag
- BallController component (WASD movement)
- PlayerInput component with "Player" action map

**Used By**: PhysicsBumper, Collection, Health, Checkpoint, Inventory, TriggerZone, Enemy examples

---

## Technical Implementation Notes

### SerializedProperty Approach
All examples use SerializedProperty to configure UnityEvents because direct manipulation via UnityEventTools or reflection won't persist to the scene file:

```csharp
SerializedObject so = new SerializedObject(component);
SerializedProperty unityEvent = so.FindProperty("onEventName");
AddPersistentListener(unityEvent, targetComponent, "MethodName");
so.ApplyModifiedProperties();
EditorUtility.SetDirty(component);
```

### EventSystem Requirement
Unity's UI button system requires an EventSystem component for click detection. All examples check and create if missing:

```csharp
if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
{
    GameObject eventSystemObj = new GameObject("EventSystem");
    eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
}
```

### Material Paths
Examples expect materials at:
- `Assets/Materials/pink.mat`
- `Assets/Materials/blue.mat`

If materials are missing, examples will log errors and fail gracefully.

### Component Lifecycle
Components with `playOnStart` behavior include `OnEnable()` handlers to restart when re-enabled. This is critical for event sequencers and spawning systems that can be disabled/enabled during gameplay.

---

## Educational Value

### For Students
- **Learn by Example**: See working implementations immediately
- **Reverse Engineer**: Open generated scenes to understand structure
- **Modify and Experiment**: Change parameters and see results
- **Copy Patterns**: Use generated setups as templates

### For Instructors
- **Quick Demos**: Generate examples during lectures
- **Assignment Starting Points**: Students modify generated examples
- **Best Practices**: Examples follow toolkit conventions
- **No-Code Approach**: All examples use Inspector-based UnityEvents

---

## Future Enhancements

Potential additions to the example generator system:

1. **Decal Animation Example**: Demonstrate ActionDecalSequence and ActionDecalSequenceLibrary with facial expressions
2. **Dialogue System Example**: Full ActionDialogueSequence demonstration with multiple characters
3. **Moving Platform Example**: CharacterControllerCC on PhysicsPlatformAnimator
4. **Animation Example**: ActionAnimateTransform with AnimationCurves
5. **Audio Example**: GameAudioManager with music crossfading
6. **Camera Example**: GameCameraManager with Cinemachine switching

---

## Version History

### October 2025 - Initial 13 Examples
- Created comprehensive suite of example generators
- Covered all major toolkit systems
- Implemented SerializedProperty event wiring
- Added EventSystem auto-creation

### November 2025 - Controller Updates
- CharacterControllerExampleGenerator: Updated to use CharacterControllerCC
- EnemyControllerExampleGenerator: Added sprint and idle time parameters
- Both examples now compliant with latest controller APIs

---

**Generated with Event Game Toolkit for Unity 6**
Educational package for Animation and Interactivity class
