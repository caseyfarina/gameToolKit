# Game UI Quick Start Guide

**Need a health bar or score counter on screen?** You have two ways to do it. Pick the one that fits your project, but don't mix them!

---

## Which Option Should I Use?

| | Option A: Individual Controls | Option B: GameUIManager |
|---|---|---|
| **Best for** | Most projects — full per-element control over position, size, color, and animation | Projects where you want every display to share one consistent font and color scheme |
| **How it works** | Each manager creates its own UI | One central component displays everything |
| **Setup** | Toggle a checkbox on the manager | Add a separate component and wire events |
| **Customization** | Each display fully styled independently (position, size, gradient, animation per element) | All displays share one font/color scheme — less per-element control |
| **Components needed** | Just the managers you already have | GameUIManager + event wiring |

**Important: Pick one option and stick with it.** If you enable Show UI or Show Bar on GameHealthManager or GameCollectionManager, do NOT also wire those managers to a GameUIManager. You'll get duplicate overlapping displays!

---

## Option A: Individual Controls (Recommended for Most Projects)

The most flexible approach. Every game manager — health, score, timer, and inventory — can create its own UI display with full independent styling and no extra wiring.

### Health Bar + Text in 1 Minute

1. Select your GameObject with **GameHealthManager**
2. In the Inspector, check **Show Bar** under "UI Bar (Optional)"
3. Done! You have a health bar.

Want text too? Also check **Show UI** under "UI Text (Optional)".

#### Customize It

With **Show UI** enabled:
- **Label Prefix** - Change "HP: " to "Health: " or "Lives: " or whatever you want
- **Show Max In Text** - Toggle between `HP: 75` and `HP: 75 / 100`
- **Text Position** - Move it around the screen (top-left origin)
- **Font Size / Color** - Make it your own
- **Animation** - PunchScale (bouncy) or FadeFlash (blinks) when health changes

With **Show Bar** enabled:
- **Bar Position / Size** - Place and size it anywhere
- **Background Color** - The empty part of the bar
- **Fill Color Gradient** - Default goes red (empty) to yellow (half) to green (full)
- **Animate Bar** - Smooth fill animation when health changes

#### Preview Without Playing

Click **Show Canvas Preview** at the bottom of the Inspector to see your UI in the Game view. Adjust settings and watch it update in real-time. Click **Hide Canvas Preview** when you're done.

### Score Counter in 1 Minute

1. Select your GameObject with **GameCollectionManager**
2. Check **Show UI** under "UI Text (Optional)"
3. Done! You have a score counter.

Want a progress bar too? Also check **Show Bar** (requires Max Value > 0).

#### Customize It

Same options as health — label prefix (default "Score: "), position, font, color, animation. The bar also has the same gradient and animation options.

### Example: Health Bar + Score Counter

```
Scene Hierarchy:
  Player
    - GameHealthManager  (Show Bar = ON, Show UI = ON)
    - GameCollectionManager  (Show UI = ON)
```

That's it. No extra GameObjects, no event wiring, no GameUIManager needed.

---

## Option B: GameUIManager (Centralized Display)

Use this when you want a quick, consistent look — one shared font and color scheme across all your UI elements. It's simpler to set up if you don't need per-element styling, but requires manual event wiring and offers less customization than Option A.

### Setup (3 Minutes)

#### Step 1: Add GameUIManager

1. Create an empty GameObject, name it "GameUI"
2. Add Component > **GameUIManager**
3. Toggle which elements you want: Show Score, Show Health Text, Show Health Bar, Show Timer, Show Inventory

#### Step 2: Wire the Events

This is the key step. You need to connect your managers to the UI manager using UnityEvents.

**For Score** (GameCollectionManager):
1. Select your GameCollectionManager
2. Find **On Value Changed** event
3. Click **+** to add a listener
4. Drag your GameUI object into the slot
5. Select **GameUIManager > UpdateScore**

**For Health** (GameHealthManager):
1. Select your GameHealthManager
2. Find **On Health Changed** event
3. Click **+** to add a listener
4. Drag your GameUI object into the slot
5. Select **GameUIManager > UpdateHealth**

**For Timer** (GameTimerManager):
1. Select your GameTimerManager
2. Find the timer update event
3. Wire it to **GameUIManager > UpdateTimer**

**For Inventory** (GameInventorySlot):
1. Wire inventory change event to **GameUIManager > UpdateInventory** or **UpdateInventoryCount**

#### Step 3: Customize

- **UI Layout Settings** - Position each element on screen
- **UI Styling** - One font and color for all text
- **Health Bar Colors** - Three-tier color (green/yellow/red)
- **Animations** - Score punch, health bar smooth fill, timer pulse, inventory bounce

#### Preview

Toggle **Enable Editor Preview** in the Inspector to see the layout in the Game view.

### Example: Full HUD

```
Scene Hierarchy:
  Player
    - GameHealthManager   (Show UI = OFF, Show Bar = OFF)
    - GameCollectionManager   (Show UI = OFF, Show Bar = OFF)
  GameTimer
    - GameTimerManager
  GameUI
    - GameUIManager   (wired to all managers via events)
```

---

## Common Mistakes

**Duplicate UI**: You enabled Show UI on GameHealthManager AND wired it to GameUIManager. Now you have two health displays stacked on top of each other. Pick one approach.

**Bar not showing**: On GameCollectionManager, the bar requires **Max Value > 0**. Set it to your target score. GameHealthManager's bar uses Max Health, which defaults to 100.

**Text not updating**: If using Option B, make sure you wired the events. The GameUIManager doesn't find managers automatically - it needs the event connections.

**Wrong position**: All positions use top-left as origin. X goes right, Y goes down (negative values). Use the Canvas Preview to see where things land before playing.

---

## Quick Decision Guide

- "I just need a health bar" → **Option A**: check Show Bar on GameHealthManager
- "I just need a score counter" → **Option A**: check Show UI on GameCollectionManager
- "I need health + score + timer + inventory" → **Option A**: every manager has its own Show UI toggle
- "I want each element positioned and styled separately" → **Option A**: full per-element control
- "I want everything to match one font/color and don't need per-element styling" → **Option B**: GameUIManager has shared styling
- "I want the quickest possible setup with a consistent look" → **Option B**: one component, one style
