# InputInteractionZone — Quick Start

Adds a "press to interact" zone or a clickable object to your scene in one component.
Choose **Proximity mode** (player walks into a trigger area and presses Interact) or **Mouse mode** (cursor hovers over the object and clicks).

---

## Proximity Mode — Step by Step

**Use this for**: Doors, NPCs, item pickups, puzzle switches — anything where the player walks up and presses a button.

### 1. Create the zone

1. Create an **Empty GameObject** in your scene (e.g. `DoorInteraction`)
2. Add a **Collider** component — Box, Sphere, or Capsule. Check **Is Trigger**.
3. Add the **InputInteractionZone** component

### 2. Configure it

| Field | What to set |
|---|---|
| **Interaction Mode** | Proximity |
| **Player Tag** | `Player` (must match your player's tag) |
| **Interact Action** | Drag in your `InputSystem_Actions` asset → select the `Interact` action |
| **Show Prompt** | On — drag in a button icon sprite |

> **No Input Action asset?** Leave Interact Action empty and the fallback key (default: **E**) will be used instead.

### 3. Wire the event

In the **On Interact** event at the bottom of the component, click **+** and drag in the GameObject or component you want to trigger.

**Example**: Player enters zone → door opens
- On Interact → `ActionPlatformAnimator.Play()`

### 4. Optional prompt settings

| Setting | What it does |
|---|---|
| **Sprite** | The icon shown above the zone (e.g. a button prompt image) |
| **Offset** | Where the sprite appears relative to the zone center (default: 2 units up) |
| **Size** | Scale of the sprite in world units |
| **Orientation** | **Face Camera** — always faces the player. **Fixed World** — stays at the rotation you set |
| **Animation** | **Fade In**, **Scale In**, or **Both** |
| **Enable Hover** | Slowly bobs the sprite up and down while visible |
| **Enable Glow** | Adds a pulsing point light under the sprite |

---

## Mouse Mode — Step by Step

**Use this for**: Point-and-click interactions, inventory items, objects in a free-cursor game.

### 1. Set up the object

1. Select the **GameObject** the player will click (e.g. a chest, a button, a prop)
2. Make sure it has a **Collider** (does not need to be a trigger)
3. Add the **InputInteractionZone** component

### 2. Configure it

| Field | What to set |
|---|---|
| **Interaction Mode** | Mouse |
| **Mouse Button** | 0 = Left click, 1 = Right click, 2 = Middle click |
| **Camera** | Leave empty to use Camera.main, or drag in a specific camera |
| **Max Distance** | How far the raycast reaches (default: 100) |
| **Layer Mask** | Which layers the cursor can detect (default: Everything) |

### 3. Wire the event

Same as Proximity — use the **On Interact** event to connect whatever should happen on click.

**Example**: Player clicks a chest → spawn an item
- On Interact → `ActionSpawnObject.Spawn()`

### 4. Hover events

In Mouse mode, **On Hover Enter** and **On Hover Exit** fire when the cursor moves onto or off the object. Use these to change cursors, highlight UI, or play sounds.

---

## All Events

| Event | Proximity fires when... | Mouse fires when... |
|---|---|---|
| **On Enter** | Player walks into the zone | Cursor moves onto the object |
| **On Exit** | Player walks out of the zone | Cursor moves off the object |
| **On Interact** | Player presses Interact (or E) while in zone | Player clicks the object |

---

## Common Patterns

### Door that opens on interact

```
Player enters zone
  → On Enter: ActionPlaySound.PlaySound()       (door creak hint)
  → On Interact: ActionPlatformAnimator.Play()  (door slides open)
  → On Exit: (nothing)
```

### NPC dialogue

```
Player enters zone
  → On Interact: ActionDialogueSequence.Play()
```

### Clickable collectible (Mouse mode)

```
Player clicks item
  → On Interact: GameInventoryManager.AddItem("Key", 1)
  → On Interact: ActionSpawnObject.Despawn()
```

### Puzzle switch (Proximity mode)

```
Player presses Interact
  → On Interact: PuzzleSwitch.Toggle()
```

---

## Tips

- **Prompt sprite**: Use a transparent PNG with a button icon (e.g. `[E]` or a controller icon). Works best at 512×512 or smaller.
- **Proximity zone size**: Make the collider slightly larger than the visible object so the prompt appears before the player is right on top of it.
- **Mouse mode + first-person**: Use **InputFPMouseInteraction** instead — it raycasts from the screen center, not the cursor position.
- **Multiple interactions**: Add multiple `InputInteractionZone` components to different GameObjects, each with its own prompt and event — they work independently.
- **Test Interact button**: In Play mode, a **Test Interact** button appears in the Inspector. Use it to fire the interact event directly without needing a player nearby.
