# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

## Development Commands

### Building and Running
- Open the project in Unity Editor (Unity version determined by ProjectSettings)
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
- `ActionDisplayImage.cs` - UI image display with effects and fade capabilities

#### Physics Components
Located in `Assets/Scripts/Physics/`:
- `PhysicsPlayerController.cs` - Ball-based player controller with camera-relative movement
- `PhysicsCharacterController.cs` - Rigidbody-based character controller with:
  - Capsule collider and constraint management
  - Ground detection and slope handling
  - Animation integration with child animator support
  - Jump mechanics and physics-based movement
- `PhysicsEnemyController.cs` - AI enemy controller with:
  - Player detection and chase behavior
  - Configurable jump modes (none, random, collision-based, combined)
  - Automatic Rigidbody and CapsuleCollider setup
  - Rotation toward movement direction
- `PhysicsBumper.cs` - Advanced bumper/repulsion system with:
  - Configurable force direction (collision normal or radial)
  - Scale animation with custom curves and per-axis scaling
  - Material emission effects for visual feedback
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
- `GameUIManager.cs` - UI data display system for:
  - Score display with punch animations
  - Health bar with color transitions
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
- `FadeInFromBlackOnRestart.cs` - Automatic scene transition fade (standalone):
  - Automatically fades from black every time scene loads/restarts
  - No event wiring required - completely automatic
  - Editable fade duration and optional start delay
  - Auto-enables Image component on play (disable Image component in editor to not block view)
  - Perfect for hiding checkpoint restoration and scene restart transitions

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
As of October 2025, **all 42 educational scripts (100%) are fully compliant** with XML documentation requirements:
- Input: 6/6 scripts ✅
- Actions: 8/8 scripts ✅
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