# Event Game Toolkit

**Version 1.0.0** | Unity 6+

An educational Unity toolkit designed for the "Animation and Interactivity" class. Provides modular, no-code components using UnityEvents to create interactive experiences without writing code.

## 📚 Overview

The Event Game Toolkit is built around an **event-driven architecture** where students visually connect components in the Unity Inspector using UnityEvents. This enables complex interactive behaviors without requiring programming knowledge.

### Core Philosophy

- **Input Components** (Event Sources) - Detect events like key presses, collisions, triggers
- **Action Components** (Event Targets) - Perform actions when triggered (spawn, display, animate)
- **UnityEvents** - Visual connections in the Inspector that wire behaviors together

### What's Included

- **65 educational components** (100% XML-documented)
- **28 custom Inspector editors** for student-friendly UI
- **Comprehensive documentation** system
- **DOTween integration** for professional animations

---

## 🚀 Installation

### Method 1: Git URL (Recommended)

1. Open Unity 6 Package Manager (**Window > Package Manager**)
2. Click **+ (plus icon)** in top-left
3. Select **Add package from git URL...**
4. Enter: `https://github.com/caseyfarina/eventGameToolKit.git`
5. Click **Add**

### Method 2: Local Installation

1. Clone this repository
2. In Unity Package Manager, click **+ > Add package from disk...**
3. Navigate to the cloned folder and select `package.json`

---

## 📦 Components Library

### Input Components (Event Sources)
Located in `Input/` folder:

- **InputTriggerZone** - 3D collision detection with tag filtering
- **InputKeyPress** - Simple key press events
- **InputKeyCountdown** - Countdown system with UI display
- **InputCheckpointZone** - Checkpoint trigger system
- **InputMouseInteraction** - Mouse click/hover on 3D objects with optional modifier keys (Alt, Ctrl, Shift)
- **InputFPMouseInteraction** - First-person reticle-based interaction with optional modifier keys
- **InputOnStart** - Fire events on Awake or Start
- **InputInteractionZone** - Proximity interaction zone
- **InputActionEvent** - Unity Input System action wrapper
- **InputQuitGame** - Application quit handler

### Action Components (Event Targets)
Located in `Actions/` folder:

**Object Management:**
- **ActionSpawnObject** - Spawn single objects
- **ActionAutoSpawner** - Automatic spawning with random timing
- **ActionRespawnPlayer** - Player respawn system

**UI & Display:**
- **ActionDisplayText** - Text display with DOTween animations
- **ActionDisplayImage** - Image fading and scaling with DOTween
- **ActionDialogueSequence** - Complete dialogue/visual novel system
- **DialogueUIController** - Dialogue UI management

**Decal Animation System:**
- **ActionDecalSequence** - Frame-by-frame URP decal animation
- **ActionDecalSequenceLibrary** - Manage multiple decal sequences
- **ActionBlinkDecal** - Automatic blinking (material-based)
- **ActionBlinkDecalOptimized** - Optimized blinking (texture-based)

**Game Control:**
- **ActionRestartScene** - Scene restart functionality
- **ActionPlaySound** - Audio playback
- **ActionEventSequencer** - Sequential event execution

### Physics Systems
Located in `Physics/` folder:

**Player Controllers:**
- **CharacterControllerCC** - Advanced third-person humanoid controller with moving platforms, slopes, dodge mechanics
- **CharacterControllerFP** - First-person controller with mouse look (LateUpdate smoothing), cursor lock, slope detection, and moving platforms
- **PhysicsBallPlayerController** - Simple physics ball controller
- **PhysicsCharacterController** - Rigidbody-based character controller

**AI Controllers:**
- **PhysicsEnemyController** - AI chase behavior with configurable jump modes
- **EnemyControllerCC** - CharacterController-based AI enemy

**Physics Effects:**
- **PhysicsBumper** - Advanced bumper with DOTween animations
- **PhysicsPlatformStick** - Moving platform attachment
- **PhysicsPlatformAnimator** - Physics-based platform movement

### Game Management
Located in `Game/` folder:

- **GameStateManager** - Pause and victory state management
- **GameTimerManager** - Comprehensive timer system (count-up/countdown)
- **GameHealthManager** - Health system with thresholds, events, and optional self-contained UI
- **GameCollectionManager** - Score/collection tracking with optional self-contained UI
- **GameInventoryManager** - Multi-slot inventory with per-slot events and optional self-contained UI
- **GameStoreManager** - Store system with persistent purchases
- **GameFlagManager** - Named boolean flags that persist across scenes
- **GameFlagListener** - Reacts to flag state on scene load and at runtime
- **GameUIManager** - UI data display with animations
- **GameAudioManager** - Audio system with mixer integration
- **GameCameraManager** - Cinemachine camera switching
- **GameCheckpointManager** - Persistent checkpoint system

