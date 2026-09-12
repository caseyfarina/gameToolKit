---
project: gameToolKit
category: unity
status: active
updated: 2026-09-11
---

## Now

Building example scenes so every student-facing component is exercised and watched by the
scene smoke test. **71 of 73 covered** (75 scripts, two excluded by decision — see below).

Read **[.claude/docs/example-scene-workflow.md](.claude/docs/example-scene-workflow.md)**
before building another scene — it has the process and the traps, including the framing and
label-depth lessons that cost real time this session.

## Next

Two pieces of work close the gap. Neither is a new station-board scene.

1. **`PuzzleSequenceChecker` into the existing `puzzleExample.unity`** — it already has three
   `PuzzleSwitch` instances and is already in Build Settings. That scene is hand-authored
   with no builder, so this needs an *idempotent migration script*, not a regenerating
   builder, which would destroy the hand-placed content.
2. **`StopMotionPostProcess` scene using the Mixamo robots.** Decision taken: copy the robot
   and a controller into the package under `ExampleScenes/GeneratedAssets/` rather than
   referencing `ProjectAssets/` in place, which would add another missing-asset case to the
   packaging bug. Accepts redistributing Mixamo assets in a public repo.

**Deliberately unexemplified — do not "fix":** `applicationFPSLimiting` and
`lockMouseCursorToDisplay` will never get an example. Downsampling a student's whole
application and confining their cursor are both bad defaults to hand out. The coverage script
excludes them explicitly.

## Recently finished

- **Decal scene** (`Example3D_DecalAnimation`) — all four DecalAnimation components, with
  procedurally generated RGBA decal textures owned by the package.
- **3D physics scene** (`Example3D_Physics`) — ball lane and push lane, covering
  `PhysicsForceZone`, `PhysicsBallPlayerController`, `PhysicsEnemyController`,
  `CharacterPushRigidBody`.
- **Label legibility pass across every scene.** All builders now share `SceneLabel.cs`, whose
  `FontSizeFactor` is the single knob for label size. Labels are 2.5x larger, re-spaced to
  remove collisions, and coloured per backdrop.
- Coverage 63 -> 71.

## Blocked

- none

## Known issues

- **Packaging**: four assets in `ProjectAssets/ThirdParty/` are referenced by shipped package
  content, so a Package Manager install has a missing texture and two example scenes with
  missing assets. Does not affect students who receive the whole project.
  See CLAUDE.md § Known Issues. The planned robot copy is a deliberate, separate decision.
- **Scene composition is only partly reviewed.** `Example3D_PlatformPairing` and
  `Example3D_Physics` both place lanes at different depths, so the further lane's labels
  render smaller. PlatformPairing has been compensated; **`Example3D_Physics` has not** — its
  BALL LANE label and the two in-lane callouts are undersized. Fix by passing a larger `size`
  for the far-lane labels, as PlatformPairing does.
- Neither 3D scene's *gameplay* has been verified. Play mode is frozen when the editor lacks
  focus, so whether the ball rolls, the enemy chases, and the crates push is still unconfirmed
  and needs a human at the Game view.
- **Docs site** (`egtk-docs`) has no 2D content and does not mention the input change.
- `EnemyController2D` was planned and never written.
