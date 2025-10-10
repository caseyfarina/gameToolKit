# Unity Interactive Toolkit - Student Reference Guide

**For Animation and Interactivity Class**

This guide provides comprehensive information about the Unity educational toolkit for this course. Use this as context when asking Claude AI questions about the codebase.

---

## Quick Start: How to Use This Guide

1. **Copy this entire document** (Ctrl+A, Ctrl+C)
2. **Start a new conversation with Claude** at claude.ai
3. **Paste this document** into your first message
4. **Add your question** after the document

Example:
```
[Paste entire CLAUDE_STUDENT.md here]

My question: How do I make an object spawn when the player enters a trigger zone?
```

---

## Project Overview

This Unity 3D project provides a **no-code interactive toolkit** for creating games and experiences. The core philosophy is **UnityEvent-driven design** - you connect components visually in the Unity Inspector without writing any code.

### Core Concept: Event-Driven Architecture

- **Input Components** (Event Sources) = Things that detect something happening
- **Action Components** (Event Targets) = Things that do something when triggered
- **UnityEvents** = The visual connections between them in the Inspector

**Example Flow:**
```
InputTriggerZone (detects player)
  → onTriggerEnterEvent
    → ActionSpawnObject.spawnSinglePrefab() (spawns enemy)
```

---

## Project Structure

```
Assets/
├── Scenes/
│   └── ballPlayer.unity         (Main scene)
├── Scripts/
│   ├── Input/                   (Event sources - detect things)
│   ├── Actions/                 (Event targets - do things)
│   ├── Physics/                 (Movement & physics systems)
│   ├── Game/                    (Score, health, inventory)
│   └── UI/                      (User interface - future)
├── InputSystem_Actions.inputactions  (Input bindings)
└── Prefabs/                     (Reusable objects)
```

---

## Available Components Library

### 📥 INPUT COMPONENTS (Event Sources)

These detect things happening and trigger UnityEvents.

#### **InputKeyPress.cs**
Detects when a key is pressed.

**Location:** `Assets/Scripts/Input/InputKeyPress.cs`

**Key Features:**
- Simple key detection
- Single UnityEvent on press

**Inspector Settings:**
```
thisKey = KeyCode.Space            // Which key to detect
onPressEvent                       // Event fired when key pressed
```

**Example Code:**
```csharp
public class InputKeyPress : MonoBehaviour
{
    public KeyCode thisKey = KeyCode.Space;
    public UnityEvent onPressEvent;

    void Update()
    {
        if (Input.GetKeyDown(thisKey))
        {
            onPressEvent?.Invoke();
        }
    }
}
```

**Common Uses:**
- Debug testing ("Press Space to spawn enemy")
- Simple interactions ("Press E to open door")
- Game mechanics ("Press Space to jump")

---

#### **InputTriggerZone.cs**
Detects when objects with specific tags enter/stay/exit a 3D trigger collider.

**Location:** `Assets/Scripts/Input/InputTriggerZone.cs`

**Key Features:**
- Tag-based detection (e.g., only detect "Player")
- Three separate events: Enter, Stay, Exit
- Configurable continuous "stay" events for damage over time
- Perfect for damage zones, collection areas, and state triggers

**Inspector Settings:**
```
triggerObjectTag = "Player"        // What tag to detect
enableStayEvent = false            // Enable continuous stay events?
stayInterval = 1f                  // How often to fire stay event (seconds)
onTriggerEnterEvent               // Fired when object enters
onTriggerStayEvent                // Fired repeatedly while inside (if enabled)
onTriggerExitEvent                // Fired when object leaves
```

**Enhanced Stay Events:**
The stay event system allows for continuous effects:
- Set `enableStayEvent = true`
- Set `stayInterval = 0.5f` for damage every half second
- Connect `onTriggerStayEvent` to damage or other continuous effects

**Example Code:**
```csharp
public class InputTriggerZone : MonoBehaviour
{
    public string triggerObjectTag = "Player";
    public UnityEvent onTriggerEnterEvent;
    public UnityEvent onTriggerExitEvent;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(triggerObjectTag))
        {
            onTriggerEnterEvent?.Invoke();
        }
    }
}
```

**Setup Requirements:**
- GameObject must have a Collider component
- Collider must have "Is Trigger" checked
- Target object must have matching tag

**Common Uses:**
- Spawn enemies when player enters area
- Display text when entering zone
- Activate checkpoint zones
- Change music in different areas

---

#### **InputKeyCountdown.cs**
Counts down key presses with UI display.

**Location:** `Assets/Scripts/Input/InputKeyCountdown.cs`

**Key Features:**
- Decrements counter on each key press
- Updates TextMeshPro UI automatically
- Events for each press AND when limit reached

**Inspector Settings:**
```
thisKey = KeyCode.Space            // Key to press
countDownValue = 10                // Starting count
onCountDownKey                     // Event on each press
onCountLimitKey                    // Event when reaching zero
countDownnNumberText              // TextMeshPro UI reference
```

**Common Uses:**
- Limited ability uses ("3 bombs remaining")
- Countdown challenges
- Resource tracking

---

#### **InputQuitGame.cs**
Quits application when Escape pressed.

**Location:** `Assets/Scripts/Input/InputQuitGame.cs`

**Common Uses:**
- Easy exit during testing
- Final build quit functionality

---

### 🎬 ACTION COMPONENTS (Event Targets)

These do things when their methods are called by UnityEvents.

#### **ActionSpawnObject.cs**
Spawns a single object at this GameObject's position.

**Location:** `Assets/Scripts/Actions/ActionSpawnObject.cs`

**Key Features:**
- Spawns one prefab instance when called
- Uses transform position/rotation of host object

