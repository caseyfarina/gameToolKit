# ActionDisplayImage Quick Start Guide

**Need to show images on screen?** This guide will get you displaying images with animations in just 3 minutes! 🖼️

## What is ActionDisplayImage?

ActionDisplayImage displays UI images on screen with smooth fade and scale animations. It creates its own Canvas automatically - no manual UI setup required!

Perfect for:
- **Achievement notifications** (show for 3 seconds then disappear)
- **Item pickups** (flash an icon when collecting items)
- **Tutorial images** (show until player presses a button)
- **Cutscene frames** (display images during story moments)
- **Pause overlays** (show/hide manually)

---

## 3-Minute Setup (Timed Display)

### Step 1: Add the Component (30 seconds)

1. Create an empty GameObject (`GameObject > Create Empty`)
2. Name it "ImageDisplay"
3. Click **Add Component**
4. Search for **"ActionDisplayImage"**

**That's it!** The script will create its own Canvas and Image at runtime.

### Step 2: Configure the Image (1 minute)

1. Select your ImageDisplay GameObject
2. In the Inspector, set:
   - **Default Image**: Drag your sprite here (e.g., achievement icon)
   - **Image Position**: `(0, 0)` for center, adjust X/Y to move it
   - **Image Size**: `(400, 400)` pixels (adjust as needed)
   - **Time On Screen**: `3` seconds (how long it stays visible)

### Step 3: Preview and Test (1 minute)

1. Click **Show Preview** button at the bottom of Inspector
2. The image appears in the Scene view - adjust position/size visually
3. Click **Hide Preview** when done

### Step 4: Call from UnityEvent (30 seconds)

Wire up any event to call `DisplayDefaultImageTimed()`:
- Example: InputKeyPress → `onKeyPressed` → ActionDisplayImage → `DisplayDefaultImageTimed()`
- Press play and test!

**Result:** Image fades in, stays for 3 seconds, then fades out automatically.

---

## Common Scenarios

### Scenario 1: Achievement Popup (Auto-Hide)

**Goal:** Show an achievement icon for 2 seconds with a pop-in effect.

**Setup:**
1. Add ActionDisplayImage to an empty GameObject
2. Configure:
   - **Default Image**: Your achievement sprite
   - **Image Position**: `(600, 300)` (top-right corner)
   - **Image Size**: `(300, 300)`
   - **Time On Screen**: `2`
   - ✅ **Use Fading**: Enabled
   - **Fade Duration**: `0.3`
   - ✅ **Use Scaling**: Enabled
   - **Start Scale**: `(0, 0, 0)` (starts at zero)
   - **Target Scale**: `(1, 1, 1)` (grows to full size)
   - **Scale Duration**: `0.3`

3. Wire event:
   - GameCollectionManager → `onItemCollected` → ActionDisplayImage → `DisplayDefaultImageTimed()`

**How It Works:**
- When player collects an item, the achievement icon pops in from zero scale
- Fades in simultaneously over 0.3 seconds
- Stays fully visible for ~1.4 seconds (2 - 0.3 - 0.3)
- Pops out and fades out over 0.3 seconds

---

### Scenario 2: Tutorial Image (Manual Control)

**Goal:** Show a tutorial image that stays until player presses a button.

