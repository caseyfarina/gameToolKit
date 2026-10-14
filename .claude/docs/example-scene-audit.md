# Example Scene Audit — 2026-10-13

Audit of every example scene in `Assets/eventGameToolKit/ExampleScenes/` (19) and
`Samples~/ExampleScenes/` (2), and the plan for closing the gaps.

**Re-run it:** menu **EGTK > Audit Example Scenes** (`SceneBuilders/Editor/AuditExampleScenes.cs`)
writes `Temp/egtk_scene_audit.txt`. It opens each scene read-only and checks the live objects,
including prefab instances, which a text scan of the `.unity` file cannot see.

## Method

1. **In-editor audit** (`AuditExampleScenes.cs`): broken persistent UnityEvent calls (target gone
   or method missing), missing scripts, unbound `InputKeyPress`/`InputKeyCountdown`, active
   `PlayerInput` count, actions assets outside the package, legacy `StandaloneInputModule`,
   transitive asset dependencies outside the package.
2. **Visual pass**: a game-view capture of every scene, reviewed for whether a student can tell
   what the scene demonstrates.
3. **Coverage recount** using only folders that actually ship.

## Findings

### A. Broken for anyone who installs through Package Manager

| # | Problem | Where | Effect |
|---|---|---|---|
| A1 | `Prefabs/playerWithCharacterController.prefab`'s `PlayerInput` uses **`ProjectAssets/Input/InputSystem_Actions.inputactions`**, not the package's `EGTK_InputSystem_Actions` | the prefab, and through it 8 scenes: checkPoint, collection, enemyPatrol, projectile, puzzle, store, teleport, Example3D_Physics | No actions asset in a package install → **the player cannot move**. Also hits every student who drags the prefab into their own scene. The two assets are structurally identical (same maps, actions, schemes), so swapping is safe |
| A2 | `InputActionEvent` (the projectile spawner) references the project actions asset too | enemyPatrol, projectile | Shooting dead in a package install |
| A3 | Assets from outside the package | checkPoint (`Jumping.fbx`, Cinemachine `GlowingGold.mat`), collection (`hot.mat`, `particleURP.shadergraph`), store (Cinemachine `CircleSprite.png`, StarterAssets `UI_Icon_Jump.png`), plus `Materials/alwaysOnTop.mat` (StarterAssets texture) | Missing materials/sprites/animation. CLAUDE.md § Known Issues listed four of these; the full list is seven plus the actions asset |

### B. Broken in this project too

| # | Problem | Where |
|---|---|---|
| B1 | Enemy damage wiring lost: `InputTriggerZone.onTriggerEnterEvent → TakeDamage()` has **no target** on all four enemies | enemyPatrolExample |
| B2 | Same on its one enemy | projectileExample |
| B3 | Player's trigger zone calls `Increment()` with no target — and the scene has no `GameCollectionManager` | enemyPatrolExample (leftover from the prefab) |
| B4 | `physicsController` sample: 4 missing scripts (Player, both checkpoints, checkpoint manager), a call to the deleted `UIFadeImage.FadeOut`, outside-package materials | Samples~ |
| B5 | `package.json` advertises a sample of **three** scenes; there are two, one broken (B4) | package.json |

Touching an enemy in enemyPatrol/projectile does nothing. None of this logs an error, so
`SceneSmokeTests` passes.

### C. Scenes that work but do not teach

| # | Problem | Where |
|---|---|---|
| C1 | **No on-screen labels or instructions at all** — a student cannot tell what the scene shows or what to press | checkPoint, collection, enemyPatrol, firstPerson, projectile, store, teleport (puzzle has only "GLOW" and the 1-2-3 step numbers) |
| C2 | No labels | Example2D_ClickToToggle |
| C3 | Title overlapped by station sprites | Example2D_InputTest |
| C4 | Station 8 caption runs into the ground | Example2D_ActionsTest |
| C5 | Far-lane labels undersized (already in STATE) | Example3D_Physics; PlatformPairing small overall |
| C6 | Odd framing — camera clips the floor edge, player tiny | enemyPatrol, checkPoint |

### D. Housekeeping

| # | Problem |
|---|---|
| D1 | `ExampleScenes/parse_events.py` — a dev script shipped inside the package's scene folder |
| D2 | `EGTK_Documentation.unity` — an empty scene whose only job is to trigger the dormant XML documentation generator on open (it spawns ~480 objects). It is in Build Settings, so the smoke test runs it, and it ships to students as an "example" |
| D3 | Empty persistent-call slots on `InputMouseInteraction.onMouseEnter` | checkPoint |
| D4 | `Documentation/EXAMPLE_SCENES_DOCUMENTATION.md` covers 3 of 19 scenes, uses old event names, and predates the sequence checker |
| D5 | The coverage script in `example-scene-workflow.md` counts `ProjectAssets/Scenes` (never shipped) and `Samples~` (not imported). **Shipped-scene coverage is 65 of 73, not 72** |