**Inspector Settings:**
```
prefabToSpawn                      // The prefab to instantiate
```

**Public Methods (for UnityEvents):**
```csharp
public void spawnSinglePrefab()    // Call this from UnityEvents
```

**Example Code:**
```csharp
public class ActionSpawnObject : MonoBehaviour
{
    public GameObject prefabToSpawn;

    public void spawnSinglePrefab()
    {
        if(prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, transform.position, transform.rotation);
        }
    }
}
```

**Common Uses:**
- Spawn enemy when player enters trigger
- Create pickup on button press
- Instantiate projectiles

**Setup Pattern:**
```
1. Add ActionSpawnObject to empty GameObject
2. Position GameObject where you want spawn point
3. Drag prefab into prefabToSpawn field
4. Connect another component's UnityEvent to spawnSinglePrefab()
```

---

#### **ActionAutoSpawner.cs**
Automatically spawns objects at random intervals.

**Location:** `Assets/Scripts/Actions/ActionAutoSpawner.cs`

**Key Features:**
- Automatic spawning with random timing
- Multiple prefab support (picks randomly)
- Optional position randomization
- Adjustable spawn rates via public methods

**Inspector Settings:**
```
spawnThings[]                      // Array of prefabs to spawn
spawnRateMin = 1f                  // Minimum seconds between spawns
spawnRateMax = 5f                  // Maximum seconds between spawns
spawnPositionRange = 0f            // Random position offset radius
```

**Public Methods:**
```csharp
public void setSpawnTimeMinimum(float _spawnTimeMinimum)
public void setSpawnTimeMaximum(float _spawnTimeMaximum)
```

**How It Works:**
```csharp
void spawnObject()
{
    // Pick random prefab from array
    int spawnNumber = (int)Mathf.Floor(Random.Range(0, spawnThings.Length));

    // Add random position if range > 0
    if(spawnPositionRange > 0)
    {
        Instantiate(
            spawnThings[spawnNumber],
            transform.position + (Random.insideUnitSphere * spawnPositionRange),
            transform.rotation);
    }

    // Schedule next spawn
    nextSpawnTime = Random.Range(spawnRateMin, spawnRateMax) + time;
}
```

**Common Uses:**
- Endless enemy spawners
- Falling object generators
- Pickup generators
- Environmental effects (leaves, particles as objects)

---

#### **ActionDisplayText.cs**
Displays text on screen with fade in/out effects.

**Location:** `Assets/Scripts/Actions/ActionDisplayText.cs`

**Key Features:**
- Automatic fade in/out (optional)
- Configurable display duration
- Custom font support
- Call from UnityEvents with string parameter

**Inspector Settings:**
```
timeOnScreen = 3f                  // How long text stays visible
font                               // Optional custom TMP font
useFading = true                   // Fade in/out vs instant
fadeDuration = 0.5f                // Fade animation time
```

**Public Methods:**
```csharp
public void DisplayText(string message)                    // Basic display
public void DisplayText(string message, float customDuration)  // Custom time
public void HideText()                                     // Immediately hide
public void SetDisplayDuration(float newDuration)          // Change duration
```

**Setup Requirements:**
- GameObject must have TextMeshProUGUI component

**Common Uses:**
- Display "Checkpoint Reached" when entering trigger
- Show instructions
- Popup notifications

**Connection Pattern:**
```
InputTriggerZone.onTriggerEnterEvent
  → ActionDisplayText.DisplayText(string)
     → Type: "Welcome to Area 2!"
```

---

#### **ActionRestartScene.cs**
Restarts current scene via public method call (no automatic key input).

**Location:** `Assets/Scripts/Actions/ActionRestartScene.cs`

**Key Features:**
- Immediate scene restart via method call
- Works with current active scene
- Resets all GameObjects to initial state
- Must be triggered by UnityEvents or UI buttons

**Public Methods:**
```csharp
public void RestartScene()        // Restart current scene
```

**Setup Pattern:**
- Add component to any GameObject
- Connect UI Button.onClick → ActionRestartScene.RestartScene()
- Or connect from other UnityEvents (health death, game over, etc.)

**Common Uses:**
- UI restart button in pause menu
- Death restart functionality
- Game over restart button
- Connected to other game events

---

#### **ActionDisplayImage.cs**
UI image display with fade effects and customizable settings.

**Location:** `Assets/Scripts/Actions/ActionDisplayImage.cs`

**Key Features:**
- Show/hide UI images with smooth fading
- Configurable fade duration and delay
- Event-driven image switching
- Perfect for tutorials, notifications, and UI feedback

**Inspector Settings:**
```
targetImage                       // UI Image component to control
fadeDuration = 0.5f              // How long fade takes
displayDuration = 3f             // How long to show image
autoHide = true                  // Automatically hide after delay
```

**Public Methods:**
```csharp
public void ShowImage()           // Show with fade in
public void HideImage()           // Hide with fade out
public void ToggleImage()         // Show if hidden, hide if shown
public void ShowImageFor(float duration)  // Show for specific time
```

**UnityEvents:**
```
onImageShown                     // When image becomes visible
onImageHidden                    // When image becomes hidden
```

**Setup Requirements:**
- GameObject must have UI Image component
- Image should start with alpha = 0 for fade effects

**Common Uses:**
- Tutorial overlays
- Achievement notifications
- Warning indicators
- UI state feedback

---

### ⚙️ PHYSICS COMPONENTS

#### **PhysicsPlayerController.cs**
Ball-based player movement controller.

**Location:** `Assets/Scripts/Physics/PhysicsPlayerController.cs`

**Key Features:**
- Camera-relative movement
- Rigidbody force-based physics
- Grounded detection
- Jump mechanics

**Common Pattern:**
- Attach to sphere GameObject with Rigidbody
- Uses Unity's new Input System

