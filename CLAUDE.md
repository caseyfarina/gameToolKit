# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Structure & Workflow

**IMPORTANT: Development & Package Relationship**

This project has a unique two-repository structure:

1. **Development/Testing Environment** (This Repository)
   - Location: `F:\Unity Projects 2025\gameToolKitFarina\gameToolKit\`
   - This is the **main Unity project** where all development and testing happens
   - Contains the full Unity project with scenes, testing assets, and the eventGameToolKit package embedded at `Assets/eventGameToolKit/`

2. **Unity Package Repository** (Separate Git Repo)
   - Location: `F:\Unity Projects 2025\eventGameToolKit\`
   - This is the **standalone Unity package** with its own git repository
   - Students install this package via Unity Package Manager
   - Contains only the package contents (no test scenes or development assets)

**Workflow:**
- ✅ **Work in**: `gameToolKitFarina/gameToolKit/Assets/eventGameToolKit/` (this project)
- ✅ **Test in**: `gameToolKitFarina/gameToolKit/` (full Unity project with scenes)
- ✅ **Sync to package**: Copy changes to `eventGameToolKit-Package/` repository
- ✅ **Push together**: ALWAYS push both repos to git at the same time

**CRITICAL SYNC RULE:**
Before pushing to git, ALWAYS sync the package repository with the development project:
1. Verify no errors in development project (`gameToolKit`)
2. Sync all changes from `gameToolKit/Assets/eventGameToolKit/` → `eventGameToolKit-Package/`
   - **Command**: `cmd //c robocopy "F:\Unity Projects 2025\gameToolKitFarina\gameToolKit\Assets\eventGameToolKit" "F:\Unity Projects 2025\eventGameToolKit-Package" //MIR //XD .git`
   - **IMPORTANT**: Use `cmd //c` and double slashes `//` to avoid Git Bash path conversion errors
   - Git Bash converts `/MIR` to `C:/Program Files/Git/MIR` without the double slashes
3. Test package in clean Unity project to verify it works standalone
4. Push BOTH repositories to GitHub together (never push just one)

When making changes to scripts, always edit them in the `gameToolKitFarina/gameToolKit/Assets/eventGameToolKit/` directory. After testing and confirming changes work, those files need to be pushed to the separate `eventGameToolKit-Package/` package repository.

## Project Overview

This is an educational Unity 3D project for the "Animation and Interactivity" class. It provides a modular toolkit of scripts that students can use to create interactive experiences without needing to write code. The core design philosophy centers around UnityEvents, allowing students to visually wire together behaviors in the Unity Inspector to create complex interactive systems.

The project features a ball physics-based game template as the foundation, with an expanding library of reusable components for triggers, animations, spawning, collection mechanics, and more.

## Key Unity Project Structure

- **Main Scene**: `Assets/Scenes/ballPlayer.unity`
- **Educational Scripts**: `Assets/Scripts/` (organized by function)
  - `Input/` - Event source components
  - `Actions/` - Event target components
  - `Physics/` - Movement and physics systems
  - `Game/` - Game management systems
  - `UI/` - User interface components
  - `Examples/` - Example combinations (future)
- **Input Configuration**: `Assets/InputSystem_Actions.inputactions`
- **Solution File**: `interactionTemplate.sln`
- **Documentation Files**:
  - `Week1_ComponentWorkflows.txt` - Detailed workflows for GameAudioManager, GameStateManager, GameUIManager
  - `CodebaseImprovements_GameGenres.txt` - Guide for implementing different game genres using the toolkit
  - `TimerExample_SceneSetup.txt` - Complete example scene setup demonstrating GameTimerManager
  - `CharacterControllerCC_Documentation.md` - Comprehensive setup guide for CharacterControllerCC with Quick Start, all parameters, troubleshooting, and 4 common scenarios
  - `DecalAnimationSystem_Documentation.md` - Complete URP decal animation guide covering ActionDecalSequence (standalone), ActionDecalSequenceLibrary, and blinking scripts with setup examples

## Unity Version

**This project uses Unity 6** (Unity 6000.0.x or later)

## Development Commands

### Building and Running
- Open the project in Unity Editor (Unity 6)
- Build using Unity's Build Settings (Ctrl+Shift+B)
- Play in editor using Unity's Play button or Ctrl+P

### Testing
- Unity Test Framework is available (`com.unity.test-framework`: "1.4.5")
- Run tests through Unity Test Runner window (Window > General > Test Runner)

### Unity Packages Used
- Input System (`com.unity.inputsystem`: "1.11.2") - Modern input handling
- Universal Render Pipeline (`com.unity.render-pipelines.universal`: "17.0.3") - Rendering
- Cinemachine (`com.unity.cinemachine`: "3.1.2") - Camera management
- AI Navigation (`com.unity.ai.navigation`: "2.0.5") - Pathfinding
- Adobe Substance 3D (`Assets/Adobe/Substance3DForUnity/`) - Material authoring
- **DOTween** (`Assets/Plugins/Demigiant/DOTween/`) - Professional animation engine for smooth tweening
  - **Note**: Project uses DOTween FREE (open-source), not DOTween Pro
  - Used for UI animations, fades, scales, and transform animations
  - All animation scripts are compatible with the free version

## Core Game Architecture

### Player Controller System
- `BallController.cs` - Main physics-based ball controller with:
  - Camera-relative movement using Rigidbody forces
  - Ground detection via sphere casting
  - Jump mechanics with grounded checking
  - Input System integration via OnMove/OnJump callbacks

