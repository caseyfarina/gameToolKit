# Runtime Folder Structure & Component Reference

Complete inventory of all educational scripts in the eventGameToolKit.

## Folder Organization

```
Runtime/
├── Actions/              # Event target components (16 scripts)
│   ├── Audio/           # Sound playback
│   ├── DecalAnimation/  # URP decal material animations
│   ├── Display/         # UI text and images
│   ├── Events/          # Event sequencing and animation triggers
│   ├── Scene/           # Scene management
│   └── Spawning/        # Object instantiation
├── Animation/           # Transform animations (1 script)
├── CharacterControllers/ # Player and enemy controllers (6 scripts)
│   ├── Enemy/           # AI enemy controllers
│   └── Player/          # Player controllers
├── Game/                # Game managers (9 scripts)
├── Input/               # Event source components (8 scripts)
├── Interfaces/          # Core interfaces (1 interface)
├── Physics/             # Physics systems (5 scripts)
│   ├── Bumpers/         # Repulsion forces
│   └── Platforms/       # Moving platforms
├── Puzzle/              # Puzzle mechanics (2 scripts)
├── UI/                  # UI helpers (1 script)
└── Utilities/           # Legacy/helper scripts (3 scripts)
```

---

## Input Components (8 scripts)

**Location**: `Runtime/Input/`

Event sources triggered by player input or game state.

### InputKeyPress.cs
- Simple key press event system
- Fires UnityEvent when specific key is pressed

### InputKeyCountdown.cs
- Key-based countdown timer with TextMeshPro display
- Displays remaining time on screen
- Fires event when countdown reaches zero

### InputTriggerZone.cs
- 3D collision detection by tag
- Enter/exit/stay events with configurable intervals
- Perfect for damage zones, collectibles, checkpoints

### InputCheckpointZone.cs
- Feature-rich checkpoint trigger zone
- Integrates with GameCheckpointManager (ISpawnPointProvider)
- One-time use or repeatable activation
- Optional full game state saving (score, health)
- Spawn point offset and rotation control
- Visual feedback (material changes, disable effects)
- Excellent gizmos showing spawn position and offset
- Tag-based filtering for player detection
- See [Checkpoint Quick Start Guide](../CheckpointSystem_QuickStart.md)

### InputQuitGame.cs
- Application quit on Escape key press
- Simple scene exit functionality

### InputActionEvent.cs
- Bridge between Unity Input System and UnityEvents
- Maps Input Actions to event triggers
- Supports button, axis, and vector inputs

### InputMouseInteraction.cs
- Mouse-based interaction system
- Click, hover, and drag detection
- Raycast-based object selection

### InputCollisionEnter.cs
**Location**: `Runtime/Utilities/`
- Collision event system (OnCollisionEnter)
- Tag-based filtering
- Fires UnityEvent on collision

---

## Action Components (16 scripts)

**Location**: `Runtime/Actions/`

Event targets that perform actions when triggered.

### Audio (1 script)

#### ActionPlaySound.cs
**Location**: `Actions/Audio/`
- Simple audio playback
- One-shot sound effects
- Volume and pitch control

### DecalAnimation (4 scripts)

**Location**: `Actions/DecalAnimation/`

#### ActionDecalSequence.cs
- Frame-by-frame material animation for URP DecalProjector
- Custom timing per frame (MaterialFrame struct)
- Playback controls: Play, Stop, Pause, Resume, JumpToFrame
- Adjustable playback speed (0.1x to 5x)
- Loop mode support
- 6 UnityEvents: onSequenceStart, onSequenceComplete, onFrameChanged, onPause, onResume, onStop
- Works standalone - no library required

#### ActionDecalSequenceLibrary.cs
- Manager for multiple ActionDecalSequence components
- Switch between sequences by index or name
- Navigation: PlayNext(), PlayPrevious(), PlaySequence(int)
- Control current sequence: Pause, Resume, Stop
- 2 UnityEvents: onSequenceChanged, onLibraryStopped

#### ActionBlinkDecal.cs
- Automatic eye blinking using material switching
- Two-state animation (open ↔ closed eyes)
- Randomized timing with configurable variation
- Manual BlinkOnce() method
- 2 UnityEvents: onBlinkStart, onBlinkComplete

#### ActionBlinkDecalOptimized.cs
- Optimized texture-based blinking (more efficient)
- Creates material instance automatically
- Configurable shader property (default "_BaseMap" for URP)
- Recommended for scenes with multiple blinking characters

### Display (3 scripts + 1 helper)

**Location**: `Actions/Display/`

