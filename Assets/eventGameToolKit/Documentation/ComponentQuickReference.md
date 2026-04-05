# EGTK Component Quick Reference

All components work the same way: wire them together using **UnityEvents** in the Inspector.
**Input** components fire events → **Action** components respond to them.

---

## Triggers (Input)
*Add these to a GameObject. They fire events when something happens.*

| Component | What it does | Basic use |
|---|---|---|
| **InputKeyPress** | Fires an event when a keyboard key is pressed | Trigger anything with a keypress |
| **InputOnStart** | Fires an event when the scene starts | Auto-run something on scene load |
| **InputTriggerZone** | Fires events when a tagged object enters/exits a collider | Invisible trigger areas, damage zones, collectibles |
| **InputCheckpointZone** | A trigger zone that also saves a spawn point | Checkpoints and respawn points |
| **InputMouseInteraction** | Fires events when the mouse clicks or hovers over this object | Clickable objects with a free cursor |
| **InputFPMouseInteraction** | Same as above but raycasts from screen center | Clickable objects in a first-person game |
| **InputClickDrag** | Makes this object draggable with the mouse | Drag-and-drop puzzles, sliders |
| **InputClickRotate** | Makes this object rotatable by clicking and dragging | Dials, wheels, rotating puzzles |
| **InputActionEvent** | Fires events from any Unity Input System action | Custom controller/keyboard bindings |
| **InputKeyCountdown** | Counts down while a key is held, fires event at zero | Hold-to-open doors, charge mechanics |
| **InputInteractionZone** | Shows a prompt sprite when the player enters; fires an event when they press Interact (E / Triangle) | Any "press to interact" mechanic — doors, NPCs, items, puzzles |
| **InputCollisionEnter** | Fires an event on physics collision with a tagged object | Bullet hits, physical triggers |

---

## Actions
*Add these to a GameObject. Wire them as the target of a UnityEvent.*

| Component | What it does | Basic use |
|---|---|---|
| **ActionPlaySound** | Plays a sound clip with randomized volume and pitch | Sound effects for any interaction |
| **ActionDisplayText** | Shows text on screen with optional typewriter effect | Subtitles, hints, UI messages |
| **ActionDisplayImage** | Shows an image on screen with fade/scale animation | Pop-up graphics, icons, overlays |
| **ActionDialogueSequence** | Plays a multi-line dialogue sequence with portraits and choices | Story dialogue, NPC conversations |
| **ActionSpawnObject** | Instantiates a prefab at a spawn point | Spawn enemies, pickups, props |
| **ActionAutoSpawner** | Continuously spawns prefabs on a timer | Enemy waves, falling objects, ambient spawning |
| **ActionSpawnProjectile** | Spawns a projectile with a direction and force | Shooting, throwing |
| **ActionRestartScene** | Reloads the current scene | Restart button, game over |
| **ActionRespawnPlayer** | Teleports the player to a spawn point | Death and respawn |
| **ActionTeleportToTransform** | Teleports any object to a target Transform | Portals, warps, repositioning |
| **ActionEventSequencer** | Fires a series of events one after another with delays | Cutscene sequences, timed chains |
| **ActionRandomEvent** | Randomly picks one event from a weighted list to fire | Random outcomes, loot drops, varied responses |
| **ActionShuffleEvent** | Cycles through events in random order, each fires once before repeating | Shuffle playlists, non-repeating random dialogue |
| **ActionTriggerAnimatorParameter** | Sets an Animator parameter (bool, int, float, trigger) | Trigger character or object animations |
| **ActionPlayCharacterEmoteAnimation** | Plays a named animation trigger on a character Animator | Character reactions, emotes |
| **ActionAnimateTransform** | Moves, rotates, or scales a GameObject using animation curves | Custom tweened animations |
| **ActionRandomMotion** | Continuously applies randomized movement/rotation/scale | Floating objects, idle wobble, environmental detail |
| **ActionPlatformAnimator** | Moves a platform between waypoints (transform-based) | Simple moving platforms, doors, elevators |
| **ActionDecalSequence** | Plays a frame-by-frame material animation on a URP decal | Animated decals, sprite-sheet effects |
| **ActionDecalSequenceLibrary** | Switches between multiple decal sequences | Character with multiple animation states |
| **ActionBlinkDecal** | Automatically blinks a character's eye decal | Any character that needs blinking |

