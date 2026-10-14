# EGTK Component Quick Reference

All components work the same way: wire them together using **UnityEvents** in the Inspector.
**Input** components fire events → **Action** components respond to them.

---

## Triggers (Input)
*Add these to a GameObject. They fire events when something happens.*

| Component | What it does | Basic use |
|---|---|---|
| **InputKeyPress** | Fires an event when its bound key or button is pressed | Trigger anything with a keypress |
| **InputOnStart** | Fires an event when the scene starts | Auto-run something on scene load |
| **InputTriggerZone** | Fires events when a tagged object enters/exits a collider | Invisible trigger areas, damage zones, collectibles |
| **InputCheckpointZone** | A trigger zone that also saves a spawn point | Checkpoints and respawn points |
| **InputMouseInteraction** | Fires events when the mouse clicks or hovers over this object | Clickable objects with a free cursor |
| **InputFPMouseInteraction** | Same as above but raycasts from screen center | Clickable objects in a first-person game |
| **InputClickDrag** | Makes this object draggable with the mouse | Drag-and-drop puzzles, sliders |
| **InputClickRotate** | Makes this object rotatable by clicking and dragging | Dials, wheels, rotating puzzles |
| **InputActionEvent** | Fires events from any Unity Input System action | Custom controller/keyboard bindings |
| **InputKeyCountdown** | Counts key presses down to zero, then fires an event | Press-it-five-times doors, mashing to break free |
| **InputInteractionZone** | Shows a prompt sprite when the player enters; fires an event when they press Interact (E / Triangle) | Any "press to interact" mechanic — doors, NPCs, items, puzzles |
| **InputCollisionEnter** | Fires an event on physics collision with a tagged object | Bullet hits, physical triggers |
| **InputQuitGame** | Quits on Escape, or when `QuitGame()` is wired to an event. Stops Play mode in the Editor | Exit buttons, quit menus |

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
| **ActionBlinkDecalOptimized** | Blinks by swapping textures on one material instead of whole materials | Many blinking characters at once |
| **ActionEmissionAnimation** | Animates a material's glow (emission) along a curve | Pulsing pickups, charging effects, alerts |
| **ActionToggle** | Flips the active state of a list of GameObjects; each object flips independently. AllOn() and AllOff() force a known state. | Show/hide groups, toggle hazards, switch between panels |
| **ActionDestroyObject** | Destroys one or more GameObjects with an optional delay. Fires onBeforeDestroy first (use for effects). No targets = destroys self. | Click to remove, death cleanup, collectible removal |

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
| **SpawnPoint** | Marks where the player appears when a scene loads | Level entrances, several doors into one scene |
| **GameFlagManager** | Named true/false flags that survive scene loads. `SetFlag("name")` from any event | Remembering opened doors, collected pickups |
| **GameFlagListener** | Reacts to a flag: already set when the scene loads, or set during play | Keeping a door open when the player returns |
| **GameStoreManager** | In-game store with purchasable items, affordability feedback, and pause-on-open. Connect to a GameCollectionManager for currency. | Upgrade shops, unlock screens, item vendors |

---


## Character Controllers
*Add one to your player or enemy GameObject.*

| Component | What it does | Basic use |
|---|---|---|
| **CharacterControllerFP** | First-person player controller with mouse look, jump, and sprint | First-person games |
| **CharacterControllerCC** | Third-person player controller with rotation and jump | Third-person / top-down games |
| **CharacterController2D** | Sprite player controller, Platformer or Top-Down. See Making a 2D Game below | 2D games |
| **CharacterPushRigidBody** | Lets a CharacterControllerCC player push Rigidbodies | Pushable crates and barrels |
| **PhysicsBallPlayerController** | Physics-based ball controller | Rolling ball games |
| **PhysicsCharacterController** | Rigidbody-based character controller | Physics-heavy platformers |
| **EnemyControllerCC** | AI enemy that chases the player using NavMesh | Basic enemy AI |
| **PhysicsEnemyController** | Rigidbody-based enemy that chases the player | Physics-based enemy AI |

