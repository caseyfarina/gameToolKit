# EGTK 2D Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make existing eventGameToolKit components work with sprites and 2D colliders by tweaking them in place, and add the 2D character controllers that cannot be adapted.

**Architecture:** Callback-driven scripts gain a parallel `*2D` callback — the callback that fires proves the dimension, so no detection is needed. Scripts that *initiate* a physics query call a new internal `EGTKPhysics` static helper that owns all dimension logic in one file. Two new controllers replace the `CharacterController`-based ones, which have no 2D equivalent.

**Tech Stack:** Unity 6000.3.7f1, URP 17.3.0, Input System 1.18.0, DOTween FREE, `com.unity.modules.physics2d` (already present).

**Spec:** `docs/superpowers/specs/2026-09-08-egtk-2d-support-design.md`

## Global Constraints

- **No new `package.json` dependencies.** `Assets/eventGameToolKit/package.json` must be byte-identical at the end of this plan.
- **No serialized field added, renamed, or removed on any existing component.** Existing scenes and prefabs must deserialize identically.
- **`[HelpURL]` required on every new MonoBehaviour**, after `[RequireComponent]` and before `public class`. Use `https://caseyfarina.github.io/egtk-docs/` (no 2D page exists yet).
- **`[Tooltip]` required on every new student-facing `[SerializeField]`.**
- **XML `<summary>` required on new classes and new public UnityEvents.** Optional on methods.
- **Naming**: `[Category][Purpose]` (e.g. `CharacterController2D`, `EnemyController2D`).
- **UnityEvent visibility**: public methods need 0 or 1 parameter to appear in the Inspector dropdown. Two-parameter methods are invisible even with C# defaults — add a single-parameter overload.
- **Physics API**: use `linearVelocity`, never `velocity`.
- **DOTween**: use `DOTween.To()`; never module extensions like `rigidbody.DOMove()`.
- **Tests never ship.** All test files live in `Assets/Tests/`, outside `Assets/eventGameToolKit/`.
- **Verify after every task**: `unity command recompile` then `unity command recompile_status` must report `"failed":false,"errors":[]`.
- **Do not run the robocopy sync or push** until Task 11.

---

## Task 1: 3D regression baseline

Establishes what "unchanged" means before 19 shared scripts are edited. **This task produces no code.** Skipping it means a regression found later cannot be attributed to a specific change.

**Files:**
- Create: `docs/superpowers/plans/2026-09-08-3d-baseline.md`

- [ ] **Step 1: Confirm the editor is connected**

Run: `unity status`
Expected: a row with `7800  ready  ...  6000.3.7f1`. If no row, ask the user to click into Unity once.

- [ ] **Step 2: Open the main test scene**

Open `Assets/Scenes/ballPlayer.unity` in the editor and enter Play mode.

- [ ] **Step 3: Record observed behavior**

Exercise and write down the result of each, one line per item:

1. Player moves with WASD and jumps
2. A trigger zone fires its event when the player enters
3. `PhysicsBumper` pushes the player on contact
4. Mouse interaction highlights/scales an object on hover
5. Collectibles increment `GameCollectionManager`
6. Checkpoint respawn returns the player to the spawn point

- [ ] **Step 4: Capture the console**

Run: `unity command get_console_logs`
Record the total count. Any *new* error after later tasks is a regression.

- [ ] **Step 5: Write the baseline file**

Write the six observations and the console count into `docs/superpowers/plans/2026-09-08-3d-baseline.md`. Mark any item that already fails as PRE-EXISTING so it is not mistaken for a regression later.

- [ ] **Step 6: Commit**

```bash
git add docs/superpowers/plans/2026-09-08-3d-baseline.md
git commit -m "docs: record 3D behavior baseline before 2D work"
```

---

## Task 2: Test assembly and EGTKPhysics

`EGTKPhysics` is pure static logic with no scene dependencies, so it gets real TDD. Every later task depends on it.

**Files:**
- Create: `Assets/Tests/EditMode/EventGameToolkit.Tests.asmdef`
- Create: `Assets/Tests/EditMode/EGTKPhysicsTests.cs`
- Create: `Assets/eventGameToolKit/Runtime/Utilities/EGTKPhysics.cs`

**Interfaces:**
- Consumes: nothing
- Produces:
  - `public enum PhysicsMode { Auto, TwoD, ThreeD }`
  - `public static bool EGTKPhysics.Is2D(GameObject go, PhysicsMode mode = PhysicsMode.Auto)`
  - `public static bool EGTKPhysics.TryAddImpulse(GameObject target, Vector3 force)`
  - `public static bool EGTKPhysics.TryAddForce(GameObject target, Vector3 force)`
  - `public static GameObject EGTKPhysics.PickAtScreenPoint(Vector2 screenPos, float maxDistance, LayerMask mask, bool is2D)`

- [ ] **Step 1: Create the test assembly definition**

Create `Assets/Tests/EditMode/EventGameToolkit.Tests.asmdef`:

