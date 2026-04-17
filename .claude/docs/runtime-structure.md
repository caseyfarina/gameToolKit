# Runtime Folder Structure & Component Reference

Complete inventory of all educational scripts in the eventGameToolKit.

## Folder Organization

```
Runtime/
├── Actions/              # Event target components (21 scripts)
│   ├── Audio/           # Sound playback
│   ├── DecalAnimation/  # URP decal material animations
│   ├── Display/         # UI text and images
│   ├── Events/          # Event sequencing and animation triggers
│   ├── Scene/           # Scene management
│   └── Spawning/        # Object instantiation
├── Animation/           # Transform animations (1 script)
├── CharacterControllers/ # Player and enemy controllers (7 scripts)
│   ├── Enemy/           # AI enemy controllers
│   └── Player/          # Player controllers
├── Game/                # Game managers (12 scripts)
├── Input/               # Event source components (9 scripts)
├── Interfaces/          # Core interfaces (1 interface)
├── Variables/           # ScriptableObject variables (2 scripts)
├── Physics/             # Physics systems (5 scripts)
│   ├── Bumpers/         # Repulsion forces
│   └── Platforms/       # Moving platforms
├── Puzzle/              # Puzzle mechanics (2 scripts)
├── UI/                  # UI helpers (1 script)
└── Utilities/           # Legacy/helper scripts (3 scripts)
```

---

## Input Components (10 scripts)

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
- Mouse-based interaction system using New Input System
- Raycasts from mouse cursor position (requires free/visible cursor)
- Click, hover, enter/exit detection via Physics.Raycast
- Visual feedback: material swap, scale animation with DOTween
- 6 UnityEvents: onMouseClick, onMouseDown, onMouseUp, onMouseEnter, onMouseExit, onMouseHover
- For FPS games with locked cursor, use InputFPMouseInteraction instead

### InputFPMouseInteraction.cs
- First-person interaction system using New Input System
- Raycasts from camera center (works with locked cursor / CharacterControllerFP reticle)
- Configurable target camera, max raycast distance, interaction layer
- Click, hover, enter/exit detection via Physics.Raycast
- Visual feedback: material swap, scale animation with DOTween
- 6 UnityEvents: onMouseClick, onMouseDown, onMouseUp, onMouseEnter, onMouseExit, onMouseHover

### InputOnStart.cs
- Fires UnityEvents at scene initialization — no code required
- `onAwake` event fires in Awake(), before any Start() in the scene
- `onStart` event fires in Start(), after all Awake() calls have completed
- Optional `startDelay` float delays the Start event by N seconds (coroutine-based)
- Custom Inspector includes help boxes explaining Awake vs Start with an execution order summary

### InputClickDrag.cs
- Allows a GameObject to be clicked and dragged; requires a Collider on the same object
- `DragPlane` enum: `CameraFacing` (depth-locked screen plane), `WorldXZ`, `WorldXY`, `WorldYZ`
- `GrabMode` enum: `MaintainOffset` (default) or `SnapToCenter`
- Optional grid snapping: `snapToGrid` + `snapSize`
- Optional world-space positional limits: `useLimits` + `minLimit`/`maxLimit` (Vector3)
- Events: `onDragStart`, `onDragEnd`, `onDragged` (Vector3 world position)
- Public: `CancelDrag()`, `IsDragging`
- Custom Inspector: hides snap/limit sub-fields when toggles are off

### InputClickRotate.cs
- Allows a GameObject to be rotated by clicking and dragging; requires a Collider on the same object
- `RotationAxis` enum: `WorldX/Y/Z`, `LocalX/Y/Z` — axis captured in world space at drag start
- `MouseDragAxis` enum: `Horizontal` (default) or `Vertical`
- `sensitivity`: degrees per pixel of mouse movement (default 0.5)
- Optional angle snapping: `snapToAngle` + `snapAngle` (degrees)
- Optional rotation limits: `useLimits` + `LimitSpace` enum (`RelativeToStart` / `WorldAbsolute`) + `minAngle`/`maxAngle`
- Events: `onRotateStart`, `onRotateEnd`, `onRotated` (float cumulative angle)
- Public: `CancelRotation()`, `IsRotating`, `GetCurrentAngle()`
- Custom Inspector: hides snap/limit sub-fields when toggles are off; shows live angle readout in play mode