---

#### **PhysicsCharacterController.cs**
Rigidbody-based character controller for humanoid characters.

**Location:** `Assets/Scripts/Physics/PhysicsCharacterController.cs`

**Key Features:**
- Capsule collider-based movement
- Ground detection and slope handling
- Animation integration (auto-finds child Animator)
- Jump mechanics with physics constraints
- Automatic Rigidbody constraint setup

**Inspector Settings:**
```
moveSpeed = 5f                     // Movement speed
jumpForce = 10f                    // Jump force magnitude
groundCheckRadius = 0.3f           // Ground detection size
slopeCheckDistance = 1f            // How far to check for slopes
maxSlopeAngle = 45f                // Maximum walkable slope
```

**UnityEvents:**
```
onGrounded                         // When landing on ground
onJump                            // When jumping
onSteepSlope                      // When hitting too-steep slope
```

**Setup Pattern:**
- Attach to capsule GameObject
- Script automatically adds Rigidbody and CapsuleCollider
- Constrains rotation on X/Z axes for character movement
- Put Animator on child GameObject for animations

---

#### **PhysicsEnemyController.cs**
AI enemy controller with player detection and multiple jump modes.

**Location:** `Assets/Scripts/Physics/PhysicsEnemyController.cs`

**Key Features:**
- Player detection within configurable radius
- Chase behavior with rotation toward movement
- Four jump modes: None, Random Interval, Collision-based, Combined
- Automatic component setup and constraints

**Inspector Settings:**
```
playerTag = "Player"               // Tag to chase
detectionRadius = 10f              // How far to detect player
moveForce = 200f                   // Movement force strength
jumpForce = 5f                     // Jump force magnitude
jumpMode = EnemyJumpMode           // Jump behavior type
randomJumpMinTime = 2f             // Random jump timing (min)
randomJumpMaxTime = 5f             // Random jump timing (max)
```

**Jump Modes:**
- **NoJumping**: Enemy never jumps
- **RandomIntervalJump**: Jumps randomly while chasing
- **CollisionJump**: Jumps when hitting obstacles
- **CombinedJump**: Both random and collision jumping

**Setup Pattern:**
- Attach to capsule GameObject
- Script auto-configures Rigidbody and CapsuleCollider
- Tag your player GameObject as "Player"
- Choose appropriate jump mode for behavior

---

#### **PhysicsBumper.cs**
Advanced bumper system with visual effects.

**Location:** `Assets/Scripts/Physics/PhysicsBumper.cs`

**Key Features:**
- Applies force to player on collision
- Scale animation with custom curves
- Material emission effects
- Cooldown system
- Comprehensive debug gizmos

**Inspector Settings:**
```
bumperForce = 20f                  // Force magnitude
useCollisionNormal = true          // Direction mode
upwardForceMultiplier = 0.5f       // Extra upward boost
cooldownDuration = 0.5f            // Time between activations
animationCurve                     // Scale animation shape
scaleMultiplier = (1.5, 1.5, 1.5) // Per-axis scale amounts
animationDuration = 0.5f           // Animation time
useEmissionAnimation = false       // Material glow effect
```

**UnityEvents:**
```
onBumperTriggered                  // When bumper activates
onBumperCooldownComplete          // When ready again
```

**Public Methods:**
```csharp
public void Trigger()              // Manually trigger bumper
public void ResetCooldown()        // Allow immediate re-trigger
public void SetCooldownDuration(float duration)  // Change cooldown
public bool CanTrigger()           // Check if ready
public float GetRemainingCooldown() // Time until ready
```

**Example Code (Simplified):**
```csharp
private void OnCollisionEnter(Collision collision)
{
    if (!collision.gameObject.CompareTag("Player")) return;
    if (!CanTrigger()) return;

    Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();

    Vector3 bounceDirection = useCollisionNormal
        ? -collision.contacts[0].normal
        : (collision.transform.position - transform.position).normalized;

    bounceDirection += Vector3.up * upwardForceMultiplier;
    bounceDirection.Normalize();

    playerRb.AddForce(bounceDirection * bumperForce, ForceMode.Impulse);
    Trigger();
    onBumperTriggered?.Invoke();
}
```

**Common Uses:**
- Jump pads
- Bounce platforms
- Repulsion fields
- Interactive obstacles

**Advanced Feature - Material Animation:**
The bumper can animate shader properties:
- For URP: Set `emissionPropertyName = "_EmissionColor"`
- For custom shaders: Use your shader's property name
- Automatically detects float vs color properties

---

#### **PhysicsPlatformStick.cs**
Makes objects stick to moving platforms using physics.

**Location:** `Assets/Scripts/Physics/PhysicsPlatformStick.cs`

**Common Uses:**
- Moving platform player attachment
- Conveyor belt effects

---

### 🎮 GAME MANAGEMENT COMPONENTS

#### **GameCollectionManager.cs**
Score/collection tracking with UI and threshold events.

**Location:** `Assets/Scripts/Game/GameCollectionManager.cs`

**Key Features:**
- Count tracking with UI display
- Threshold detection
- Increment/decrement methods
- TextMeshPro integration

**Inspector Settings:**
```
currentValue = 0                   // Starting count
threshold = 10                     // Goal/trigger amount
displayText                        // TextMeshPro UI reference
```

**UnityEvents:**
```
onThresholdReached                 // When count >= threshold
onValueChanged                     // Every time count changes
```

**Public Methods:**
```csharp
public void Increment(int amount = 1)  // Add to counter
public void Decrement(int amount = 1)  // Subtract from counter
```

