---
project: gameToolKit
category: unity
status: active
updated: 2026-10-13
---

## Now

Building example scenes so every student-facing component is exercised and watched by the
scene smoke test. **72 of 73 covered** (75 scripts, two excluded by decision — see below).

Read **[.claude/docs/example-scene-workflow.md](.claude/docs/example-scene-workflow.md)**
before building another scene — it has the process and the traps, including the framing and
label-depth lessons that cost real time.

## Next

1. **`StopMotionPostProcess` scene using the Mixamo robots** — the last uncovered component.
   Decision taken: copy the robot and a controller into the package under
   `ExampleScenes/GeneratedAssets/` rather than referencing `ProjectAssets/` in place, which
   would add another missing-asset case to the packaging bug. Accepts redistributing Mixamo
   assets in a public repo.
2. **Human play-through of `puzzleExample`** — step on pads 1-2-3 in order and the bonus orb
   should appear; a second lap should still open the door. Not verifiable from the CLI.

**Deliberately unexemplified — do not "fix":** `applicationFPSLimiting` and
`lockMouseCursorToDisplay` will never get an example. Downsampling a student's whole
application and confining their cursor are both bad defaults to hand out. The coverage script
excludes them explicitly.

## Recently finished

- **`Example2D_TopDown`** (2026-10-13) — first scene using CharacterController2D's Top-Down
  style, which until now had only one PlayMode test. Coins → score, a door opened with E
  (`InputInteractionZone` → `ActionToggle.AllOff`), a gem behind it. Movement itself still
  needs a human at the Game view.
- **`PuzzleSequenceChecker` in `puzzleExample`**, via the idempotent
  `MigratePuzzleSequenceScene.cs`. Found and fixed on the way: the scene's pads called
  `NextState()`, which the checker cannot hear; they now call `Activate()`, and the checker's
  Inspector warns about it. Coverage 71 -> 72.
- **`CharacterController2D` animation and facing** (from a cloud session that pushed to the
  *package* repo's `claude/2d-controller-animator-params` branch; ported here with `git am`):
  robust Animator parameters, Visual Root for multi-sprite characters, Art Faces Left.
- **Dead controls in both 3D scenes** (2026-09-14) — one active `PlayerInput` per scene, ball
  ground-check radius fixed, guarded by `ScenePlayerInputTests`.
- CLAUDE.md corrected: sync command now excludes `STATE.md`, example-scene policy matches
  practice, package versions, script counts, 2D status table, test table.

## Blocked

- none

## Known issues

- **Packaging**: four assets in `ProjectAssets/ThirdParty/` are referenced by shipped package
  content, so a Package Manager install has a missing texture and two example scenes with
  missing assets. Does not affect students who receive the whole project.
  See CLAUDE.md § Known Issues. The planned robot copy is a deliberate, separate decision.
- **`Example3D_Physics` far-lane labels are undersized** — BALL LANE and its two callouts.
  Fix by passing a larger `size` for the far-lane labels, as PlatformPairing does.
- 3D scene *feel* (ball rolling, enemy chasing, crates pushing) still needs a human at the
  Game view; the dead-control bugs are fixed and tested, but play mode cannot be watched from
  the CLI.
- `EnemyController2D` was planned and never written.
- `ActionTeleportToTransform` with a 2D player is untested and does not clear `Rigidbody2D`
  velocity. Checkpoint respawn reaches `CharacterController2D.TeleportTo` via `SendMessage`, so it
  should work in 2D — established by reading the code, not by play-testing.
- Unmerged remote branches, left alone: `origin/feature/multi-scene-support` (harness; the old
  ScriptableObject-variable approach, superseded by `GameData`) and the package repo's
  `claude/fix-canvas-cleanup-WcjQt` (May 2026, `ActionDialogueSequence` preview-canvas
  orphaning — not yet reviewed or ported).
