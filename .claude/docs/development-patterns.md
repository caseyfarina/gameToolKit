# Development Patterns & Best Practices

Critical patterns, conventions, and best practices for developing in the eventGameToolKit.

---

## Table of Contents

1. [Spawn Point Provider Pattern](#spawn-point-provider-pattern)
2. [Critical Physics Patterns](#critical-physics-patterns)
3. [System Integration Patterns](#system-integration-patterns)
4. [Cross-System Coordination](#cross-system-coordination)
5. [Editor Scene Generator Patterns](#editor-scene-generator-patterns)
6. [Code Conventions](#code-conventions)
7. [Adding New Educational Components](#adding-new-educational-components)

---

## Spawn Point Provider Pattern

**NEW ARCHITECTURE**: Interface-based spawn system that eliminates race conditions.

### The Problem (Old Approach)

Previous checkpoint systems tried to **teleport** players AFTER scene loaded:

```csharp
// ❌ OLD WAY - Race condition!
void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    // Physics is already running
    // Player might have already fallen
    // Teleportation fights with physics → flickering/conflicts
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    player.transform.position = savedPosition;
}
```

**Problems**:
- Physics runs before teleportation
- Visual flickering as player "snaps" to checkpoint
- Race condition between spawn system and character initialization
- Requires coroutines with WaitForFixedUpdate hacks

### The Solution (ISpawnPointProvider)

Players **ask** for spawn points BEFORE physics runs:

```csharp
// ✅ NEW WAY - No race condition!
private void Awake()
{
    // Check for spawn point BEFORE Start() runs
    // Check BEFORE physics initializes
    if (useSpawnPointProviders)
    {
        CheckForSpawnPoint();
    }
}

private void CheckForSpawnPoint()
{
    // Find ANY system that implements ISpawnPointProvider
    MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

    foreach (MonoBehaviour behaviour in allBehaviours)
    {
        if (behaviour is ISpawnPointProvider provider && provider.HasSpawnPoint)
        {
            // Spawn at position BEFORE physics runs
            controller.enabled = false;
            transform.position = provider.SpawnPosition;
            transform.rotation = provider.SpawnRotation;
            controller.enabled = true;

            provider.OnSpawnPointUsed(); // Notify provider
            return;
        }
    }
}
```

**Benefits**:
- ✅ No race conditions
- ✅ No flickering or visual artifacts
- ✅ No coroutines needed
- ✅ Clean, predictable initialization order

### Interface Design

```csharp
public interface ISpawnPointProvider
{
    bool HasSpawnPoint { get; }           // Is there a spawn point available?
    Vector3 SpawnPosition { get; }        // Where should player spawn?
    Quaternion SpawnRotation { get; }     // What orientation?
    void OnSpawnPointUsed();              // Called after player spawns
}
```

**Key Design Decisions**:
1. **Read-only properties** - Providers answer questions, don't take commands
2. **HasSpawnPoint guard** - Allows providers to be "empty" (no checkpoint saved yet)
3. **OnSpawnPointUsed callback** - Providers can fire events for feedback (audio, UI, etc.)
4. **Passive data holder** - Provider stores data, doesn't actively teleport

### Implementation Example

**GameCheckpointManager** implements the interface:

```csharp
public class GameCheckpointManager : MonoBehaviour, ISpawnPointProvider
{
    private bool hasCheckpoint = false;
    private Vector3 savedPosition;
    private Quaternion savedRotation;

    // ISpawnPointProvider implementation
    public bool HasSpawnPoint => hasCheckpoint;
    public Vector3 SpawnPosition => savedPosition;
    public Quaternion SpawnRotation => savedRotation;

    public void OnSpawnPointUsed()
    {
        Debug.Log("Player spawned at checkpoint");
        onCheckpointRestored.Invoke(); // Fire events for audio/UI
        RestoreScore();  // Restore game data
        RestoreHealth();
    }

    // Public methods for saving checkpoints
    public void SaveCheckpointAtPosition(Vector3 position)
    {
        savedPosition = position;
        savedRotation = Quaternion.identity;
        hasCheckpoint = true;
        onCheckpointSaved.Invoke();
    }
}
```

### Extensibility

**Other systems can implement ISpawnPointProvider**:

```csharp
// Wave-based spawn system
public class WaveSpawnManager : MonoBehaviour, ISpawnPointProvider
{
    [SerializeField] private Transform[] spawnPoints;
    private int currentWave = 0;

    public bool HasSpawnPoint => spawnPoints.Length > 0;
    public Vector3 SpawnPosition => spawnPoints[currentWave % spawnPoints.Length].position;
    public Quaternion SpawnRotation => spawnPoints[currentWave % spawnPoints.Length].rotation;

    public void OnSpawnPointUsed()
    {
        currentWave++;
        onWaveAdvanced.Invoke(currentWave);
    }
}

// Multiplayer spawn selector
public class MultiplayerSpawnSelector : MonoBehaviour, ISpawnPointProvider
{
    [SerializeField] private Transform[] teamSpawnPoints;
    private int playerTeam;

    public bool HasSpawnPoint => teamSpawnPoints.Length > 0;
    public Vector3 SpawnPosition => teamSpawnPoints[playerTeam].position;
    public Quaternion SpawnRotation => teamSpawnPoints[playerTeam].rotation;

    public void OnSpawnPointUsed()
    {
        onPlayerSpawned.Invoke(playerTeam);
    }
}
```

### Timing Diagram

**Old System** (Race Condition):
```
Scene Load → Start() → Physics Frame 1 → OnSceneLoaded() → Teleport Player
                            ↑
                     Player has already moved/fallen!
```

**New System** (Clean Initialization):
```
Scene Load → Awake() → CheckForSpawnPoint() → Set Position → Start() → Physics
                            ↑
                    Player positioned BEFORE physics!
```

### Best Practices

**When implementing ISpawnPointProvider**:

1. **Always check HasSpawnPoint** before accessing Position/Rotation
2. **Use OnSpawnPointUsed** for feedback (events, audio, UI)
3. **Don't teleport actively** - let characters ask for spawn points
4. **Store data passively** - just answer questions, don't take action

**When using ISpawnPointProvider**:

1. **Check in Awake()** not Start() - must run before physics
2. **Disable/enable controllers** when setting position directly
3. **Only use first valid provider** - don't spawn at multiple points!
4. **Make it optional** - use a toggle (useSpawnPointProviders) for flexibility

### Legacy Support

For same-scene respawns (player death without reload):

```csharp
// GameCheckpointManager can still provide legacy teleportation
public void TeleportPlayerToCheckpoint()
{
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    player.SendMessage("TeleportTo", savedPosition, SendMessageOptions.DontRequireReceiver);
}
```

**When to use**:
- Player dies but scene doesn't reload
- Quick respawn without scene transition
- Testing/debugging

**Prefer ISpawnPointProvider** for scene reloads and initial spawns.

---

## Critical Physics Patterns

### Rigidbody Position Setting

When teleporting or repositioning objects with Rigidbody components:

**⚠️ CRITICAL: Use `rb.position` not `transform.position`**

```csharp
// ❌ WRONG - Causes physics conflicts
transform.position = newPosition;
transform.rotation = newRotation;

// ✅ CORRECT - Waits for physics system
IEnumerator TeleportPlayer()
{
    // Wait for physics to be ready
    yield return new WaitForFixedUpdate();

    // Zero out velocities first
    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;

    // Set position via Rigidbody
    rb.position = newPosition;
    rb.rotation = newRotation;
}
```

**Why**:
- `transform.position` bypasses physics system
- Causes "flashing" or "snapping back" when physics interpolates
- `WaitForFixedUpdate()` ensures physics system is ready
- Zeroing velocities prevents carryover momentum

**Example**: See `GameCheckpointManager.cs` restoration after scene reload

### Scene Persistence

Objects using `DontDestroyOnLoad` must subscribe to scene load events:

```csharp
private void Awake()
{
    DontDestroyOnLoad(gameObject);

    // Subscribe to scene load events
    SceneManager.sceneLoaded += OnSceneLoaded;
}

private void OnDestroy()
{
    // Always unsubscribe
    SceneManager.sceneLoaded -= OnSceneLoaded;
}

private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    // React to scene changes
    // Start() only runs once, but this runs every scene load
}
```

**Why**: `Start()` only runs once on initial creation, not on scene reloads.

### Visual Hiding During Teleportation

When teleporting with cameras (Cinemachine):

```csharp
IEnumerator TeleportPlayer()
{
    // Hide renderers to prevent visual flash
    foreach (Renderer r in GetComponentsInChildren<Renderer>())
    {
        r.enabled = false;
    }

    yield return new WaitForFixedUpdate();

    // Teleport
    rb.position = newPosition;
    rb.rotation = newRotation;

    yield return new WaitForFixedUpdate();

    // Show renderers again
    foreach (Renderer r in GetComponentsInChildren<Renderer>())
    {
        r.enabled = true;
    }
}
```

**Why**: Cameras need time to adjust; hiding prevents visual artifacts.

**Example**: See `GameCheckpointManager.cs`

### Ground Detection

Use `Physics.CheckSphere` instead of raycasts:

```csharp
// ✅ CORRECT - More reliable
bool isGrounded = Physics.CheckSphere(
    groundCheckPosition,
    groundCheckRadius,
    groundLayers
);

// ❌ LESS RELIABLE - Misses edges/slopes
bool isGrounded = Physics.Raycast(
    transform.position,
    Vector3.down,
    groundDistance
);
```

**Why**: CheckSphere detects ground on slopes and edges better.

---

## System Integration Patterns

### Automatic Component Discovery

Systems find and coordinate with each other automatically:

```csharp
// GameStateManager automatically finds all timers
private void Start()
{
    timerManagers = FindObjectsByType<GameTimerManager>(FindObjectsSortMode.None);
}

public void PauseGame()
{
    isPaused = true;

    // Automatically pause all discovered timers
    foreach (var timer in timerManagers)
    {
        timer.PauseTimer();
    }
}
```

**Benefits**:
- No manual wiring required
- Works with multiple instances
- Self-configuring systems

**Example**: `GameStateManager` auto-discovers `GameTimerManager` instances

### Unified Input Coordination

Clear key separation prevents conflicts:

| Key | Function | Script |
|-----|----------|--------|
| P | Pause | GameStateManager |
| ESC | Quit | InputQuitGame |
| M | Menu | (future) |
| Space | Jump | CharacterControllerCC |
| Restart | Button only | ActionRestartScene |

**Never use same key for multiple functions!**

### Event-Driven Architecture

All systems communicate through UnityEvents:

```csharp
// System A fires event
public UnityEvent onThresholdReached;

private void CheckThreshold()
{
    if (score >= targetScore)
    {
        onThresholdReached?.Invoke();
    }
}

// System B listens (wired in Inspector)
// No direct coupling between systems!
```

**Benefits**:
- Visual connections in Inspector
- No-code for students
- Decoupled systems
- Easy to modify behavior

### Modular Design

Each component works independently OR integrates seamlessly:

```csharp
// Works standalone
public class GameHealthManager : MonoBehaviour
{
    public void TakeDamage(float amount)
    {
        health -= amount;
        onDamageReceived?.Invoke(amount);

        if (health <= 0)
        {
            onDeath?.Invoke();
        }
    }
}

// But also integrates with GameStateManager via events:
// onDeath → GameStateManager.GameOver()
```

---

## Cross-System Coordination

### Timer-State Integration

```csharp
// GameTimerManager automatically pauses/resumes
public void PauseTimer()
{
    isPaused = true;
}

public void ResumeTimer()
{
    isPaused = false;
}

// GameStateManager calls these automatically
```

### Health-State Integration

```csharp
// Health manager fires death event
onDeath?.Invoke();

// Wired to GameStateManager.GameOver() in Inspector
// No code coupling!
```

### UI-Data Integration

```csharp
// GameUIManager automatically updates from managers
public void UpdateScore(int newScore)
{
    scoreText.text = $"Score: {newScore}";
    scoreText.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
}

// Called by GameCollectionManager.onScoreChanged event
```

### Audio-State Integration

```csharp
// Audio responds to game events
public void PlayVictoryMusic()
{
    CrossfadeMusic(victoryMusic, 1.0f);
}

// Triggered by GameStateManager.onVictoryAchieved
```

### Camera-Event Integration

```csharp
// Cameras switch on any event
public void SwitchToCamera(string cameraName)
{
    // Find and activate camera
    // Disable all others
}

// Can be triggered by ANY event (trigger zone, score threshold, timer, etc.)
```

---

## Editor Scene Generator Patterns

When creating editor tools to generate example scenes, follow these patterns:

### Programmatic UnityEvent Configuration

**⚠️ CRITICAL: Use SerializedProperty, not UnityEventTools**

```csharp
// ✅ CORRECT - Persists to scene
private static void AddPersistentListener(SerializedProperty unityEvent, Object target, string methodName)
{
    SerializedProperty calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls");
    int index = calls.arraySize;
    calls.InsertArrayElementAtIndex(index);

    SerializedProperty call = calls.GetArrayElementAtIndex(index);
    call.FindPropertyRelative("m_Target").objectReferenceValue = target;
    call.FindPropertyRelative("m_MethodName").stringValue = methodName;
    call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.EventDefined;
    call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
}

// ❌ WRONG - Doesn't persist
UnityEventTools.AddPersistentListener(myEvent, target.MyMethod);
```

**For methods with parameters**:

```csharp
private static void AddPersistentListener(SerializedProperty unityEvent, Object target, string methodName, bool boolValue)
{
    // ... same as above, plus:
    call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.Bool;
    call.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue = boolValue;
}
```

### UI EventSystem Requirement

**Always create EventSystem for UI buttons**:

```csharp
// Create EventSystem if it doesn't exist
if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
{
    GameObject eventSystemObj = new GameObject("EventSystem");
    eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
}
```

**Note**: Use `FindFirstObjectByType` (Unity 6+), not deprecated `FindObjectOfType`.

### Component Enable/Disable Lifecycle

**Handle playOnStart with OnEnable**:

```csharp
// ❌ PROBLEM - Only runs once
void Start()
{
    if (playOnStart) Play();
}

// ✅ SOLUTION - Runs on re-enable
private bool hasStarted = false;

void OnEnable()
{
    // Only play if Start() has run
    if (hasStarted && playOnStart)
    {
        Play();
    }
}

void Start()
{
    // Initialization
    hasStarted = true;
    if (playOnStart) Play();
}
```

**Why**: Ensures behaviors restart when GameObjects are re-enabled.

### SerializedObject Best Practices

```csharp
// Get component and create SerializedObject
var component = gameObject.AddComponent<MyComponent>();
SerializedObject so = new SerializedObject(component);

// Modify properties
SerializedProperty prop = so.FindProperty("myField");
prop.intValue = 10;

// Apply changes and mark dirty
so.ApplyModifiedProperties();
EditorUtility.SetDirty(component);

// Support undo
Undo.RegisterCreatedObjectUndo(gameObject, "Create Example");
```

### Example Generator Checklist

All example generators should:

- ✅ Use pink/blue materials from `Assets/Materials/`
- ✅ Create TMP annotations explaining the example
- ✅ Wire UnityEvents via SerializedProperty
- ✅ Create EventSystem if needed for UI
- ✅ Position camera for optimal viewing
- ✅ Use consistent naming (e.g., "ExampleName_Player")
- ✅ Add to Tools > Examples menu
- ✅ Support undo operations

---

## Code Conventions

### Naming Conventions

**Educational Scripts**: `[Category][Purpose]` format

```csharp
// Input components
InputKeyPress.cs
InputTriggerZone.cs
InputCheckpointZone.cs

// Action components
ActionSpawnObject.cs
ActionDisplayText.cs
ActionDialogueSequence.cs

// Physics components
PhysicsBumper.cs
PhysicsPlatformAnimator.cs

// Game managers
GameHealthManager.cs
GameStateManager.cs
```

**Categories**: Input, Action, Physics, Game, UI, Puzzle, Animation

### Physics API

**Use Unity 6 physics API**:

```csharp
// ✅ NEW API (Unity 6)
rb.linearVelocity
rb.angularVelocity

// ❌ OLD API (deprecated)
rb.velocity
rb.angularVelocity // (same but prefer linear/angular consistency)
```

### Input System

**Mixed approaches** (both are acceptable):

```csharp
// New Input System (preferred for new scripts)
private void OnMove(InputValue value)
{
    moveInput = value.Get<Vector2>();
}

// Legacy Input (acceptable for simple cases)
if (Input.GetKeyDown(KeyCode.Space))
{
    Jump();
}
```

### UI Text

**Always use TextMeshPro**:

```csharp
using TMPro;

[SerializeField] private TextMeshProUGUI scoreText;

scoreText.text = $"Score: {score}";
```

### UnityEvents

**Heavy use for designer connections**:

```csharp
[Header("Events")]
public UnityEvent onTriggered;
public UnityEvent<int> onScoreChanged;
public UnityEvent<float> onDamageReceived;

// Invoke
onTriggered?.Invoke();
onScoreChanged?.Invoke(newScore);
```

### Editor Tools

**Advanced scripts include gizmos**:

```csharp
private void OnDrawGizmos()
{
    if (showGizmos)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckPosition, groundCheckRadius);
    }
}
```

---

## Adding New Educational Components

### Checklist for New Scripts

When creating new scripts for student use:

#### 1. **Expose Parameters**
```csharp
[Header("Settings")]
[Tooltip("Maximum movement speed")]
[SerializeField] private float maxSpeed = 10f;
```

#### 2. **Include UnityEvents**
```csharp
[Header("Events")]
public UnityEvent onActivated;
public UnityEvent onDeactivated;
```

#### 3. **Add Tooltips**
```csharp
[Tooltip("Speed at which the platform moves between waypoints")]
[SerializeField] private float moveSpeed = 5f;
```

#### 4. **Use Headers**
```csharp
[Header("Movement Settings")]
[SerializeField] private float speed;

[Header("Jump Settings")]
[SerializeField] private float jumpHeight;
```

#### 5. **Add Gizmos (if spatial)**
```csharp
private void OnDrawGizmos()
{
    Gizmos.color = Color.cyan;
    Gizmos.DrawWireCube(transform.position, boxSize);
}
```

#### 6. **REQUIRED: XML Documentation**
```csharp
/// <summary>
/// Moves the platform along a series of waypoints in a loop
/// </summary>
public class ActionPlatformAnimator : MonoBehaviour
{
    /// <summary>
    /// Fires when the platform reaches a waypoint
    /// </summary>
    public UnityEvent onWaypointReached;

    /// <summary>
    /// Sets the movement speed of the platform
    /// </summary>
    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }
}
```

See [Documentation Generator Guide](documentation-generator.md) for complete XML requirements.

#### 7. **Follow Naming Convention**
- `Input*` for event sources
- `Action*` for event targets
- `Physics*` for physics-based systems
- `Game*` for managers

#### 8. **Material Instances**
```csharp
// ✅ CORRECT - Create instance
private Material materialInstance;

void Start()
{
    materialInstance = new Material(renderer.sharedMaterial);
    renderer.material = materialInstance;
}

void OnDestroy()
{
    Destroy(materialInstance);
}

// ❌ WRONG - Modifies shared asset
renderer.material.color = Color.red;
```

### Component Design Principles

**1. Single Responsibility**
- Each script does ONE thing well
- Don't combine unrelated features

**2. Inspector-First**
- All configuration via Inspector
- No hardcoded magic numbers

**3. Event-Driven**
- Use UnityEvents for communication
- Avoid GetComponent dependencies

**4. Student-Friendly**
- Clear parameter names
- Helpful tooltips
- Good default values

**5. Reusable**
- Works in multiple scenarios
- No scene-specific dependencies

---

## Platform Movement Patterns

**Use physics forces, not transform manipulation**:

```csharp
// ✅ CORRECT - Physics-based
rb.MovePosition(rb.position + movement);

// ❌ WRONG - Breaks physics interaction
transform.position += movement;
```

**Why**: Direct transform manipulation bypasses physics system and won't move attached players correctly.

---

## Spawning Patterns

**Support both random and manual triggering**:

```csharp
[Header("Spawn Settings")]
[SerializeField] private GameObject[] prefabs;
[SerializeField] private bool randomSelection = true;

public void Spawn()
{
    GameObject prefab = randomSelection
        ? prefabs[Random.Range(0, prefabs.Length)]
        : prefabs[0];

    Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
}
```

---

## DOTween Usage

**All animations use DOTween FREE**:

```csharp
// ✅ CORRECT - DOTween FREE compatible
DOTween.To(
    () => image.color,
    x => image.color = x,
    targetColor,
    duration
);

// ❌ WRONG - Requires DOTween Pro
image.DOFade(0f, duration);
```

**Why**: Students don't need to purchase DOTween Pro.

See [Changelog](changelog.md) for DOTween FREE refactoring details.

---

## Summary

✅ **Physics**: Use rb.position, WaitForFixedUpdate, zero velocities
✅ **Events**: Use UnityEvents for all student-facing communication
✅ **Naming**: Follow [Category][Purpose] convention
✅ **XML Docs**: Document all public methods and events
✅ **Materials**: Always create instances, never modify shared
✅ **Input**: Both Input System and legacy are acceptable
✅ **UI**: Always use TextMeshPro
✅ **DOTween**: Use FREE-compatible methods only
✅ **Editors**: Update both MonoBehaviour and Editor scripts

Following these patterns ensures consistency, maintainability, and student success!