```json
{
    "name": "EventGameToolkit.Tests",
    "rootNamespace": "",
    "references": [
        "EventGameToolkit.Runtime",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Write the failing tests**

Create `Assets/Tests/EditMode/EGTKPhysicsTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class EGTKPhysicsTests
{
    private GameObject _go;

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
    }

    [Test]
    public void Is2D_ReturnsTrue_WhenCollider2DPresent()
    {
        _go = new GameObject("test2d");
        _go.AddComponent<BoxCollider2D>();
        Assert.IsTrue(EGTKPhysics.Is2D(_go));
    }

    [Test]
    public void Is2D_ReturnsFalse_WhenOnly3DColliderPresent()
    {
        _go = new GameObject("test3d");
        _go.AddComponent<BoxCollider>();
        Assert.IsFalse(EGTKPhysics.Is2D(_go));
    }

    [Test]
    public void Is2D_ReturnsFalse_WhenNoColliderPresent()
    {
        _go = new GameObject("bare");
        Assert.IsFalse(EGTKPhysics.Is2D(_go));
    }

    [Test]
    public void Is2D_HonorsExplicitOverride()
    {
        _go = new GameObject("override");
        _go.AddComponent<BoxCollider>();
        Assert.IsTrue(EGTKPhysics.Is2D(_go, PhysicsMode.TwoD));
        Assert.IsFalse(EGTKPhysics.Is2D(_go, PhysicsMode.ThreeD));
    }

    [Test]
    public void Is2D_ResolvesTo2D_WhenBothCollidersPresent()
    {
        _go = new GameObject("ambiguous");
        _go.AddComponent<BoxCollider>();
        _go.AddComponent<BoxCollider2D>();
        Assert.IsTrue(EGTKPhysics.Is2D(_go));
    }

    [Test]
    public void Is2D_UsesRigidbody2D_WhenNoColliderPresent()
    {
        _go = new GameObject("rb2d");
        _go.AddComponent<Rigidbody2D>();
        Assert.IsTrue(EGTKPhysics.Is2D(_go));
    }

    [Test]
    public void TryAddImpulse_ReturnsTrue_ForRigidbody2D()
    {
        _go = new GameObject("body2d");
        _go.AddComponent<Rigidbody2D>();
        Assert.IsTrue(EGTKPhysics.TryAddImpulse(_go, Vector3.up));
    }

    [Test]
    public void TryAddImpulse_ReturnsTrue_ForRigidbody3D()
    {
        _go = new GameObject("body3d");
        _go.AddComponent<Rigidbody>();
        Assert.IsTrue(EGTKPhysics.TryAddImpulse(_go, Vector3.up));
    }

    [Test]
    public void TryAddImpulse_ReturnsFalse_WhenNoBody()
    {
        _go = new GameObject("nobody");
        Assert.IsFalse(EGTKPhysics.TryAddImpulse(_go, Vector3.up));
    }

    [Test]
    public void TryAddImpulse_ReturnsFalse_ForNullTarget()
    {
        Assert.IsFalse(EGTKPhysics.TryAddImpulse(null, Vector3.up));
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

```bash
unity command recompile
unity command recompile_status
```

Expected: `"failed":true` with errors naming `EGTKPhysics` as undefined. That failure is the point — it proves the tests are actually bound to the class you are about to write.

- [ ] **Step 4: Write EGTKPhysics**

Create `Assets/eventGameToolKit/Runtime/Utilities/EGTKPhysics.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// How a component decides whether it is operating in 2D or 3D.
/// </summary>
public enum PhysicsMode
{
    /// <summary>Detect from the colliders and bodies present on the object.</summary>
    Auto,
    /// <summary>Force 2D physics regardless of what is attached.</summary>
    TwoD,
    /// <summary>Force 3D physics regardless of what is attached.</summary>
    ThreeD
}

/// <summary>
/// Internal helper that owns all 2D-versus-3D physics decisions for the toolkit.
/// Students never see or use this class.
///
/// Unity's 2D and 3D physics are separate engines that never interact, so every
/// component resolves its own dimension independently. Keeping the rules here means
/// they cannot drift across the components that call them.
/// </summary>
public static class EGTKPhysics
{
    // Instance IDs already warned about, so a warning fires once per object rather
    // than every frame.
    private static readonly HashSet<int> WarnedObjects = new HashSet<int>();

    /// <summary>
    /// True when the object should use 2D physics. Honors an explicit mode override;
    /// otherwise detects from attached colliders and bodies, preferring 2D when both
    /// are present.
    /// </summary>
    public static bool Is2D(GameObject go, PhysicsMode mode = PhysicsMode.Auto)
    {
        if (mode == PhysicsMode.TwoD) return true;
        if (mode == PhysicsMode.ThreeD) return false;
        if (go == null) return false;

        bool has2D = go.GetComponent<Collider2D>() != null
                     || go.GetComponent<Rigidbody2D>() != null;
        bool has3D = go.GetComponent<Collider>() != null
                     || go.GetComponent<Rigidbody>() != null;

        if (has2D && has3D) WarnOnce(go, "has both 2D and 3D colliders. Using 2D. " +
                                         "Set Physics Mode explicitly to choose.");
        return has2D;
    }

    /// <summary>
    /// Applies an instantaneous impulse to whichever body type the target has.
    /// Returns false when the target has no Rigidbody or Rigidbody2D.
    /// </summary>
    public static bool TryAddImpulse(GameObject target, Vector3 force)
    {
        if (target == null) return false;

        Rigidbody2D body2D = target.GetComponent<Rigidbody2D>();
        if (body2D != null)
        {
            body2D.AddForce(force, ForceMode2D.Impulse);
            return true;
        }

        Rigidbody body3D = target.GetComponent<Rigidbody>();
        if (body3D != null)
        {
            body3D.AddForce(force, ForceMode.Impulse);
            return true;
        }

        WarnOnce(target, "has no Rigidbody or Rigidbody2D, so no force was applied. " +
                         "A 3D component cannot push a 2D object, or vice versa.");
        return false;
    }

    /// <summary>
    /// Applies a continuous force to whichever body type the target has.
    /// Returns false when the target has no Rigidbody or Rigidbody2D.
    /// </summary>
    public static bool TryAddForce(GameObject target, Vector3 force)
    {
        if (target == null) return false;

        Rigidbody2D body2D = target.GetComponent<Rigidbody2D>();
        if (body2D != null)
        {
            body2D.AddForce(force, ForceMode2D.Force);
            return true;
        }

        Rigidbody body3D = target.GetComponent<Rigidbody>();
        if (body3D != null)
        {
            body3D.AddForce(force, ForceMode.Force);
            return true;
        }

        WarnOnce(target, "has no Rigidbody or Rigidbody2D, so no force was applied.");
        return false;
    }

    /// <summary>
    /// Returns the GameObject under a screen point, using the matching physics engine.
    /// Returns null when nothing is hit.
    /// </summary>
    public static GameObject PickAtScreenPoint(Vector2 screenPos, float maxDistance,
                                               LayerMask mask, bool is2D)
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        if (is2D)
        {
            Vector2 worldPoint = cam.ScreenToWorldPoint(screenPos);
            Collider2D hit2D = Physics2D.OverlapPoint(worldPoint, mask);
            return hit2D != null ? hit2D.gameObject : null;
        }

        Ray ray = cam.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit, maxDistance, mask,
                               QueryTriggerInteraction.Collide)
            ? hit.collider.gameObject
            : null;
    }

    private static void WarnOnce(GameObject go, string message)
    {
        int id = go.GetInstanceID();
        if (!WarnedObjects.Add(id)) return;
        Debug.LogWarning($"[EGTK] '{go.name}' {message}", go);
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
unity command recompile
unity command recompile_status
unity command list_tests
unity command run_tests
unity command test_status
```

Expected: `recompile_status` shows `"failed":false,"errors":[]`; `list_tests` finds 10 tests; `test_status` reports all passing.

- [ ] **Step 6: Commit**

```bash
git add Assets/Tests Assets/eventGameToolKit/Runtime/Utilities/EGTKPhysics.cs
git commit -m "feat: add EGTKPhysics 2D/3D helper with EditMode tests"
```

---

## Task 3: Trigger-only zones

**Files:**
- Modify: `Assets/eventGameToolKit/Runtime/Input/InputTriggerZone.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Input/InputCheckpointZone.cs`

**Interfaces:**
- Consumes: nothing (Pattern A needs no helper)
- Produces: nothing new — public API is unchanged

- [ ] **Step 1: Convert InputTriggerZone to Component-typed handlers**

In `InputTriggerZone.cs`, change the occupant set and split each callback. `Collider` and `Collider2D` both derive from `Component`, so one set holds either:

```csharp
private readonly HashSet<Component> occupants = new HashSet<Component>();

private void OnTriggerEnter  (Collider   other) => HandleEnter(other, other.tag);
private void OnTriggerEnter2D(Collider2D other) => HandleEnter(other, other.tag);

private void HandleEnter(Component other, string otherTag)
{
    if (otherTag != triggerObjectTag) return;

    bool wasEmpty = occupants.Count == 0;
    occupants.Add(other);
    if (wasEmpty) lastStayEventTime = Time.time;
    onTriggerEnterEvent?.Invoke();
}
```

Apply the same split to `OnTriggerStay`/`OnTriggerStay2D` (delegating to `HandleStay`) and `OnTriggerExit`/`OnTriggerExit2D` (delegating to `HandleExit`). Move each original body verbatim into its handler, replacing `other.CompareTag(triggerObjectTag)` with `otherTag != triggerObjectTag` early-return.

- [ ] **Step 2: Verify IsObjectInTrigger still compiles**

`IsObjectInTrigger` reads `occupants.Count`, which is unchanged by the type widening. Confirm no call site references `HashSet<Collider>` specifically.

- [ ] **Step 3: Apply the same pattern to InputCheckpointZone**

Split its trigger callback into `OnTriggerEnter` / `OnTriggerEnter2D`, both delegating to a `HandleEnter(Component other, string otherTag)`.

- [ ] **Step 4: Verify compilation**

```bash
unity command recompile
unity command recompile_status
```

Expected: `"failed":false,"errors":[]`

- [ ] **Step 5: Verify no 3D regression**

Enter Play mode in `Assets/Scenes/ballPlayer.unity`. Confirm baseline items 2 and 6 (trigger zone fires, checkpoint respawn works) still behave as recorded in Task 1.

- [ ] **Step 6: Commit**

```bash
git add Assets/eventGameToolKit/Runtime/Input/InputTriggerZone.cs \
        Assets/eventGameToolKit/Runtime/Input/InputCheckpointZone.cs
git commit -m "feat: add 2D trigger callbacks to zone inputs"
```

---

## Task 4: Physics reactors

The five scripts that need both Pattern A (dual callbacks) and Pattern B (body-agnostic force).

**Files:**
- Modify: `Assets/eventGameToolKit/Runtime/Physics/Bumpers/PhysicsBumper.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Physics/Bumpers/PhysicsBumperTag.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Physics/PhysicsForceZone.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Utilities/InputCollisionEnter.cs`
- Modify: `Assets/eventGameToolKit/Runtime/CharacterControllers/Enemy/PhysicsEnemyController.cs`

**Interfaces:**
- Consumes: `EGTKPhysics.TryAddImpulse`, `EGTKPhysics.TryAddForce` from Task 2
- Produces: nothing new — public API is unchanged

- [ ] **Step 1: Restructure PhysicsBumper**

Extract the force logic so both callbacks feed it. The math is already dimension-neutral: `Vector3.up` is still up in a 2D platformer, and a `Vector2` normal promotes to `Vector3` implicitly.

```csharp
private void OnCollisionEnter(Collision collision)
{
    HandleCollision(collision.gameObject,
                    collision.contacts[0].normal,
                    collision.transform.position);
}

private void OnCollisionEnter2D(Collision2D collision)
{
    HandleCollision(collision.gameObject,
                    collision.GetContact(0).normal,
                    collision.transform.position);
}

private void HandleCollision(GameObject other, Vector3 contactNormal, Vector3 otherPosition)
{
    if (!other.CompareTag("Player")) return;
    if (!CanTrigger()) return;

    Vector3 bounceDirection = useCollisionNormal
        ? -contactNormal
        : (otherPosition - transform.position).normalized;

    bounceDirection += Vector3.up * upwardForceMultiplier;
    bounceDirection.Normalize();

    if (!EGTKPhysics.TryAddImpulse(other, bounceDirection * bumperForce)) return;

    Trigger();
}
```

Note the reordering: `Trigger()` now runs only after the force lands, so a bumper does not play its animation when it failed to push anything.

- [ ] **Step 2: Apply the same restructuring to PhysicsBumperTag**

Identical shape. Its tag check reads its own serialized tag field rather than the literal `"Player"` — preserve whichever field it already uses.

- [ ] **Step 3: Restructure PhysicsForceZone**

It uses `OnTriggerStay`. Add `OnTriggerStay2D(Collider2D)`, both delegating to `HandleStay(GameObject other)`, and replace its `GetComponent<Rigidbody>()` plus `AddForce` with `EGTKPhysics.TryAddForce(other, force)`. Use `TryAddForce`, not `TryAddImpulse` — this is continuous force.

- [ ] **Step 4: Restructure InputCollisionEnter**

Add the `2D` variant of whichever callbacks it defines, both delegating to a shared handler typed on `GameObject`.

- [ ] **Step 5: Restructure PhysicsEnemyController**

Add `2D` variants of its trigger/collision callbacks, and route any force application through `EGTKPhysics`. Leave its movement logic on 3D `Rigidbody` — this task does not make the enemy itself 2D-capable; `EnemyController2D` in Task 8 covers that.

- [ ] **Step 6: Verify compilation**

```bash
unity command recompile
unity command recompile_status
```

Expected: `"failed":false,"errors":[]`

- [ ] **Step 7: Verify no 3D regression**

In `ballPlayer.unity`, confirm baseline item 3 (bumper pushes the player) still works, and that the console has no new warnings.

- [ ] **Step 8: Commit**

```bash
git add Assets/eventGameToolKit/Runtime/Physics Assets/eventGameToolKit/Runtime/Utilities/InputCollisionEnter.cs \
        Assets/eventGameToolKit/Runtime/CharacterControllers/Enemy/PhysicsEnemyController.cs
git commit -m "feat: make physics reactors work with 2D bodies and colliders"
```

---

## Task 5: Rigidbody actions

Pattern B only — no callbacks to split.

**Files:**
- Modify: `Assets/eventGameToolKit/Runtime/Actions/Scene/ActionRespawnPlayer.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Actions/Spawning/ActionSpawnProjectile.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Physics/Platforms/ActionPlatformAnimator.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Physics/Platforms/PhysicsPlatformStick.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Utilities/unity_attractor_script.cs`
- Modify: `Assets/eventGameToolKit/Runtime/CharacterControllers/Player/CharacterPushRigidBody.cs`

**Interfaces:**
- Consumes: `EGTKPhysics.TryAddImpulse`, `EGTKPhysics.TryAddForce` from Task 2
- Produces: nothing new

- [ ] **Step 1: Replace body lookups in each file**

In each file, find every `GetComponent<Rigidbody>()` followed by a null check and an `AddForce`, and collapse it:

```csharp
// Before
Rigidbody rb = target.GetComponent<Rigidbody>();
if (rb == null) return;
rb.AddForce(direction * strength, ForceMode.Impulse);

// After
if (!EGTKPhysics.TryAddImpulse(target, direction * strength)) return;
```

Use `TryAddForce` where the original used `ForceMode.Force` or `ForceMode.Acceleration` (continuous), and `TryAddImpulse` where it used `ForceMode.Impulse` or `ForceMode.VelocityChange` (instantaneous).

- [ ] **Step 2: Handle velocity-zeroing sites separately**

Where a script sets `rb.linearVelocity = Vector3.zero` (respawn and teleport do this), `EGTKPhysics` has no helper. Branch inline:

```csharp
Rigidbody2D body2D = target.GetComponent<Rigidbody2D>();
if (body2D != null) body2D.linearVelocity = Vector2.zero;
else
{
    Rigidbody body3D = target.GetComponent<Rigidbody>();
    if (body3D != null) body3D.linearVelocity = Vector3.zero;
}
```

- [ ] **Step 3: Note the ObjectAttractor class name**

`unity_attractor_script.cs` contains a class named `ObjectAttractor` — the filename and class name do not match. Do not rename either; renaming breaks the GUID binding in existing scenes.

- [ ] **Step 4: Verify compilation**

```bash
unity command recompile
unity command recompile_status
```

Expected: `"failed":false,"errors":[]`

- [ ] **Step 5: Verify no 3D regression**

In `ballPlayer.unity`, confirm baseline items 1 and 6 still work — moving platforms carry the player, and respawn zeroes velocity.

- [ ] **Step 6: Commit**

```bash
git add Assets/eventGameToolKit/Runtime
git commit -m "feat: route rigidbody actions through EGTKPhysics"
```

---

## Task 6: Mouse-picking inputs

Pattern C. These are the only scripts that genuinely need `Is2D`.

**Files:**
- Modify: `Assets/eventGameToolKit/Runtime/Input/InputMouseInteraction.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Input/InputFPMouseInteraction.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Input/InputClickDrag.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Input/InputClickRotate.cs`

**Interfaces:**
- Consumes: `EGTKPhysics.Is2D`, `EGTKPhysics.PickAtScreenPoint` from Task 2
- Produces: nothing new

- [ ] **Step 1: Add the cached detection field to each script**

Add to each class (this is a private runtime field, not a `[SerializeField]`, so no scene data changes):

```csharp
private bool _is2D;

private void Awake()
{
    _is2D = EGTKPhysics.Is2D(gameObject);
}
```

If the class already has `Awake`, add the assignment to it rather than declaring a second one.

- [ ] **Step 2: Replace the raycast in InputMouseInteraction**

```csharp
// Before
bool isHit = Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance,
                             interactionLayer, QueryTriggerInteraction.Collide)
             && hit.collider.gameObject == gameObject;

// After
bool isHit = EGTKPhysics.PickAtScreenPoint(Input.mousePosition, maxRaycastDistance,
                                           interactionLayer, _is2D) == gameObject;
```

- [ ] **Step 3: Relax the collider warning**

Line ~107 warns when `GetComponent<Collider>()` is null. In 2D that fires spuriously. Change the condition to accept either:

```csharp
if (GetComponent<Collider>() == null && GetComponent<Collider2D>() == null)
{
    Debug.LogWarning($"InputMouseInteraction on {gameObject.name} requires a Collider or Collider2D!");
}
```

- [ ] **Step 4: Apply the same three changes to the other three scripts**

`InputFPMouseInteraction`, `InputClickDrag`, and `InputClickRotate` each have the same raycast-and-compare shape and the same collider warning. Repeat steps 1-3 in each. Leave the hover, scale, and highlight code untouched — `SpriteRenderer` inherits from `Renderer`, so the existing `GetComponent<Renderer>()` calls already work on sprites.

- [ ] **Step 5: Verify compilation**

```bash
unity command recompile
unity command recompile_status
```

Expected: `"failed":false,"errors":[]`

- [ ] **Step 6: Verify no 3D regression**

In `ballPlayer.unity`, confirm baseline item 4 — hovering an interactive object still highlights and scales it, and clicking still fires its event.

- [ ] **Step 7: Commit**

```bash
git add Assets/eventGameToolKit/Runtime/Input
git commit -m "feat: add 2D picking to mouse interaction inputs"
```

---

## Task 7: ITeleportableCharacter and the dual-pattern files

Two files need both Pattern A and C, or B and C. This task also removes the hard-coded controller lookup that would silently break teleport and respawn for a 2D player.

**Files:**
- Create: `Assets/eventGameToolKit/Runtime/Interfaces/ITeleportableCharacter.cs`
- Modify: `Assets/eventGameToolKit/Runtime/CharacterControllers/Player/CharacterControllerCC.cs`
- Modify: `Assets/eventGameToolKit/Runtime/CharacterControllers/Player/CharacterControllerFP.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Actions/Scene/ActionTeleportToTransform.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Game/GameCheckpointManager.cs`
- Modify: `Assets/eventGameToolKit/Runtime/Input/InputInteractionZone.cs`

**Interfaces:**
- Consumes: `EGTKPhysics.Is2D`, `EGTKPhysics.PickAtScreenPoint` from Task 2
- Produces: `public interface ITeleportableCharacter { void TeleportTo(Vector3 position); }`

- [ ] **Step 1: Create the interface**

Create `Assets/eventGameToolKit/Runtime/Interfaces/ITeleportableCharacter.cs`:

```csharp
using UnityEngine;

/// <summary>
/// A player controller that can be moved directly to a position. Teleporters and
/// checkpoints use this so they work with any controller without naming it.
/// </summary>
public interface ITeleportableCharacter
{
    /// <summary>
    /// Moves the character to a world position, bypassing normal movement.
    /// </summary>
    void TeleportTo(Vector3 position);
}
```

- [ ] **Step 2: Implement it on the existing controllers**

`CharacterControllerCC` already has `public void TeleportTo(Vector3 position)`, so only the declaration changes:

```csharp
public class CharacterControllerCC : MonoBehaviour, ITeleportableCharacter
```

Do the same for `CharacterControllerFP`. If `CharacterControllerFP` lacks a matching `TeleportTo(Vector3)`, add one that mirrors `CharacterControllerCC`'s: disable the `CharacterController`, set `transform.position`, re-enable it.

- [ ] **Step 3: Replace the hard-coded lookup in ActionTeleportToTransform**

Around line 265:

```csharp
// Before
CharacterControllerCC characterCC = target.GetComponent<CharacterControllerCC>();
if (characterCC != null) { characterCC.TeleportTo(position); return; }

// After
ITeleportableCharacter character = target.GetComponent<ITeleportableCharacter>();
if (character != null) { character.TeleportTo(position); return; }
```

Leave the Rigidbody fallback below it in place — it now serves objects that are not controllers at all.

- [ ] **Step 4: Replace the hard-coded lookup in GameCheckpointManager**

Around line 298 it does `player.GetComponent<CharacterController>()` and disables it before repositioning. Prefer the interface when present:

```csharp
ITeleportableCharacter character = player.GetComponent<ITeleportableCharacter>();
if (character != null)
{
    character.TeleportTo(checkpointPosition);
}
else
{
    CharacterController cc = player.GetComponent<CharacterController>();
    if (cc != null) cc.enabled = false;
    player.transform.position = checkpointPosition;
    if (cc != null) cc.enabled = true;
}
```

- [ ] **Step 5: Update InputInteractionZone**

It needs Pattern A (dual trigger callbacks for the proximity zone) and Pattern C (`Is2D` plus `PickAtScreenPoint` for the interact raycast). Apply both, exactly as in Tasks 3 and 6.

- [ ] **Step 6: Verify compilation**

```bash
unity command recompile
unity command recompile_status
```

Expected: `"failed":false,"errors":[]`

- [ ] **Step 7: Verify no 3D regression**

In `ballPlayer.unity`, confirm baseline item 6 (checkpoint respawn) and any interaction zone still work. This is the highest-risk task for regressions because it changes how the player is located.

- [ ] **Step 8: Commit**

```bash
git add Assets/eventGameToolKit/Runtime
git commit -m "feat: add ITeleportableCharacter so teleport and checkpoints are controller-agnostic"
```

---

## Task 8: CharacterController2D

**Files:**
- Create: `Assets/eventGameToolKit/Runtime/CharacterControllers/Player/CharacterController2D.cs`
- Create: `Assets/eventGameToolKit/Editor/CharacterControllerEditors/CharacterController2DEditor.cs`

**Interfaces:**
- Consumes: `ITeleportableCharacter` from Task 7
- Produces: `CharacterController2D` with `MovementStyle` enum, and the event names `onGrounded`, `onJump`, `onLanding`, `onStartMoving`, `onStopMoving`, `onTeleport`, `onSpawnPointUsed`

- [ ] **Step 1: Write the controller**

Create the file with this structure. Field names and event names deliberately mirror `CharacterControllerCC` so student wiring transfers.

```csharp
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 2D player controller for sprites. Choose Platformer for side-on games with
/// gravity and jumping, or Top-Down for overhead games with free movement.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class CharacterController2D : MonoBehaviour, ITeleportableCharacter
{
    public enum MovementStyle { Platformer, TopDown }

    [Header("Movement Style")]
    [Tooltip("Platformer uses gravity and jumping. Top-Down moves freely in all directions.")]
    public MovementStyle movementStyle = MovementStyle.Platformer;

    [Header("Movement Settings")]
    [Tooltip("Maximum movement speed in units per second")]
    [SerializeField] private float moveSpeed = 8f;
    [Tooltip("How quickly the character reaches full speed")]
    [SerializeField] private float acceleration = 50f;
    [Tooltip("How quickly the character slows to a stop")]
    [SerializeField] private float deceleration = 50f;

    [Header("Platformer Settings")]
    [Tooltip("Peak height of a jump, in world units")]
    [SerializeField] private float jumpHeight = 3.5f;
    [Tooltip("Seconds after leaving a ledge during which a jump still works")]
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("Seconds before landing that a jump press is remembered")]
    [SerializeField] private float jumpBufferTime = 0.1f;
    [Tooltip("Multiplies Unity's gravity. Higher values feel snappier.")]
    [SerializeField] private float gravityScale = 3f;
    [Tooltip("Fastest the character may fall, in units per second")]
    [SerializeField] private float maxFallSpeed = 20f;
    [Tooltip("Which layers count as ground")]
    [SerializeField] private LayerMask groundLayer = 1;
    [Tooltip("Size of the box used to check for ground beneath the character")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.45f, 0.1f);
    [Tooltip("How far below the character's centre to check for ground")]
    [SerializeField] private float groundCheckOffset = 0.5f;

    [Header("Top-Down Settings")]
    [Tooltip("Flip the sprite to face the direction of travel")]
    [SerializeField] private bool faceMovementDirection = true;

    [Header("Animation")]
    [Tooltip("Animator driven with Speed, Grounded and VerticalVelocity parameters")]
    [SerializeField] private Animator characterAnimator;
    [Tooltip("Flip the sprite horizontally based on movement direction")]
    [SerializeField] private bool flipSpriteOnDirection = true;

    [Header("Events")]
    /// <summary>Fires when the character lands on the ground.</summary>
    public UnityEvent onGrounded;
    /// <summary>Fires the moment a jump begins.</summary>
    public UnityEvent onJump;
    /// <summary>Fires when the character touches down after falling.</summary>
    public UnityEvent onLanding;
    /// <summary>Fires when the character starts moving from a standstill.</summary>
    public UnityEvent onStartMoving;
    /// <summary>Fires when the character comes to a stop.</summary>
    public UnityEvent onStopMoving;
    /// <summary>Fires after the character is teleported, passing the destination.</summary>
    public UnityEvent<Vector3> onTeleport;
    /// <summary>Fires when a spawn point positions the character, passing the position.</summary>
    public UnityEvent<Vector3> onSpawnPointUsed;

    private Rigidbody2D _body;
    private SpriteRenderer _sprite;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private bool _wasGrounded;
    private bool _wasMoving;
    private float _coyoteCounter;
    private float _jumpBufferCounter;

    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDVerticalVelocity;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.freezeRotation = true;
        _sprite = GetComponentInChildren<SpriteRenderer>();

        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDVerticalVelocity = Animator.StringToHash("VerticalVelocity");

        ApplyStyleToBody();
    }

    private void ApplyStyleToBody()
    {
        _body.gravityScale = movementStyle == MovementStyle.Platformer ? gravityScale : 0f;
    }

    /// <summary>Reads movement input from the Input System.</summary>
    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();

    /// <summary>Reads jump input from the Input System.</summary>
    public void OnJump(InputValue value)
    {
        if (value.isPressed) _jumpBufferCounter = jumpBufferTime;
    }

    private void Update()
    {
        if (movementStyle == MovementStyle.Platformer) UpdateGroundState();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (movementStyle == MovementStyle.Platformer) MovePlatformer();
        else MoveTopDown();
    }

    private void UpdateGroundState()
    {
        Vector2 checkCentre = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        _isGrounded = Physics2D.OverlapBox(checkCentre, groundCheckSize, 0f, groundLayer) != null;

        if (_isGrounded && !_wasGrounded)
        {
            onGrounded?.Invoke();
            onLanding?.Invoke();
        }
        _wasGrounded = _isGrounded;

        _coyoteCounter = _isGrounded ? coyoteTime : _coyoteCounter - Time.deltaTime;
        _jumpBufferCounter -= Time.deltaTime;
    }

    private void MovePlatformer()
    {
        float targetSpeed = _moveInput.x * moveSpeed;
        float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        float newX = Mathf.MoveTowards(_body.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
        float newY = Mathf.Max(_body.linearVelocity.y, -maxFallSpeed);

        if (_jumpBufferCounter > 0f && _coyoteCounter > 0f)
        {
            newY = Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(Physics2D.gravity.y) * gravityScale);
            _jumpBufferCounter = 0f;
            _coyoteCounter = 0f;
            onJump?.Invoke();
        }

        _body.linearVelocity = new Vector2(newX, newY);
        UpdateFacing(_moveInput.x);
        ReportMovementState(Mathf.Abs(newX) > 0.1f);
    }

    private void MoveTopDown()
    {
        Vector2 target = Vector2.ClampMagnitude(_moveInput, 1f) * moveSpeed;
        float rate = target.sqrMagnitude > 0.01f ? acceleration : deceleration;
        _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, target,
                                                   rate * Time.fixedDeltaTime);

        if (faceMovementDirection) UpdateFacing(_moveInput.x);
        ReportMovementState(_body.linearVelocity.sqrMagnitude > 0.01f);
    }

    private void UpdateFacing(float horizontal)
    {
        if (!flipSpriteOnDirection || _sprite == null) return;
        if (Mathf.Abs(horizontal) < 0.01f) return;
        _sprite.flipX = horizontal < 0f;
    }

    private void ReportMovementState(bool isMoving)
    {
        if (isMoving && !_wasMoving) onStartMoving?.Invoke();
        else if (!isMoving && _wasMoving) onStopMoving?.Invoke();
        _wasMoving = isMoving;
    }

    private void UpdateAnimator()
    {
        if (characterAnimator == null) return;
        characterAnimator.SetFloat(_animIDSpeed, Mathf.Abs(_body.linearVelocity.x));
        characterAnimator.SetBool(_animIDGrounded, _isGrounded);
        characterAnimator.SetFloat(_animIDVerticalVelocity, _body.linearVelocity.y);
    }

    /// <summary>Moves the character to a world position and stops its motion.</summary>
    public void TeleportTo(Vector3 position)
    {
        _body.linearVelocity = Vector2.zero;
        transform.position = position;
        onTeleport?.Invoke(position);
    }

    /// <summary>Sets the maximum movement speed.</summary>
    public void SetMoveSpeed(float newSpeed) => moveSpeed = newSpeed;

    /// <summary>Sets the jump height in world units.</summary>
    public void SetJumpHeight(float newHeight) => jumpHeight = newHeight;

    private void OnDrawGizmosSelected()
    {
        if (movementStyle != MovementStyle.Platformer) return;
        Gizmos.color = Color.green;
        Vector2 centre = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        Gizmos.DrawWireCube(centre, groundCheckSize);
    }
}
```

- [ ] **Step 2: Write the custom editor**

Create `Assets/eventGameToolKit/Editor/CharacterControllerEditors/CharacterController2DEditor.cs`. It hides whichever style section is inactive:

```csharp
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterController2D))]
public class CharacterController2DEditor : Editor
{
    private static readonly string[] PlatformerOnly =
    {
        "jumpHeight", "coyoteTime", "jumpBufferTime", "gravityScale",
        "maxFallSpeed", "groundLayer", "groundCheckSize", "groundCheckOffset"
    };

    private static readonly string[] TopDownOnly = { "faceMovementDirection" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty style = serializedObject.FindProperty("movementStyle");
        bool isPlatformer = style.enumValueIndex == (int)CharacterController2D.MovementStyle.Platformer;

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "m_Script") continue;

            if (!isPlatformer && System.Array.IndexOf(PlatformerOnly, prop.name) >= 0) continue;
            if (isPlatformer && System.Array.IndexOf(TopDownOnly, prop.name) >= 0) continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
```

- [ ] **Step 3: Verify compilation**

```bash
unity command recompile
unity command recompile_status
```

Expected: `"failed":false,"errors":[]`

- [ ] **Step 4: Verify the editor hides the right fields**

Create an empty GameObject, add `CharacterController2D`, and toggle `Movement Style`. Confirm the Platformer fields disappear in Top-Down and `Face Movement Direction` disappears in Platformer.

- [ ] **Step 5: Commit**

```bash
git add Assets/eventGameToolKit/Runtime/CharacterControllers/Player/CharacterController2D.cs \
        Assets/eventGameToolKit/Editor/CharacterControllerEditors/CharacterController2DEditor.cs
git commit -m "feat: add CharacterController2D with platformer and top-down styles"
```

---

## Task 9: EnemyController2D

**Files:**
- Create: `Assets/eventGameToolKit/Runtime/CharacterControllers/Enemy/EnemyController2D.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks
- Produces: `EnemyController2D` with events `onPlayerDetected`, `onPlayerLost`, `onReachedWaypoint`

- [ ] **Step 1: Write the controller**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 2D enemy that patrols between waypoints and chases the player when detected.
/// Platformer enemies stop at ledges instead of walking off them.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class EnemyController2D : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Speed while patrolling between waypoints")]
    [SerializeField] private float patrolSpeed = 2f;
    [Tooltip("Speed while chasing the player")]
    [SerializeField] private float chaseSpeed = 4f;
    [Tooltip("How close counts as having reached a waypoint")]
    [SerializeField] private float waypointTolerance = 0.2f;

    [Header("Patrol")]
    [Tooltip("Points to patrol between, in order. Leave empty to stand still.")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    [Header("Detection")]
    [Tooltip("How far the enemy can see the player")]
    [SerializeField] private float detectionRadius = 5f;
    [Tooltip("Which layers the player is on")]
    [SerializeField] private LayerMask playerLayer = 1;

    [Header("Ledge Check")]
    [Tooltip("Stop at ledges instead of walking off. Turn off for flying or top-down enemies.")]
    [SerializeField] private bool stopAtLedges = true;
    [Tooltip("How far ahead and below to check for ground")]
    [SerializeField] private float ledgeCheckDistance = 1f;
    [Tooltip("Which layers count as ground")]
    [SerializeField] private LayerMask groundLayer = 1;

    [Header("Events")]
    /// <summary>Fires once when the player comes into detection range.</summary>
    public UnityEvent onPlayerDetected;
    /// <summary>Fires once when the player leaves detection range.</summary>
    public UnityEvent onPlayerLost;
    /// <summary>Fires each time a patrol waypoint is reached.</summary>
    public UnityEvent onReachedWaypoint;

    private Rigidbody2D _body;
    private SpriteRenderer _sprite;
    private Transform _player;
    private int _waypointIndex;
    private bool _wasChasing;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.freezeRotation = true;
        _sprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        DetectPlayer();

        if (_player != null) Chase();
        else Patrol();
    }

    private void DetectPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);
        _player = hit != null ? hit.transform : null;

        bool isChasing = _player != null;
        if (isChasing && !_wasChasing) onPlayerDetected?.Invoke();
        else if (!isChasing && _wasChasing) onPlayerLost?.Invoke();
        _wasChasing = isChasing;
    }

    private void Chase() => MoveHorizontally(
        Mathf.Sign(_player.position.x - transform.position.x), chaseSpeed);

    private void Patrol()
    {
        if (waypoints.Count == 0)
        {
            _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
            return;
        }

        Transform target = waypoints[_waypointIndex];
        if (target == null) return;

        if (Mathf.Abs(target.position.x - transform.position.x) < waypointTolerance)
        {
            _waypointIndex = (_waypointIndex + 1) % waypoints.Count;
            onReachedWaypoint?.Invoke();
            return;
        }

        MoveHorizontally(Mathf.Sign(target.position.x - transform.position.x), patrolSpeed);
    }

    private void MoveHorizontally(float direction, float speed)
    {
        if (stopAtLedges && !HasGroundAhead(direction))
        {
            _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);
            return;
        }

        _body.linearVelocity = new Vector2(direction * speed, _body.linearVelocity.y);
        if (_sprite != null) _sprite.flipX = direction < 0f;
    }

    private bool HasGroundAhead(float direction)
    {
        Vector2 origin = (Vector2)transform.position + Vector2.right * direction * 0.5f;
        return Physics2D.Raycast(origin, Vector2.down, ledgeCheckDistance, groundLayer).collider != null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
```

- [ ] **Step 2: Verify compilation**

```bash
unity command recompile
unity command recompile_status
```

Expected: `"failed":false,"errors":[]`

- [ ] **Step 3: Commit**

```bash
git add Assets/eventGameToolKit/Runtime/CharacterControllers/Enemy/EnemyController2D.cs
git commit -m "feat: add EnemyController2D with patrol, chase and ledge detection"
```

---

## Task 10: Example scene

**Files:**
- Modify: `Packages/manifest.json`
- Create: `Assets/eventGameToolKit/ExampleScenes/Example2D_Platformer.unity`
- Create: `Assets/eventGameToolKit/ExampleScenes/Art2D/` (Kenney sprites)

- [ ] **Step 1: Add the tilemap authoring packages**

Add to `Packages/manifest.json` dependencies:

```json
"com.unity.2d.tilemap": "1.0.0",
"com.unity.2d.tilemap.extras": "6.0.1",
```

Do **not** touch `Assets/eventGameToolKit/package.json`.

- [ ] **Step 2: Download and import the Kenney art**

Download Pixel Platformer from `https://kenney.nl/assets/pixel-platformer` (CC0 public domain — redistributable). Import the tilesheet and character sprites into `Assets/eventGameToolKit/ExampleScenes/Art2D/`.

Set on each texture: `Texture Type: Sprite (2D and UI)`, `Filter Mode: Point (no filter)`, `Compression: None`, `Pixels Per Unit: 18`.

- [ ] **Step 3: Build the level geometry**

Create the scene. Add a Tilemap, paint a small platformer level from the tilesheet, and add `TilemapCollider2D` + `CompositeCollider2D` (set the Rigidbody2D to `Static` and tick `Used By Composite` on the TilemapCollider2D). Put the tilemap on a `Ground` layer.

- [ ] **Step 4: Add the player**

Create a GameObject with a `SpriteRenderer` (a Kenney character), `Rigidbody2D`, `CapsuleCollider2D`, `PlayerInput` (using `EGTK_InputSystem_Actions`), and `CharacterController2D`. Set `Movement Style: Platformer` and `Ground Layer: Ground`. Tag it `Player`.

- [ ] **Step 5: Wire the EGTK components**

- A collectible: sprite + `BoxCollider2D` (Is Trigger) + `InputTriggerZone` with `Trigger Object Tag: Player`, wired to `GameCollectionManager.Increment()` and `ActionDestroyObject`
- A `GameCollectionManager` with `Show UI` enabled
- A `PhysicsBumper` with a `BoxCollider2D`, to prove the 2D collision path
- A `SpawnPoint` and a `GameCheckpointManager`, to prove `ITeleportableCharacter`
- An `EnemyController2D` patrolling between two empty GameObjects

- [ ] **Step 6: Set the camera**

Set the Main Camera to `Projection: Orthographic`, `Size: 5`.

- [ ] **Step 7: Play-test the scene**

Enter Play mode and confirm: the player moves and jumps; the collectible fires and increments the counter; the bumper pushes the player; the enemy patrols and turns at ledges; falling and respawning returns the player to the spawn point.

- [ ] **Step 8: Check the console**

Run: `unity command get_console_logs`
Expected: no errors. An `[EGTK]` ambiguity warning means an object carries both 2D and 3D colliders — remove the 3D one.

- [ ] **Step 9: Commit**

```bash
git add Packages/manifest.json Packages/packages-lock.json Assets/eventGameToolKit/ExampleScenes
git commit -m "feat: add 2D platformer example scene with Kenney CC0 art"
```

---

## Task 11: Documentation, sync and push

**Files:**
- Modify: `CLAUDE.md`
- Modify: `.claude/docs/runtime-structure.md`
- Modify: `.claude/docs/custom-editors.md`
- Modify: `Assets/eventGameToolKit/Documentation/ComponentQuickReference.md`

- [ ] **Step 1: Update the script counts**

Re-derive from disk rather than hand-counting:

```bash
find Assets/eventGameToolKit/Runtime -name '*.cs' | wc -l
grep -rl "CustomEditor" Assets/eventGameToolKit/Editor --include=*.cs | wc -l
```

Update the per-folder tables in `CLAUDE.md` and `runtime-structure.md`. Four runtime files were added (`EGTKPhysics`, `ITeleportableCharacter`, `CharacterController2D`, `EnemyController2D`) and one editor.

- [ ] **Step 2: Add entries to runtime-structure.md**

Add `CharacterController2D` and `EnemyController2D` to the Character Controllers section, `EGTKPhysics` to Utilities marked **internal**, and `ITeleportableCharacter` to Interfaces.

- [ ] **Step 3: Add the editor to custom-editors.md**

Add a row for `CharacterController2D` / `CharacterControllerEditors/CharacterController2DEditor.cs`.

- [ ] **Step 4: Add a 2D section to ComponentQuickReference.md**

Add rows for both new controllers, and a short note that existing components work with 2D colliders automatically — the student-facing payoff of this whole plan.

- [ ] **Step 5: Update the Unity packages table in CLAUDE.md**

It still lists Input System 1.11.2, URP 17.0.3, Cinemachine 3.1.2, AI Navigation 2.0.5. Replace with the real values from `Packages/manifest.json`.

- [ ] **Step 6: Sync to the package repo**

```bash
cmd //c robocopy "F:\Unity Projects 2026\eventGameToolKit\gameToolKit\Assets\eventGameToolKit" "F:\Unity Projects 2026\eventGameToolKit\eventGameToolKit-Package" //MIR //XD .git //XF STATE.md
```

The `//XF STATE.md` is required — the package repo has its own `STATE.md` that a plain `/MIR` would delete.

- [ ] **Step 7: Confirm package.json is unchanged**

```bash
cd ../eventGameToolKit-Package && git diff --stat package.json
```

Expected: no output. Any diff violates the global constraint — investigate before continuing.

- [ ] **Step 8: Commit both repos and push together**

```bash
git add -A && git commit -m "docs: document 2D support and new controllers"
cd ../eventGameToolKit-Package && git add -A && git commit -m "feat: add 2D support and 2D controllers"
cd ../gameToolKit && git push origin main
cd ../eventGameToolKit-Package && git push origin main
```

Never push one without the other.

---

## Deferred

Recorded so they are not lost, explicitly out of scope for this plan:

- **Sprite animation action** to replace the URP decal components in 2D
- **Docs website pages** — a 2D section, a `CharacterController2D` page, and updating `[HelpURL]` from the index to those pages
- **`universal-config` version mismatch** — pinned at 17.0.3 while URP is 17.3.0
- **`com.unity.textmeshpro: 3.0.6`** in `package.json` — the pre-Unity-6 TMP package, a plausible resolve failure for students on a clean Unity 6 project
