# eventGameToolKit — 2D Sprite and Collider Support

**Date**: 2026-09-08
**Status**: Design approved, pending implementation plan

## Goal

Make the existing eventGameToolKit components work with sprites and 2D colliders,
by tweaking the scripts that are already there rather than shipping a parallel 2D
library. Add 2D character controllers, which are the one thing that cannot be
adapted.

## Why tweaking works

An audit of all 76 runtime scripts found the 3D coupling is shallow and localized —
a callback signature, a `GetComponent<Rigidbody>()`, a `Physics.Raycast` line — not
architectural entanglement.

| Bucket | Files | Work |
|---|---:|---|
| Dimension-agnostic (no physics, colliders, transforms) | 29 | None |
| `Vector3`/`Quaternion` math only | 16 | None — `Vector3` with z=0 is valid in 2D |
| Rigidbody force/velocity | 12 | `Rigidbody2D`, `AddForce(Vector2, ForceMode2D)` |
| Mouse-picking raycasts | 5 | `Physics2D.OverlapPoint` |
| Trigger callbacks only | 2 | `OnTriggerEnter2D` |
| URP Decals | 3 | Out of scope — no 2D analog exists |
| Cinemachine | 1 | Already 2D-capable |
| CharacterController | 4 | Cannot be adapted — new scripts |

**45 of 76 scripts (59%) already work in 2D or need nothing.** The real surface is
~19 files.

## Non-goals

- **URP decal components** (`ActionBlinkDecal`, `ActionBlinkDecalOptimized`,
  `ActionDecalSequence`) stay 3D-only. Decal projectors have no 2D equivalent. A
  sprite-animation replacement is deferred to a later release.
- **`PhysicsBallPlayerController` and `PhysicsCharacterController`** stay 3D-only.
  They would adapt mechanically, but `CharacterController2D` is the 2D player
  answer and three overlapping options would confuse students.
- **Docs site and website pages** are deferred until the code settles.
- **No new `package.json` dependencies.** Student-facing package footprint is unchanged.

## Detection contract

Detection is **per-component-instance**, resolved from the object each component is
attached to — never a project or scene setting. Unity's 2D and 3D physics are
separate engines (Box2D and PhysX) that share a scene but never interact, so 2D and
3D components coexist in one scene as independent physics worlds.

### Callback-driven scripts need no detection

The callback that fires already proves the dimension. A bumper with a `BoxCollider`
only ever hears from PhysX; one with a `BoxCollider2D` only ever hears from Box2D:

```csharp
void OnCollisionEnter  (Collision   c) => Push(c.gameObject, ...);   // 3D by definition
void OnCollisionEnter2D(Collision2D c) => Push(c.gameObject, ...);   // 2D by definition
```

### Initiator scripts use the helper

Scripts that initiate a physics query have nothing to infer from, so they call
`EGTKPhysics.Is2D(gameObject, modeOverride)`, which checks for `Collider2D` /
`Rigidbody2D`, caches the result in `Awake()`, and honors an advanced-collapsed
`Physics Mode` enum (`Auto` / `2D` / `3D`).

### Rules

- **Ambiguity** (both 2D and 3D colliders present, mode `Auto`): resolve to 2D, log a
  one-time warning naming the object.
- **Cross-boundary mismatch**: warn when a callback fires but the target carries the
  opposite body type. A 3D bumper cannot push a 2D player — this is an engine limit,
  and silent no-ops are a real teaching-failure mode.
- **Backward compatibility**: no serialized field is added, renamed, or removed on any
  existing component. Existing scenes and prefabs deserialize identically, and `Auto`
  reproduces today's behavior exactly.

### Camera constraint

Sprites and meshes render together in URP, but one camera is either orthographic or
perspective. Mixing 2D and 3D gameplay in one scene generally means an orthographic
camera for both, or a second camera. This is scene setup, not a component concern.

## `EGTKPhysics` helper

Internal static class, invisible to students — same posture as `GameData`. Owns all
dimension logic in one file so the rules cannot drift across 19 call sites.