### InputCollisionEnter.cs
**Location**: `Runtime/Utilities/`
- Collision event system (OnCollisionEnter)
- Tag-based filtering
- Fires UnityEvent on collision

---

## Action Components (21 scripts)

**Location**: `Runtime/Actions/`

Event targets that perform actions when triggered.

### Audio (1 script)

#### ActionPlaySound.cs
**Location**: `Actions/Audio/`
- Plays a randomly selected clip from an array using `PlayOneShot`
- Randomized volume range (`volumeMin` / `volumeMax`)
- Randomized pitch range (`pitchMin` / `pitchMax`) — set both to 1 for no variation
- `onPlay` UnityEvent fires on each successful playback
- `SetVolume()` and `SetPitch()` for runtime control via UnityEvents
- Custom Inspector shows friendly range summaries and a Play-mode test button

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
- Optional `fpController` field — assign a `CharacterControllerFP` to automatically unlock the cursor and pause movement during decision panels; restores both on completion
- `Reset()` seeds a default "Hello player!" line when the component is first added
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

### Toggle (1 script)

**Location**: `Actions/`

#### ActionToggle.cs
- Toggles the active state of a list of GameObjects independently
- Each object flips based on its own current `activeSelf` state — no internal state tracked
- `Toggle()` — flips each GO independently (active→inactive, inactive→active)
- `AllOn()` — sets all GOs active regardless of current state
- `AllOff()` — sets all GOs inactive regardless of current state
- Null-safe: skips missing or destroyed entries silently

### Random (2 scripts)

**Location**: `Actions/`

#### ActionRandomEvent.cs
- Randomly fires one UnityEvent from a weighted list on Trigger()
- `WeightedEvent` entries with label, probability weight, and onSelected event
- Weights are normalized at runtime — any positive values work (e.g., 1/1/2 = 25%/25%/50%)
- `Reset()` provides two 50/50 defaults when added in Inspector
- Logs warning if no events defined or all weights are zero
- Custom Inspector shows live normalized percentages per entry and a Play-mode "▶ Trigger" button

#### ActionShuffleEvent.cs
- Cycles through all entries in a random (shuffled) order — each fires exactly once per cycle before reshuffling (urn model / sampling without replacement)
- `ShuffleEntry` entries with label and onSelected event (no weights — all equal)
- `preventLastRepeat` option ensures the first entry of a new cycle differs from the last of the previous
- `onCycleComplete` UnityEvent fires when every entry has been used once
- `Reshuffle()` and `ResetFull()` public methods for manual control
- `Reset()` provides three default entries (A, B, C)
- Custom Inspector shows per-entry queue status (fired / next / queued), cycle progress bar, and Play-mode "▶ Trigger Next" and "↺ Reshuffle" buttons

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

## Character Controllers (7 scripts)

**Location**: `Runtime/CharacterControllers/`

Controllers for player and enemy characters.

### Player Controllers (5 scripts)

**Location**: `CharacterControllers/Player/`

#### CharacterControllerFP.cs
- CharacterController-based first-person controller
- **Auto-requires**: CharacterController + PlayerInput components
- **Auto-configures**: PlayerInput with "Player" action map via Reset()
- **Spawn System**: Checks for ISpawnPointProvider in Awake() before physics runs
- Mouse and gamepad look with separate sensitivity settings
- Camera pitch (up/down) on camera Transform, yaw (left/right) on character body
- Vertical look limit prevents over-rotation
- Smooth movement with acceleration/deceleration (character-relative forward/strafe)
- Optional sprint functionality with speed multiplier
- Height-based jumping with physics formula and anti-spam timeout
- Robust slope detection with automatic sliding on steep surfaces
- Moving platform support (Tag/Layer/Both detection modes)
- Cursor lock/unlock management with toggle key and events
- `SetInputEnabled(bool)` — freezes look/move/jump/cursor-toggle without disabling gravity; used by ActionDialogueSequence during decision panels
- Animator integration with 5 parameters (Speed, Grounded, VerticalVelocity, IsWalking, IsSprinting)
- TeleportTo() method for portals, cutscenes, respawns
- 9 UnityEvents for no-code interaction (includes onCursorLockChanged)
- Extensive debug gizmos
- **Documentation**: See CharacterControllerFP_Documentation.md

