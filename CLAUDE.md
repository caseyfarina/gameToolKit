# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**⚠️ IMPORTANT: Keep this file in the `main` branch.** CLAUDE.md is project-wide documentation that should be visible regardless of which branch is checked out. When documenting feature branches, add the documentation here in main (with clear branch labels), not in the feature branch itself.

## Quick Links

- **[Runtime Folder Structure & Component Reference](.claude/docs/runtime-structure.md)** - Complete inventory of all scripts
- **[Custom Editor Scripts Guide](.claude/docs/custom-editors.md)** - Critical info for modifying Inspector UI
- **[Development Patterns & Best Practices](.claude/docs/development-patterns.md)** - Physics patterns, system integration, Unity conventions
- **[Documentation Generator Guide](.claude/docs/documentation-generator.md)** - XML documentation requirements
- **[Changelog](.claude/docs/changelog.md)** - Recent updates and refactorings

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

**IMPORTANT**: The package repo's `README.md` is overwritten on every robocopy sync. Always edit `gameToolKit/Assets/eventGameToolKit/README.md` (the source), never the copy in `eventGameToolKit-Package/`.

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
│   │   └── Variables/     # Internal persistence (GameData singleton — not student-facing)
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
| New script added | `runtime-structure.md`, `ComponentQuickReference.md`, CLAUDE.md script count, add `[HelpURL]` attribute |
| Script renamed or removed | `runtime-structure.md`, `ComponentQuickReference.md`, `custom-editors.md` (if applicable) |
| New `[SerializeField]` on a script with a custom editor | The editor script (see [Custom Editors Guide](.claude/docs/custom-editors.md)) |
| New custom editor created | `custom-editors.md` table |
| Public API changed | XML doc comments in the script |
| New documentation page published | Update `[HelpURL]` on all components that page covers |

**`ComponentQuickReference.md`** (`Assets/eventGameToolKit/Documentation/`) is the student-facing one-page guide. It must stay current — students use it to discover what components exist.

### Adding New Fields to Components

**⚠️ CRITICAL: Check for custom editor scripts before adding fields!**

Some components have custom Inspector UI. When adding `[SerializeField]` fields to these scripts, you must update BOTH:
1. The MonoBehaviour script (`.cs` in Runtime folder)
2. The Editor script (`.cs` in Editor folder)

See **[Custom Editor Scripts Guide](.claude/docs/custom-editors.md)** for the complete list and workflow.

### UnityEvent Inspector Visibility Rule

Unity's event dropdown only shows public methods with **0 or 1 parameter**. Methods with 2+ parameters are invisible — even if extra parameters have C# default values.

**The trap:** `public void Increment(int slotIndex, int amount = 1)` looks callable with one arg, but Unity's reflection sees 2 parameters and hides it entirely.

**The fix:** Add a single-parameter overload that delegates to the full version:

```csharp
// Event-friendly (shows in Inspector)
public void Increment(int slotIndex) => Increment(slotIndex, 1);

// Full version (code use only)
public void Increment(int slotIndex, int amount) { ... }
```

**Safe patterns (always visible in inspector):**
- `public void DoThing()` — no params, always shows
- `public void DoThing(int amount)` — 1 primitive param, shows as static field
- `public void DoThing(string name)` — 1 string param, shows as static field

**Hidden (needs overload if student-facing):**
- `public void DoThing(int a, int b = 1)` — 2 params, hidden even with default

### Code Conventions

- **Naming**: `[Category][Purpose]` format (e.g., `InputKeyPress`, `ActionSpawnObject`)
- **Physics**: Use Unity's `linearVelocity` (new physics API)
- **UI**: TextMeshPro (`TMPro` namespace)
- **Events**: UnityEvents for all student-facing interactions
- **Documentation**: XML comments required for all public methods and UnityEvents

### Inspector Help Button (`[HelpURL]`)

**ALL educational scripts MUST have a `[HelpURL]` attribute** so the `?` button in the Inspector links to the docs site.

```csharp
[HelpURL("https://caseyfarina.github.io/egtk-docs/health.html")]
public class GameHealthManager : MonoBehaviour
```

- If the component has a dedicated deep-dive page, link to that page directly.
- If no dedicated page exists yet, link to the index: `https://caseyfarina.github.io/egtk-docs/`
- When a new documentation page is published, update all relevant components to point to it.