**Example Code:**
```csharp
public void Increment(int amount = 1)
{
    currentValue += amount;
    UpdateDisplay();
    CheckThreshold();
}

private void CheckThreshold()
{
    if (currentValue >= threshold)
    {
        onThresholdReached.Invoke();
    }
}
```

**Common Uses:**
- Coin collection ("Collect 10 coins to unlock door")
- Enemy kill counter
- Checkpoint progress
- Achievement tracking

**Connection Pattern:**
```
Coin prefab → OnDestroy script
  → GameCollectionManager.Increment(1)

GameCollectionManager.onThresholdReached
  → Door.Open()
```

---

#### **GameHealthManager.cs**
Comprehensive health system with multiple states and events.

**Location:** `Assets/Scripts/Game/GameHealthManager.cs`

**Key Features:**
- Max health tracking
- Low health threshold detection
- Death/revival system
- UI display integration
- Damage over time support

**Inspector Settings:**
```
maxHealth = 100                    // Maximum health
currentHealth = 100                // Starting health
lowHealthThreshold = 25            // When to trigger low health warning
healthDisplay                      // TextMeshPro UI reference
```

**UnityEvents:**
```
onHealthChanged                    // Any health change
onDamageReceived                   // When taking damage
onHealthGained                     // When healing
onLowHealthReached                 // When crossing low threshold down
onLowHealthRecovered               // When crossing low threshold up
onDeath                            // When health reaches 0
onRevived                          // When health restored from 0
```

**Public Methods:**
```csharp
public void TakeDamage(int damageAmount)       // Reduce health
public void Heal(int healAmount)               // Increase health
public void SetHealth(int newHealth)           // Set to specific value
public void FullHeal()                         // Restore to max
public void SetMaxHealth(int newMaxHealth)     // Change max
public void SetLowHealthThreshold(int newThreshold)  // Change threshold
```

**Public Properties:**
```csharp
public int MaxHealth               // Get max health
public int CurrentHealth           // Get current health
public bool IsLowHealth            // Is below threshold?
public bool IsDead                 // Is health 0?
public float HealthPercentage      // 0-1 value for health bars
```

**Example Code:**
```csharp
public void TakeDamage(int damageAmount)
{
    if (isDead || damageAmount <= 0) return;

    int previousHealth = currentHealth;
    currentHealth = Mathf.Max(0, currentHealth - damageAmount);

    UpdateDisplay();
    onDamageReceived.Invoke();
    onHealthChanged.Invoke();

    // Check if crossed threshold
    if (previousHealth > lowHealthThreshold && currentHealth <= lowHealthThreshold)
    {
        isLowHealth = true;
        onLowHealthReached.Invoke();
    }

    // Check for death
    if (currentHealth <= 0 && !isDead)
    {
        isDead = true;
        onDeath.Invoke();
    }
}
```

**Common Uses:**
- Player health system
- Enemy health
- Object durability
- Shield systems (separate health pool)

**Event Chain Example:**
```
Enemy collision → GameHealthManager.TakeDamage(10)
  ↓
onDamageReceived → PlayHurtSound()
  ↓
onLowHealthReached → ChangeScreenEffect()
  ↓
onDeath → ShowGameOverScreen()
```

---

#### **GameInventorySlot.cs**
Individual inventory slot management with capacity limits.

**Location:** `Assets/Scripts/Game/GameInventorySlot.cs`

**Key Features:**
- Item storage with counts and capacity limits
- Add/remove functionality with overflow detection
- UI display integration
- Full/empty state events

**Inspector Settings:**
```
maxCapacity = 10                   // Maximum items in slot
currentValue = 0                   // Starting count
itemName = "Keys"                  // Display name
displayText                        // TextMeshPro UI reference
```

**UnityEvents:**
```
onValueChanged                     // When count changes
onSlotFull                        // When reaching max capacity
onSlotEmpty                       // When count reaches zero
```

**Public Methods:**
```csharp
public void Increment(int amount = 1)      // Add items
public void Decrement(int amount = 1)      // Remove items
public void SetValue(int newValue)         // Set to specific count
public void Clear()                        // Empty the slot
```

**Common Uses:**
- Key collection with limits
- Ammo tracking
- Resource gathering
- Inventory management

---

#### **GameStateManager.cs**
Simplified pause and victory state management with automatic system coordination.

**Location:** `Assets/Scripts/Game/GameStateManager.cs`

**Key Features:**
- Configurable pause key (default: P)
- Victory state detection
- Automatic timer coordination
- UI panel management

**Inspector Settings:**
```
pauseKey = KeyCode.P               // Key to pause/resume
startPaused = false               // Start game paused?
autoPauseTimers = true            // Auto-coordinate with timers?
pausePanel                        // UI panel shown when paused
restartButton                     // Button shown when paused/won
```

**UnityEvents:**
```
onGamePaused                      // When game pauses
onGameResumed                     // When game resumes
onVictory                         // When victory achieved
```

**Public Methods:**
```csharp
public void TogglePause()         // Switch pause state
public void PauseGame()           // Pause the game
public void ResumeGame()          // Resume the game
public void Victory()             // Trigger victory
public void RestartScene()        // Restart current scene
```

**Automatic Features:**
- Finds all GameTimerManager instances and pauses/resumes them automatically
- Shows/hides pause panel and restart button
- Handles Time.timeScale for pause functionality

---

#### **GameTimerManager.cs**
Comprehensive timer system with multiple thresholds and automatic game state integration.

**Location:** `Assets/Scripts/Game/GameTimerManager.cs`

**Key Features:**
- Count-up or countdown modes
- Multiple configurable thresholds with individual events
- Periodic event system (e.g., every 10 seconds)
- Three display formats (MM:SS, decimal seconds, HH:MM:SS)
- Automatic pause/resume with GameStateManager