#### CharacterControllerCC.cs
- CharacterController-based humanoid controller (Unity TPC style)
- **Auto-requires**: CharacterController + PlayerInput components
- **Auto-configures**: PlayerInput with "Player" action map via Reset()
- **Spawn System**: Checks for ISpawnPointProvider in Awake() before physics runs
- Spawns at checkpoint position BEFORE scene physics initializes (eliminates race conditions)
- Smooth movement with configurable slide feel (slideAmount: 0 = instant stop, 1 = icy)
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
- Score/collection value tracking with threshold-based events
- Optional self-contained UI text display (enable Show UI)
  - Label prefix, font size, alignment, color, custom font
  - PunchScale or FadeFlash animation on value change
- Optional self-contained bar display (enable Show Bar)
  - Configurable position, size, background color, gradient fill
  - Smooth animated fill transitions
- Editor canvas preview for positioning UI elements
- Min/max value clamping with limit events
- Increment/Decrement/SetValue methods

### GameInventoryManager.cs
- Multi-slot inventory system — configure any number of slots in the Inspector
- Each slot: itemName, optional icon Sprite, maxCapacity, currentCount
- Per-slot events: onFull, onEmpty, onChanged(int)
- Methods by index: Increment, Decrement, UseItem, GetCount
- Methods by name: IncrementByName, DecrementByName, GetCountByName
- Optional self-contained UI (enable Show UI)
  - Horizontal row of cards, one per slot
  - Each card shows the slot's icon (if assigned)
  - Optional count number in each card (toggle Show Count)
  - Configurable card size, spacing, background color, font, and position
  - Editor canvas preview for positioning
- Custom Inspector editor with conditional UI settings


### GameHealthManager.cs
- Health system with damage and healing
- Health threshold detection and events (low health, death, revival)
- Death state management with revival support via SetHealth/FullHeal
- Optional self-contained UI text display (enable Show UI)
  - Label prefix, "current / max" format toggle
  - Font size, alignment, color, custom font
  - PunchScale or FadeFlash animation on value change
- Optional self-contained health bar display (enable Show Bar)
  - Configurable position, size, background color
  - Gradient fill color (red → yellow → green by default)
  - Smooth animated fill transitions
- Editor canvas preview for positioning UI elements
- Damage-over-time coroutine for testing
- One-shot kills skip low health warning (only onDeath fires)

### GameStateManager.cs
- Simplified pause and victory state management
- Configurable pause key with automatic timer coordination
- Pause panel and restart button management
- Automatic discovery and control of GameTimerManager

### GameTimerManager.cs
- Count-up or countdown timer with configurable start/total time
- Optional self-contained UI text display (enable Show UI)
  - Display formats: MM:SS, Seconds, Seconds with decimal, HH:MM:SS
  - Label prefix, font size, alignment, custom font
  - Static color or gradient color mapped across the full timer duration
- Optional self-contained bar display (enable Show Bar)
  - Configurable position, size, background color
  - Gradient fill color mapped across the full timer duration
  - Smooth animated fill transitions
- Editor canvas preview shows at 75% time progress
- Configurable threshold events (fire at specific times)
- Periodic event system (e.g., every 10 seconds)
- Automatic pause/resume with GameStateManager
- For count-up bar: set Total Time as the 100% reference duration

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

