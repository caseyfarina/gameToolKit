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
- **Desktop** (hostname: `BLD`): `F:\Unity Projects 2026\eventGameToolKit\`

Run `hostname` to determine the current machine, then use the matching paths and sync command below.

### 1. Development/Testing Environment (This Repository)
- **Purpose**: Main Unity project where all development and testing happens
- **Contains**: Full Unity project with scenes, testing assets, and eventGameToolKit package at `Assets/eventGameToolKit/`
- **Laptop path**: `C:\Users\casey\Documents\unityProjects\egtkWorkingProject\gameToolKit\`
- **Desktop path**: `F:\Unity Projects 2026\eventGameToolKit\gameToolKit\`

### 2. Unity Package Repository (Separate Git Repo)
- **Purpose**: Standalone Unity package with its own git repository
- **Contains**: Only package contents (no test scenes or development assets)
- **Used By**: Students via Unity Package Manager
- **Laptop path**: `C:\Users\casey\Documents\unityProjects\egtkWorkingProject\eventGameToolKit-Package\`
- **Desktop path**: `F:\Unity Projects 2026\eventGameToolKit\eventGameToolKit-Package\`

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
cmd //c robocopy "F:\Unity Projects 2026\eventGameToolKit\gameToolKit\Assets\eventGameToolKit" "F:\Unity Projects 2026\eventGameToolKit\eventGameToolKit-Package" //MIR //XD .git
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
│   │   ├── PostProcessingAnimation/  # Stop motion look (global FPS + per-Animator)
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
├── ProjectAssets/         # Everything the project owns but students do not edit
│   ├── Animations/        # Clips and controllers
│   ├── Art/               # Models, materials, textures, shadergraphs
│   ├── Input/             # Project input settings and generated actions
│   ├── Materials/
│   ├── Scenes/TestScenes/ # ballPlayer, physics
│   ├── Dev/Tests/         # EditMode + PlayMode tests (never shipped)
│   ├── Dev/SceneBuilders/ # Example-scene generators (never shipped)
│   └── ThirdParty/        # StarterAssets, package Samples
├── Plugins/               # DOTween
├── Resources/  Settings/  "TextMesh Pro"/
└── _STUDENT_WORK/         # Students save everything here
```

### Assets folder layout

The Project window is ordered for students:

1. **`_STUDENT_WORK/`** — the underscore pins it to the top. Scenes, Scripts, Art, Prefabs.
2. **`eventGameToolKit/`** — the toolkit. Students read it, never edit it.
3. Everything else sorts below.

**Moving assets must go through `AssetDatabase.MoveAsset`**, never the filesystem: moving
folders with `mv` orphans `.meta` files and silently breaks every scene reference.
`ProjectAssets/Dev/SceneBuilders/ReorganizeAssets.cs` did the original move and is re-runnable.

`TextMesh Pro/` must stay at the Assets root — TMP resolves its settings by folder location.

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
| **PostProcessingAnimation** | Stop motion / low-framerate look | applicationFPSLimiting, StopMotionPostProcess |
| **2D** | Sprite-based games (see § 2D Support) | CharacterController2D |
| **Utilities** | Cursor and force helpers | lockMouseCursorToDisplay, ObjectAttractor |

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
| New student-facing component added | Add a row to the website catalog (`egtk-docs` repo) — the site does not update itself |

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

### Documentation Strategy — Transitioning to the Website

