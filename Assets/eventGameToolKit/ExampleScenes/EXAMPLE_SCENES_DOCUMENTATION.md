# Example Scenes - UnityEvent Wiring Documentation

This document explains all UnityEvent → Method connections in the example scenes for the eventGameToolKit package.

---

## collectionExample.unity

### Overview
Demonstrates the **GameCollectionManager** system with collectible objects, UI updates, and a victory condition when reaching a target count.

### Scene Components

#### Managers
- **GameCollectionManager** - Tracks collected items with threshold events
- **GameUIManager** - Displays inventory count on screen
- **GameStateManager** - Handles pause and victory states

#### Collectible Objects
- Multiple collectible GameObjects with **InputTriggerZone** components
- Trigger on collision with "Player" tag

### Event Wiring Flow

```
1. COLLECTION SYSTEM:
   Collectible.InputTriggerZone.onTriggerEnter
   → GameCollectionManager.Increment()

   GameCollectionManager.onValueChanged(string, int)
   → GameUIManager.UpdateInventoryCount(string, int)

2. VICTORY CONDITION:
   GameCollectionManager.onCountUpToThreshold
   → GameStateManager.Victory()

   GameStateManager.onVictory
   → (Shows victory panel/UI)

3. UI CONTROLS:
   RestartButton.Button.OnClick()
   → GameStateManager.RestartScene()
```

### Key Learning Points
- **InputTriggerZone** detects player collision and triggers increment
- **onValueChanged** passes item name and count to UI
- **Threshold events** fire once when crossing a target value


---

## projectileExample.unity

### Overview
Demonstrates a **projectile spawning system** with ammo limiting using GameCollectionManager for resource management.

### Scene Components

#### Managers
- **GameCollectionManager** - Tracks ammunition count with min/max limits
- **GameUIManager** - Displays ammo count
- **GameStateManager** - Handles game over when out of ammo

#### Spawning System
- **ActionAutoSpawner** - Spawns projectiles automatically or on input
- Projectile prefabs with physics/collision

### Event Wiring Flow

```
1. AMMO SYSTEM:
   SpawnButton/Input.onInputTriggered
   → GameCollectionManager.Decrement()

   GameCollectionManager.onValueChanged(string, int)
   → GameUIManager.UpdateInventoryCount(string, int)

2. SPAWN CONTROL:
   (Check ammo available before spawning)
   GameCollectionManager.onValueChanged
   → Check if value > 0
   → ActionAutoSpawner.SpawnObject() OR disable spawning

3. OUT OF AMMO (GAME OVER):
   GameCollectionManager.onMinReached
   → GameStateManager.GameOver()

   GameStateManager.onGameOver
   → (Shows game over panel)

4. RELOAD/REFILL:
   RefillTrigger.InputTriggerZone.onTriggerEnter
   → GameCollectionManager.Increment(10)  // Add 10 ammo

5. UI CONTROLS:
   RestartButton.Button.OnClick()
   → GameStateManager.RestartScene()
```

### Key Learning Points
- **Resource limiting** with min/max values on GameCollectionManager
- **onMinReached** triggers game over condition
- **onCountDownToThreshold** can warn when ammo is low
- **Decrement** reduces count (for consuming resources)
- Can wire multiple events to single Decrement (fire weapon, spawn projectile, etc.)

---

## puzzleExample.unity

### Overview
Demonstrates a **timed puzzle system** using GameTimerManager with countdown and GameStateManager for win/lose conditions.

### Scene Components

#### Managers
- **GameTimerManager** - Countdown timer with threshold at 0
- **GameUIManager** - Displays timer in MM:SS format
- **GameStateManager** - Handles victory and game over states

#### Puzzle Elements
- Interactive puzzle switches/buttons
- **InputTriggerZone** components on puzzle pieces
- Victory trigger when puzzle complete

### Event Wiring Flow

```
1. TIMER SYSTEM:
   GameTimerManager.onTimerUpdate(float)
   → GameUIManager.UpdateTimer(float)

   (Timer automatically starts in countdown mode)

2. TIME RUNS OUT (GAME OVER):
   GameTimerManager (threshold at 0).onThresholdReached
   → GameStateManager.GameOver()

   GameTimerManager.onTimerStopped
   → (Alternative: also triggers at 0)

3. PUZZLE SOLVED (VICTORY):
   PuzzleComplete.onPuzzleComplete
   → GameTimerManager.StopTimer()

   PuzzleComplete.onPuzzleComplete
   → GameStateManager.Victory()

   GameStateManager.onVictory
   → (Shows victory panel with time)

4. PAUSE INTEGRATION:
   GameStateManager.onGamePaused
   → GameTimerManager.PauseTimer()

   GameStateManager.onGameResumed
   → GameTimerManager.ResumeTimer()

   (OR use GameTimerManager.respondToGamePause = true for automatic)

5. UI CONTROLS:
   RestartButton.Button.OnClick()
   → GameStateManager.RestartScene()
```