### Input System
- Uses Unity's new Input System with action-based bindings
- Input actions defined in `InputSystem_Actions.inputactions`
- Movement mapped to WASD/gamepad stick
- Jump mapped to Space/gamepad button

### Educational Component Library

#### Input Components (Event Sources)
Located in `Assets/Scripts/Input/`:
- `InputKeyPress.cs` - Simple key press event system
- `InputKeyCountdown.cs` - Key-based countdown system with TextMeshPro display
- `InputTriggerZone.cs` - 3D collision detection by tag with:
  - Enter/exit/stay events
  - Configurable stay event intervals for continuous damage
  - Tag-based filtering
- `InputQuitGame.cs` - Application quit on Escape key

#### Action Components (Event Targets)
Located in `Assets/Scripts/Actions/`:
- `ActionSpawnObject.cs` - Manual single object spawning
- `ActionAutoSpawner.cs` - Automatic object spawner with:
  - Random spawn timing between min/max intervals
  - Multiple prefab support with random selection
  - Positional variance using insideUnitSphere
- `ActionRestartScene.cs` - Scene restart functionality (button/event only)
- `ActionDisplayImage.cs` - UI image display with DOTween animations:
  - Fade in/out effects with customizable durations
  - Scale animations with start/target scale control
  - Simultaneous fade and scale animations using DOTween Sequences
- `ActionDisplayText.cs` - TextMeshPro text display with DOTween animations:
  - Fade in/out transitions
  - Typewriter effect with adjustable speed
  - Automatic text clearing after display duration
- `ActionDialogueSequence.cs` - Complete dialogue system for visual novels/story games:
  - Sequential dialogue line playback with auto-advance or manual progression
  - Character portrait support with left/right positioning
  - Multiple animation types for images (None, SlideUpFromBottom, SlideInFromSide, FadeIn, PopIn)
  - Multiple animation types for text (None, TypeOn, FadeIn, SlideUpFromBottom)
  - Customizable background image with position/size control
  - DOTween-based animations with configurable durations and easing
  - Loop mode for repeating dialogues
  - Preview system in Editor with custom inspector
  - Events: onDialogueStart, onDialogueComplete, onLineChanged
  - Companion script: `DialogueUIController.cs` handles UI creation and management
- `ActionDecalSequence.cs` - URP Decal Projector material animation system:
  - Frame-by-frame material sequence playback with custom timing per frame
  - Works standalone (no library required) for simple animations
  - Playback controls: Play, Stop, Pause, Resume, JumpToFrame
  - Adjustable playback speed (0.1x to 5x) and loop mode
  - Custom Inspector with live playback controls and duration calculator
  - Events: onSequenceStart, onSequenceComplete, onFrameChanged, onPause, onResume, onStop
  - Perfect for: animated facial expressions, flashing signs, texture cycling
- `ActionDecalSequenceLibrary.cs` - Manager for multiple ActionDecalSequence components:
  - Switch between different decal animation sequences by index or name
  - Navigation controls: PlayNext, PlayPrevious, PlaySequence(int)
  - Supports pause/resume/stop for current sequence
  - Custom Inspector with sequence switcher buttons and live status
  - Perfect for: character expressions, animation state machines, multi-state objects
- `ActionBlinkDecal.cs` - Automatic eye blinking using material switching:
  - Two-state animation (open eyes ↔ closed eyes)
  - Randomized timing with configurable variation (natural feel)
  - Manual BlinkOnce() for triggered blinks
  - Events: onBlinkStart, onBlinkComplete
  - Simple setup: assign two materials and configure timing
- `ActionBlinkDecalOptimized.cs` - Optimized blinking using texture switching:
  - More efficient than ActionBlinkDecal (switches textures, not materials)
  - Creates material instance automatically (safe for multiple characters)
  - Configurable shader property name (default: "_BaseMap" for URP)
  - Recommended for scenes with multiple blinking characters
  - Same functionality as ActionBlinkDecal with better performance

#### Physics Components
Located in `Assets/Scripts/Physics/`:
- `PhysicsPlayerController.cs` - Ball-based player controller with camera-relative movement
- `PhysicsCharacterController.cs` - Rigidbody-based character controller with:
  - Capsule collider and constraint management
  - Ground detection and slope handling
  - Animation integration with child animator support
  - Jump mechanics and physics-based movement
- `CharacterControllerCC.cs` - CharacterController-based humanoid controller (Unity TPC style):
  - **Auto-requires**: CharacterController + PlayerInput components (added automatically)
  - **Auto-configures**: PlayerInput with "Player" default action map via Reset()
  - Smooth movement with acceleration/deceleration (speedChangeRate)
  - Height-based jumping with physics formula and anti-spam timeout
  - SmoothDampAngle rotation for natural turning
  - Robust slope detection with automatic sliding on steep surfaces
  - Moving platform support (Tag/Layer/Both detection modes)
  - Dodge mechanic with cooldown and air dodge option
  - Full animator integration with 5 parameters (Speed, Grounded, VerticalVelocity, IsDodging, IsWalking)
  - StringToHash optimization with parameter safety checks
  - 8 UnityEvents for no-code interaction (onJump, onLanding, onDodge, etc.)
  - Extensive debug gizmos for grounded check, slopes, platforms
  - **Documentation**: See CharacterControllerCC_Documentation.md for complete setup guide
- `PhysicsEnemyController.cs` - AI enemy controller with:
  - Player detection and chase behavior
  - Configurable jump modes (none, random, collision-based, combined)
  - Automatic Rigidbody and CapsuleCollider setup
  - Rotation toward movement direction