```csharp
static bool Is2D(GameObject go, PhysicsMode modeOverride);
static bool TryAddImpulse(GameObject target, Vector3 force);
static bool TryAddForce(GameObject target, Vector3 force);
static GameObject PickAtScreenPoint(Vector2 screenPos, float maxDistance,
                                    LayerMask mask, bool is2D);
```

`TryAddImpulse` / `TryAddForce` find whichever body exists, apply the matching force
call, and return `false` with the cross-boundary warning if the target has no body or
the opposite kind.

## The three tweak patterns

> **Note on counts**: the three patterns total 26 entries but touch **19 unique files** —
> some scripts need two patterns. `PhysicsBumper`, `PhysicsBumperTag`, `PhysicsForceZone`,
> `InputCollisionEnter`, and `PhysicsEnemyController` need A and B; `InputInteractionZone`
> needs A and C; `ActionTeleportToTransform` needs B and C.

### Pattern A — Callback duplication (8 files)

`InputTriggerZone`, `InputCheckpointZone`, `InputInteractionZone`, `PhysicsBumper`,
`PhysicsBumperTag`, `PhysicsForceZone`, `InputCollisionEnter`, `PhysicsEnemyController`

Body moves into a private method typed on `Component` (both `Collider` and
`Collider2D` derive from it); the callbacks become thin adapters:

```diff
-    private readonly HashSet<Collider> occupants = new HashSet<Collider>();
+    private readonly HashSet<Component> occupants = new HashSet<Component>();

-    private void OnTriggerEnter(Collider other)
+    private void OnTriggerEnter  (Collider   other) => HandleEnter(other, other.tag);
+    private void OnTriggerEnter2D(Collider2D other) => HandleEnter(other, other.tag);
+
+    private void HandleEnter(Component other, string otherTag)
     {
-        if (other.CompareTag(triggerObjectTag))
+        if (otherTag == triggerObjectTag)
```

No new fields, no mode, no helper call. Occupant bookkeeping and `IsObjectInTrigger`
keep working unchanged.

### Pattern B — Body access and force (12 files)

`ActionRespawnPlayer`, `ActionSpawnProjectile`, `PhysicsEnemyController`,
`PhysicsBumper`, `PhysicsBumperTag`, `PhysicsForceZone`, `ActionPlatformAnimator`,
`PhysicsPlatformStick`, `InputCollisionEnter`, `ObjectAttractor`,
`CharacterPushRigidBody`, `ActionTeleportToTransform`

The math is already dimension-neutral — `Vector3.up` is still up in a 2D platformer,
and a `Vector2` normal promotes to `Vector3` implicitly. Only the body handle and the
force call differ:

```diff
-        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
-        if (playerRb == null) return;
-        playerRb.AddForce(bounceDirection * bumperForce, ForceMode.Impulse);
+        if (!EGTKPhysics.TryAddImpulse(other, bounceDirection * bumperForce)) return;
```

### Pattern C — Initiators (6 files)

`InputMouseInteraction`, `InputFPMouseInteraction`, `InputClickDrag`,
`InputClickRotate`, `InputInteractionZone`, `ActionTeleportToTransform`

The only scripts that genuinely need `Is2D`:

```diff
-        bool isHit = Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance,
-                                     interactionLayer, QueryTriggerInteraction.Collide)
-                     && hit.collider.gameObject == gameObject;
+        bool isHit = EGTKPhysics.PickAtScreenPoint(
+                         Input.mousePosition, maxRaycastDistance,
+                         interactionLayer, _is2D) == gameObject;
```

Hover, scale, and highlight code is untouched — `SpriteRenderer` inherits from
`Renderer`, so the existing `GetComponent<Renderer>()` calls already work on sprites.

## New scripts

### `CharacterController2D`

`Rigidbody2D`-based (dynamic, `freezeRotation`), with a `Movement Style` dropdown
(`Platformer` / `Top-Down`). A custom editor hides the inactive section, following
the pattern already used by `GameTimerManager` and `InputInteractionZone`.

Mirrors `CharacterControllerCC`'s public surface exactly so student muscle memory and
existing tutorial wiring transfer:

- **Events**: `onGrounded`, `onJump`, `onLanding`, `onStartMoving`, `onStopMoving`,
  `onTeleport(Vector3)`, `onSpawnPointUsed(Vector3)`
