# Desk-Check Findings — All EGTK Scripts

**Session started**: 2026-04-02  
**Status**: IN PROGRESS — bugs below were fixed in session 1. Remaining scripts still to check noted at bottom.

---

## BUGS FIXED (Session 1)

### 1. `GameCheckpointManager` — Double restore in RestoreAll()
**File**: `Runtime/Game/GameCheckpointManager.cs`  
**Method**: `RestoreAll()` → `TeleportPlayerToCheckpoint()`  
**Problem**: `RestoreAll()` calls `RestoreScore()`/`RestoreHealth()` directly, then calls `TeleportPlayerToCheckpoint()` which also calls both. Double restore fires events (onValueChanged, onHealthChanged) twice.  
Also breaks `RestoreCheckpoint()` intent: its comment says "leaves score/health as they are" but `TeleportPlayerToCheckpoint()` restores them anyway.  
**Fix**: Removed `RestoreScore()`/`RestoreHealth()` calls from `TeleportPlayerToCheckpoint()`.

---

### 2. `PuzzleSwitchChecker` — Lambda listeners never removed
**File**: `Runtime/Puzzle/PuzzleSwitchChecker.cs`  
**Methods**: `Start()`, `OnDestroy()`, `SetAutomaticChecking()`  
**Problem**: `AddListener((newState) => OnSwitchChanged(...))` and `RemoveListener((newState) => OnSwitchChanged(...))` each create a **new lambda object**. Lambda delegates are never reference-equal, so `RemoveListener` silently does nothing. Listeners accumulate on PuzzleSwitch objects — memory leak and potential duplicate `CheckConfiguration` calls if `SetAutomaticChecking` is toggled.  
**Fix**: Replaced lambdas with a named method `OnAnySwitchChanged(int)` that can be added and removed by reference.

---

### 3. `ActionDialogueSequence` — Typewriter skip display bug
**File**: `Runtime/Actions/Display/ActionDialogueSequence.cs`  
**Method**: `NextDialogue()`  
**Problem**: When `isTyping=true`, the method sets `maxVisibleCharacters = MAX_VISIBLE_CHARACTERS` directly. The still-running DOTween tween overwrites this every frame with its current interpolated value, briefly un-completing the text before the tween finishes.  
**Fix**: Set `skipRequested = true` instead. The TypewriterEffect OnUpdate callback calls `typeTween.Complete()`, which correctly drives `maxVisibleCharacters` to `totalCharacters` in one shot.

---

### 4. `PhysicsPlatformAnimator` — Division by zero
**File**: `Runtime/Physics/Platforms/PhysicsPlatformAnimator.cs`  
**Method**: `Update()`  
**Problem**: `currentTime += (Time.deltaTime / totalAnimationTime)` — if `totalAnimationTime = 0`, this is `Infinity`, immediately triggering loop/pingpong logic every frame.  
**Fix**: Added `totalAnimationTime <= 0f` early-return guard in `Update()`.

---