**Setup:**
1. Add ActionDisplayImage to an empty GameObject
2. Configure:
   - **Default Image**: Your tutorial sprite
   - **Image Position**: `(0, 0)` (center screen)
   - **Image Size**: `(800, 600)`
   - **Time On Screen**: `5` (doesn't matter for manual mode)
   - ✅ **Use Fading**: Enabled
   - **Fade Duration**: `0.5`
   - **Use Scaling**: Disabled (tutorial images don't need scaling)

3. Wire events:
   - **Show tutorial**: Start trigger → `DisplayDefaultImage()` (NOT DisplayDefaultImageTimed)
   - **Hide tutorial**: InputKeyPress (Space) → `HideImage()`

**How It Works:**
- Game starts, `DisplayDefaultImage()` shows the tutorial
- Image fades in and stays visible indefinitely
- Player reads tutorial at their own pace
- Player presses Space, `HideImage()` fades it out

**Why not DisplayDefaultImageTimed?**
- `DisplayDefaultImageTimed()` auto-hides after duration
- `DisplayDefaultImage()` stays forever until you call `HideImage()`

---

### Scenario 3: Item Pickup Flash (No Animations)

**Goal:** Instant image display for fast-paced games (no fade, just flash).

**Setup:**
1. Add ActionDisplayImage
2. Configure:
   - **Default Image**: Item icon
   - **Image Position**: `(0, 400)` (top center)
   - **Image Size**: `(200, 200)`
   - **Time On Screen**: `0.5` (very short)
   - ❌ **Use Fading**: Disabled (instant on/off)
   - ❌ **Use Scaling**: Disabled

3. Wire event:
   - InputTriggerZone → `onTriggerEnter` → ActionDisplayImage → `DisplayDefaultImageTimed()`

**How It Works:**
- Image appears instantly (no fade)
- Stays for 0.5 seconds
- Disappears instantly
- Fast and snappy for action games

---

### Scenario 4: Pause Overlay (Full Screen)

**Goal:** Show a semi-transparent overlay when game is paused.

**Setup:**
1. Add ActionDisplayImage
2. Configure:
   - **Default Image**: Semi-transparent black square (create in Photoshop/GIMP)
   - **Image Position**: `(0, 0)`
   - **Image Size**: `(1920, 1080)` (full screen at reference resolution)
   - ✅ **Use Fading**: Enabled
   - **Fade Duration**: `0.2` (quick fade)
   - **Use Scaling**: Disabled

3. Wire events:
   - GameStateManager → `onPaused` → ActionDisplayImage → `DisplayDefaultImage()`
   - GameStateManager → `onUnpaused` → ActionDisplayImage → `HideImage()`

**How It Works:**
- Game pauses, overlay fades in and stays visible
- Game unpauses, overlay fades out
- Manual control for pause/unpause workflow

---

## Essential Parameters Reference

### Image Settings
- **Default Image** - Sprite to display (assign in Inspector)
- **Image Position** - XY position on screen (0,0 = center)
  - Positive X = right, Negative X = left
  - Positive Y = up, Negative Y = down
  - Examples: `(600, 300)` = top-right, `(-600, -300)` = bottom-left
- **Image Size** - Width and height in pixels (at 1920x1080 reference)

### Display Duration
- **Time On Screen** - How long image stays visible (used by DisplayDefaultImageTimed)

### Fade Animation
- **Use Fading** - Enable fade in/out animations
- **Fade Duration** - How long fade animations take (seconds)

### Scale Animation
- **Use Scaling** - Enable scale in/out animations
- **Start Scale** - Initial scale (0,0,0 = invisible point, good for pop-in)
- **Target Scale** - Scale during display (1,1,1 = normal size)
- **Scale Duration** - How long scale animations take (seconds)

### Events
- **onImageDisplayStart** - Fires when image starts showing
- **onImageDisplayComplete** - Fires when image finishes hiding

---

## Three Ways to Display Images

### 1. DisplayDefaultImageTimed() - Auto-Hide
**Use when:** You want images to disappear automatically after a duration.

**Example:** Achievement popups, item pickups, notifications

```
Wire: InputKeyPress → onKeyPressed → DisplayDefaultImageTimed()
Result: Shows for 'Time On Screen' seconds, then auto-hides
```

### 2. DisplayDefaultImage() - Manual Hide
**Use when:** You want full control over when the image disappears.

**Example:** Tutorial screens, pause menus, story images

```
Wire: Start trigger → DisplayDefaultImage()
Wire: Button click → HideImage()
Result: Shows until you manually call HideImage()
```

### 3. HideImage() - Hide Displayed Image
**Use with:** `DisplayDefaultImage()` to manually hide.

**Respects animations:** Fades out and scales out based on settings.

```
Wire: InputKeyPress → onKeyPressed → HideImage()
Result: Currently displayed image animates out
```

**Bonus:** `HideImageImmediate()` - Instant hide without animations

---

## Public Methods (UnityEvent Callable)

### Display Methods
- `DisplayDefaultImageTimed()` - Show default image, auto-hide after duration
- `DisplayDefaultImage()` - Show default image, stays until HideImage()
- `DisplayImageTimed(Sprite)` - Show specific sprite, auto-hide
- `DisplayImage(Sprite)` - Show specific sprite, stays until HideImage()

### Hide Methods
- `HideImage()` - Hide with fade-out/scale-out animations
- `HideImageImmediate()` - Hide instantly without animations

### Runtime Adjustments
- `SetDisplayDuration(float)` - Change duration at runtime
- `SetDefaultImage(Sprite)` - Change default image at runtime
- `SetImagePosition(Vector2)` - Reposition at runtime
- `SetImageSize(Vector2)` - Resize at runtime

---

## Editor Preview Feature

**The preview is your best friend!** 🔍

1. Assign a **Default Image** in Inspector
2. Click **Show Preview** button
3. The image appears in **Scene view** (not Game view)
4. Adjust **Image Position** and **Image Size** - preview updates live
5. Click **Hide Preview** when done

**Tips:**
- Preview shows at 80% opacity (actual display is 100%)
- Works in edit mode only (hides when you press Play)
- Great for positioning images before testing

---

## Common Issues & Solutions

### ❌ Image doesn't appear
- ✅ Check **Default Image** is assigned
- ✅ Verify you called `DisplayDefaultImageTimed()` or `DisplayDefaultImage()`
- ✅ Check image isn't off-screen (use preview to verify position)

### ❌ Image appears instantly (no fade)
- ✅ Enable **Use Fading** checkbox
- ✅ Set **Fade Duration** > 0 (try 0.5)

### ❌ Image doesn't scale in
- ✅ Enable **Use Scaling** checkbox
- ✅ Set **Start Scale** to `(0, 0, 0)` for pop-in effect
- ✅ Set **Target Scale** to `(1, 1, 1)` for normal size

### ❌ Image stays forever (won't auto-hide)
- ✅ Use `DisplayDefaultImageTimed()` (NOT `DisplayDefaultImage()`)
- ✅ Check **Time On Screen** is set > 0

### ❌ Image position is wrong
- ✅ Use **Show Preview** to position visually
- ✅ Remember (0,0) is screen center, not corner
- ✅ Use Scene view grid to align precisely

### ❌ Preview doesn't show
- ✅ Assign a **Default Image** first (preview needs a sprite)
- ✅ Look in **Scene view**, not Game view
- ✅ Try clicking **Hide Preview** then **Show Preview** again

---

## Animation Timing Explained

**Total Duration = Fade In + Display Time + Fade Out**

Example with **Time On Screen = 3** seconds:
- Fade in: 0.5s
- Display: 2.0s (3 - 0.5 - 0.5 = 2.0)
- Fade out: 0.5s
- **Total**: 3 seconds

The script automatically calculates display time so the total matches your setting!

**If scaling is enabled:**
- Uses the **longer** of fade duration or scale duration
- Example: Fade 0.5s, Scale 0.8s → uses 0.8s for timing calculation

---

## Pro Tips

### Multiple Display Scripts
You can have multiple ActionDisplayImage components for different purposes:
- One for achievements (top-right)
- One for item pickups (center)
- One for tutorial overlays (full screen)

Each one is independent and can be triggered separately!

### Positioning Cheat Sheet (1920x1080 reference)
- **Center**: `(0, 0)`
- **Top-right corner**: `(600, 400)`
- **Top-left corner**: `(-600, 400)`
- **Bottom-right**: `(600, -400)`
- **Bottom-left**: `(-600, -400)`
- **Top center**: `(0, 400)`
- **Bottom center**: `(0, -400)`

### Animation Presets

**Quick Popup:**
- Use Scaling: ✅
- Start Scale: `(0.5, 0.5, 0.5)`
- Target Scale: `(1, 1, 1)`
- Scale Duration: `0.2`
- Use Fading: ✅
- Fade Duration: `0.2`

**Smooth Fade:**
- Use Scaling: ❌
- Use Fading: ✅
- Fade Duration: `0.8` (slower)

**Instant Flash:**
- Use Scaling: ❌
- Use Fading: ❌
- Time On Screen: `0.5` (short)

---

## Next Steps

**You're ready to display images!** 🎉

Try these challenges:
1. Create an achievement system that shows icons for 2 seconds
2. Make a tutorial overlay that stays until player presses Space
3. Add item pickup icons that pop in when collecting items
4. Create a pause menu overlay with fade in/out

**Remember:**
- Use `DisplayDefaultImageTimed()` for auto-hide (achievements, pickups)
- Use `DisplayDefaultImage()` + `HideImage()` for manual control (tutorials, menus)
- Use **Show Preview** to position images visually
- All methods respect your animation settings!

**Need more control?** Call methods with specific sprites using `DisplayImageTimed(Sprite)` or `DisplayImage(Sprite)` from code!