**Inspector Settings:**
```
countUp = true                    // Count up vs countdown
startTime = 0f                    // Starting time value
startAutomatically = true         // Auto-start on scene load
displayFormat = TimeFormat.MinutesSeconds  // Display format
periodicInterval = 10f            // Periodic event interval
enablePeriodicEvents = false      // Enable periodic events?
thresholds[]                      // Array of time thresholds
```

**Threshold Configuration:**
Each threshold has:
- **thresholdTime**: When to trigger (in seconds)
- **thresholdName**: Descriptive name for organization
- **onThresholdReached**: Individual UnityEvent for this threshold

**UnityEvents:**
```
onTimerStarted                    // When timer starts
onTimerStopped                    // When timer stops
onTimerPaused                     // When timer pauses
onTimerResumed                    // When timer resumes
onTimerRestarted                  // When timer resets
onPeriodicEvent                   // Fires every interval
onTimerUpdate                     // Every frame while running
```

**Public Methods:**
```csharp
public void StartTimer()          // Start the timer
public void StopTimer()           // Stop completely
public void PauseTimer()          // Pause (can resume)
public void ResumeTimer()         // Resume from pause
public void TogglePause()         // Switch pause state
public void RestartTimer()        // Reset to start time
public void SetTime(float time)   // Set current time
public void AddTime(float time)   // Add time to current
```

**Example Threshold Setup:**
```
[0] 30s → "FirstChallenge" → Spawn enemy wave
[1] 60s → "Midpoint" → Increase difficulty
[2] 90s → "FinalWarning" → Play warning sound
[3] 120s → "Victory" → GameStateManager.Victory()
```

---

#### **GameUIManager.cs**
UI data display system with animated feedback.

**Location:** `Assets/Scripts/Game/GameUIManager.cs`

**Key Features:**
- Score display with punch animations
- Health bar integration
- Timer display with multiple formats
- Victory screen with final stats
- Automatic data binding to game managers

**Inspector Settings:**
```
scoreText                         // TextMeshPro for score
healthText                        // TextMeshPro for health numbers
healthBar                         // Slider for health bar
timerText                         // TextMeshPro for timer
victoryText                       // TextMeshPro for victory message
```

**Public Methods:**
```csharp
public void AddScore(int points)          // Add points with animation
public void UpdateHealth(int current, int max)  // Update health display
public void DisplayVictory()              // Show victory with stats
public void RefreshAllUI()               // Update all displays
```

**Automatic Features:**
- Score changes trigger punch animations
- Health bar color changes based on percentage
- Victory display shows final score and time

---

#### **GameAudioManager.cs**
Audio system with mixer integration and automatic fading.

**Location:** `Assets/Scripts/Game/GameAudioManager.cs`

**Key Features:**
- Audio mixer integration with volume control
- Music crossfading and fade in/out
- Sound effect management
- Event-driven audio feedback

**Inspector Settings:**
```
audioMixer                        // Main Audio Mixer reference
soundEffects[]                    // Array of sound effect clips
defaultFadeDuration = 2f          // Default fade time
```

**Public Methods:**
```csharp
public void PlayMusic(AudioClip clip)     // Play music with fade
public void StopMusic(bool fadeOut)       // Stop with optional fade
public void PlaySoundEffect(int index)   // Play SFX by array index
public void SetMasterVolume(float vol)   // Control master volume
public void SetMusicVolume(float vol)    // Control music volume
public void SetSFXVolume(float vol)      // Control SFX volume
```

**Setup Requirements:**
- Create Audio Mixer with Master, Music, SFX groups
- Expose volume parameters in mixer
- Assign sound effects to array

---

#### **GameCameraManager.cs**
Cinemachine camera switching system with string-based identification.

**Location:** `Assets/Scripts/Game/GameCameraManager.cs`

**Key Features:**
- String-based camera names (no array indices)
- Automatic exclusive camera activation
- Event system for camera transitions
- Support for any number of cameras

**Inspector Settings:**
```
cameras[]                         // Array of camera setups
defaultCameraName = "Player"      // Camera to enable on start
```

**Camera Setup Structure:**
Each camera entry has:
- **cameraName**: String identifier (NO SPACES - e.g., "Player", "Victory")
- **cameraGameObject**: GameObject with CinemachineCamera component

**Public Methods:**
```csharp
public void EnableCamera(string cameraName)  // Switch to named camera
public void DisableAllCameras()              // Turn off all cameras
```

**UnityEvents:**
```
onCameraChanged                   // When camera switches
```

**Example Usage:**
```
Camera Array:
[0] "Player" → Player follow camera
[1] "Victory" → Celebration camera
[2] "Boss" → Boss battle camera

Events:
GameStateManager.onVictory → GameCameraManager.EnableCamera("Victory")
TriggerZone.onEnter → GameCameraManager.EnableCamera("Boss")
```

---

## Common Patterns & Recipes

### Pattern 1: Spawn Enemy on Trigger Entry

**Components Needed:**
- Empty GameObject with InputTriggerZone
- Empty GameObject with ActionSpawnObject
- Enemy Prefab

**Setup:**
```
1. Create empty GameObject "SpawnZone"
   - Add BoxCollider, check "Is Trigger"
   - Add InputTriggerZone script
   - Set triggerObjectTag = "Player"

2. Create empty GameObject "SpawnPoint"
   - Add ActionSpawnObject script
   - Drag enemy prefab to prefabToSpawn

3. Connect the event:
   SpawnZone.InputTriggerZone
     → onTriggerEnterEvent
       → SpawnPoint.ActionSpawnObject.spawnSinglePrefab()
```

---

### Pattern 2: Collect Coins to Open Door

**Components Needed:**
- Coin prefab with trigger collider
- GameCollectionManager on scene manager
- Door with animation/movement script
- TextMeshPro UI for score display