### GameSceneManager.cs
- Scene loading for multi-scene games with persistent player and UI
- Singleton with DontDestroyOnLoad persistence
- Additive or single scene loading modes
- Automatic previous scene unloading
- Pre/post load delays for transition effects (fade out/in)
- Coordinates with SpawnPoint components for player positioning
- LoadScene(string) - load by name, spawn at default SpawnPoint
- LoadSceneAtSpawnPoint(string, string) - load with specific spawn point ID
- ReloadCurrentScene() - reload current level
- LoadSceneSingle(string) - non-additive load (for main menu returns)
- 5 UnityEvents: onSceneLoadStarted, onSceneLoadCompleted, onSceneLoadFailed, onSceneLoading, onLoadProgress

### SpawnPoint.cs
- Marks spawn locations in scenes, implements ISpawnPointProvider
- Priority system: GameCheckpointManager > SpawnPoint with matching ID > default SpawnPoint
- spawnId field for targeting specific entry points per scene
- isDefaultSpawnPoint toggle for fallback positioning
- spawnOffset for offsetting from transform position
- Static RequestSpawnId()/ClearRequestedSpawnId() for GameSceneManager coordination
- Editor gizmos: player silhouette, direction arrow, offset indicator
- 1 UnityEvent: onPlayerSpawned

### GameStoreManager.cs
- In-game store connected to a GameCollectionManager for currency
- Expandable list of StoreItem (name, icon, price, oneTimePurchase, onPurchased, onCannotAfford)
- KeyCode field (default: B) to open/close — mirrors GameStateManager.pauseKey pattern
- Pauses game (Time.timeScale=0) on open; restores on close
- Optional CharacterControllerFP reference for cursor lock/unlock (ActionDialogueSequence pattern)
- Optional GameAudioManager + storeMusic/previousMusic clips for music crossfade
- Self-contained panel UI (sortingOrder=20): title bar, scrollable item rows, balance display
- Per-row affordability feedback: greyed rows + disabled buttons for items player can't afford
- One-time purchase items show "SOLD" after purchase and stay disabled
- Full styling via Inspector: panel/row/button sprites and colors, icon size, fonts, spacing
- Editor preview button (HideFlags.DontSave canvas in scene view)
- 3 manager UnityEvents: onStoreOpened, onStoreClosed, onAnyPurchase
- 2 per-item UnityEvents: onPurchased, onCannotAfford

---

## Variables (2 scripts)

**Location**: `Runtime/Variables/`

ScriptableObject assets for cross-scene data persistence.

### IntVariable.cs
- ScriptableObject holding an integer value that persists across scene loads
- Resets to defaultValue when entering Play mode
- Optional min/max constraints
- Add(), Subtract(), SetValue(), ResetToDefault()
- 1 UnityEvent: onValueChanged

### FloatVariable.cs
- ScriptableObject holding a float value that persists across scene loads
- Same API as IntVariable plus GetNormalized() for 0-1 percentage
- Optional min/max constraints
- 1 UnityEvent: onValueChanged

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

**Total: 60 Scripts + 1 Interface**

- **Input Components**: 10 scripts
- **Action Components**: 21 scripts (+ 1 helper)
- **Animation Components**: 1 script
- **Character Controllers**: 7 scripts
- **Game Managers**: 9 scripts
- **Physics Components**: 5 scripts
- **Puzzle Components**: 2 scripts
- **UI Components**: 1 script
- **Utilities**: 3 scripts
- **Interfaces**: 1 interface (ISpawnPointProvider)

**XML Documentation Compliance**: 50/50 educational scripts (100%)
(Utilities excluded from doc generator)

---

## Custom Editor Scripts

18 scripts have custom Inspector UI (see [Custom Editors Guide](custom-editors.md)):

- ActionDialogueSequence
- ActionDecalSequence
- ActionDecalSequenceLibrary
- ActionDisplayImage
- ActionDisplayText
- ActionPlatformAnimator
- ActionRandomEvent
- ActionShuffleEvent
- GameCollectionManager
- GameHealthManager
- GameTimerManager
- InputFPMouseInteraction
- InputOnStart
- InputMouseInteraction
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
