# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**⚠️ IMPORTANT: Keep this file in the `main` branch.** CLAUDE.md is project-wide documentation that should be visible regardless of which branch is checked out. When documenting feature branches, add the documentation here in main (with clear branch labels), not in the feature branch itself.

## Quick Links

- **[Runtime Folder Structure & Component Reference](.claude/docs/runtime-structure.md)** - Complete inventory of all scripts
- **[Custom Editor Scripts Guide](.claude/docs/custom-editors.md)** - Critical info for modifying Inspector UI
- **[Development Patterns & Best Practices](.claude/docs/development-patterns.md)** - Physics patterns, system integration, Unity conventions
- **[Documentation Generator Guide](.claude/docs/documentation-generator.md)** - XML documentation requirements
- **[Changelog](.claude/docs/changelog.md)** - Recent updates and refactorings

## Multi-Scene Support (Now in Main)

Multi-scene architecture has been ported to main from `feature/multi-scene-support`. The following components are now available:

- **GameData** ScriptableObject singleton for invisible cross-scene data persistence
- **SpawnPoint** component for marking player spawn locations
- **GameSceneManager** for scene loading with transitions
- Updated **GameHealthManager** and **GameCollectionManager** with optional SO variable support
- Updated **CharacterControllerCC** with spawn point priority logic