**Setup:**
```
1. Create "GameManager" GameObject
   - Add GameCollectionManager
   - Set threshold = 10
   - Link UI text field

2. Coin prefab needs:
   - Script to detect player collision
   - On collision: GameCollectionManager.Increment(1)
   - Then: Destroy(gameObject)

3. Connect threshold event:
   GameCollectionManager.onThresholdReached
     → Door.Open() or Animator.SetTrigger("Open")
```

---

### Pattern 3: Display Message on Zone Entry

**Components Needed:**
- Trigger zone with InputTriggerZone
- Canvas with TextMeshPro component + ActionDisplayText

**Setup:**
```
1. Create trigger zone
   - Add InputTriggerZone
   - Set tag to "Player"

2. Create UI Canvas
   - Add TextMeshPro component
   - Add ActionDisplayText script (requires TMP component)
   - Set timeOnScreen = 3f

3. Connect with string parameter:
   InputTriggerZone.onTriggerEnterEvent
     → ActionDisplayText.DisplayText(string)
       → Type message: "You entered the forest!"
```

---

### Pattern 4: Health System with Low Health Warning

**Components Needed:**
- GameHealthManager on player
- UI for health display
- Warning effect (screen overlay, sound, etc.)

**Setup:**
```
1. Add GameHealthManager to Player
   - Set maxHealth = 100
   - Set lowHealthThreshold = 25
   - Link TextMeshPro UI

2. Connect damage sources:
   Enemy collision → GameHealthManager.TakeDamage(10)

3. Connect warning effects:
   GameHealthManager.onLowHealthReached
     → ScreenOverlay.FadeIn()
     → WarningSound.Play()

   GameHealthManager.onLowHealthRecovered
     → ScreenOverlay.FadeOut()

4. Connect death:
   GameHealthManager.onDeath
     → GameOverCanvas.SetActive(true)
     → ActionRestartScene (after delay)
```

---

### Pattern 5: Bumper Jump Pad

**Components Needed:**
- Cylinder or platform mesh
- PhysicsBumper script
- Optional: Animator for visual feedback

**Setup:**
```
1. Create platform GameObject
   - Add MeshCollider or BoxCollider (NOT trigger)
   - Add PhysicsBumper script
   - Set bumperForce = 25
   - Set upwardForceMultiplier = 1.5 (more vertical)
   - Configure animation curve for bounce feel

2. Visual feedback:
   PhysicsBumper.onBumperTriggered
     → Animator.SetTrigger("Bounce")
     → BounceSound.Play()
```

---

### Pattern 6: Auto-Spawning Falling Obstacles

**Components Needed:**
- Obstacle prefabs (with Rigidbody)
- Empty GameObject as spawn point
- ActionAutoSpawner

**Setup:**
```
1. Create "ObstacleSpawner" GameObject
   - Position above play area
   - Add ActionAutoSpawner
   - Drag obstacle prefabs into spawnThings array
   - Set spawnRateMin = 1f
   - Set spawnRateMax = 3f
   - Set spawnPositionRange = 5f (random X/Z offset)

2. Obstacle prefabs need:
   - Rigidbody (gravity enabled)
   - Collider
   - Auto-destroy after time or on hit
```

---

## Unity Technical Details

### Input System
- Uses Unity's new Input System package (`com.unity.inputsystem`)
- Actions defined in `InputSystem_Actions.inputactions`
- Some scripts use legacy `Input.GetKeyDown()` for simplicity
- Player controller uses modern callback pattern (OnMove, OnJump)

### Physics
- Uses Rigidbody physics with `linearVelocity` (new Unity API, replaces `velocity`)
- Ground detection via `Physics.CheckSphere()`
- Forces applied with `AddForce()` using ForceMode.Impulse or ForceMode.Force
- Colliders can be triggers (pass-through detection) or solid (physics collision)

### Rendering
- Universal Render Pipeline (URP)
- Materials use URP shaders
- Emission effects via material property animation

### UI
- TextMeshPro (TMPro namespace) for all text
- Better than legacy Unity UI.Text
- Requires `using TMPro;` in scripts

### Tags
- "Player" tag used extensively for detection
- Custom tags can be created in Tag Manager
- Comparison via `CompareTag("TagName")`

---

## Naming Conventions

Scripts follow educational naming pattern:

**Format:** `[Category][Purpose].cs`

**Categories:**
- **Input** - Event sources (key press, triggers, etc.)
- **Action** - Event targets (spawn, display, restart, etc.)
- **Physics** - Movement and physics systems
- **Game** - Game state management (score, health, inventory)
- **UI** - User interface components (future)

**Examples:**
- `InputKeyPress` - Input category, detects key press
- `ActionSpawnObject` - Action category, spawns objects
- `PhysicsBumper` - Physics category, bumper mechanics
- `GameHealthManager` - Game category, manages health

---

## Debugging Tips

### Inspector Debugging
- Use Debug.Log() to print to Console
- Check "onEvent" connections in Inspector
- Ensure colliders have "Is Trigger" set correctly
- Verify tags match exactly (case-sensitive)

### Common Issues

**Q: Trigger zone not detecting player**
- Check collider has "Is Trigger" enabled
- Verify tag matches exactly ("Player" not "player")
- Ensure player has a Collider component
- At least one object needs Rigidbody (usually player)

**Q: UnityEvent not firing**
- Check event is connected in Inspector
- Verify method signature matches (e.g., needs string parameter)
- Look for errors in Console
- Test with Debug.Log in the target method

**Q: Spawner not spawning**
- Check prefabToSpawn is assigned
- Verify spawn point position is visible in Scene view
- For ActionAutoSpawner, check spawnRateMin/Max are reasonable
- Use Debug.Log in spawn method to verify it's being called