- `PhysicsBumper.cs` - Advanced bumper/repulsion system with DOTween animations:
  - Configurable force direction (collision normal or radial)
  - Scale animation using DOTween with AnimationCurve support and per-axis scaling
  - Material emission effects animated with DOTween (color or float properties)
  - Cooldown system with events
  - Comprehensive editor gizmos and debugging
- `PhysicsPlatformStick.cs` - Moving platform attachment system using physics forces

#### Game Management Components
Located in `Assets/Scripts/Game/`:
- `GameCollectionManager.cs` - Score/collection system with TextMeshPro display and threshold events
- `GameInventorySlot.cs` - Inventory management with capacity limits and overflow detection
- `GameHealthManager.cs` - Health system with:
  - Damage and healing mechanics
  - Health threshold detection and events
  - Death state management
  - TextMeshPro display integration
- `GameStateManager.cs` - Simplified pause and victory state management with:
  - Configurable pause key and automatic timer coordination
  - Pause panel and restart button management
  - Automatic discovery and control of GameTimerManager instances
- `GameTimerManager.cs` - Comprehensive timer system with:
  - Count-up or countdown modes
  - Multiple configurable thresholds with individual events
  - Periodic event system (e.g., every 10 seconds)
  - Three display formats (MM:SS, decimal seconds, HH:MM:SS)
  - Automatic pause/resume integration with GameStateManager
- `GameUIManager.cs` - UI data display system with DOTween animations:
  - Score display with DOTween punch scale animations
  - Health bar with smooth value tweening and color transitions
  - Timer display with multiple formats
  - Victory text with score and time summary
- `GameAudioManager.cs` - Audio system with:
  - Mixer integration and volume control
  - Music crossfading and fade in/out
  - Sound effect management
  - Event-driven audio feedback
- `GameCameraManager.cs` - Cinemachine camera switching with:
  - String-based camera identification
  - Automatic exclusive camera activation
  - Event system for camera transitions
- `GameCheckpointManager.cs` - Persistent checkpoint system with:
  - DontDestroyOnLoad singleton pattern for cross-scene persistence
  - Automatic restoration via SceneManager.sceneLoaded event
  - Position and optional game state (score, health) saving
  - CRITICAL: Uses rb.position/rb.rotation after WaitForFixedUpdate to prevent physics conflicts
  - Hides player renderers during restoration to prevent visual flashing

#### UI Components
Located in `Assets/Scripts/UI/`:
- `FadeInFromBlackOnRestart.cs` - Automatic scene transition fade using DOTween:
  - Automatically fades from black every time scene loads/restarts
  - No event wiring required - completely automatic
  - DOTween-based fade with unscaled time support (works during pause)
  - Editable fade duration and optional start delay
  - Auto-enables Image component on play (disable Image component in editor to not block view)
  - Perfect for hiding checkpoint restoration and scene restart transitions

#### Animation Components
Located in `Assets/Scripts/Animation/`:
- `ActionAnimateTransform.cs` - Procedural transform animation using DOTween with AnimationCurves:
  - Animates 9 transform properties independently: PositionX/Y/Z, RotationX/Y/Z, ScaleX/Y/Z
  - Uses AnimationCurve for complex easing (set in Inspector)
  - DOTween integration with `.SetEase(curve)` for curve support
  - Supports Offset mode (add to initial value) and Absolute mode (replace value)
  - Loop modes: Normal loop and Ping-Pong (Yoyo)
  - Unscaled time support
  - Events: onAnimationStart, onAnimationComplete, onAnimationLoop, onAnimationUpdate
  - **Note**: usePhysicsUpdate not supported with DOTween (use PhysicsPlatformAnimator for physics-based platforms)

## Code Conventions

- **Naming**: Educational naming convention implemented:
  - **New Scripts**: `[Category][Purpose]` format (e.g., `InputKeyPress`, `ActionSpawnObject`)
  - **Legacy Scripts**: Mixed conventions remain in root folder for migration
  - **Categories**: Input, Action, Physics, Game, UI
- **Physics**: Uses Unity's Rigidbody system with `linearVelocity` (new Unity physics API)
- **Input**: Mixed approaches - new Input System callbacks and legacy `Input.GetKeyDown()`
- **UI**: TextMeshPro integration for text display (`TMPro` namespace)
- **Events**: Heavy use of UnityEvents for designer-friendly connections
- **Editor Tools**: Advanced scripts include custom gizmos and editor debugging features
- **Organization**: Scripts grouped by functionality in `Assets/Scripts/`

## Educational Design Philosophy

### UnityEvent-Driven Architecture
- **No-Code Approach**: Students create interactivity by connecting UnityEvents in the Inspector
- **Visual Learning**: Event connections are visible and easy to understand
- **Modular Design**: Each script serves a specific purpose and can be combined with others
- **Designer-Friendly**: Non-programmers can create complex interactions

### Script Categories for Student Use

#### Input Components (Event Sources)
- Key press events, collision detection, trigger zones
- Countdown systems, collection thresholds

#### Action Components (Event Targets)  
- Object spawning, scene management, animation triggers
- Score updates, platform movement, material effects

#### Hybrid Components
- Scripts that both receive and send events for complex chains

### Learning Outcomes
- Understanding event-driven programming concepts
- Grasping component-based architecture
- Learning physics and animation principles
- Developing systems thinking for interactive design

## Development Notes