#### ActionDisplayImage.cs
- UI image display with DOTween animations
- Fade in/out effects with customizable durations
- Scale animations with start/target scale control
- Simultaneous fade and scale using DOTween Sequences

#### ActionDisplayText.cs
- TextMeshPro text display with DOTween animations
- Fade in/out transitions
- Typewriter effect with adjustable speed
- Automatic text clearing after duration
- 4 TMP formatting controls: fontSize, textAlignment, textColor, font

#### ActionDialogueSequence.cs
- Complete dialogue system for visual novels/story games
- Sequential dialogue line playback (auto or manual advance)
- Character portrait support (left/right positioning)
- Image animations: None, SlideUpFromBottom, SlideInFromSide, FadeIn, PopIn
- Text animations: None, TypeOn, FadeIn, SlideUpFromBottom
- Customizable background image with position/size control
- DOTween-based animations with durations and easing
- Loop mode for repeating dialogues
- Preview system in Editor with custom inspector
- Decision system with player choices
- 4 TMP formatting controls: fontSize, textAlignment, textColor, font
- 4 UnityEvents: onDialogueStart, onDialogueComplete, onLineChanged, onDecisionStart

#### DialogueUIController.cs (Helper)
- Companion script for ActionDialogueSequence
- Creates and manages dialogue UI elements
- Auto-created at runtime (not student-facing)

### Events (3 scripts)

**Location**: `Actions/Events/`

#### ActionEventSequencer.cs
- Sequential event triggering with delays
- Chain multiple events with custom timing
- Loop support for repeating sequences

#### ActionPlayCharacterEmoteAnimation.cs
- Trigger character animations via events
- String-based animation trigger names
- Works with Unity Animator

#### ActionTriggerAnimatorParameter.cs
- Set Animator parameters via events
- Supports bool, int, float, and trigger parameters
- No-code animation control

### Scene (2 scripts)

**Location**: `Actions/Scene/`

#### ActionRestartScene.cs
- Scene restart functionality
- Button/event only (no key binding)
- Reloads current scene

#### ActionRespawnPlayer.cs
- Player respawn system
- Teleports player to spawn point
- Resets velocity and rotation

### Spawning (3 scripts)

**Location**: `Actions/Spawning/`

#### ActionSpawnObject.cs
- Manual single object spawning
- Instantiate prefabs at spawn point
- Optional parent assignment

#### ActionAutoSpawner.cs
- Automatic object spawner
- Random spawn timing (min/max intervals)
- Multiple prefab support with random selection
- Positional variance using insideUnitSphere

#### ActionSpawnProjectile.cs
- Projectile spawning with velocity
- Direction and force control
- Pooling support for performance

---

## Animation Components (1 script)

**Location**: `Runtime/Animation/`

### ActionAnimateTransform.cs
- Procedural transform animation using DOTween with AnimationCurves
- Animates 9 transform properties independently: PositionX/Y/Z, RotationX/Y/Z, ScaleX/Y/Z
- Uses AnimationCurve for complex easing (set in Inspector)
- DOTween integration with `.SetEase(curve)` for curve support
- Supports Offset mode (add to initial) and Absolute mode (replace)
- Loop modes: Normal loop and Ping-Pong (Yoyo)
- Unscaled time support
- 4 UnityEvents: onAnimationStart, onAnimationComplete, onAnimationLoop, onAnimationUpdate
- **Note**: usePhysicsUpdate not supported (use PhysicsPlatformAnimator instead)

---

## Character Controllers (6 scripts)

**Location**: `Runtime/CharacterControllers/`

Controllers for player and enemy characters.

### Player Controllers (4 scripts)

**Location**: `CharacterControllers/Player/`

#### CharacterControllerCC.cs
- CharacterController-based humanoid controller (Unity TPC style)
- **Auto-requires**: CharacterController + PlayerInput components
- **Auto-configures**: PlayerInput with "Player" action map via Reset()
- **Spawn System**: Checks for ISpawnPointProvider in Awake() before physics runs
- Spawns at checkpoint position BEFORE scene physics initializes (eliminates race conditions)
- Smooth movement with acceleration/deceleration (speedChangeRate)
- Optional sprint functionality with speed multiplier
- Height-based jumping with physics formula and anti-spam timeout
- SmoothDampAngle rotation for natural turning
- Robust slope detection with automatic sliding on steep surfaces
- Moving platform support (Tag/Layer/Both detection modes)
- Dodge mechanic with cooldown and air dodge option
- Full animator integration with 6 parameters (Speed, Grounded, VerticalVelocity, IsDodging, IsWalking, IdleTime)
- TeleportTo() method for portals, cutscenes, respawns
- 9 UnityEvents for no-code interaction (includes onSpawnPointUsed, onTeleport)
- Extensive debug gizmos
- **Documentation**: See CharacterControllerCC_Documentation.md