See [Multi-Scene Architecture](#multi-scene-architecture) section below for details.

## Project Overview

**Educational Unity Toolkit for "Animation and Interactivity" Class**

This project provides a modular, no-code toolkit for students to create interactive Unity experiences using UnityEvents. The design philosophy centers on visual, Inspector-based connections between components - no programming required.

- **Unity Version**: Unity 6 (6000.0.x or later)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Animation Engine**: DOTween FREE (open-source, no Pro license required)

## Critical Two-Repository Structure

**⚠️ IMPORTANT: This project has TWO git repositories that must stay synchronized!**

### Machines
- **Laptop** (hostname: `electricEye`): `C:\Users\casey\Documents\unityProjects\egtkWorkingProject\`
- **Desktop** (hostname: `BLD`): `F:\Unity Projects 2025\gameToolKitFarina\`

Run `hostname` to determine the current machine, then use the matching paths and sync command below.

### 1. Development/Testing Environment (This Repository)
- **Purpose**: Main Unity project where all development and testing happens
- **Contains**: Full Unity project with scenes, testing assets, and eventGameToolKit package at `Assets/eventGameToolKit/`
- **Laptop path**: `C:\Users\casey\Documents\unityProjects\egtkWorkingProject\gameToolKit\`
- **Desktop path**: `F:\Unity Projects 2025\gameToolKitFarina\gameToolKit\`

### 2. Unity Package Repository (Separate Git Repo)
- **Purpose**: Standalone Unity package with its own git repository
- **Contains**: Only package contents (no test scenes or development assets)
- **Used By**: Students via Unity Package Manager
- **Laptop path**: `C:\Users\casey\Documents\unityProjects\egtkWorkingProject\eventGameToolKit-Package\`
- **Desktop path**: `F:\Unity Projects 2025\eventGameToolKit-Package\`

### **CRITICAL SYNC RULE**

Before pushing to git, ALWAYS sync both repositories:

1. ✅ **Work in**: `gameToolKit/Assets/eventGameToolKit/` (this project)
2. ✅ **Test in**: `gameToolKit/` (full Unity project with scenes)
3. ✅ **Sync to package**: Use robocopy command (see below)
4. ✅ **Push together**: ALWAYS push both repos at the same time (never just one!)

**Sync Command (Laptop)**:
```bash
cmd //c robocopy "C:\Users\casey\Documents\unityProjects\egtkWorkingProject\gameToolKit\Assets\eventGameToolKit" "C:\Users\casey\Documents\unityProjects\egtkWorkingProject\eventGameToolKit-Package" //MIR //XD .git
```

**Sync Command (Desktop)**:
```bash
cmd //c robocopy "F:\Unity Projects 2025\gameToolKitFarina\gameToolKit\Assets\eventGameToolKit" "F:\Unity Projects 2025\eventGameToolKit-Package" //MIR //XD .git
```

**IMPORTANT**: Use `cmd //c` and double slashes `//` to avoid Git Bash path conversion errors (Git Bash converts `/MIR` to `C:/Program Files/Git/MIR` without the double slashes).

## Project Structure

### Main Directories

```
Assets/
├── eventGameToolKit/
│   ├── Runtime/           # All student-facing components
│   │   ├── Actions/       # Event targets (spawning, UI, audio, etc.)
│   │   ├── Animation/     # Transform animations
│   │   ├── CharacterControllers/  # Player and enemy controllers
│   │   ├── Game/          # Managers (health, timer, state, audio, etc.)
│   │   ├── Input/         # Event sources (triggers, keys, mouse, etc.)
│   │   ├── Interfaces/    # Core interfaces (ISpawnPointProvider)
│   │   ├── Physics/       # Bumpers, platforms, physics systems
│   │   ├── Puzzle/        # Puzzle mechanics
│   │   ├── UI/            # UI helpers and effects
│   │   ├── Utilities/     # Legacy/helper scripts
│   │   └── Variables/     # ⚠️ FEATURE BRANCH: ScriptableObject variables
│   └── Editor/            # Custom Inspector scripts
│       ├── ActionEditors/
│       ├── GameEditors/
│       ├── InputEditors/
│       ├── PhysicsEditors/
│       └── PuzzleEditors/
├── Scenes/                # Test scenes
└── Materials/             # Shared materials
```

### Key Files

- **`InputSystem_Actions.inputactions`** - Unity Input System configuration
- **`CharacterControllerCC_Documentation.md`** - Complete character controller guide
- **`DecalAnimationSystem_Documentation.md`** - URP decal animation guide

## Unity Packages Used

| Package | Version | Purpose |
|---------|---------|---------|
| Input System | 1.11.2 | Modern input handling |
| URP | 17.0.3 | Rendering pipeline |
| Cinemachine | 3.1.2 | Camera management |
| AI Navigation | 2.0.5 | Pathfinding |
| DOTween FREE | - | Animation tweening |
| Adobe Substance 3D | - | Material authoring |

### DOTween FREE Compatibility

**CRITICAL: This project uses DOTween FREE, not DOTween Pro.**

**IMPORTANT: Due to asmdef conflicts, avoid DOTween module-specific extensions in package code.** Use `DOTween.To()` instead - it's just as good and has no assembly reference issues.

| Avoid (asmdef conflicts) | Use Instead |
|--------------------------|-------------|
| `audioSource.DOFade()` | `DOTween.To(() => source.volume, x => source.volume = x, target, duration)` |
| `rigidbody.DOMove()` | `DOTween.To()` or `transform.DOMove()` |
| `spriteRenderer.DOFade()` | `DOTween.To(() => sr.color, x => sr.color = x, target, duration)` |

**Safe to use (core DOTween, no module dependencies):**
- `transform.DOMove()`, `DORotate()`, `DOScale()`, `DOPunchScale()`
- `rectTransform.DOAnchorPos()`
- `canvasGroup.DOFade()`, `image.DOFade()`, `image.DOColor()`
- `DOTween.To()` for any value type (universal, always works)
- `DOTween.Sequence()` for chaining
- `.SetUpdate()`, `.SetEase()`, `.OnComplete()`, `.Kill()`

**DOTween Pro Only (DO NOT USE):**
- `text.DOText()` (TextMesh Pro module)
- Path tweening
- DeAudio, DeUnityExtended

## Educational Design Philosophy

### UnityEvent-Driven Architecture

Students create interactions by wiring UnityEvents in the Inspector:
- **No-Code Approach**: Visual connections replace programming
- **Event Sources**: Input components (triggers, keys, mouse)
- **Event Targets**: Action components (spawn, display, animate)
- **Modular**: Mix and match components to create complex systems

### Core Component Categories

| Category | Description | Example Scripts |
|----------|-------------|-----------------|
| **Input** | Event sources triggered by player or game state | InputKeyPress, InputTriggerZone, InputCheckpointZone |
| **Actions** | Event targets that perform actions | ActionSpawnObject, ActionDisplayText, ActionDialogueSequence |
| **Physics** | Movement, forces, collisions | PhysicsBumper, CharacterControllerCC, PhysicsPlatformAnimator |
| **Game** | Managers for health, score, timer, audio, etc. | GameHealthManager, GameStateManager, GameAudioManager |
| **Puzzle** | Switch and checker mechanics | PuzzleSwitch, PuzzleSwitchChecker, PuzzleSequenceChecker |
| **UI** | User interface helpers | FadeInFromBlackOnRestart |
| **Animation** | Transform animations | ActionAnimateTransform |

## Development Workflow

### Before Pushing to Git — Maintenance Checklist

When adding, renaming, or modifying scripts, update these files before committing:

| What changed | Files to update |
|---|---|
| New script added | `runtime-structure.md`, `ComponentQuickReference.md`, CLAUDE.md script count |
| Script renamed or removed | `runtime-structure.md`, `ComponentQuickReference.md`, `custom-editors.md` (if applicable) |
| New `[SerializeField]` on a script with a custom editor | The editor script (see [Custom Editors Guide](.claude/docs/custom-editors.md)) |
| New custom editor created | `custom-editors.md` table |
| Public API changed | XML doc comments in the script |

**`ComponentQuickReference.md`** (`Assets/eventGameToolKit/Documentation/`) is the student-facing one-page guide. It must stay current — students use it to discover what components exist.

### Adding New Fields to Components

**⚠️ CRITICAL: Check for custom editor scripts before adding fields!**

Some components have custom Inspector UI. When adding `[SerializeField]` fields to these scripts, you must update BOTH:
1. The MonoBehaviour script (`.cs` in Runtime folder)
2. The Editor script (`.cs` in Editor folder)

See **[Custom Editor Scripts Guide](.claude/docs/custom-editors.md)** for the complete list and workflow.

### Code Conventions

- **Naming**: `[Category][Purpose]` format (e.g., `InputKeyPress`, `ActionSpawnObject`)
- **Physics**: Use Unity's `linearVelocity` (new physics API)
- **UI**: TextMeshPro (`TMPro` namespace)
- **Events**: UnityEvents for all student-facing interactions
- **Documentation**: XML comments required for all public methods and UnityEvents

### XML Documentation Requirement

**ALL educational scripts MUST have XML documentation for the Documentation Generator:**

```csharp
/// <summary>
/// Brief description of component purpose
/// </summary>
public class MyComponent : MonoBehaviour
{
    /// <summary>
    /// Fires when the player enters the zone
    /// </summary>
    public UnityEvent onEnter;

    /// <summary>
    /// Manually triggers the enter event
    /// </summary>
    public void TriggerEnter() { }
}
```

See **[Documentation Generator Guide](.claude/docs/documentation-generator.md)** for complete requirements.

## Common Tasks

### Running the Project
1. Open in Unity 6 Editor
2. Press Play (Ctrl+P) to test
3. Open main scene: `Assets/Scenes/ballPlayer.unity`

### Building
- Build Settings: Ctrl+Shift+B
- Test Framework: Window > General > Test Runner

### Syncing Repositories
1. Make changes in `gameToolKit/Assets/eventGameToolKit/`
2. Test thoroughly in Unity
3. Run robocopy sync command (see above)
4. Test package in clean Unity project
5. Push both repos together

### Example Scenes
Example scenes live in `Assets/eventGameToolKit/ExampleScenes/`. Add new ones directly as Unity scene files — no code-based generators.

## Custom Slash Commands

### `/desk-check <ScriptName>`

Performs a manual trace review (desk check) of a script. Traces every public method with forecasted inputs to verify logic without executing it. Produces per-method trace tables with BUG / EDGE CASE / OK verdicts, checks editor property name matches, and validates flag ordering and event timing. Asks before applying fixes.

**Usage**: `/desk-check GameHealthManager`, `/desk-check ActionDialogueSequence`

## Scene Persistence

Four managers support a `Persist Across Scenes` checkbox. The underlying mechanism is `GameData` — an auto-created runtime ScriptableObject singleton that is completely invisible to students. They just tick the box.

| Manager | Persists | Mechanism | Internal slot |
|---|---|---|---|
| `GameHealthManager` | Optional | `GameData` int slot 0 | Automatic |
| `GameCollectionManager` | Optional | `GameData` int slot 1 | Automatic |
| `GameInventoryManager` | Optional | `GameData` int slots 2–21 | Automatic (max 20 slots) |
| `GameCheckpointManager` | Always | DontDestroyOnLoad singleton | N/A |
| All others | Never | — | — |

**Rules:**
- Add the manager to **each scene** that needs it — only the *value* carries over, not the manager itself. This means event wiring in each scene is local and works normally.
- On the first scene load of a new play session, managers use their own Inspector defaults. On subsequent loads, they read the last written value.
- GameInventoryManager logs a warning if more than 20 slots are configured with persistence enabled.
- `GameData` resets automatically at the start of each play session — no student action needed for "new game."

## Self-Contained UI Pattern

**GameHealthManager**, **GameCollectionManager**, and **GameInventoryManager** all support optional self-contained UI toggled via `showUI`. When enabled, the manager creates its own Canvas at runtime — no GameUIManager wiring needed.

**Important**: Students should use EITHER the individual controls (Option A) OR GameUIManager (Option B), never both on the same manager. Mixing them creates duplicate overlapping displays. See [GameUI_QuickStart.md](Assets/eventGameToolKit/Documentation/GameUI_QuickStart.md) for student guidance.

**GameTimerManager** also supports self-contained UI: a clock text display (with optional gradient color) and a fill bar (with gradient), both with independent positioning controls. Count-down timers automatically use `startTime` as 100%; count-up timers require `totalTime` to be set for bar/gradient to work.

## GameInventoryManager

`GameInventoryManager` (`Runtime/Game/GameInventoryManager.cs`) replaces the single-slot `GameInventorySlot` with a configurable list of inventory slots in a single component.

`GameInventorySlot` has been removed — it's in git history if needed.

### Architecture

```
GameInventoryManager
├── List<InventorySlot> slots    // configurable in Inspector
├── showUI toggle                // self-contained UI (row of icon+count cards)
└── showCount toggle             // optional count number in each card

[Serializable] InventorySlot
├── string itemName
├── Sprite icon (optional)       // shown in UI card
├── int maxCapacity
├── int currentCount
├── UnityEvent onFull            // fires when count reaches maxCapacity
├── UnityEvent onEmpty           // fires when count reaches 0
└── UnityEvent<int> onChanged    // fires on any count change, passes new count
```

### Migration from GameInventorySlot

Students using `GameInventorySlot` will need to:
1. Remove `GameInventorySlot` components
2. Add `GameInventoryManager`
3. Re-configure slots and re-wire events

## Getting Help

- **Script Reference**: See [Runtime Structure](.claude/docs/runtime-structure.md)
- **Custom Editors**: See [Custom Editors Guide](.claude/docs/custom-editors.md)
- **Physics Patterns**: See [Development Patterns](.claude/docs/development-patterns.md)
- **Recent Changes**: See [Changelog](.claude/docs/changelog.md)

## Quick Reference

**61 Educational Scripts (100% XML Documented) | 25 Custom Editors**
- 12 Input components
- 20 Action components
- 7 Physics components
- 12 Game managers (includes GameSceneManager + SpawnPoint)
- 2 Puzzle components
- 1 UI component
- 3 Animation components
- 3 Root character controllers
- 1 ScriptableObject variable (GameData — internal, invisible to students)

For complete script inventory with features, see **[Runtime Structure](.claude/docs/runtime-structure.md)**.

---

## Multi-Scene Architecture

### The Problem

The toolkit's no-code design relies on dragging references in the Inspector. This breaks with multi-scene games because:
- UnityEvents can't reference objects in other scenes
- Managers in a "Bootstrap" scene can't be wired to enemies/collectibles in level scenes
- Cross-scene references are blocked in Edit mode

### The Solution: ScriptableObject Variables

Instead of storing data in MonoBehaviours (which are scene-bound), data lives in **ScriptableObject assets** (which are project-level):

```
Project Assets:
├── Variables/
│   ├── PlayerHealth.asset (IntVariable, default: 100)
│   └── PlayerScore.asset (IntVariable, default: 0)

Level1 Scene:
├── GameHealthManager → reads/writes PlayerHealth.asset
├── Enemy → wired to GameHealthManager.TakeDamage()
└── Coin → wired to GameCollectionManager.Increment()

Level2 Scene:
├── GameHealthManager → reads/writes PlayerHealth.asset (SAME asset)
└── ... level content
```

When scenes change, manager instances are destroyed and recreated, but **the data persists in the assets**.

### New Components (Feature Branch)

| Component | Location | Purpose |
|-----------|----------|---------|
| `IntVariable` | `Runtime/Variables/` | SO that holds an int, resets on Play |
| `FloatVariable` | `Runtime/Variables/` | SO that holds a float, resets on Play |
| `SpawnPoint` | `Runtime/Game/` | Marks spawn locations, implements ISpawnPointProvider |
| `GameSceneManager` | `Runtime/Game/` | Scene loading with spawn point support |

### Updated Components (Feature Branch)

| Component | Change |
|-----------|--------|
| `GameHealthManager` | New `healthVariable` field (optional) |
| `GameCollectionManager` | New `valueVariable` field (optional) |
| `CharacterControllerCC` | Improved spawn priority logic |

### Backward Compatibility

The SO variable fields are **optional**. If not assigned:
- Managers work exactly as before (single-scene behavior)
- No breaking changes for existing projects

### Value Reset Behavior

```csharp
// IntVariable uses [System.NonSerialized] for runtime value
// OnEnable() resets the initialized flag
// First access initializes from defaultValue
```

- **Editor**: Values reset to default when entering Play mode
- **Build**: Values reset when game launches, persist during gameplay

### Future SO Architecture Opportunities

The ScriptableObject pattern could improve other systems:

| System | Current | SO-Based Improvement |
|--------|---------|---------------------|
| **GameStateManager** | Local pause state | `BoolVariable` for global pause state |
| **GameTimerManager** | Local timer value | `FloatVariable` for persistent timer |
| **GameAudioManager** | Local volume settings | `FloatVariable` for volume levels |
| **GameCheckpointManager** | Already uses DontDestroyOnLoad | Could use SO for checkpoint data |
| **PuzzleSwitchChecker** | Checks switches in scene | `BoolVariable` per switch for cross-scene puzzles |

### Event Channels (Future Consideration)

Beyond variables, SO-based **Event Channels** could enable fully decoupled communication:

```csharp
// Event channel asset
[CreateAssetMenu]
public class VoidEventChannel : ScriptableObject
{
    public event System.Action OnEventRaised;
    public void RaiseEvent() => OnEventRaised?.Invoke();
}

// Any scene can raise/listen
public class Enemy : MonoBehaviour
{
    [SerializeField] VoidEventChannel onPlayerDamaged;
    void DealDamage() => onPlayerDamaged.RaiseEvent();
}
```

This would allow enemies in Level1 to communicate with UI in a persistent scene without direct references.

### Documentation

- **[MultiSceneSetup_QuickStart.md](Assets/eventGameToolKit/Documentation/MultiSceneSetup_QuickStart.md)** - Student guide (in feature branch)

### Merge Status

Multi-scene components were cherry-picked from `feature/multi-scene-support` into main (April 2026). The feature branch can be archived — it contains destructive changes (deleted components) that should NOT be merged directly.