- Project uses Universal Render Pipeline (URP) for rendering
- Ground detection implemented via Physics.CheckSphere rather than raycast
- Camera system expects a main camera for movement direction calculation
- Force-based movement with velocity clamping for responsive physics feel
- UnityEvent system is the primary interface for student interactions
- Advanced scripts feature comprehensive editor tooling with custom gizmos and scene handles
- Material instances are properly managed to avoid shared material modification
- Mixed Input System usage - newer scripts use Input System, older ones use legacy Input class
- Platform movement uses physics forces rather than direct transform manipulation
- Spawning systems support both random and manual triggering patterns

### Critical Physics Patterns
- **Rigidbody Position Setting**: When teleporting or repositioning objects with Rigidbody components:
  - Use `rb.position` and `rb.rotation` instead of `transform.position/rotation` to avoid physics conflicts
  - Wait for `WaitForFixedUpdate()` before setting position to ensure physics system is ready
  - Always zero out velocities (`rb.linearVelocity` and `rb.angularVelocity`) before repositioning
  - This prevents "flashing" or "snapping back" issues when physics tries to interpolate from old positions
  - Example: GameCheckpointManager restoration after scene reload
- **Scene Persistence**: Objects using `DontDestroyOnLoad` must subscribe to `SceneManager.sceneLoaded` event to react to scene reloads, as `Start()` only runs once on initial creation
- **Visual Hiding During Teleportation**: When teleporting with cameras (Cinemachine), disable renderers during position changes to prevent visual flashing, as seen in GameCheckpointManager

### System Integration Patterns
- **Automatic Component Discovery**: GameStateManager automatically finds and coordinates with GameTimerManager instances
- **Unified Input Coordination**: Clear key separation (P=pause, ESC=quit, M=menu, restart=button only)
- **Event-Driven Architecture**: All systems communicate through UnityEvents for visual Inspector connections
- **Modular Design**: Each component can work independently or integrate seamlessly with others
- **No-Code Philosophy**: Students create complex interactions without writing scripts

### Cross-System Coordination
- **Timer-State Integration**: GameTimerManager automatically pauses/resumes with GameStateManager
- **Health-State Integration**: Health events can trigger game state changes
- **UI-Data Integration**: UI components automatically update from game data managers
- **Audio-State Integration**: Audio system responds to game state and timer events
- **Camera-Event Integration**: Camera switches can be triggered by any game event

### Adding New Educational Components
When creating new scripts for student use:
1. Expose key parameters as public SerializeField fields
2. Include UnityEvents for both input and output where appropriate
3. Add helpful tooltips and headers for student understanding
4. Consider editor gizmos for visual feedback
5. Follow the underscore naming convention for utility scripts (`_scriptName`)
6. **REQUIRED**: Add XML documentation comments (see Documentation Generator section below)

### Editor Scene Generator Patterns

When creating editor tools to generate example scenes (like `EventSequencerExampleGenerator`), follow these critical patterns:

#### Programmatic UnityEvent Configuration
UnityEvents must be configured via **SerializedProperty** to persist properly. Direct manipulation via UnityEventTools or reflection won't save to the scene:

```csharp
private static void AddPersistentListener(SerializedProperty unityEvent, Object target, string methodName)
{
    SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
    int index = calls.arraySize;
    calls.InsertArrayElementAtIndex(index);

    SerializedProperty call = calls.GetArrayElementAtIndex(index);
    call.FindPropertyRelative("m_Target").objectReferenceValue = target;
    call.FindPropertyRelative("m_MethodName").stringValue = methodName;
    call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.EventDefined;
    call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
}

// For methods with bool parameters:
private static void AddPersistentListener(SerializedProperty unityEvent, Object target, string methodName, bool boolValue)
{
    // ... same as above, plus:
    call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.Bool;
    call.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue = boolValue;
}
```

#### UI EventSystem Requirement
Unity's UI system requires an **EventSystem** component for button clicks to work. Always check and create if missing:

```csharp
// Create EventSystem if it doesn't exist (required for UI button interaction)
if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
{
    GameObject eventSystemObj = new GameObject("EventSystem");
    eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
}
```

**Note**: Use `FindFirstObjectByType` (Unity 6+), not the deprecated `FindObjectOfType`.

#### Component Enable/Disable Lifecycle
Components with `playOnStart` behavior need **OnEnable** handlers to work with enable/disable cycles:

```csharp
// PROBLEM: Start() only runs once when object is created
void Start() {
    if (playOnStart) Play();
}

// SOLUTION: Add OnEnable to restart on re-enable
private bool hasStarted = false;

void OnEnable() {
    // Only play if Start() has already been called
    if (hasStarted && playOnStart) {
        Play();
    }
}

void Start() {
    // ... initialization ...
    hasStarted = true;
    if (playOnStart) Play();
}
```

This pattern ensures animations/behaviors restart when GameObjects are re-enabled, critical for event sequencers and spawning systems.

#### SerializedObject Best Practices
- Always call `so.ApplyModifiedProperties()` after making changes
- Call `EditorUtility.SetDirty(component)` to mark the object as modified
- Use `Undo.RegisterCreatedObjectUndo()` for undo support when creating GameObjects
- Access private fields via `SerializedObject.FindProperty("fieldName")`

## Script Documentation Generator

### Purpose
The Script Documentation Generator (`Assets/Scripts/Documentation/Editor/script_doc_generator.cs`) is an automated tool that creates visual, in-Unity documentation of all educational scripts. It generates an interactive UI canvas displaying all scripts organized by folder, showing their public methods (functions) and UnityEvents in a color-coded, easy-to-read format.

