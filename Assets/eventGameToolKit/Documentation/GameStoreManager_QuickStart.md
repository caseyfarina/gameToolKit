# GameStoreManager — Quick Start

`GameStoreManager` creates a fully self-contained in-game store: scrollable item list, buy buttons, balance display, and optional music switching. Students configure items and wire purchase events in the Inspector — no code required.

---

## Minimal Setup

1. Create an empty GameObject and add **GameStoreManager**
2. Assign the **Currency Source** (a `GameCollectionManager` tracking the player's currency)
3. Add items in the **Store Items** list
4. Press Play — the store opens with the B key by default

---

## Store Items

Each item in the list has:

| Field | Purpose |
|---|---|
| **Item Name** | Display name shown in the store panel |
| **Icon** | Optional sprite shown on the left of the row |
| **Price** | Cost deducted from the currency source on purchase |
| **Max Purchases** | `0` = unlimited (consumable), `1` = one-time unlock, `3` = up to three times |
| **Persist Purchase** | Whether the purchase count survives scene loads (see Persistence below) |
| **On Purchased** | Fires when the item is successfully bought |
| **On Cannot Afford** | Fires when the player tries to buy but lacks funds |

### On Purchased — Important

Wire `onPurchased` to **game-world effects only**: enabling a spawner, increasing jump height, unlocking an ability. Sound effects and UI feedback are handled automatically by the store. This event also re-fires on scene load for already-purchased persistent items, so game state is correctly restored.

---

## Open Mode

| Mode | Behavior |
|---|---|
| **Key** | Press a key (default: B) to toggle the store from anywhere |
| **Event Only** | Call `OpenStore()` or `ToggleStore()` from a UnityEvent |

**Walk-up shopkeeper setup (Event Only):**
1. Set Open Mode → Event Only on the GameStoreManager
2. Add `InputTriggerZone` near the shopkeeper
3. Wire `onEnter` → `GameStoreManager.OpenStore()`
4. Wire `onExit` → `GameStoreManager.CloseStore()` (optional — the X button also closes)

---

## Character Controllers

Assign the player's controller to freeze movement and release the cursor while the store is open:

- **FP Controller** — `CharacterControllerFP`: disables look input and unlocks cursor
- **CC Controller** — `CharacterControllerCC`: disables movement

---

## Purchase Limit

| Setting | Effect |
|---|---|
| **One Per Visit** | Each item can only be bought once per store opening, regardless of Max Purchases |
| **On Restart** | What happens to persistent purchases when `RestartScene()` is called (see Persistence) |

---

## Audio

| Field | Purpose |
|---|---|
| **Audio Manager** | `GameAudioManager` for music switching when store opens/closes |
| **Store Music** | Music to play while the store is open |
| **Previous Music** | Music to restore on close |
| **Audio Source** | Source for sound effects (falls back to GetComponent if unassigned) |
| **Purchase Sound** | Plays on successful purchase |
| **Can't Afford Sound** | Plays when the player can't afford an item |

---

## Store UI

Enable **Show UI** to create a self-contained store panel at runtime. The panel is fully configurable:

- **Panel**: title, position, size, background color/sprite
- **Item Rows**: height, spacing, padding, colors
- **Buy Button**: size, colors, sprite
- **Typography**: font sizes, colors, custom font
- **Balance Display**: label prefix and currency symbol

Use the **Show Canvas Preview** button in the Inspector to see the layout without entering Play mode.

---

## Persistence

`GameStoreManager` integrates with the `GameData` persistence system. Each item has a **Persist Purchase** toggle (default: on).

| Setting | Use For |
|---|---|
| **Persist Purchase: on** | Permanent unlocks, upgrades — purchase count survives scene loads |
| **Persist Purchase: off** | Consumables (health packs, ammo) — count resets each scene |

**On scene load**, previously purchased persistent items have their `onPurchased` event re-fired automatically to restore game state (re-enable double jump, re-apply speed upgrades, etc.).

**On Restart** (when `GameSceneManager.RestartScene()` is called):

| On Restart setting | Behavior |
|---|---|
| **Keep Value** (default) | Purchased upgrades survive death — recommended for permanent unlocks |
| **Reset To Default** | All purchase counts clear — use for roguelike-style "lose everything on death" |

---

## Events

| Event | When it fires |
|---|---|
| `onStoreOpened` | Store panel becomes visible |
| `onStoreClosed` | Store panel is hidden |
| `onAnyPurchase` | Any item is successfully purchased |

---

## Common Recipes

### Upgrade shop (permanent unlocks)
- Items: `maxPurchases = 1`, `persistPurchase = true`
- Wire `onPurchased` → game effect (enable ability, increase stat)
- On Restart: Keep Value

### Consumable shop (health packs, ammo)
- Items: `maxPurchases = 0`, `persistPurchase = false`
- Wire `onPurchased` → `GameHealthManager.Heal(25)`
- No persistence needed

### Stackable upgrades (buy up to 3 speed boosts)
- Items: `maxPurchases = 3`, `persistPurchase = true`
- Wire `onPurchased` → `PlayerController.IncreaseSpeed(5)`
- Each upgrade re-fires on scene load — two bought = fires twice = both applied

### Walk-up shopkeeper NPC
- Set Open Mode → Event Only
- Place `InputTriggerZone` collider near the NPC
- Wire `onEnter` → `OpenStore()`, `onExit` → `CloseStore()`
