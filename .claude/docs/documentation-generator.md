# Script Documentation Generator Guide

> ## ⚠️ Status: Dormant — superseded by the website
>
> **The primary student-facing documentation is now https://caseyfarina.github.io/egtk-docs/**,
> reached from the Inspector `?` button via `[HelpURL]`. See
> [CLAUDE.md § Documentation Strategy](../../CLAUDE.md) for the current policy.
>
> This generator is the only tool that ever consumed XML comments, and it is **not currently
> working**:
>
> - Its default `rootFolderPath` is `"Assets/Scripts"`, a folder that no longer exists in this
>   project. Runtime scripts live in `Assets/eventGameToolKit/Runtime/`. (`Assets/Scripts.meta`
>   survives as an orphaned meta file.)
> - The tool itself moved to `Assets/eventGameToolKit/Editor/Documentation/script_doc_generator.cs`.
> - The compliance table below (46/46) describes a codebase that had 46 scripts. There are now 76.
>
> **Before relying on anything in this document**, decide the generator's fate — repoint
> `rootFolderPath` to `Assets/eventGameToolKit/Runtime`, or retire the tool. Until then, treat this
> page as historical background on the XML conventions, not as an active requirement.
>
> **What is still required** regardless of the generator: `[HelpURL]` on every MonoBehaviour,
> `[Tooltip]` on student-facing serialized fields, and XML `<summary>` on classes and UnityEvents.

---

## Purpose

**Tool**: `Assets/eventGameToolKit/Editor/Documentation/script_doc_generator.cs`

**Access**: Tools > Script Documentation Generator

**What It Does**:
- Scans all scripts in `Assets/Scripts/` and subfolders
- Extracts class and method descriptions from XML comments
- Creates interactive UI canvas showing:
  - Scripts organized by folder category
  - Public methods (FUNCTIONS) with parameters
  - UnityEvents (EVENTS) for Inspector wiring
- Displays using TextMeshPro with color coding
- Organizes spatially in columns by category

---

## XML Documentation Requirements

**Historical requirement.** These were mandatory when the generator was the documentation pipeline.
Under the current policy, class summaries and UnityEvent summaries are kept; per-method summaries
are optional. The guidance below is still the house style for XML you *do* write.

---

### Required: Class-Level Summary

Every MonoBehaviour class needs an XML `<summary>` tag:

```csharp
/// <summary>
/// Brief description of what this component does and its purpose in the toolkit
/// </summary>
public class MyComponent : MonoBehaviour
{
    // class implementation
}
```

**Example**:
```csharp
/// <summary>
/// Detects when objects with specific tags enter a trigger zone and fires events
/// </summary>
public class InputTriggerZone : MonoBehaviour
```

---

### Required: Method-Level Summaries

Every public method (except Unity lifecycle methods) needs an XML `<summary>` tag:

```csharp
/// <summary>
/// Description of what this method does and when to call it
/// </summary>
public void MyPublicMethod()
{
    // method implementation
}
```

**Example**:
```csharp
/// <summary>
/// Sets the maximum speed for the controller
/// </summary>
public void SetMaxSpeed(float speed)
{
    maxSpeed = speed;
}

/// <summary>
/// Manually triggers the enter event (useful for testing)
/// </summary>
public void TriggerEnterEvent()
{
    onTriggerEnter?.Invoke();
}
```

---

### Required: UnityEvent Descriptions

Every public UnityEvent field needs an XML `<summary>` tag:

```csharp
/// <summary>
/// Fires when the object enters the trigger zone
/// </summary>
public UnityEvent onTriggerEnter;

/// <summary>
/// Fires when health reaches zero
/// </summary>
public UnityEvent onDeath;

/// <summary>
/// Fires when the countdown timer reaches zero, passing the final time as a float parameter
/// </summary>
public UnityEvent<float> onCountdownComplete;
```

---

## What Gets Documented

The generator automatically includes:

✅ **Public methods** with 0-4 parameters (UnityEvent compatible)
✅ **UnityEvent fields** (outputs that can be wired in Inspector)