---

## Game Managers
*Add one of each to your scene. They track game-wide state.*

| Component | What it does | Basic use |
|---|---|---|
| **GameHealthManager** | Tracks a health value with damage, healing, and a death event. Enable Persist Across Scenes to carry health between levels. | Player or enemy health |
| **GameCollectionManager** | Tracks a numeric value (score, coins) with threshold events. Enable Persist Across Scenes to carry the value between levels. | Score counter, collectible tracker |
| **GameInventoryManager** | Tracks multiple item slots, each with counts and capacity. Enable Persist Across Scenes to carry all counts between levels (first 20 slots). | Inventory with keys, ammo, resources |
| **GameTimerManager** | Counts up or down, fires events at thresholds | Level timer, countdown clock |
| **GameCheckpointManager** | Stores the player's last checkpoint position for respawning | Respawn system (pairs with InputCheckpointZone) |
| **GameStateManager** | Handles pause and victory screens | Pause menu, win condition |
| **GameAudioManager** | Controls background music, ambient audio, and sound effects with fading | Music system, ambient audio |
| **GameCameraManager** | Switches between Cinemachine cameras by name | Multiple camera angles, cutscenes |
| **GameUIManager** | Connects health, score, and timer values to UI display elements | HUD wired to existing UI objects |
| **GameSceneManager** | Loads scenes with transitions and spawn point selection | Multi-level games, hub worlds |

---


## Character Controllers
*Add one to your player or enemy GameObject.*

| Component | What it does | Basic use |
|---|---|---|
| **CharacterControllerFP** | First-person player controller with mouse look, jump, and sprint | First-person games |
| **CharacterControllerCC** | Third-person player controller with rotation and jump | Third-person / top-down games |
| **PhysicsBallPlayerController** | Physics-based ball controller | Rolling ball games |
| **PhysicsCharacterController** | Rigidbody-based character controller | Physics-heavy platformers |
| **EnemyControllerCC** | AI enemy that chases the player using NavMesh | Basic enemy AI |
| **PhysicsEnemyController** | Rigidbody-based enemy that chases the player | Physics-based enemy AI |

---

## Physics
*Add these to objects in the world.*

| Component | What it does | Basic use |
|---|---|---|
| **PhysicsBumper** | Bounces any Rigidbody that touches it | Pinball bumpers, bounce pads |
| **PhysicsBumperTag** | Same as above but only affects objects with a specific tag | Targeted bounce zones |
| **PhysicsForceZone** | Applies forces to tagged Rigidbodies inside a trigger area | Wind zones, jump pads, explosion areas |
| **PhysicsPlatformAnimator** | Moves a platform between waypoints using Rigidbody physics | Moving platforms the player can stand on |
| **PhysicsPlatformStick** | Keeps the player attached to a moving platform | Required companion to PhysicsPlatformAnimator |

---

## Puzzles
*Add these to build switch-based puzzles.*

| Component | What it does | Basic use |
|---|---|---|
| **PuzzleSwitch** | A switch that can be activated and deactivated | Any interactive switch or button |
| **PuzzleSwitchChecker** | Fires an event when a specific set of switches are all activated | Combination puzzles, all-on solutions |
| **PuzzleSequenceChecker** | Fires an event when switches are activated in the correct order | Sequence puzzles, ordered combinations |

---

## UI & Utilities

| Component | What it does | Basic use |
|---|---|---|
| **FadeInFromBlackOnRestart** | Fades the screen in from black every time the scene loads | Scene transitions, respawn fade-in |

---

## The Basic Pattern

Every interaction in EGTK follows the same structure:

```
[Something happens]  →  [Something responds]
   Input component          Action component
   (fires an event)         (receives the event)
```

**Example**: Player walks into a zone → a sound plays
1. Add `InputTriggerZone` to an empty GameObject with a Trigger Collider
2. Add `ActionPlaySound` to any GameObject with an AudioSource
3. In `InputTriggerZone` → `onEnter` → drag in the `ActionPlaySound` object → select `PlaySound()`