### Puzzle System
Located in `Puzzle/` folder:

- **PuzzleSwitch** - Multi-state switch component
- **PuzzleSwitchChecker** - Verify switch state combinations

### Animation System
Located in `Animation/` folder:

- **ActionAnimateTransform** - DOTween-based transform animation with AnimationCurve support

### UI Components
Located in `UI/` folder:

- **FadeInFromBlackOnRestart** - Automatic scene transition fade

---

## 📖 Documentation

### Online Documentation
**[https://caseyfarina.github.io/egtk-docs/index.html](https://caseyfarina.github.io/egtk-docs/index.html)**

Full component reference, setup guides, and tutorials.

### In-Package Documentation
The package includes a **Script Documentation Generator** accessible via **Tools > Script Documentation Generator**. This creates an interactive Canvas-based UI showing all components organized by category with their functions and events.

### Reference Files (included in package)
- **CharacterControllerCC_Documentation.md** - Complete setup guide with Quick Start
- **CharacterControllerFP_Documentation.md** - First-person controller setup guide
- **DecalAnimationSystem_Documentation.md** - Complete URP decal guide
- **ComponentQuickReference.md** - One-page component discovery guide for students

---

## 🎓 Educational Use

### Learning Outcomes

Students will understand:
- Event-driven programming concepts
- Component-based architecture
- Physics and animation principles
- Systems thinking for interactive design

### No-Code Approach

Students create complex interactions by:
1. Adding components to GameObjects
2. Configuring settings in the Inspector
3. Connecting UnityEvents visually
4. Testing and iterating

**Example Flow:**
```
InputTriggerZone (detects player)
  → onTriggerEnterEvent
    → ActionSpawnObject.SpawnSinglePrefab() (spawns enemy)
```

---

## 🔧 Technical Requirements

- **Unity Version:** Unity 6 (6000.0.0f1 or later)
- **Render Pipeline:** Universal Render Pipeline (URP) 17.0.3+
- **Required Packages:**
  - Input System 1.11.2+
  - TextMeshPro 3.0.6+
  - Cinemachine 3.1.2+
  - DOTween FREE (included in package)

---

## 📐 Code Conventions

### Naming Pattern
Scripts follow educational naming:
- **Input**[Purpose] - Event sources (e.g., `InputKeyPress`)
- **Action**[Purpose] - Event targets (e.g., `ActionSpawnObject`)
- **Physics**[Purpose] - Movement systems (e.g., `PhysicsBumper`)
- **Game**[Purpose] - Game managers (e.g., `GameHealthManager`)

### XML Documentation
All educational scripts (65/65) include XML documentation comments:
```csharp
/// <summary>
/// Detects when objects with specific tags enter a trigger zone and fires events
/// </summary>
public class InputTriggerZone : MonoBehaviour
```

---

## 🔄 Updates

Students can update the package to get new features and bug fixes:

1. Open Package Manager
2. Find "Event Game Toolkit" in the list
3. Click **Update** button when available

Or via git:
```bash
# In your Unity project's Packages folder
cd Packages/com.caseyfarina.eventgametoolkit
git pull origin main
```

---

## 🤝 Contributing

This is an educational package. For bug reports or feature requests, please contact your instructor.

---

## 📄 License

[Add appropriate license information]

---

## 🙏 Credits

**Author:** Casey Farina
**Course:** Animation and Interactivity
**Powered by:** DOTween FREE, Unity 6, URP

---

## 📞 Support

- **Online Docs:** [https://caseyfarina.github.io/egtk-docs/index.html](https://caseyfarina.github.io/egtk-docs/index.html)
- **Component Questions:** Use the Script Documentation Generator (Tools menu) or ComponentQuickReference.md
- **Instructor Help:** Contact Casey Farina

---

## 📋 Version History

See [CHANGELOG.md](CHANGELOG.md) for detailed version history.

**Current Version: 1.0.0** - 65 educational components, 28 custom editors

### Recent Changes
- **InputMouseInteraction / InputFPMouseInteraction** — modifier key support (Alt, Ctrl, Shift); `resetOnModifierRelease` safety checkbox
- **CharacterControllerFP** — camera look moved to `LateUpdate` for smoother feel; optional `mouseSmoothTime` smoothing; `onSteepSlope` event now fires when walking onto slopes; `TeleportTo()` resets camera smoothing state
- **GameFlagManager / GameFlagListener** — named boolean flags persisting across scenes
- **GameStoreManager** — store system with persistent purchases
- **GameHealthManager / GameCollectionManager / GameInventoryManager** — self-contained UI (text + bar)