#### PhysicsBallPlayerController.cs
- Ball-based player controller (Rigidbody physics)
- Camera-relative movement using forces
- Ground detection via sphere casting
- Jump mechanics with grounded checking
- Input System integration

#### PhysicsCharacterController.cs
- Rigidbody-based character controller
- Capsule collider and constraint management
- Ground detection and slope handling
- Animation integration with child animator support
- Jump mechanics and physics-based movement

#### CharacterPushRigidBody.cs
- Player push physics for moving objects
- Force-based pushing with configurable strength
- Works with CharacterControllerCC

### Enemy Controllers (2 scripts)

**Location**: `CharacterControllers/Enemy/`

#### PhysicsEnemyController.cs
- Rigidbody-based AI enemy controller
- Player detection and chase behavior
- Configurable jump modes (none, random, collision-based, combined)
- Automatic Rigidbody and CapsuleCollider setup
- Rotation toward movement direction

#### EnemyControllerCC.cs
- CharacterController-based enemy AI
- Pathfinding and chase behavior
- Jump and obstacle avoidance
- Works with NavMesh for advanced AI

---

## Game Management Components (9 scripts)

**Location**: `Runtime/Game/`

Manager systems for health, score, audio, state, etc.

### GameCollectionManager.cs
- Score/collection system
- TextMeshPro display integration
- Threshold events (e.g., "score reaches 10")
- Add/subtract/set score methods

### GameInventorySlot.cs
- Inventory management
- Capacity limits and overflow detection
- Item add/remove with quantity tracking
- Full/empty state events

### GameHealthManager.cs
- Health system with damage and healing
- Health threshold detection and events
- Death state management
- TextMeshPro display integration
- Damage immunity cooldown

### GameStateManager.cs
- Simplified pause and victory state management
- Configurable pause key with automatic timer coordination
- Pause panel and restart button management
- Automatic discovery and control of GameTimerManager

### GameTimerManager.cs
- Comprehensive timer system
- Count-up or countdown modes
- Multiple configurable thresholds with individual events
- Periodic event system (e.g., every 10 seconds)
- Three display formats (MM:SS, decimal seconds, HH:MM:SS)
- Automatic pause/resume with GameStateManager

### GameUIManager.cs
- UI data display system with DOTween animations
- Score display with punch scale animations
- Health bar with smooth value tweening and color transitions
- Timer display with multiple formats
- Victory text with score and time summary

### GameAudioManager.cs
- Audio system with mixer integration
- Volume control for music, SFX, master
- Music crossfading and fade in/out
- Sound effect management
- Event-driven audio feedback

### GameCameraManager.cs
- Cinemachine camera switching
- String-based camera identification
- Automatic exclusive camera activation
- Event system for camera transitions

### GameCheckpointManager.cs
- **NEW ARCHITECTURE**: Implements ISpawnPointProvider (passive data holder pattern)
- Players ask "where should I spawn?" instead of manager teleporting them
- Eliminates race conditions between spawn systems and physics
- DontDestroyOnLoad singleton for cross-scene persistence
- Position, rotation, and optional game state (score, health) saving
- SaveCheckpointPosition() - saves current player location
- SaveCheckpointAtPosition() - saves specific position
- SaveCheckpointFull() - saves position + game data
- TeleportPlayerToCheckpoint() - manual teleport for same-scene respawns (legacy support)
- OnSpawnPointUsed() - fires events when player spawns at checkpoint
- RestoreScore() and RestoreHealth() - automatic game state restoration
- 3 UnityEvents: onCheckpointSaved, onCheckpointRestored, onPositionSaved
- Works seamlessly with InputCheckpointZone
- See [Checkpoint Quick Start Guide](../CheckpointSystem_QuickStart.md)

---

## Physics Components (5 scripts)

**Location**: `Runtime/Physics/`

Physics-based systems for forces, platforms, and movement.

### Bumpers (2 scripts)

**Location**: `Physics/Bumpers/`

#### PhysicsBumper.cs
- Advanced bumper/repulsion system with DOTween animations
- Configurable force direction (collision normal or radial)
- Scale animation using DOTween with AnimationCurve support
- Material emission effects animated with DOTween
- Cooldown system with events
- Comprehensive editor gizmos and debugging

#### PhysicsBumperTag.cs
- Tag-based bumper variant
- Only affects objects with specific tags
- Simpler setup for targeted bumper zones

### Platforms (3 scripts)