❌ **Excludes**:
- Unity lifecycle methods (Start, Update, Awake, OnEnable, etc.)
- Property getters/setters
- Private/protected methods
- Editor scripts in Editor folders

---

## Compliance Status (historical)

**⚠️ Stale — retained for context only. This describes a 46-script codebase; there are now 76
runtime scripts, and XML coverage is no longer tracked as a percentage.**

The tracked metric today is `[HelpURL]` coverage, which is at 100% of runtime MonoBehaviours.

**As of October 2025: 46/46 scripts (100%) fully compliant**

| Category | Count | Status |
|----------|-------|--------|
| Input | 6/6 | ✅ |
| Actions | 12/12 | ✅ |
| Physics | 7/7 | ✅ |
| Game | 9/9 | ✅ |
| UI | 1/1 | ✅ |
| Puzzle | 2/2 | ✅ |
| Animation | 1/1 | ✅ |
| Root Scripts | 2/2 | ✅ |
| **Total** | **46/46** | **✅ 100%** |

---

## Example: Fully Compliant Script

```csharp
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detects when objects with specific tags enter a trigger zone and fires events
/// </summary>
public class InputTriggerZone : MonoBehaviour
{
    [SerializeField] private string targetTag = "Player";

    /// <summary>
    /// Fires when an object with the target tag enters the trigger zone
    /// </summary>
    public UnityEvent onTriggerEnter;

    /// <summary>
    /// Fires when an object with the target tag exits the trigger zone
    /// </summary>
    public UnityEvent onTriggerExit;

    /// <summary>
    /// Sets which tag to detect for trigger events
    /// </summary>
    public void SetTargetTag(string newTag)
    {
        targetTag = newTag;
    }

    /// <summary>
    /// Manually triggers the enter event (useful for testing)
    /// </summary>
    public void TriggerEnterEvent()
    {
        onTriggerEnter?.Invoke();
    }

    // Unity lifecycle methods don't need documentation
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            onTriggerEnter?.Invoke();
        }
    }
}
```

---

## Best Practices

### 1. Keep Summaries Concise

**Good**:
```csharp
/// <summary>
/// Sets the maximum speed for the controller
/// </summary>
```

**Too Verbose**:
```csharp
/// <summary>
/// This method allows you to set the maximum speed that the controller can reach when moving, and it takes a float parameter called speed which represents the new maximum speed value that you want to assign to the controller
/// </summary>
```

**Aim for**: 1-2 sentences explaining the purpose.

### 2. Focus on "What" and "When"

**Good**:
```csharp
/// <summary>
/// Manually triggers the enter event (useful for testing)
/// </summary>
```

Explains **what** it does and **when** students should use it.

**Less Helpful**:
```csharp
/// <summary>
/// Invokes the onTriggerEnter event
/// </summary>
```

Too technical, doesn't explain when to use it.

### 3. Use Student-Friendly Language

**Good**:
```csharp
/// <summary>
/// Starts the countdown timer from the specified seconds
/// </summary>
```

**Too Technical**:
```csharp
/// <summary>
/// Initializes the countdown coroutine with the specified duration parameter
/// </summary>
```

Avoid jargon like "coroutine", "parameter", "initialize" when simpler words work.

### 4. Document All Public Methods

**If it's callable from UnityEvents, it needs documentation.**

```csharp
// ✅ GOOD - All public methods documented
/// <summary>
/// Starts the timer
/// </summary>
public void StartTimer() { }

/// <summary>
/// Stops the timer
/// </summary>
public void StopTimer() { }

/// <summary>
/// Pauses the timer
/// </summary>
public void PauseTimer() { }
```

```csharp
// ❌ BAD - Missing documentation
public void StartTimer() { }
public void StopTimer() { }
public void PauseTimer() { }
```

### 5. Update Docs When Changing Functionality

Keep XML comments in sync with code changes:

```csharp
// Old
/// <summary>
/// Sets the player speed
/// </summary>
public void SetSpeed(float speed)

// Updated after adding max cap
/// <summary>
/// Sets the player speed (capped at maxSpeed)
/// </summary>
public void SetSpeed(float speed)
{
    moveSpeed = Mathf.Min(speed, maxSpeed);
}
```

---