**Q: Physics not working correctly**
- Ensure Rigidbody is attached and not kinematic
- Check mass and drag values
- Verify forces are reasonable magnitude
- Check collision layers in Physics settings

**Q: UI not updating**
- Verify TextMeshPro component is assigned
- Check Canvas is set to Screen Space
- Ensure Canvas is not disabled
- Use Debug.Log to verify update method is called

---

## Advanced Concepts

### UnityEvents with Parameters

Some methods accept parameters in UnityEvents:

**Example: DisplayText(string message)**
```
1. In Inspector, click "+" on UnityEvent
2. Drag GameObject with ActionDisplayText
3. Select ActionDisplayText → DisplayText(string)
4. Type string directly in Inspector field: "Hello World!"
```

**Example: Increment(int amount)**
```
1. Connect to GameCollectionManager.Increment
2. Change parameter from 1 to 5 to add 5 points
```

### Animation Curves

PhysicsBumper uses AnimationCurve for custom bounce feel:
- X-axis: Time (0 to 1, normalized)
- Y-axis: Progress (0 to 1)
- Curve shape controls easing
- Access in Inspector to draw custom curve

### Material Instancing

PhysicsBumper creates material instances to avoid modifying shared materials:
```csharp
material = new Material(renderer.sharedMaterial);
renderer.material = material;
```
This prevents changing all objects using the same material.

### Coroutines

Many scripts use Coroutines for animations over time:
```csharp
private IEnumerator AnimateEffects()
{
    while (elapsedTime < duration)
    {
        // Update animation
        yield return null;  // Wait one frame
    }
}
```

### Property Getters

Some scripts expose read-only properties:
```csharp
public bool IsDead => isDead;  // Read-only access
```
Use in UnityEvents with Dynamic bool parameters or for scripting checks.

---

## Code Writing Guidelines

If you need to create new components following this system:

### 1. Class Structure Template

```csharp
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [Category] - [Brief description]
/// For educational use in Animation and Interactivity class.
/// Connect via UnityEvents in Inspector.
/// </summary>
public class CategoryPurpose : MonoBehaviour
{
    [Header("Main Settings")]
    [SerializeField] private float setting = 1f;

    [Header("Events")]
    public UnityEvent onSomethingHappened;

    // Public method callable from UnityEvents
    public void DoSomething()
    {
        // Logic here
        onSomethingHappened?.Invoke();
    }
}
```

### 2. Best Practices

**Expose Settings:**
```csharp
[SerializeField] private float speed = 5f;  // Private but visible in Inspector
public GameObject target;                    // Public, Inspector-editable
```

**Use Headers:**
```csharp
[Header("Movement Settings")]
[SerializeField] private float moveSpeed = 5f;

[Header("Jump Settings")]
[SerializeField] private float jumpForce = 10f;
```

**Add Tooltips:**
```csharp
[Tooltip("Force applied to player on collision")]
[SerializeField] private float bumperForce = 20f;
```

**Safe UnityEvent Invocation:**
```csharp
onEvent?.Invoke();  // Null-safe with ?.
```

**Check References:**
```csharp
if (prefabToSpawn != null)
{
    Instantiate(prefabToSpawn, transform.position, transform.rotation);
}
```

### 3. Public Methods for UnityEvents

Methods students will connect to should be public and simple:

```csharp
// Good - simple, clear
public void Spawn()
{
    SpawnObject();
}

// Good - with parameter
public void SetSpeed(float newSpeed)
{
    speed = newSpeed;
}

// Avoid - too many parameters
public void ComplexMethod(float a, int b, string c, GameObject d)
{
    // Students can't easily use this in UnityEvents
}
```

---

## Frequently Asked Questions

### General Questions

**Q: Do I need to write code for this class?**
A: No! The toolkit is designed for no-code interaction. You connect components visually in the Inspector using UnityEvents.

**Q: What are UnityEvents?**
A: Visual connections in the Inspector that call methods when something happens. Like wiring components together.

**Q: Can I modify these scripts?**
A: Yes, but save copies first. Understanding the patterns helps you create your own components.

### Specific Component Questions

**Q: How do I detect when player touches something?**
A: Use InputTriggerZone with a trigger collider, tag your player "Player", and connect the onTriggerEnterEvent.

**Q: How do I make something spawn automatically?**
A: Use ActionAutoSpawner - it spawns on its own. Use ActionSpawnObject if you want manual control via events.

**Q: How do I show text on screen?**
A: Add ActionDisplayText to a TextMeshPro component, then call DisplayText(string) from a UnityEvent.

**Q: How do I track score/collectibles?**
A: Use GameCollectionManager. Connect Increment(1) to collection events, set threshold, connect onThresholdReached to rewards.

**Q: How do I make a jump pad?**
A: Use PhysicsBumper with high upwardForceMultiplier. Attach to platform with collider (NOT trigger).

**Q: Why isn't my trigger detecting the player?**
A: Check: 1) Collider has "Is Trigger" enabled, 2) Tag exactly matches "Player", 3) Player has a collider, 4) One has a Rigidbody.

### Advanced Questions

**Q: Can I chain multiple events?**
A: Yes! Components can both receive and send events. Example: KeyPress → SpawnObject → CollectionManager → Display Text

**Q: How do I delay an action?**
A: Use Invoke() or create a timer script with UnityEvent output. Or use Animation timeline.

**Q: Can I pass data between components?**
A: Limited. UnityEvents support simple parameters (int, float, string, GameObject). For complex data, use public properties.

**Q: How do I make enemies move?**
A: Use PhysicsEnemyController or create movement scripts with Vector3.MoveTowards() or NavMesh for pathfinding.