**Location**: `Physics/Platforms/`

#### PhysicsPlatformAnimator.cs
- Physics-based platform animation
- Waypoint movement with configurable speed
- Loop, ping-pong, and one-way modes
- Rigidbody integration for proper physics interaction
- Works with PhysicsPlatformStick for player attachment

#### ActionPlatformAnimator.cs
- Non-physics platform animation (transform-based)
- Waypoint movement with DOTween
- Simpler than physics version but no physics interaction
- Good for visual-only moving platforms

#### PhysicsPlatformStick.cs
- Moving platform attachment system
- Uses physics forces to keep player attached
- Tag/layer detection for automatic attachment
- Works with PhysicsPlatformAnimator

---

## Puzzle Components (2 scripts)

**Location**: `Runtime/Puzzle/`

Switch and checker mechanics for puzzle design.

### PuzzleSwitch.cs
- Interactive switch component
- On/off state with visual feedback
- Multiple activation methods (trigger, click, event)
- UnityEvents for state changes

### PuzzleSwitchChecker.cs
- Multi-switch validation system
- Checks if all required switches are activated
- Fires event when puzzle is solved
- Reset functionality

---

## UI Components (1 script)

**Location**: `Runtime/UI/`

### FadeInFromBlackOnRestart.cs
- Automatic scene transition fade using DOTween
- Fades from black every time scene loads/restarts
- No event wiring required - completely automatic
- DOTween-based with unscaled time support (works during pause)
- Editable fade duration and optional start delay
- Auto-enables Image component on play
- Perfect for hiding checkpoint restoration and scene transitions

---

## Utilities (3 scripts)

**Location**: `Runtime/Utilities/`

Legacy and helper scripts.

### InputCollisionEnter.cs
- Collision event system (documented above in Input section)

### lockMouseCursorToDisplay.cs
- Cursor management utility
- Locks and hides cursor for first-person games
- Toggle with Escape key

### unity_attractor_script.cs
- Legacy attraction/gravity system
- Applies forces to pull objects toward attractor
- Configurable range and strength

---

## Interfaces (1 interface)

**Location**: `Runtime/Interfaces/`

Core interfaces for extensible systems.

### ISpawnPointProvider
- Interface for any system that can provide spawn points
- Implemented by GameCheckpointManager, but can be used by:
  - Wave spawn systems
  - Multiplayer spawn selectors
  - Random spawn point managers
  - Portal exit points
- **Properties**:
  - `bool HasSpawnPoint` - Returns true if spawn point is available
  - `Vector3 SpawnPosition` - World position where player should spawn
  - `Quaternion SpawnRotation` - World rotation for player spawn orientation
- **Methods**:
  - `void OnSpawnPointUsed()` - Called after player consumes spawn point
- **Architecture**: Enables decoupled communication between character controllers and spawn systems
- CharacterControllerCC checks for this interface in Awake() before physics runs
- See [Development Patterns: Spawn Point Provider](development-patterns.md#spawn-point-provider-pattern)

---

## Script Count Summary

**Total: 52 Scripts + 1 Interface**

- **Input Components**: 8 scripts
- **Action Components**: 16 scripts (+ 1 helper)
- **Animation Components**: 1 script
- **Character Controllers**: 6 scripts
- **Game Managers**: 9 scripts
- **Physics Components**: 5 scripts
- **Puzzle Components**: 2 scripts
- **UI Components**: 1 script
- **Utilities**: 3 scripts
- **Interfaces**: 1 interface (ISpawnPointProvider)

**XML Documentation Compliance**: 46/46 educational scripts (100%)
(Utilities excluded from doc generator)

---

## Custom Editor Scripts

9 scripts have custom Inspector UI (see [Custom Editors Guide](custom-editors.md)):

- ActionDialogueSequence
- ActionDecalSequence
- ActionDecalSequenceLibrary
- ActionDisplayImage
- ActionDisplayText
- ActionPlatformAnimator
- PhysicsPlatformAnimator
- PuzzleSwitch
- PuzzleSwitchChecker

---

## Finding Scripts

**By Category**:
- Event Sources → `Input/`
- Event Targets → `Actions/`
- Controllers → `CharacterControllers/`
- Managers → `Game/`
- Physics → `Physics/`

**By Function**:
- Audio → `Actions/Audio/`
- Animations → `Actions/DecalAnimation/` or `Animation/`
- UI → `Actions/Display/` or `UI/`
- Spawning → `Actions/Spawning/`
- Puzzles → `Puzzle/`

**Search Pattern**:
```
Runtime/[Category]/[Subcategory]/[ScriptName].cs
```