### 5. `CharacterControllerCC` — Dead failed cast
**File**: `Runtime/CharacterControllers/Player/CharacterControllerCC.cs`  
**Method**: `CheckForSpawnPoint()`  
**Problem**: `ISpawnPointProvider[] providers = FindObjectsByType<MonoBehaviour>(...) as ISpawnPointProvider[];` — this cast always returns `null` (array covariance doesn't work this way in C#). The variable is never used; the immediately following `allBehaviours` loop is the real logic. Dead code that misleads readers.  
**Fix**: Removed the dead line.

---

## EDGE CASES — Catalogued (not fixed, acceptable for educational toolkit)

| Script | Method | Issue |
|---|---|---|
| `GameTimerManager` | `ResetAndStart()` while running | Timer resets but `onTimerStarted` doesn't re-fire (guard in `StartTimer()` blocks it). Rare scenario, expected behavior for most uses. |
| `InputCheckpointZone` | `Start()` | `if (hasBeenActivated)` branch is dead code — field is never serialized, always false on Start. Harmless. |
| `InputCheckpointZone` | `ActivateCheckpoint()` | `saveFullState=true` path calls `SaveCheckpointFullAtPosition()` which uses `Quaternion.identity` — ignores `useCheckpointRotation`. Students wanting full state + custom rotation can't get both. |
| `ActionEventSequencer` | `PlaySequence()` | Events at `triggerTime=0` fire one frame late (coroutine increments time before checking). Barely perceptible. |
| `ActionEventSequencer` | `OnDisable()` | Calls `StopSequence()` → fires `onSequenceStopped`. May surprise students who disable the GameObject expecting a silent pause. |
| `PhysicsBumper` | `OnCollisionEnter()` | Gets `Rigidbody` from player — only works with Rigidbody-based players. Won't apply force to `CharacterControllerCC` players (CharacterController, not Rigidbody). Expected limitation for physics-based bumpers. |
| `GameStateManager` | `Update()` | Uses legacy `Input.GetKeyDown()` for pause key. Fails silently if project is set to "New Input System Only" backend. |
| `GameStateManager` | `ResumeGame()` | Resumes ALL paused timers, including ones the student manually paused for unrelated reasons. |
| `GameAudioManager` | `PlaySFXByName()` | Silent failure if Resources.Load returns null — no warning logged. `PlayMusicByName` and `PlayAmbientByName` do log warnings; inconsistent. |
| `ActionAutoSpawner` | `ActiveSpawnCount` property | Returns stale count when objects are destroyed externally. Null cleanup only happens inside `SpawnObject()`. |
| `ActionDialogueSequence` | `GetTextInDuration()` SlideUp case | Returns `imageFadeInDuration` instead of `textFadeInDuration`. Intentional for "consistency" per code comment, but naming is confusing. |
| `GameAudioManager` | `PlayMusic(fadeIn=true)` | If `musicSource.volume` is 0 at call time, fades from 0 to 0 → silent music. Unlikely in practice. |

---

## Scripts Checked (Session 1)

- ✅ GameTimerManager
- ✅ GameCheckpointManager  
- ✅ InputCheckpointZone  
- ✅ ActionDialogueSequence  
- ✅ ActionEventSequencer  
- ✅ PuzzleSwitchChecker  
- ✅ PuzzleSwitch  
- ✅ PhysicsPlatformAnimator  
- ✅ ActionAutoSpawner  
- ✅ InputTriggerZone  
- ✅ ActionRandomEvent  
- ✅ ActionShuffleEvent  
- ✅ InputOnStart  
- ✅ ActionPlaySound  
- ✅ GameStateManager  
- ✅ GameAudioManager  
- ✅ PhysicsBumper  
- ✅ CharacterControllerCC (spawn logic + teleport)
- ✅ GameUIManager (fields scan only)
- ✅ ActionRespawnPlayer
- ✅ GameInventoryManager (previous session)
- ✅ GameHealthManager (previous session)
- ✅ GameCollectionManager (previous session)

---

## Scripts Still To Check (Session 2+)

- ⬜ CharacterControllerFP
- ⬜ CharacterControllerCC (full movement/physics logic)
- ⬜ PhysicsCharacterController
- ⬜ PhysicsBallPlayerController
- ⬜ EnemyControllerCC
- ⬜ PhysicsEnemyController
- ⬜ CharacterPushRigidBody
- ⬜ PhysicsBumperTag
- ⬜ PhysicsPlatformStick
- ⬜ ActionPlatformAnimator
- ⬜ ActionDisplayText
- ⬜ ActionDisplayImage
- ⬜ ActionSpawnObject
- ⬜ ActionSpawnProjectile
- ⬜ ActionRestartScene
- ⬜ ActionTeleportToTransform
- ⬜ ActionPlayCharacterEmoteAnimation
- ⬜ ActionTriggerAnimatorParameter
- ⬜ ActionBlinkDecal / ActionBlinkDecalOptimized / ActionDecalSequence / ActionDecalSequenceLibrary
- ⬜ ActionAnimateTransform
- ⬜ ActionEmissionAnimation
- ⬜ InputKeyPress / InputKeyCountdown / InputQuitGame / InputActionEvent
- ⬜ InputMouseInteraction / InputFPMouseInteraction
- ⬜ InputClickDrag (new)
- ⬜ InputClickRotate (new)
- ⬜ FadeInFromBlackOnRestart
- ⬜ GameCameraManager
- ⬜ DialogueUIController
- ⬜ All Editor scripts (FindProperty name audits)
