---
project: gameToolKit
category: unity
status: active
updated: 2026-09-11
---

## Now

Building example scenes so every student-facing component is exercised and watched by the
scene smoke test. **63 of 75 covered.**

Read **[.claude/docs/example-scene-workflow.md](.claude/docs/example-scene-workflow.md)**
before building another scene — it has the process and the traps.

## Next

Three scenes close the remaining 12 components:

1. **Decals** (3D) — ActionBlinkDecal, ActionBlinkDecalOptimized, ActionDecalSequence,
   ActionDecalSequenceLibrary
2. **3D physics** — PhysicsForceZone, PhysicsBallPlayerController, PhysicsEnemyController,
   CharacterPushRigidBody
3. **Utility and puzzle** — applicationFPSLimiting, StopMotionPostProcess,
   lockMouseCursorToDisplay, PuzzleSequenceChecker

## Recently finished

- Whole package migrated off the legacy `Input` class to the Input System, which does not
  work in a student's Unity 6.3 project. `InputKeyPress` and `InputKeyCountdown` now use
  inline Input Actions with a binding UI.
- 2D support: `CharacterController2D`, 2D picking and trigger zones, `PhysicsBumper2D`,
  `PhysicsForceZone2D`, `PhysicsPlatformStick2D`. `PhysicsBumperTag` merged into
  `PhysicsBumper` as an optional tag filter.
- Assets folder reorganised for students: `_STUDENT_WORK/`, `eventGameToolKit/`,
  `ProjectAssets/`.
- `SceneSmokeTests` and `CustomEditorBindingTests` — the two guards that make the above
  safe to keep changing.

## Blocked

- none

## Known issues

- **Packaging**: four assets in `ProjectAssets/ThirdParty/` are referenced by shipped
  package content, so a Package Manager install has a missing texture and two example
  scenes with missing assets. Does not affect students who receive the whole project.
  See CLAUDE.md § Known Issues.
- **Docs site** (`egtk-docs`) has no 2D content and does not mention the input change.
- `EnemyController2D` was planned and never written.