### Key Learning Points
- **Countdown mode** (countUp = false) counts from startTime down to 0
- **Threshold at 0** triggers when time expires
- **onTimerUpdate** fires every frame with current time
- **Automatic timer coordination** with GameStateManager pause
- Victory stops the timer to show completion time
- **Multiple events** can wire to same game state (puzzle complete → stop timer AND victory)

---

## Common Patterns Across All Examples

### 1. GameStateManager Integration
All three scenes use GameStateManager for consistent state handling:
- **pausePanel** - Shows during pause (P key)
- **victoryPanel** - Shows when Victory() is called
- **gameOverPanel** - Shows when GameOver() is called
- **restartButton** - Shows for all three states

### 2. GameUIManager Updates
UI Manager receives data from game managers via UnityEvents:
- `onValueChanged(string, int)` → `UpdateInventoryCount(string, int)`
- `onHealthChanged(int, int)` → `UpdateHealth(int, int)`
- `onTimerUpdate(float)` → `UpdateTimer(float)`

UI Manager never directly reads values - always event-driven updates.

### 3. Initial Value Events
All managers fire their initial values on Start():
- **GameCollectionManager** → fires `onValueChanged` with starting value
- **GameHealthManager** → fires `onHealthChanged` with starting health
- **GameTimerManager** → fires `onTimerUpdate` every frame when running

This ensures UI displays correctly from scene start.

### 4. Threshold Events (GameCollectionManager)
- **onCountUpToThreshold** - Fires ONCE when crossing threshold going UP
  - Example: Score goes from 9 → 10 (threshold = 10)
- **onCountDownToThreshold** - Fires ONCE when crossing threshold going DOWN
  - Example: Ammo goes from 1 → 0 (threshold = 0)
- **onMaxReached** - Fires when value hits maximum limit
- **onMinReached** - Fires when value hits minimum limit

### 5. Restart Pattern
Every scene has a restart button that calls:
```
RestartButton.Button.OnClick() → GameStateManager.RestartScene()
```

GameStateManager ensures Time.timeScale is reset to 1 before reloading.

---

## For Students: How to Wire Events

### Step 1: Select Source Component
In the Inspector, find the component with the UnityEvent you want to wire (e.g., InputTriggerZone)

### Step 2: Add Event Listener
Click the **+** button under the event (e.g., `onTriggerEnter`)

### Step 3: Drag Target GameObject
Drag the GameObject containing the target component into the object field

### Step 4: Select Function
- Click the dropdown that says "No Function"
- Navigate to the component type (e.g., GameCollectionManager)
- Select the method (e.g., Increment)

### Step 5: Set Parameters (if needed)
Some methods have parameters you can set:
- **Increment(int amount)** - Set the number to add
- **Decrement(int amount)** - Set the number to subtract
- **SetValue(int newValue)** - Set a specific value

### Common Mistakes to Avoid
1. **Wrong parameter type** - Make sure event parameter matches method parameter
   - `UnityEvent<int>` → method must take `int` parameter
2. **Null references** - Ensure the target GameObject is assigned
3. **Missing components** - Target GameObject must have the component you're calling
4. **Calling private methods** - Only public methods appear in the dropdown

---

## Testing Event Wiring

### Visual Confirmation
1. **Play the scene**
2. **Open the Inspector** and select the source GameObject
3. **Trigger the event** (enter trigger zone, press button, etc.)
4. **Watch the Inspector** - wired events will briefly highlight when invoked

### Debug Logging
Many components log to the Console when events fire:
- GameTimerManager: "Timer threshold 'Name' reached at X seconds"
- GameStateManager: "Victory!", "Game Over!", "Game Paused"
- GameCollectionManager: Value changes visible in Inspector

### Common Issues
- **Event doesn't fire**: Check if source condition is met (collision tag, input key, etc.)
- **Wrong method called**: Verify dropdown shows correct component and method
- **No visual change**: Ensure UI Manager is wired to receive data updates
- **Restart doesn't work**: Check GameStateManager.RestartScene() is wired to button

---

## Next Steps

After understanding these three examples, try:
1. **Combine systems** - Add timer to collection scene, add health to puzzle scene
2. **Modify thresholds** - Change victory conditions, add low-ammo warnings
3. **Add new events** - Wire additional reactions to existing events
4. **Create variations** - Make collection scene require specific item types

Remember: **Everything connects through UnityEvents** - no code required!