### Functionality
The generator:
- **Scans** all scripts in `Assets/Scripts/` and its subfolders (Input, Actions, Physics, Game, UI, Puzzle, Animation)
- **Extracts** class-level descriptions and method-level descriptions from XML documentation comments
- **Creates** a Canvas-based UI visualization showing:
  - Folder categories with color-coded columns
  - Script names and descriptions
  - Public methods (FUNCTIONS) with parameter signatures and descriptions
  - UnityEvents (EVENTS) that can be wired in the Inspector
- **Displays** information using TextMeshPro with configurable font sizes and custom fonts
- **Organizes** scripts spatially in columns by category for easy browsing

Access via **Tools > Script Documentation Generator** in Unity's menu bar.

### Compliance Requirements

**ALL educational MonoBehaviour scripts MUST include XML documentation comments to work properly with the generator.**

#### Required: Class-Level Summary
Every MonoBehaviour class must have an XML `<summary>` tag immediately before the class declaration:

```csharp
/// <summary>
/// Brief description of what this component does and its purpose in the toolkit
/// </summary>
public class MyComponent : MonoBehaviour
{
    // class implementation
}
```

#### Required: Method-Level Summaries
Every public method (except Unity lifecycle methods like Start, Update, etc.) must have an XML `<summary>` tag:

```csharp
/// <summary>
/// Description of what this method does and when to call it
/// </summary>
public void MyPublicMethod()
{
    // method implementation
}

/// <summary>
/// Sets the maximum speed for the controller
/// </summary>
public void SetMaxSpeed(float speed)
{
    maxSpeed = speed;
}
```

#### Required: UnityEvent Descriptions
Every public UnityEvent field must have an XML `<summary>` tag describing when it fires:

```csharp
/// <summary>
/// Fires when the object enters the trigger zone
/// </summary>
public UnityEvent onTriggerEnter;

/// <summary>
/// Fires when health reaches zero
/// </summary>
public UnityEvent onDeath;

/// <summary>
/// Fires when the countdown timer reaches zero, passing the final time as a float parameter
/// </summary>
public UnityEvent<float> onCountdownComplete;
```

#### What Gets Documented
The generator automatically includes:
- ✅ **Public methods** with 0-4 parameters (UnityEvent compatible)
- ✅ **UnityEvent fields** (outputs that can be wired in Inspector)
- ❌ **Excludes**: Unity lifecycle methods (Start, Update, Awake, OnEnable, etc.)
- ❌ **Excludes**: Property getters/setters
- ❌ **Excludes**: Private/protected methods
- ❌ **Excludes**: Editor scripts in Editor folders

#### Current Compliance Status
As of October 2025, **all 46 educational scripts (100%) are fully compliant** with XML documentation requirements:
- Input: 6/6 scripts ✅
- Actions: 12/12 scripts ✅ (includes 4 decal animation scripts)
- Physics: 7/7 scripts ✅
- Game: 9/9 scripts ✅
- UI: 1/1 scripts ✅
- Puzzle: 2/2 scripts ✅
- Animation: 1/1 scripts ✅
- Root Scripts: 2/2 scripts ✅

#### Example: Fully Compliant Script

```csharp
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detects when objects with specific tags enter a trigger zone and fires events
/// </summary>
public class InputTriggerZone : MonoBehaviour
{
    [SerializeField] private string targetTag = "Player";

    /// <summary>
    /// Fires when an object with the target tag enters the trigger zone
    /// </summary>
    public UnityEvent onTriggerEnter;

    /// <summary>
    /// Fires when an object with the target tag exits the trigger zone
    /// </summary>
    public UnityEvent onTriggerExit;

    /// <summary>
    /// Sets which tag to detect for trigger events
    /// </summary>
    public void SetTargetTag(string newTag)
    {
        targetTag = newTag;
    }

    /// <summary>
    /// Manually triggers the enter event (useful for testing)
    /// </summary>
    public void TriggerEnterEvent()
    {
        onTriggerEnter?.Invoke();
    }

    // Unity lifecycle methods don't need documentation
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            onTriggerEnter?.Invoke();
        }
    }
}
```

### Best Practices
1. **Keep summaries concise** - One or two sentences explaining the purpose
2. **Focus on "what" and "when"** - Describe what the method does and when students should call it
3. **Use student-friendly language** - Avoid overly technical jargon
4. **Document all public methods** - If it's callable from UnityEvents, it needs documentation
5. **Update documentation when changing functionality** - Keep XML comments in sync with code changes

### Future Enhancement Roadmap

#### High-Priority Enhancements
1. **Parameter Documentation Support** - Extract and display `<param>` tags to show parameter descriptions inline with method signatures
2. **Remarks/Usage Examples** - Support `<remarks>` or `<example>` tags for detailed guidance on common setups and compatible scripts
3. **UnityEvent Descriptions** ✅ *IN PROGRESS* - Add XML comments above UnityEvent fields to describe when they fire and what data they provide

#### Medium-Priority Enhancements
4. **Category/Difficulty Tags** - Add custom XML tags or attributes to mark script complexity (Beginner/Intermediate/Advanced) and suggest compatible scripts
5. **Export to Student-Facing Markdown** - Generate markdown or PDF documentation that students can reference outside Unity
6. **Interactive Search/Filter** - Add UI controls to the generated canvas for searching, filtering by category, and difficulty level
7. **Example Scene References** - Link each script to example scenes that demonstrate its usage