## UnityEvent Descriptions

**Describe WHEN the event fires**:

```csharp
// ✅ GOOD - Clear trigger condition
/// <summary>
/// Fires when the player enters the trigger zone
/// </summary>
public UnityEvent onTriggerEnter;

/// <summary>
/// Fires when health reaches zero
/// </summary>
public UnityEvent onDeath;

/// <summary>
/// Fires every 10 seconds while the timer is running
/// </summary>
public UnityEvent onPeriodicTick;
```

```csharp
// ❌ BAD - Doesn't explain when
/// <summary>
/// Trigger enter event
/// </summary>
public UnityEvent onTriggerEnter;
```

**For events with parameters**:

```csharp
/// <summary>
/// Fires when the score changes, passing the new score value
/// </summary>
public UnityEvent<int> onScoreChanged;

/// <summary>
/// Fires when damage is received, passing the damage amount
/// </summary>
public UnityEvent<float> onDamageReceived;
```

---

## Running the Generator

### In Unity Editor

1. Go to **Tools > Script Documentation Generator**
2. The tool scans all scripts
3. A Canvas is created showing all documentation
4. Navigate the UI to browse scripts by category

### Output

The generator creates:
- **Canvas** with TextMeshPro UI
- **Organized columns** by folder category
- **Color-coded** functions and events
- **Parameter signatures** for methods
- **Descriptions** from XML comments

Students can reference this visual guide instead of digging through code!

---

## Troubleshooting

### Script Doesn't Appear in Generator

**Possible Causes**:
1. Script is in Editor folder (excluded)
2. Script isn't under the window's `rootFolderPath` (default `Assets/Scripts`, which no longer exists — set it to `Assets/eventGameToolKit/Runtime`)
3. Script doesn't inherit from MonoBehaviour
4. Script is private or internal

**Solution**: Ensure script is a public MonoBehaviour under `Assets/eventGameToolKit/Runtime/`, and that `rootFolderPath` points there.

### Methods Don't Show

**Possible Causes**:
1. Methods are private/protected
2. Methods have more than 4 parameters (not UnityEvent compatible)
3. Methods are properties (get/set)
4. Methods are Unity lifecycle (Start, Update, etc.)

**Solution**: Only public methods with 0-4 parameters are included.

### Missing Descriptions

**Cause**: No XML `<summary>` tags

**Solution**: Add XML documentation:
```csharp
/// <summary>
/// Description here
/// </summary>
public void MyMethod() { }
```

---

## Future Enhancements

### High-Priority

1. **Parameter Documentation Support**
   - Extract `<param>` tags
   - Display parameter descriptions inline

2. **Remarks/Usage Examples**
   - Support `<remarks>` or `<example>` tags
   - Show common setups and compatible scripts

3. **UnityEvent Descriptions** ✅ *COMPLETE*
   - XML comments above UnityEvent fields
   - Describe when they fire and what data they provide

### Medium-Priority

4. **Category/Difficulty Tags**
   - Mark script complexity (Beginner/Intermediate/Advanced)
   - Suggest compatible scripts

5. **Export to Markdown/PDF**
   - Generate student-facing documentation
   - Reference outside Unity

6. **Interactive Search/Filter**
   - UI controls to search/filter
   - Filter by category, difficulty

7. **Example Scene References**
   - Link scripts to example scenes

### Low-Priority

8. **Visual Connection Diagrams**
   - Show common script combinations
   - Visual connecting lines (e.g., InputTriggerZone → ActionSpawnObject)

9. **Tooltips in Visualization**
   - Hover tooltips with additional details

10. **Version History**
    - Track when scripts were modified
    - Log changes

---

## Summary

✅ **All scripts MUST have XML documentation**
✅ **Document class, public methods, and UnityEvents**
✅ **Keep summaries concise and student-friendly**
✅ **Focus on "what" and "when"**
✅ **Update docs when changing functionality**
✅ **Use the generator to verify compliance**

**Current Status**: Generator dormant; see the status banner at the top of this page.

Students now get accessible reference material from the website
(https://caseyfarina.github.io/egtk-docs/) via the Inspector `?` button, not from this generator.