---

## Physics
*Add these to objects in the world.*

| Component | What it does | Basic use |
|---|---|---|
| **PhysicsBumper** | Bounces Rigidbodies that hit it. Set Bumper Tag to limit which, or leave it empty for all | Pinball bumpers, bounce pads |
| **PhysicsBumper2D** | The same for sprites with a Rigidbody 2D | 2D bumpers and bounce pads |
| **PhysicsForceZone** | Applies forces to tagged Rigidbodies inside a trigger area | Wind zones, jump pads, explosion areas |
| **PhysicsForceZone2D** | The same for sprites with a Rigidbody 2D | 2D wind, updrafts, conveyor belts |
| **PhysicsPlatformAnimator** | Moves a platform between waypoints; character controllers ride it | Moving platforms the player can stand on |
| **PhysicsPlatformStick** | Keeps the player attached to a moving platform | Required companion to PhysicsPlatformAnimator |
| **PhysicsPlatformStick2D** | Lets a CharacterController2D ride a moving platform. Goes on the player | 2D moving platforms |

---

## Puzzles
*Add these to build switch-based puzzles.*

| Component | What it does | Basic use |
|---|---|---|
| **PuzzleSwitch** | A switch with 2–5 states that changes when activated | Any interactive switch, lever, or dial |
| **PuzzleSwitchChecker** | Fires an event when every assigned switch is in its required state | Combination puzzles, all-on solutions |
| **PuzzleSequenceChecker** | Fires an event when switches are activated in the correct order. Wire switches to `Activate()`; `NextState()` does not count | Sequence puzzles, ordered combinations |

---

## UI & Utilities

| Component | What it does | Basic use |
|---|---|---|
| **FadeInFromBlackOnRestart** | Fades the screen in from black every time the scene loads | Scene transitions, respawn fade-in |
| **lockMouseCursorToDisplay** | Confines the mouse cursor to the game window, toggled with Escape | First-person and windowed games |
| **ObjectAttractor** | Pulls its own Rigidbody (or Rigidbody 2D) toward a target, with optional distance falloff | Gravity wells, magnets, homing |

---

## Stop Motion Look

Two ways to get a choppy, stop motion cadence. Pick the one that matches the scope you want.

| Component | What it does | Basic use |
|---|---|---|
| **applicationFPSLimiting** | Locks the **whole game** to a low framerate (`Target FPS`, default 6) | Whole-scene stop motion style — drop one anywhere in the scene |
| **StopMotionPostProcess** | Quantizes **one character's** animation to a target framerate while everything else stays smooth | One stop motion character in a normal-looking game |

**StopMotionPostProcess setup**: put it on the GameObject that has the Animator, and make that
GameObject a **child** of the one with the CharacterController. That way the character still glides
smoothly around the level while only the pose snaps frame to frame. Set `targetFPS` to 12 for the
classic "on twos" look, or 8 for a chunkier Rankin/Bass feel.

**applicationFPSLimiting setup**: drop it on any GameObject and set `Target FPS`. You can also wire
`SetTargetFPS()` to a UnityEvent to change the framerate during play — for example, dropping to 6
FPS during a cutscene and back to 60 for normal gameplay.

---

## Setting a Key

**This changed.** Key components no longer have a key dropdown. They have an **Input
Action** with a binding list:

1. Find **Activation** in the Inspector
2. Click **+** to add a binding
3. Click **Listen**, then press the key you want
4. Click anywhere to finish

A newly added component comes pre-bound to **Space**, so it works before you touch it.

Because it is a real Input Action rather than a fixed key, the same binding also accepts a
gamepad button, and one action can hold several bindings at once — keyboard *and* gamepad
for the same event.

Affects **InputKeyPress** and **InputKeyCountdown**.