The `[HelpURL]` line goes **after** any `[RequireComponent]` attributes and **before** `public class`.

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

The underlying mechanism is `GameData` — an auto-created runtime ScriptableObject singleton that is completely invisible to students. `GameData` resets at the start of each play session automatically.

### Persist Across Scenes

| Manager | Persists | Mechanism | Default on Restart |
|---|---|---|---|
| `GameHealthManager` | Optional checkbox | `GameData` int slot 0 | Reset to default |
| `GameCollectionManager` | Optional checkbox | `GameData` int slot 1 | Reset to default |
| `GameInventoryManager` | Optional checkbox | `GameData` int slots 2–21 (max 20) | Reset to default |
| `GameStoreManager` | Per-item `persistPurchase` bool | `GameData` int slots 22–41 (max 20 items) | Keep value |
| `GameFlagManager` | Always (no checkbox needed) | `GameData` flag set | Keep value |
| `GameCheckpointManager` | Always | DontDestroyOnLoad singleton | N/A |

**Rules:**
- Add the manager to **each scene** that needs it — only the *value* carries over, not the manager itself.
- On the first scene load of a new play session, managers use their own Inspector defaults.
- `GameData` resets automatically at the start of each play session — no student action needed for "new game."

### Restart vs. Progression

`GameSceneManager` exposes two distinct load methods:
- `LoadScene(name)` — progression load, persisted values carry over
- `RestartScene(name)` / `RestartCurrentScene()` — death/failure restart, managers check their **On Restart** setting

Each persistent manager has an `On Restart` dropdown (`Reset To Default` / `Keep Value`) visible in the Inspector when persistence is enabled. Defaults are chosen to match the most common game design expectation (health resets, store upgrades survive).

### GameFlagManager — Arbitrary State Persistence

For objects that need to remember one-time events across scene loads (doors opened, pickups collected, NPCs talked to):

1. Place `GameFlagManager` anywhere in the scene — no configuration needed
2. Wire `SetFlag("my_flag")` to any UnityEvent on the triggering object
3. Place `GameFlagListener` on the object to restore, set the same flag name
4. Wire `onFlagAlreadySet` to the restore action (e.g. `SetActive(false)`)

**Future consideration:** A `GamePersistentPickup` component that wraps steps 2–4 into a single self-contained component for the common pickup case.

### ResetPersistence

`GameCollectionManager` and `GameInventoryManager` expose a public `ResetPersistence()` method callable from UnityEvents. Use it when game design calls for resetting score/inventory on specific events beyond a simple scene restart.

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

**66 Educational Scripts (100% XML Documented) | 28 Custom Editors**
- 12 Input components
- 22 Action components
- 7 Physics components
- 15 Game managers (includes GameSceneManager, SpawnPoint, GameStoreManager, GameFlagManager, GameFlagListener)
- 2 Puzzle components
- 1 UI component
- 3 Animation components
- 3 Root character controllers
- 1 ScriptableObject variable (GameData — internal, invisible to students)

For complete script inventory with features, see **[Runtime Structure](.claude/docs/runtime-structure.md)**.

---

## Multi-Scene Architecture

The toolkit solves multi-scene persistence through `GameData` — a runtime SO singleton invisible to students — rather than student-configured SO assets. Students add managers per-scene and tick checkboxes; data carries automatically.

### Key Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `GameData` | `Runtime/Variables/` | Internal persistence hub — students never touch this |
| `SpawnPoint` | `Runtime/Game/` | Marks spawn locations, implements ISpawnPointProvider |
| `GameSceneManager` | `Runtime/Game/` | Scene loading with restart vs. progression distinction |
| `GameFlagManager` | `Runtime/Game/` | Named boolean flags that persist across scenes |
| `GameFlagListener` | `Runtime/Game/` | Reacts to flag state on scene load and at runtime |

### Scene Loading Pattern

```
Bootstrap Scene (optional):
└── GameSceneManager (DontDestroyOnLoad)

Level1 Scene:
├── GameHealthManager (persistAcrossScenes ✓)
├── GameCollectionManager (persistAcrossScenes ✓)
└── Enemy → GameHealthManager.TakeDamage()

Level2 Scene:
├── GameHealthManager (persistAcrossScenes ✓) ← reads carried value
└── ... level content
```

Wire death/game-over events to `GameSceneManager.RestartScene()` for reset behavior, or `LoadScene()` for progression.