- **Input**: `OnMove(InputValue)` / `OnJump(InputValue)` — the same PlayerInput
  SendMessage pattern, reusing `EGTK_InputSystem_Actions`
- **Runtime setters**: `SetMoveSpeed(float)`, `SetJumpHeight(float)` — one-line,
  UnityEvent-friendly
- **Animator**: cached parameter IDs driving `Speed`, `Grounded`, `VerticalVelocity`
- **Platformer section**: jump height, coyote time, jump buffer, gravity scale, max
  fall speed, ground check via `Physics2D.OverlapBox` on a layer
- **Top-Down section**: 8-way movement, no gravity, optional face-direction
- Sprite flip on horizontal direction change

### `EnemyController2D`

Patrol between waypoints, detect via `Physics2D.OverlapCircle`, chase, with a
ledge-check raycast so platformer enemies do not walk off edges. Events
`onPlayerDetected`, `onPlayerLost`, `onReachedWaypoint`, mirroring
`EnemyControllerCC`'s shape.

## `ITeleportableCharacter` extraction

`ActionTeleportToTransform` and `GameCheckpointManager` both hard-code
`GetComponent<CharacterControllerCC>()`. A 2D player would silently miss teleport and
respawn — the two systems students hit first.

```csharp
public interface ITeleportableCharacter { void TeleportTo(Vector3 position); }
```

Implemented by `CharacterControllerCC`, `CharacterControllerFP`, and
`CharacterController2D`. The two call sites query the interface instead of the
concrete class. Roughly 6 lines changed, follows the existing `ISpawnPointProvider`
pattern in `Interfaces/`, and means checkpoints and teleporters work in 2D with no
per-controller special-casing.

## Example scene

**Art**: Kenney Pixel Platformer (https://kenney.nl/assets/pixel-platformer) — CC0
public domain, so it ships in the package repo without restriction. 200 files at
18x18 px.

**Location**: `Assets/eventGameToolKit/ExampleScenes/` (mirrors to the package repo).

**Construction**: Tilemap for level geometry with `TilemapCollider2D` +
`CompositeCollider2D` for one-piece collision. Individual sprite GameObjects for
anything an EGTK component attaches to — player, collectibles, bumpers, trigger
zones — since those are what students wire.

**Wiring demonstrated**: `CharacterController2D` -> `InputTriggerZone` (2D) ->
`GameCollectionManager`, plus a `PhysicsBumper` proving the 2D collision path and a
checkpoint proving `ITeleportableCharacter`.

## Package changes

**Harness manifest** (`Packages/manifest.json`): add `com.unity.2d.tilemap` and
`com.unity.2d.tilemap.extras` for Tile Palette and Rule Tile authoring.

**Package manifest** (`Assets/eventGameToolKit/package.json`): **unchanged.**
`com.unity.modules.physics2d` and `com.unity.2d.sprite` are already in the project, so
`Rigidbody2D`, `Collider2D`, and `SpriteRenderer` compile today with no additions.
`com.unity.modules.tilemap` is a built-in module present in essentially every Unity
project, so the shipped scene opens without adding a student-facing dependency.

## Testing

No automated test framework is in use for runtime components, so verification is
manual plus compile checks:

1. `unity command recompile_status` — zero CS errors after each pattern is applied.
2. **3D regression**: open `Assets/Scenes/ballPlayer.unity` and confirm existing
   behavior is unchanged — trigger zones fire, bumpers push, mouse interaction picks.
   This is the critical check, since 19 shared scripts are being edited.
3. **2D verification**: the example scene exercises trigger zones, collection,
   bumper collision, mouse picking on a sprite, and checkpoint respawn.
4. **Mixed scene**: one throwaway scene with both a 3D and a 2D trigger zone,
   confirming independent resolution and that the cross-boundary warning fires.

## Open questions

None. Scope, detection strategy, controller shape, and asset licensing are settled.

## Sequencing note

The three patterns are independent of each other and of the new controllers, so they
can be implemented and verified in any order. `ITeleportableCharacter` should land
before or with `CharacterController2D`, since the controller implements it.