> **Upgrading an older project?** Key fields were replaced, not renamed, so any key you
> set previously is gone. The upgraded component's Activation list is **empty** and does nothing
> until you add a binding again.

### Two ways to read input

| Component | Use when |
|---|---|
| **InputKeyPress** | One key on one object. Self-contained, nothing else to set up. |
| **InputActionEvent** | An action shared across objects, from an Input Actions asset. |

Both go through Unity's Input System. The whole toolkit does — the old `Input` class does
not work in Unity 6.3 projects, which are created with Input System only.

---

## Try Everything: the Input Test Scene

**`ExampleScenes/Example2D_InputTest.unity`** has one labelled station for every input
component. Walk the character through the floor stations, and use the mouse and keys for
the row above. Each station toggles a sprite so you can see it fire.

Fastest way to see what each input component does before choosing one.

---

## Making a 2D Game

EGTK works with sprites. The rule is simple: **put a Collider 2D on a sprite instead of a
Collider, and the component figures out the rest.** There is no 2D checkbox to find.

### 2D components

| Component | What it does | Basic use |
|---|---|---|
| **CharacterController2D** | Moves a sprite as a player. Movement Style picks Platformer (gravity + jumping) or Top-Down (free movement) | Any 2D player character |
| **InputMouseInteraction** | Click and hover a sprite | Point-and-click, clickable objects |
| **InputClickDrag** | Drag a sprite with the mouse. Set Drag Plane to **World XY** for 2D | Puzzle pieces, draggable items |
| **InputClickRotate** | Rotate a sprite by dragging | Dials, valves, rotating puzzles |
| **InputTriggerZone** | Fires events when a tagged object enters a 2D trigger | Collectibles, damage zones, doors |
| **InputCheckpointZone** | Saves the player's position when entered | Platformer checkpoints |
| **InputInteractionZone** | Fires when the player is close and presses a key | "Press E to open" doors, NPCs |
| **InputCollisionEnter** | Fires when something collides, with impact strength | Impact sounds, breakables |

### CharacterController2D setup

1. Add **Rigidbody 2D** and a **Capsule Collider 2D** to your sprite
2. Add **Player Input**, set its Actions to `EGTK_InputSystem_Actions`
3. Add **CharacterController2D** and pick a Movement Style
4. For Platformer: put your floor objects on a layer, then set **Ground Layer** to it (include your moving platforms' layer too)

For a character made of several sprites, set **Visual Root** to the parent holding the parts
so it flips as one piece. Tick **Art Faces Left** if the art was drawn facing left.

> **If your character will not jump**, Ground Layer is almost certainly empty or pointing at the
> wrong layer. The Inspector shows a warning when it is unset.

### Also works in 2D

**PhysicsBumper2D**, **PhysicsForceZone2D** and **PhysicsPlatformStick2D** are separate 2D
components. **PhysicsPlatformAnimator** works in 2D unchanged. **ActionSpawnProjectile**,
**ActionRespawnPlayer**, **GameCheckpointManager** and **ObjectAttractor** handle 2D bodies.

### What is not 2D yet

- Enemy controllers, **CharacterPushRigidBody**, and the Decal components
- **InputFPMouseInteraction** — first-person only, no 2D version planned
- **ActionTeleportToTransform** moves a 2D player but is untested with one

### Example scenes

- **Example2D_Platformer** — jumping, collectibles and a score display
- **Example2D_ClickToToggle** — click a sprite to turn another sprite on and off
- **Example2D_PhysicsTest** — bumper, force zone and a rideable moving platform
- **Example2D_TopDown** — Top-Down movement: collect coins, press E to open a door

Both are in `ExampleScenes/`, with Kenney pixel art (public domain) in `ExampleScenes/Art2D/`.

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
3. In `InputTriggerZone` → `On Trigger Enter Event` → drag in the `ActionPlaySound` object → select `Play()`