#### Low-Priority/Nice-to-Have
8. **Visual Connection Diagrams** - Show common script combinations with visual connecting lines (e.g., InputTriggerZone → ActionSpawnObject)
9. **Tooltips in Visualization** - Make the generated UI interactive with hover tooltips showing additional details
10. **Version History** - Track when scripts were last modified and log changes

**Next Implementation**: UnityEvent descriptions (#3) to help students understand when events fire and how to use them in the Inspector.

---

## Changelog

### October 2025 - DOTween FREE Compatibility & CharacterControllerCC Animation Fix

**CRITICAL: DOTween FREE Compatibility Refactor**

The project was using DOTween UI extension methods (`DOFade()`, `DOAnchorPos()`) that are **NOT included in DOTween FREE**. All UI animation scripts have been refactored to use core `DOTween.To()` methods instead.

**Scripts Refactored for DOTween FREE (4 scripts, 12 total changes):**

1. **ActionDisplayImage.cs** - UI Image fade animations
   - Replaced `imageComponent.DOFade()` with `DOTween.To()` animating Color alpha
   - 2 occurrences fixed (fade in, fade out)
   - Functionality identical, now FREE-compatible

2. **ActionDisplayText.cs** - TextMeshProUGUI fade animations
   - Replaced `textComponent.DOFade()` with `DOTween.To()` animating Color alpha
   - 2 occurrences fixed (fade in, fade out)
   - Typewriter effect unchanged (already used `DOTween.To()`)

3. **ActionDialogueSequence.cs** - Dialogue system animations
   - Replaced `Image.DOFade()` with `DOTween.To()` animating Color (4 occurrences)
   - Replaced `RectTransform.DOAnchorPos()` with `DOTween.To()` animating anchoredPosition (3 occurrences)
   - 7 total occurrences fixed across portrait and text animations
   - All animation types still work: FadeIn, SlideUpFromBottom, SlideInFromSide, TypeOn

4. **FadeInFromBlackOnRestart.cs** - Scene transition fade
   - Replaced `imageComponent.DOFade()` with `DOTween.To()` animating Color alpha
   - 1 occurrence fixed
   - Unscaled time support preserved with `.SetUpdate(true)`

**Technical Implementation:**

The project uses **lambda expressions** with `DOTween.To()` to tween properties directly, which is the DOTween FREE-compatible approach:

```csharp
// BEFORE (DOTween Pro UI Extensions - NOT FREE):
imageComponent.DOFade(0f, duration)
rectTransform.DOAnchorPos(targetPosition, duration)

// AFTER (DOTween FREE Core Methods with Lambdas):
DOTween.To(
    () => imageComponent.color,              // Getter lambda: returns current value
    x => imageComponent.color = x,           // Setter lambda: sets new value
    new Color(color.r, color.g, color.b, 0f), // Target value
    duration                                  // Duration
)

DOTween.To(
    () => rectTransform.anchoredPosition,    // Getter lambda
    x => rectTransform.anchoredPosition = x, // Setter lambda
    targetPosition,                           // Target value
    duration                                  // Duration
)
```

**Lambda Pattern Explanation:**
- **Getter `() => property`**: Lambda that returns the current property value
- **Setter `x => property = x`**: Lambda that sets the property to a new value
- DOTween calls the getter/setter repeatedly to animate between current and target values
- This works with ANY property (color, position, scale, custom values)

**Impact:**
- ✅ **100% DOTween FREE compatible** - No Pro license required
- ✅ All animations work identically to before
- ✅ No breaking changes to public APIs or Inspector fields
- ✅ Students can use toolkit without purchasing DOTween Pro

---

**CharacterControllerCC Animation System Fix**

Fixed critical issue where the `Grounded` animator parameter was being set **every frame**, causing jump animation retriggering and transition problems.

**Problem:**
- `Grounded` bool was updated every frame in `UpdateAnimations()` (line 733)
- This caused Unity Animator transitions to continuously retrigger
- Made it difficult/impossible to play full jump animations
- "Any State → Jump" transitions would fire repeatedly while airborne

**Solution:**
- Added dedicated state tracking variable `_lastAnimatorGroundedState` (line 151)
- Grounded parameter now only updates when state actually changes (line 737-741)
- Uses comparison: `if (isGrounded != _lastAnimatorGroundedState)` before calling `SetBool()`
- Immediately updates tracking variable after setting animator parameter

**Code Changes:**
```csharp
// Added state tracking variable
private bool _lastAnimatorGroundedState;

// Updated animation method (line 737-741)
if (HasParameter(_animIDGrounded) && isGrounded != _lastAnimatorGroundedState)
{
    characterAnimator.SetBool(_animIDGrounded, isGrounded);
    _lastAnimatorGroundedState = isGrounded;
}
```

**Benefits:**
- ✅ Jump animations play correctly without retriggering
- ✅ Landing animations trigger properly on state change
- ✅ Better performance (fewer animator updates)
- ✅ Follows Unity's official character controller best practices
- ✅ No changes to public API or Inspector

**Files Modified:**
- `CharacterControllerCC.cs` (lines 151, 737-741)

---

### October 2025 - DOTween Animation Refactoring

**Major Changes:**
- **Added DOTween FREE** to the project (`Assets/Plugins/Demigiant/DOTween/`)
  - Professional animation engine for smooth, efficient tweening
  - All scripts use DOTween FREE (open-source), not DOTween Pro
  - Students can use the toolkit without requiring paid assets

**Scripts Refactored to Use DOTween:**

1. **ActionDisplayImage.cs** - Replaced ~70 lines of manual coroutine code
   - Now uses `DOTween.To()` for color animations and `DOScale()` for smooth transitions
   - Simultaneous fade and scale animations using `Sequence.Join()`
   - Automatic cleanup with `OnDestroy()`
   - Code reduction: 318 → 265 lines
   - **Note**: Originally used `DOFade()` UI extension, refactored to `DOTween.To()` for FREE compatibility

2. **ActionDisplayText.cs** - Replaced 17-line fade coroutine
   - Uses `DOTween.To()` for text fade in/out (refactored from `DOFade()` for FREE compatibility)
   - Typewriter effect uses `DOTween.To()` for `maxVisibleCharacters` animation
   - More reliable than `DOText()` for maintaining text formatting

3. **FadeInFromBlackOnRestart.cs** - Replaced 28-line fade coroutine
   - Entire fade now compact DOTween code using `DOTween.To()`
   - Built-in unscaled time support with `.SetUpdate(true)`
   - Automatic delay handling with `.SetDelay()`
   - **Note**: Originally used `DOFade()` UI extension, refactored to `DOTween.To()` for FREE compatibility

4. **GameUIManager.cs** - Replaced score punch and health bar coroutines
   - Uses built-in `DOPunchScale()` for score animations
   - Health bar uses `DOTween.To()` for smooth value interpolation
   - Simpler, more readable code

5. **PhysicsBumper.cs** - Replaced ~60 lines of animation coroutine
   - **Preserves AnimationCurve support** via `.SetEase(curve)`
   - Scale and emission animations run simultaneously with `Sequence.Join()`
   - Supports both float and color material properties

6. **ActionAnimateTransform.cs** - Replaced ~200 lines of coroutine logic
   - Complex multi-property animations using DOTween
   - **Full AnimationCurve support** for custom easing
   - All 9 transform properties (Position/Rotation/Scale XYZ) animated independently
   - Maintains Offset and Absolute modes
   - Loop and PingPong support via `LoopType.Yoyo`
   - **Note**: `usePhysicsUpdate` not supported (use `PhysicsPlatformAnimator` instead)
   - **Note**: `loopDelay` not currently supported

**New Scripts Added:**

7. **ActionDialogueSequence.cs** - Complete dialogue system for visual novels
   - Sequential dialogue line playback with multiple animation types
   - Character portrait support with left/right positioning
   - Image animations: None, SlideUpFromBottom, SlideInFromSide, FadeIn, PopIn
   - Text animations: None, TypeOn, FadeIn, SlideUpFromBottom
   - Customizable background with position/size controls
   - Default fade durations: 0.2 seconds for snappy transitions
   - Loop mode, preview system, and comprehensive events
   - All animations use `DOTween.To()` for FREE compatibility (refactored from UI extensions)

8. **DialogueUIController.cs** - Dialogue UI management (companion script)
   - Handles creation and manipulation of all dialogue UI elements
   - Decoupled from dialogue playback logic for clean separation of concerns
   - Dynamic Canvas creation with proper CanvasScaler setup
   - Preview mode support for Editor workflow

9. **ActionDialogueSequenceEditor.cs** - Custom Inspector for dialogue system
   - Context-aware animation settings (only shows relevant fields)
   - Live preview in Editor with line selection
   - Visual layout improvements for student usability

**Technical Benefits:**
- **~405 total lines of code removed** and replaced with cleaner DOTween API
- Consistent animation approach across all scripts
- Better performance (DOTween is highly optimized)
- Easier to maintain and extend
- Students learn professional-grade animation techniques
- AnimationCurve support preserved where needed for advanced customization

**Compatibility:**
- All refactored scripts work with **DOTween FREE** (no Pro required)
- Backwards compatible - existing scenes continue to work
- No breaking changes to public APIs or Inspector fields

---

### October 2025 - Comprehensive Documentation & Example Generators

**Documentation Added:**

1. **CharacterControllerCC_Documentation.md** - Complete 940-line setup guide
   - **Quick Start section** for overwhelmed students (5-minute setup)
   - All 40+ parameters explained with tables, defaults, and descriptions
   - Complete animator setup guide with recommended transition settings
   - Hierarchy structure and component requirements
   - 12 public methods and 8 read-only properties reference
   - 8 UnityEvents fully documented
   - Scene gizmo visualization guide (7 different gizmos explained)
   - 4 common setup scenarios: Basic TPC, Platformer with Moving Platforms, Combat with Dodge, Slope-Based Level Design
   - Comprehensive troubleshooting section (8 common issues with solutions)
   - Best practices for performance, level design, animation, and events
   - Technical notes on physics timing, grounded detection algorithm, moving platform system, and slope physics

2. **DecalAnimationSystem_Documentation.md** - Complete 830-line URP decal guide
   - **Quick Start section** (3 easy steps for first animation)
   - Clearly answers: "Can ActionDecalSequence be used without library?" (YES!)
   - 4 complete script references: ActionDecalSequence, ActionDecalSequenceLibrary, ActionBlinkDecal, ActionBlinkDecalOptimized
   - Material vs Texture switching explained
   - URP project setup guide for enabling decals (one-time setup)
   - 4 common scenarios: Flashing neon sign, character facial expressions, realistic NPC blinking, interactive poster
   - Comprehensive troubleshooting (8 common issues: visibility, playback, materials)
   - Best practices for performance, organization, animation design
   - Complete example: Full facial expression system with hierarchy and event wiring

**New Decal Animation Scripts (4 main + 2 editors):**

1. **ActionDecalSequence.cs** - Frame-by-frame material animation for URP DecalProjector
   - Material sequence with custom timing per frame (MaterialFrame struct)
   - Playback controls: Play(), Stop(), Pause(), Resume(), JumpToFrame(int)
   - Adjustable playback speed (0.1x to 5x) with loop support
   - Runtime frame manipulation: AddFrame(), ClearFrames(), SetPlaybackSpeed()
   - 6 UnityEvents: onSequenceStart, onSequenceComplete, onSequencePause, onSequenceResume, onSequenceStop, onFrameChanged
   - Public properties: IsPlaying, IsPaused, CurrentFrameIndex, TotalFrames
   - Pause state preservation (remembers remaining time when paused)
   - Auto-cleanup on destroy/disable

2. **ActionDecalSequenceLibrary.cs** - Manager for multiple sequences
   - Switch between sequences by index or name
   - Navigation: PlayNext(), PlayPrevious(), PlaySequence(int), PlaySequenceByName(string)
   - Control current sequence: Pause, Resume, Stop
   - Get sequence references: GetCurrentSequence(), GetSequence(int)
   - Default sequence with auto-play on start
   - 2 UnityEvents: onSequenceChanged, onLibraryStopped
   - Automatic null checking and validation

3. **ActionBlinkDecal.cs** - Simple automatic blinking with material switching
   - Two materials (open eyes / closed eyes)
   - Randomized timing with variation percentage (natural feel)
   - Configurable: timeBetweenBlinks, randomPercentage, blinkDuration
   - Manual BlinkOnce() for triggered blinks
   - Runtime material/timing changes
   - 2 UnityEvents: onBlinkStart, onBlinkComplete

4. **ActionBlinkDecalOptimized.cs** - Optimized texture-based blinking
   - Switches textures instead of materials (more efficient)
   - Creates material instance automatically (prevents asset modification)
   - Configurable shader property name (default "_BaseMap" for URP)
   - Shader.PropertyToID caching for performance
   - Recommended for multiple characters
   - Same API as ActionBlinkDecal

5. **ActionDecalSequenceEditor.cs** - Custom Inspector for sequences
   - Shows total frames and duration in edit mode
   - Calculates adjusted duration based on playback speed
   - Live playback controls in play mode (Play/Pause/Resume/Stop buttons)
   - Real-time status display (Playing/Paused/Stopped)
   - Current frame counter (e.g., "3 / 5")
   - Auto-repaint in play mode for live updates

6. **ActionDecalSequenceLibraryEditor.cs** - Custom Inspector for library
   - Lists all sequences with names and indices
   - Shows current playing sequence with status
   - Number buttons (0-9) for quick sequence switching
   - Previous/Next navigation buttons
   - Pause/Resume/Stop controls
   - Live status updates in play mode

**New Example Generator Scripts (11 + 1 helper):**

Educational example scene generators accessible via **Tools > Examples** menu:

1. **PhysicsBumperExampleGenerator.cs** - Demonstrates PhysicsBumper with DOTween animations
2. **CharacterControllerExampleGenerator.cs** - Shows CharacterControllerCC setup with moving platform
3. **EnemyControllerExampleGenerator.cs** - PhysicsEnemyController chasing player
4. **CollectionSystemExampleGenerator.cs** - GameCollectionManager with collectibles
5. **HealthSystemExampleGenerator.cs** - GameHealthManager with damage zones and UI
6. **TimerSystemExampleGenerator.cs** - GameTimerManager countdown example
7. **InventorySystemExampleGenerator.cs** - GameInventorySlot system with UI
8. **CheckpointSystemExampleGenerator.cs** - GameCheckpointManager demonstration
9. **TriggerZoneExampleGenerator.cs** - InputTriggerZone examples
10. **AutoSpawnerExampleGenerator.cs** - ActionAutoSpawner with random spawning
11. **PuzzleSystemExampleGenerator.cs** - ActionPuzzleRequirement with multiple targets
12. **ExamplePlayerBallFactory.cs** - Helper for creating proper player balls with PlayerInput

**All example generators follow consistent patterns:**
- Use pink/blue materials from Assets/Materials/
- Create TMP annotations explaining the example
- Wire UnityEvents via SerializedProperty for persistence
- Create EventSystem if needed for UI interaction
- Position camera for optimal viewing
- Use ExamplePlayerBallFactory for consistent player setup

**CharacterControllerCC Improvements:**

- Added **animator parameter safety checks** to prevent crashes
  - New HasParameter(int paramHash) method checks if parameter exists before setting
  - All animator.Set calls wrapped with existence checks
  - Prevents "Parameter 'Hash XXXXX' does not exist" errors
- Fixed animator parameter naming: `"IsGrounded"` → `"Grounded"` (Unity convention)
- Now gracefully handles incomplete Animator Controllers (won't crash if parameters missing)
- StringToHash optimization already in place for performance

**Documentation Features:**

Both documentation files feature:
- **Supportive, encouraging tone** for overwhelmed students
- **Quick Start sections** (5 minutes or less) at the beginning
- "Don't worry!" and "You've got this!" messaging
- Progressive disclosure: simple first, then complex details
- Clear "When NOT to use" sections
- Extensive troubleshooting with checkmarks
- Real-world examples and scenarios
- Best practices for students
- Professional formatting with tables, code blocks, hierarchies

**Educational Impact:**

- **6,434 new lines** of documentation and code added
- Students can now generate 11 example scenes with one click
- Complete reference documentation for two major systems
- Decal animation system provides texture-based animation without rigging
- CharacterControllerCC rivals Unity's official Starter Assets in features
- All scripts maintain 100% XML documentation compliance (46/46 scripts)