**The website at https://caseyfarina.github.io/egtk-docs/ is the primary student-facing
documentation.** Source repo: `egtk-docs`, cloned alongside this project at
`F:\Unity Projects 2026\eventGameToolKit\egtk-docs\`. XML comments are being demoted from "required everywhere" to a smaller,
targeted role.

**Why the change:**

- Unity's Inspector does **not** read XML comments. Students see `[Tooltip]` text and the `?`
  button (`[HelpURL]`) — never a `<summary>`. XML has no student-facing payoff in the editor.
- The one tool that consumed XML, the in-Unity Script Documentation Generator, still defaults to
  scanning `Assets/Scripts/` — a folder that no longer exists in this project. It is effectively
  dormant. See [Documentation Generator Guide](.claude/docs/documentation-generator.md).
- The website's component catalog is **hand-authored prose** (`Component / What It Does / Good For`),
  not generated from XML. It lists no methods or events at all, so XML could not have produced it.
  There is no pipeline between code and site — the site is maintained by hand in the `egtk-docs` repo.

**Current requirements, in priority order:**

| Requirement | Status | Why |
|---|---|---|
| `[HelpURL]` on every MonoBehaviour | **Required** | The `?` button is how students reach the docs |
| `[Tooltip]` on every student-facing `[SerializeField]` | **Required** | The only text students read in the Inspector |
| Website page or catalog row for every student-facing component | **Required** | Primary documentation |
| XML `<summary>` on the class | **Keep** | Cheap; seeds the website entry and orients anyone reading the code |
| XML `<summary>` on public UnityEvents | **Keep** | Describes *when* an event fires — the hardest thing to infer from code |
| XML `<summary>` on every public method | **Optional** | Was for the dormant generator; write it where the method is non-obvious |

**Do not** claim "100% XML documented" in docs or commit messages — it is no longer a tracked goal.

### XML Documentation Format

When you do write XML (class summaries and UnityEvents at minimum):

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

See **[Documentation Generator Guide](.claude/docs/documentation-generator.md)** for the generator's
status and the historical XML requirements.

## Common Tasks

### Running the Project
1. Open in Unity 6 Editor
2. Press Play (Ctrl+P) to test
3. Open main scene: `Assets/ProjectAssets/Scenes/TestScenes/ballPlayer.unity`

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

**80 Runtime Scripts | 29 Custom Editors | 3 Documentation Tools**

Runtime breakdown (`Assets/eventGameToolKit/Runtime/`, verified against disk):

| Folder | Count | Notes |
|---|---:|---|
| `Input/` | 12 | Event sources |
| `Actions/` | 22 | 21 actions + `DialogueUIController` (helper, not student-facing) |
| `Game/` | 14 | Managers, incl. GameSceneManager, SpawnPoint, GameStoreManager, GameFlagManager, GameFlagListener |
| `CharacterControllers/` | 8 | 6 Player + 2 Enemy (incl. `CharacterController2D`) |
| `Physics/` | 6 | Bumpers (2), Platforms (3), PhysicsForceZone |
| `Animation/` | 3 | |
| `PostProcessingAnimation/` | 3 | `applicationFPSLimiting`, `StopMotionPostProcess`, `StopMotionJob` (Burst job, internal) |
| `Puzzle/` | 3 | |
| `Utilities/` | 5 | `InputCollisionEnter`, `lockMouseCursorToDisplay`, `ObjectAttractor`, plus `EGTKPhysics` and `EGTKInput` (internal) |
| `UI/` | 1 | |
| `Variables/` | 1 | `GameData` — internal, invisible to students |
| `Interfaces/` | 2 | `ISpawnPointProvider`, `ITeleportableCharacter` |
| **Total** | **80** | |

`Editor/` holds 32 files: 29 with `[CustomEditor]` plus 3 documentation tools in `Editor/Documentation/`.

**Counting rule**: the totals above are raw `.cs` file counts. Not every file is a student-facing
component — `StopMotionJob`, `GameData`, `EGTKPhysics`, `EGTKInput`, `ISpawnPointProvider`,
`ITeleportableCharacter`, and `DialogueUIController` are internal. When you update these numbers, get them from disk:

```bash
find Assets/eventGameToolKit/Runtime -name '*.cs' | wc -l
grep -rl "CustomEditor" Assets/eventGameToolKit/Editor --include=*.cs | wc -l
```

For complete script inventory with features, see **[Runtime Structure](.claude/docs/runtime-structure.md)**.

---

## 2D Support

The toolkit works with sprites and 2D colliders. Support is **partial** — the table below is
the authoritative list of what has been converted. Anything not listed is still 3D-only.

### How it works

Components resolve 2D-versus-3D **per instance**, from the collider on their own GameObject.
There is no project setting and nothing for students to choose: put a `Collider2D` on a sprite
and the component uses 2D physics; put a `Collider` on a mesh and it uses 3D.

Unity's 2D and 3D physics are separate engines (Box2D and PhysX) that never interact, so both
can coexist in one scene as independent worlds. A 3D bumper cannot push a 2D player — that is an
engine limit, not a toolkit one.

Unity refuses to put 2D and 3D physics components on the same GameObject (`AddComponent` returns
null, either order, colliders and rigidbodies alike — verified on 6000.3.7f1). Detection therefore
cannot be ambiguous. `EGTKPhysicsTests.Unity_RefusesToMix2DAnd3DPhysicsOnOneObject` pins that
behaviour so a future Unity relaxing it fails loudly.

Two callback shapes are involved:

- **Callback-driven components** (trigger zones) gain a parallel `OnTriggerEnter2D` alongside
  `OnTriggerEnter`. The callback that fires proves the dimension, so no detection is needed.
- **Initiator components** (mouse picking) call `EGTKPhysics.Is2D(gameObject)`, cached in `Start`.

### Status

| Component | 2D | Notes |
|---|---|---|
| `InputTriggerZone` | ✅ | `Collider2D` or `Collider`, no setting |
| `InputCheckpointZone` | ✅ | `[RequireComponent(typeof(Collider))]` removed — it made 2D impossible |
| `InputMouseInteraction` | ✅ | Clicking and hovering sprites |
| `InputClickDrag` | ✅ | Use Drag Plane `WorldXY` for 2D |
| `InputClickRotate` | ✅ | |
| `CharacterController2D` | ✅ | New. Platformer or Top-Down |
| `InputInteractionZone` | ✅ | Proximity and mouse modes both |
| `InputCollisionEnter` | ✅ | `OnCollisionEnter2D` feeds the same handler |
| `InputFPMouseInteraction` | ❌ | **Intentional.** First-person reticle raycast has no 2D counterpart |
| `PhysicsBumper`, `PhysicsBumperTag` | ❌ | Not yet converted |
| `PhysicsForceZone` | ❌ | Not yet converted |
| `PhysicsEnemyController` | ❌ | No 2D enemy controller exists yet |
| `ActionRespawnPlayer`, `ActionSpawnProjectile` | ❌ | Not yet converted |
| `ActionPlatformAnimator`, `PhysicsPlatformStick` | ❌ | Not yet converted |
| `CharacterPushRigidBody`, `ObjectAttractor` | ❌ | Not yet converted |
| `GameCheckpointManager`, `ActionTeleportToTransform` | ⚠️ | Still hard-code `CharacterControllerCC`, so **2D respawn and teleport silently do nothing** |
| Decal components | ❌ | URP decals are 3D-only; no 2D analog exists |

Everything with no physics dependency — managers, timers, flags, audio, key input, most Actions —
already worked in 2D and needed no change.

### `EGTKPhysics`

`Runtime/Utilities/EGTKPhysics.cs` — internal static helper owning every 2D/3D decision, so the
rules cannot drift across call sites. Students never see it, same posture as `GameData`.

```csharp
static bool Is2D(GameObject go, PhysicsMode mode = PhysicsMode.Auto);
static bool TryAddImpulse(GameObject target, Vector3 force);
static bool TryAddForce(GameObject target, Vector3 force);
static bool TryStopMotion(GameObject target);
static GameObject PickAtScreenPoint(Vector2 screenPos, float maxDistance, LayerMask mask, bool is2D);
```

An object with **no collider and no body resolves to 3D**, matching pre-2D behaviour. A 2D
component on a bare GameObject needs an explicit `PhysicsMode.TwoD` override — that, not
ambiguity, is what the override exists for.

### `CharacterController2D`

`Rigidbody2D`-based, with a `Movement Style` dropdown (Platformer / Top-Down) and a custom editor
that hides the inactive section. Mirrors `CharacterControllerCC`'s public surface — same event
names, same `OnMove`/`OnJump` PlayerInput pattern, same one-line setters — so wiring transfers.

Needs a `Collider2D` and a `PlayerInput` using `EGTK_InputSystem_Actions`. **Ground Layer must be
set** or jumping never works; the custom editor warns when it is empty.

Implements `ITeleportableCharacter`, so teleporters and checkpoints can find any controller
without naming a concrete type. `CharacterControllerCC` and `CharacterControllerFP` do **not**
implement it yet — that is why 2D respawn is still broken.

### Example scenes

- `ExampleScenes/Example2D_Platformer.unity` — collectibles wired to `GameCollectionManager`
- `ExampleScenes/Example2D_ClickToToggle.unity` — click a sprite to toggle another

Art is Kenney Pixel Platformer (CC0 public domain) in `ExampleScenes/Art2D/`. Terrain tiles are
18x18 px and characters 24x24, so each folder has its own Pixels Per Unit to make every sprite
exactly one world unit. The pack readme's "24x24" describes tilesheet cells including padding.

Both scenes are generated by `Assets/ProjectAssets/Dev/SceneBuilders/` (harness only, never shipped) and rebuildable
from the **EGTK** menu. Level geometry is sprite GameObjects with `BoxCollider2D` rather than a
Tilemap, which keeps the package free of the 2D Tilemap authoring dependency.

### Tests

`Assets/ProjectAssets/Dev/Tests/` — outside the package, so never shipped. 13 EditMode + 37 PlayMode.

```bash
unity command run_tests                              # EditMode
unity command run_tests --mode playmode --async_tests # PlayMode, then poll test_status
```

**The two modes cannot run at once** — starting one aborts the other. Run them separately.

**Frame-sensitive behaviour cannot be tested from outside the editor.** Each command-server request
stalls Unity's main thread, so the next `Time.deltaTime` absorbs the delay and drains coyote/jump
buffers, and click state machines never see down-then-up on consecutive frames. Anything timing
dependent belongs in a PlayMode test.

---

## Input System

**The toolkit reads all input through the Input System. The legacy `UnityEngine.Input`
class is not used anywhere and must not be reintroduced.**

Unity 6.3 creates projects with Active Input Handling set to **Input System Package (New)**,
which disables the legacy backend. Verified across the Unity projects on this machine: every
6.3 project is New-only; only the 6.0-era one is "Both". A student's fresh project is
New-only, so any legacy `Input` call is dead code for them.

This bit hard once: seven scripts still called `UnityEngine.Input`, including `InputKeyPress`,
the toolkit's most-used component. Pressing a key did nothing in a student's project. It was
invisible here only because this project is set to "Both".

### Three ways input is read

| Mechanism | Used by | Why |
|---|---|---|
| `PlayerInput` + `EGTK_InputSystem_Actions` | Character controllers | A whole control scheme; Unity's recommended workflow |
| Inline serialized `InputAction` | `InputKeyPress`, `InputKeyCountdown` | Student picks one key per object via the binding UI |
| `EGTKInput` direct polling | Manager secondary keys (pause, store, cursor toggle) | Not input-source components; a binding UI there is noise |

**Why inline actions rather than `InputActionReference`.** The package's action asset ships
inside the package, and the recommended install is a git URL, so it lands read-only in
`Library/PackageCache`. A student cannot add an action to it. Requiring a reference would
mean creating their own asset, action map, action, control type and binding before a key
press does anything — six concepts ahead of "press E opens the door".

Unity documents Actions as the recommended workflow and direct polling as suitable for
simple single-platform cases. The split above is a deliberate reading of that for a no-code
educational toolkit, not an oversight.

### `EGTKInput`

`Runtime/Utilities/EGTKInput.cs` — internal, invisible to students. Every method is
null-safe, because `Keyboard.current` and `Mouse.current` are null when no device is
attached and reading them directly throws.

```csharp
static bool WasKeyPressedThisFrame(Key key);
static bool IsKeyHeld(Key key);
static bool WasKeyReleasedThisFrame(Key key);
static bool WasMouseButtonPressedThisFrame(int button);
static bool IsMouseButtonHeld(int button);
static Vector2 MousePosition { get; }
```

### Renaming a serialized field

**Rename the field and its custom editor in the same commit.** `FindProperty` takes a
string, so a rename that misses the editor makes `PropertyField(null)` throw and breaks that
component's whole Inspector — with no compiler error.

This has happened twice: `storeKey` → `storeInputKey` and `fallbackKey` → `fallbackInputKey`
broke `GameStoreManager`'s and `InputInteractionZone`'s Inspectors in their *default*
configurations, and neither was noticed until a manual review days later.

`CustomEditorBindingTests` now fails the test run when it happens.

---

## Testing

`Assets/ProjectAssets/Dev/Tests/` — outside the package, so never shipped.

```bash
unity command run_tests                                # EditMode
unity command run_tests --mode playmode --async_tests  # PlayMode, then poll test_status
```

**The two modes cannot run at once** — starting one aborts the other. Run them separately
and wait for `test_status` to report `completed`.

| Suite | Covers |
|---|---|
| `SceneSmokeTests` | Loads all 12 example scenes, fails on any error or exception |
| `CustomEditorBindingTests` | Every `FindProperty` matches a real serialized field |
| `EGTKPhysicsTests`, `EGTKPhysicsPickingTests` | 2D/3D detection and screen-point picking |
| `EGTKInputTests` | Input System reads via a virtual keyboard and mouse |
| `InputTriggerZoneTests`, `InputMouseInteraction2DTests` | Trigger and click behaviour, 2D and 3D |
| `CharacterController2DTests` | Movement, jumping, teleport |

**`SceneSmokeTests` is the highest-value suite.** It needs no per-scene assertions and
catches the class of bug that compiles fine and only fails when a scene runs — a null
dereference after a `RequireComponent` is removed, a validation routine that rejects a valid
setup, a missing reference. It reads its scene list from Build Settings, so a new example
scene is covered as soon as it is added there. It is also what made a 1463-file asset move
safe to attempt.

It is named to sort last on purpose: loading real scenes pairs input devices via
`PlayerInput`, which corrupts the virtual devices `InputTestFixture` creates. NUnit's
`[Order]` cannot express this — it is valid on methods only, and orders within a fixture.

**Frame-sensitive behaviour cannot be tested from outside the editor.** Each command-server
request stalls Unity's main thread, so the next `Time.deltaTime` absorbs the delay and drains
coyote and jump-buffer timers, and click state machines never see down-then-up on
consecutive frames. Anything timing dependent belongs in a PlayMode test.

---

## Known Issues

**The package depends on assets it does not ship.** A transitive GUID walk found four
references from package content into `ProjectAssets/ThirdParty/`:

| Asset | Lives in | Referenced by |
|---|---|---|
| `Armature_Arms_RGB.tif` | StarterAssets | `eventGameToolKit/Materials/alwaysOnTop.mat` |
| `UI_Icon_Jump.png` | StarterAssets | `storeExample.unity` |
| `CircleSprite.png` | Cinemachine Samples | `storeExample.unity` |
| `GlowingGold.mat` | Cinemachine Samples | `checkPointExample.unity` |

A student installing via Package Manager gets a material with a missing texture and two
example scenes with missing assets. Copying Unity's sample assets into a public repo is a
licensing grey area, so the fix is to replace these four references with assets the package
owns. This is why StarterAssets and Samples were kept rather than deleted.

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