### Coverage — what students actually receive

Not in any shipped scene: `ActionRestartScene`, `FadeInFromBlackOnRestart` (only the broken
sample), `GameAudioManager`, `GameCameraManager`, `PhysicsBumper` (only harness `ballPlayer`),
`ObjectAttractor` (only harness `physics`), `InputFPMouseInteraction` (only a prefab spawned by
pressing F in firstPerson — never loaded by the smoke test), `StopMotionPostProcess`.

### What passed

- No missing scripts in any ExampleScenes scene; no legacy `StandaloneInputModule`.
- Every scene has at most one active `PlayerInput`.
- Every bound key component has a binding (InputTest 1/2, Decal N/R, firstPerson F).
- All generated `Example2D_*` / `Example3D_*` scenes are self-contained (except
  `Example3D_Physics`'s actions asset, A1).

## Plan

Ordered by what breaks students first. Each phase ends with tests green and a sync.

### Phase 1 — Package installs work (A1–A3, B1–B3)

1. **Repoint the player prefab** and every scene override to `EGTK_InputSystem_Actions`; repoint
   the two `InputActionEvent` references to the matching action in the package asset. One
   idempotent migration script.
2. **Rewire enemy damage** in enemyPatrol and projectile to each scene's `GameHealthManager`;
   delete the dead `Increment()` call on enemyPatrol's player. Same migration style as
   `MigratePuzzleSequenceScene` — these scenes are hand-authored.
3. **Replace the outside-package assets** with package-owned ones: extend
   `GenerateExampleAssets.cs` for materials/sprites; swap `Jumping.fbx` for no animation or a
   package-owned clip.
4. **Lock it in with an EditMode test** that runs the audit's checks on every scene in Build
   Settings and fails on: missing call target/method, actions asset outside the package, any
   dependency outside `Assets/eventGameToolKit` + TextMesh Pro. This is the class of bug that
   nothing else catches.

### Phase 2 — Samples and housekeeping (B4, B5, D1–D5)

5. **Remove the `Samples~` sample** and its `package.json` entry. ExampleScenes already ship whole;
   fixing physicsController means rebuilding a scene nobody can see. (Alternative: rebuild it.)
6. Delete `parse_events.py`; remove `EGTK_Documentation.unity` from the package and Build
   Settings (the generator is dormant); clear the empty call slots.
7. Fix the coverage script to count shipped folders only; record 65 → target.
8. Replace `EXAMPLE_SCENES_DOCUMENTATION.md` with a one-table index of all scenes (what each
   shows, controls, components) — or delete it and point at the website.

### Phase 3 — Every scene teaches (C1–C6)

9. **Label the eight hand-made 3D scenes** with idempotent migrations using `SceneLabel`: a title,
   the controls, and a caption per demonstrated object. Fix framing (C6) in the same pass.
10. Fix the generated-scene layout issues (C2–C5) in their builders and rebuild.
11. Capture every scene and have a person review it — the decal scene once passed every check
    with white-on-white text.

### Phase 4 — Close the coverage gap (8 components)

12. **`StopMotionPostProcess`** — Mixamo robot scene (planned; copy assets into the package).
13. **Restart & fade**: put `ActionRestartScene` + `FadeInFromBlackOnRestart` into an existing
    scene with a death state (enemyPatrol: death → fade → restart).
14. **`PhysicsBumper` + `ObjectAttractor`** → stations in `Example3D_Physics`.
15. **`GameAudioManager` + `GameCameraManager`** → a small `Example3D_AudioCamera` scene
    (music crossfade on zone entry; camera switch on zone entry). Needs a package-owned
    Cinemachine setup and generated audio.
16. **`InputFPMouseInteraction`** → place one `clickToExplodeFPCube` in firstPersonExample at
    load, so the smoke test covers it rather than only after pressing F.

Target after Phase 4: **73 of 73** from shipped scenes, every scene labelled, no package-install
breakage, and an EditMode test that keeps it that way.

### Needs a human (cannot be verified from the CLI)

Play mode freezes without editor focus. After each phase someone should play: movement in every
player scene, enemy damage, the puzzle 1-2-3 bonus, the top-down door, and — once — a fresh
project that installs the package by git URL, which is the only real test of Phase 1.