---

## Example Questions to Ask Claude

Once you've pasted this guide into Claude, here are good questions:

**Beginner:**
- "How do I make an enemy spawn when the player enters a trigger zone?"
- "How do I count how many coins the player collected?"
- "How do I display a message when the player enters a new area?"
- "What's the difference between ActionSpawnObject and ActionAutoSpawner?"

**Intermediate:**
- "How do I create a checkpoint system that tracks progress?"
- "How do I make a door that opens when I collect 10 keys?"
- "How do I create a health system with damage zones?"
- "How do I make a jump pad that launches the player upward?"

**Advanced:**
- "How do I chain multiple events together for a complex interaction?"
- "How do I create a new component that follows the educational pattern?"
- "How do I use AnimationCurves to create custom bounce feels in PhysicsBumper?"
- "How do I extend GameHealthManager to include armor/shields?"

**Debugging:**
- "My trigger zone isn't detecting the player. What could be wrong?"
- "Why isn't my spawner creating objects?"
- "My UnityEvent isn't firing when I press the button. Help debug."
- "The bumper isn't applying force correctly. What should I check?"

---

## Quick Reference Card

### Most Common Components

| Need to detect... | Use component... |
|---|---|
| Key press | InputKeyPress |
| Player entering area | InputTriggerZone |
| Limited key presses | InputKeyCountdown |

| Need to do... | Use component... |
|---|---|
| Spawn single object | ActionSpawnObject |
| Spawn continuously | ActionAutoSpawner |
| Show text message | ActionDisplayText |
| Restart scene | ActionRestartScene |

| Need to manage... | Use component... |
|---|---|
| Score/collectibles | GameCollectionManager |
| Health/damage | GameHealthManager |
| Inventory items | GameInventorySlot |

| Need physics... | Use component... |
|---|---|
| Player movement | PhysicsPlayerController |
| Jump pads/bumpers | PhysicsBumper |
| Moving platforms | PhysicsPlatformStick |

### Required Components

- **Triggers need:** Collider (Is Trigger ✓) + matching tag
- **Text display needs:** TextMeshProUGUI component
- **Physics needs:** Rigidbody (on player or dynamic objects)
- **Spawning needs:** Prefab assigned in Inspector

### Event Connection Steps

1. Select GameObject with event source
2. Find UnityEvent field (e.g., onTriggerEnterEvent)
3. Click "+" to add listener
4. Drag target GameObject into object field
5. Select Component → Method from dropdown
6. For parameters: enter value in Inspector field

---

## Input Key Coordination

The toolkit uses coordinated input to avoid conflicts:

### System-Wide Key Mapping:
- **P Key**: Pause/Resume game (GameStateManager)
- **M Key**: Return to menu (when paused/game over)
- **ESC Key**: Quit application (InputQuitGame)
- **Restart**: Button/UnityEvent only (ActionRestartScene) - NO automatic key to prevent accidents
- **Custom Keys**: Student-configurable for game mechanics

### Automatic System Coordination:
- **GameStateManager** automatically finds and pauses all **GameTimerManager** instances
- **GameTimerManager** can opt-out of automatic pausing with `respondToGamePause = false`
- **UI panels** show/hide automatically with pause states
- **Audio systems** can be connected to state change events

---

## Unity Version & Packages

**Unity Version:** Determined by ProjectSettings (check project)

**Key Packages:**
- Input System: 1.11.2
- Universal Render Pipeline: 17.0.3
- TextMeshPro: Built-in
- Cinemachine: 3.1.2 (using CinemachineCamera, not deprecated CinemachineVirtualCamera)
- AI Navigation: 2.0.5

---

## Documentation Files

The project includes comprehensive guides:

### For Instructors:
- **`Week1_ComponentWorkflows.txt`**: Detailed setup workflows for GameAudioManager, GameStateManager, GameUIManager
- **`CodebaseImprovements_GameGenres.txt`**: Guide for implementing platformers, survival games, puzzles, racing, and shooters
- **`TimerExample_SceneSetup.txt`**: Complete example scene demonstrating GameTimerManager

### For Students:
- **`CLAUDE_STUDENT.md`**: This comprehensive reference guide (copy and paste into Claude conversations)

---

## File Locations Quick Reference

```
Assets/Scripts/Input/InputKeyPress.cs
Assets/Scripts/Input/InputTriggerZone.cs
Assets/Scripts/Input/InputKeyCountdown.cs
Assets/Scripts/Input/InputQuitGame.cs

Assets/Scripts/Actions/ActionSpawnObject.cs
Assets/Scripts/Actions/ActionAutoSpawner.cs
Assets/Scripts/Actions/ActionDisplayText.cs
Assets/Scripts/Actions/ActionRestartScene.cs

Assets/Scripts/Physics/PhysicsPlayerController.cs
Assets/Scripts/Physics/PhysicsBumper.cs
Assets/Scripts/Physics/PhysicsPlatformStick.cs

Assets/Scripts/Game/GameCollectionManager.cs
Assets/Scripts/Game/GameHealthManager.cs
Assets/Scripts/Game/GameInventorySlot.cs
```

---

## Credits & Notes

**Project:** Educational Unity Toolkit for Animation and Interactivity Class
**Philosophy:** No-code, UnityEvent-driven, modular component architecture
**Learning Goals:** Event-driven programming, component composition, systems thinking

**For Instructors:** This guide provides comprehensive context for AI-assisted learning. Students can paste it into Claude conversations to get accurate, project-specific help.

**For Students:** This is your reference when asking Claude questions. The more specific your question, the better the answer. Include error messages, describe what you tried, and explain what you want to achieve.

---

**End of Student Reference Guide**
*Copy this entire document when starting a new Claude conversation*
