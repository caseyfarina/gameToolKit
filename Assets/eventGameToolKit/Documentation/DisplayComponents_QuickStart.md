# Display Components Quick Start Guide

**Need to show text or images on screen?** This guide covers both ActionDisplayText and ActionDisplayImage - get either one working in just 3 minutes! 📝🖼️

## What Are Display Components?

**ActionDisplayText** and **ActionDisplayImage** are UI display components that show messages and images on screen with smooth animations. Both create their own Canvas automatically - no manual UI setup required!

### ActionDisplayText
Perfect for:
- **Tutorial hints** (show until player presses continue)
- **Dialogue systems** (show messages with typewriter effect)
- **Notification messages** (flash for 3 seconds then disappear)
- **Score popups** ("You collected 10 coins!")
- **Objective updates** ("Find the key")

### ActionDisplayImage
Perfect for:
- **Achievement notifications** (show for 3 seconds then disappear)
- **Item pickups** (flash an icon when collecting items)
- **Tutorial images** (show until player presses a button)
- **Cutscene frames** (display images during story moments)
- **Pause overlays** (show/hide manually)

---

## 3-Minute Setup (Text Display)

### Step 1: Add the Component (30 seconds)

1. Create an empty GameObject (`GameObject > Create Empty`)
2. Name it "TextDisplay"
3. Click **Add Component**
4. Search for **"ActionDisplayText"**

**That's it!** The script will create its own Canvas and TextMeshProUGUI at runtime.

### Step 2: Configure the Text (1 minute)

1. Select your TextDisplay GameObject
2. In the Inspector, set:
   - **Default Text**: "Press Space to Continue" (or your message)
   - **Text Position**: `(0, 0)` for center, adjust X/Y to move it
   - **Text Size**: `(800, 200)` pixels (adjust as needed)
   - **Font Size**: `48` (adjust for readability)
   - **Time On Screen**: `3` seconds (how long it stays visible)

### Step 3: Preview and Test (1 minute)

1. Click **Show Preview** button at the bottom of Inspector
2. The text appears in the Scene view - adjust position/size visually
3. Click **Hide Preview** when done

### Step 4: Call from UnityEvent (30 seconds)

Wire up any event to call `DisplayDefaultTextTimed()`:
- Example: InputKeyPress → `onKeyPressed` → ActionDisplayText → `DisplayDefaultTextTimed()`
- Press play and test!

**Result:** Text fades in (optional typewriter effect), stays for 3 seconds, then fades out automatically.

---

## 3-Minute Setup (Image Display)

### Step 1: Add the Component (30 seconds)

1. Create an empty GameObject (`GameObject > Create Empty`)
2. Name it "ImageDisplay"
3. Click **Add Component**
4. Search for **"ActionDisplayImage"**

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

## Three Ways to Display (Both Components)

Both ActionDisplayText and ActionDisplayImage use the **same three-method pattern**:

### 1. DisplayDefaultXxxTimed() - Auto-Hide
**Use when:** You want content to disappear automatically after a duration.

**Example:** Achievement popups, item pickups, notifications

```
Wire: InputKeyPress → onKeyPressed → DisplayDefaultTextTimed() or DisplayDefaultImageTimed()
Result: Shows for 'Time On Screen' seconds, then auto-hides
```

### 2. DisplayDefaultXxx() - Manual Hide
**Use when:** You want full control over when the content disappears.

**Example:** Tutorial screens, pause menus, story text

```
Wire: Start trigger → DisplayDefaultText() or DisplayDefaultImage()
Wire: Button click → HideText() or HideImage()
Result: Shows until you manually call Hide
```

### 3. HideXxx() - Hide Displayed Content
**Use with:** `DisplayDefaultText()` or `DisplayDefaultImage()` to manually hide.

**Respects animations:** Fades out based on settings.

```
Wire: InputKeyPress → onKeyPressed → HideText() or HideImage()
Result: Currently displayed content animates out
```

**Bonus:** `HideTextImmediate()` and `HideImageImmediate()` - Instant hide without animations

---

## Common Scenarios

### Scenario 1: Tutorial Text (Manual Control)

**Goal:** Show tutorial text that stays until player presses Space.

**Setup:**
1. Add ActionDisplayText to an empty GameObject
2. Configure:
   - **Default Text**: "Use WASD to move"
   - **Text Position**: `(0, 300)` (top center)
   - **Text Size**: `(800, 200)`
   - **Font Size**: `48`
   - ✅ **Use Fading**: Enabled
   - **Fade Duration**: `0.5`
   - **Use Typewriter**: Disabled (tutorial should be instant)

3. Wire events:
   - **Show tutorial**: Start trigger → `DisplayDefaultText()` (NOT DisplayDefaultTextTimed)
   - **Hide tutorial**: InputKeyPress (Space) → `HideText()`

**How It Works:**
- Game starts, `DisplayDefaultText()` shows the tutorial
- Text fades in and stays visible indefinitely
- Player reads tutorial at their own pace
- Player presses Space, `HideText()` fades it out

---

### Scenario 2: Score Popup with Typewriter (Auto-Hide)

**Goal:** Show "+10 Points!" message with typewriter effect for 2 seconds.

**Setup:**
1. Add ActionDisplayText
2. Configure:
   - **Default Text**: "+10 Points!"
   - **Text Position**: `(0, 400)` (top center)
   - **Text Size**: `(600, 150)`
   - **Font Size**: `64`
   - **Time On Screen**: `2`
   - ✅ **Use Fading**: Enabled
   - **Fade Duration**: `0.3`
   - ✅ **Use Typewriter**: Enabled
   - **Characters Per Second**: `30` (fast typing)

3. Wire event:
   - GameCollectionManager → `onItemCollected` → `DisplayDefaultTextTimed()`

**How It Works:**
- Player collects item, text fades in
- Characters appear one-by-one (typewriter)
- Stays visible for ~1.4 seconds (2 - 0.3 fade in - 0.3 fade out)
- Fades out automatically

---

### Scenario 3: Achievement Icon (Pop-In Animation)

**Goal:** Show achievement icon that pops in from zero scale.

**Setup:**
1. Add ActionDisplayImage
2. Configure:
   - **Default Image**: Achievement sprite
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
   - GameCollectionManager → `onThresholdReached` → `DisplayDefaultImageTimed()`

**How It Works:**
- Player reaches achievement threshold
- Icon pops in from zero scale while fading in (0.3 seconds)
- Stays fully visible for ~1.4 seconds
- Pops out and fades out (0.3 seconds)

---

### Scenario 4: Pause Overlay (Full Screen Image)

**Goal:** Show semi-transparent overlay when game is paused.

**Setup:**
1. Add ActionDisplayImage
2. Configure:
   - **Default Image**: Semi-transparent black square
   - **Image Position**: `(0, 0)`
   - **Image Size**: `(1920, 1080)` (full screen)
   - ✅ **Use Fading**: Enabled
   - **Fade Duration**: `0.2`
   - **Use Scaling**: Disabled

3. Wire events:
   - GameStateManager → `onPaused` → `DisplayDefaultImage()`
   - GameStateManager → `onUnpaused` → `HideImage()`

**How It Works:**
- Game pauses, overlay fades in and stays
- Game unpauses, overlay fades out
- Manual control for pause/unpause workflow

---

### Scenario 5: Dialogue System with Typewriter

**Goal:** Show dialogue text that types on character-by-character.

**Setup:**
1. Add ActionDisplayText
2. Configure:
   - **Default Text**: "Welcome, traveler. I have a quest for you..."
   - **Text Position**: `(0, -300)` (bottom center)
   - **Text Size**: `(1400, 250)`
   - **Font Size**: `42`
   - **Text Alignment**: Left
   - **Text Color**: White
   - ✅ **Use Fading**: Enabled
   - **Fade Duration**: `0.3`
   - ✅ **Use Typewriter**: Enabled
   - **Characters Per Second**: `20` (natural reading pace)

3. Wire events:
   - NPC Trigger Zone → `onTriggerEnter` → `DisplayDefaultText()`
   - InputKeyPress (Space) → `onKeyPressed` → `HideText()`

**How It Works:**
- Player enters NPC zone, text fades in
- Characters appear one-by-one at natural pace
- Text stays visible until player presses Space to continue
- Perfect for visual novel-style dialogue

---

### Scenario 6: Item Pickup Flash (No Animations)

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
   - InputTriggerZone → `onTriggerEnter` → `DisplayDefaultImageTimed()`

**How It Works:**
- Image appears instantly (no fade)
- Stays for 0.5 seconds
- Disappears instantly
- Fast and snappy for action games

---

## Essential Parameters Reference

### ActionDisplayText Parameters

#### Text Settings
- **Default Text** - Message to display (optional, can pass via code)
- **Text Position** - XY position on screen (0,0 = center)
  - Positive X = right, Negative X = left
  - Positive Y = up, Negative Y = down
- **Text Size** - Width and height in pixels (at 1920x1080 reference)
- **Font Size** - Size of the font (default: 48)
- **Text Alignment** - How text aligns (Center, Left, Right, etc.)
- **Text Color** - Color of the text (default: White)
- **Font** - Optional TMP_FontAsset to use (leave empty for default)

#### Display Duration
- **Time On Screen** - How long text stays visible (used by DisplayDefaultTextTimed)

#### Fade Animation
- **Use Fading** - Enable fade in/out animations
- **Fade Duration** - How long fade animations take (seconds)

#### Typewriter Effect
- **Use Typewriter** - Enable character-by-character reveal
- **Characters Per Second** - Speed of typewriter effect (default: 20)

#### Events
- **onTextDisplayStart** - Fires when text starts showing
- **onTextDisplayComplete** - Fires when text finishes hiding

---

### ActionDisplayImage Parameters

#### Image Settings
- **Default Image** - Sprite to display (assign in Inspector)
- **Image Position** - XY position on screen (0,0 = center)
  - Positive X = right, Negative X = left
  - Positive Y = up, Negative Y = down
  - Examples: `(600, 300)` = top-right, `(-600, -300)` = bottom-left
- **Image Size** - Width and height in pixels (at 1920x1080 reference)

#### Display Duration
- **Time On Screen** - How long image stays visible (used by DisplayDefaultImageTimed)

#### Fade Animation
- **Use Fading** - Enable fade in/out animations
- **Fade Duration** - How long fade animations take (seconds)

#### Scale Animation
- **Use Scaling** - Enable scale in/out animations
- **Start Scale** - Initial scale (0,0,0 = invisible point, good for pop-in)
- **Target Scale** - Scale during display (1,1,1 = normal size)
- **Scale Duration** - How long scale animations take (seconds)

#### Events
- **onImageDisplayStart** - Fires when image starts showing
- **onImageDisplayComplete** - Fires when image finishes hiding

---

## Public Methods (UnityEvent Callable)

### ActionDisplayText Methods

**Display Methods:**
- `DisplayDefaultTextTimed()` - Show default text, auto-hide after duration
- `DisplayDefaultText()` - Show default text, stays until HideText()
- `DisplayTextTimed(string)` - Show specific message, auto-hide
- `DisplayText(string)` - Show specific message, stays until HideText()
- `DisplayTextTimed(string, float)` - Show with custom duration

**Hide Methods:**
- `HideText()` - Hide with fade-out animation
- `HideTextImmediate()` - Hide instantly without animations

**Runtime Adjustments:**
- `SetDisplayDuration(float)` - Change duration at runtime
- `SetDefaultText(string)` - Change default text at runtime
- `SetTextPosition(Vector2)` - Reposition at runtime
- `SetTextSize(Vector2)` - Resize at runtime
- `SetFontSize(float)` - Change font size at runtime
- `SetTypewriterEffect(bool)` - Enable/disable typewriter at runtime
- `SetTypewriterSpeed(float)` - Adjust typewriter speed at runtime

**Status Check:**
- `IsDisplaying()` - Returns true if text is currently showing

---

### ActionDisplayImage Methods

**Display Methods:**
- `DisplayDefaultImageTimed()` - Show default image, auto-hide after duration
- `DisplayDefaultImage()` - Show default image, stays until HideImage()
- `DisplayImageTimed(Sprite)` - Show specific sprite, auto-hide
- `DisplayImage(Sprite)` - Show specific sprite, stays until HideImage()
- `DisplayImageTimed(Sprite, float)` - Show with custom duration

**Hide Methods:**
- `HideImage()` - Hide with fade-out/scale-out animations
- `HideImageImmediate()` - Hide instantly without animations

**Runtime Adjustments:**
- `SetDisplayDuration(float)` - Change duration at runtime
- `SetDefaultImage(Sprite)` - Change default image at runtime
- `SetImagePosition(Vector2)` - Reposition at runtime
- `SetImageSize(Vector2)` - Resize at runtime

**Status Check:**
- `IsDisplaying()` - Returns true if image is currently showing
- `GetCurrentSprite()` - Returns the currently displayed sprite

---

## Editor Preview Feature

**The preview is your best friend!** 🔍

Both components support live preview in the Unity Editor:

### For ActionDisplayText:
1. Enter **Default Text** in Inspector
2. Click **Show Preview** button
3. The text appears in **Scene view** (not Game view)
4. Adjust **Text Position**, **Text Size**, **Font Size**, etc. - preview updates live
5. Click **Hide Preview** when done

### For ActionDisplayImage:
1. Assign a **Default Image** in Inspector
2. Click **Show Preview** button
3. The image appears in **Scene view** (not Game view)
4. Adjust **Image Position** and **Image Size** - preview updates live
5. Click **Hide Preview** when done

**Tips:**
- Preview shows at 80% opacity (actual display is 100%)
- Works in edit mode only (hides when you press Play)
- Great for positioning content before testing
- Both text and image previews use the same 1920x1080 reference resolution

---

## Common Issues & Solutions

### ❌ Text/Image doesn't appear
- ✅ Check **Default Text** or **Default Image** is set
- ✅ Verify you called `DisplayDefaultTextTimed()` or `DisplayDefaultImageTimed()`
- ✅ Check content isn't off-screen (use preview to verify position)

### ❌ Text/Image appears instantly (no fade)
- ✅ Enable **Use Fading** checkbox
- ✅ Set **Fade Duration** > 0 (try 0.5)

### ❌ Image doesn't scale in
- ✅ Enable **Use Scaling** checkbox
- ✅ Set **Start Scale** to `(0, 0, 0)` for pop-in effect
- ✅ Set **Target Scale** to `(1, 1, 1)` for normal size

### ❌ Text/Image stays forever (won't auto-hide)
- ✅ Use `DisplayDefaultTextTimed()` or `DisplayDefaultImageTimed()` (NOT the non-Timed versions)
- ✅ Check **Time On Screen** is set > 0

### ❌ Text/Image position is wrong
- ✅ Use **Show Preview** to position visually
- ✅ Remember (0,0) is screen center, not corner
- ✅ Use Scene view grid to align precisely

### ❌ Preview doesn't show
- ✅ Assign **Default Text** or **Default Image** first
- ✅ Look in **Scene view**, not Game view
- ✅ Try clicking **Hide Preview** then **Show Preview** again

### ❌ Typewriter effect doesn't work
- ✅ Enable **Use Typewriter** checkbox
- ✅ Set **Characters Per Second** > 0 (try 20)
- ✅ Only works with ActionDisplayText (not ActionDisplayImage)

### ❌ Custom font doesn't appear
- ✅ Make sure you're assigning a **TMP_FontAsset** (not a regular Unity Font)
- ✅ Import TextMeshPro essentials if you haven't already
- ✅ Leave font field empty to use TextMeshPro default

---

## Animation Timing Explained

**Total Duration = Fade In + Display Time + Fade Out**

### For ActionDisplayText with Typewriter:
Example with **Time On Screen = 5** seconds, **Use Typewriter = true**:
- Fade in: 0.5s
- Typewriter: ~2.0s (40 characters ÷ 20 chars/sec)
- Display: 2.0s (5 - 0.5 - 0.5 - 2.0 typewriter = 2.0)
- Fade out: 0.5s
- **Total**: 5 seconds

### For ActionDisplayImage with Scaling:
Example with **Time On Screen = 3** seconds:
- Fade in + Scale in: 0.5s (simultaneous)
- Display: 2.0s (3 - 0.5 - 0.5 = 2.0)
- Fade out + Scale out: 0.5s (simultaneous)
- **Total**: 3 seconds

The scripts automatically calculate display time so the total matches your setting!

---

## Pro Tips

### Multiple Display Components
You can have multiple instances for different purposes:
- One ActionDisplayText for tutorial hints (top)
- One ActionDisplayText for dialogue (bottom)
- One ActionDisplayImage for achievements (top-right)
- One ActionDisplayImage for item pickups (center)

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

**Quick Popup (Image):**
- Use Scaling: ✅
- Start Scale: `(0.5, 0.5, 0.5)`
- Target Scale: `(1, 1, 1)`
- Scale Duration: `0.2`
- Use Fading: ✅
- Fade Duration: `0.2`

**Smooth Fade (Both):**
- Use Scaling: ❌
- Use Fading: ✅
- Fade Duration: `0.8` (slower)

**Instant Flash (Both):**
- Use Scaling: ❌
- Use Fading: ❌
- Time On Screen: `0.5` (short)

**Natural Dialogue (Text):**
- Use Fading: ✅
- Fade Duration: `0.3`
- Use Typewriter: ✅
- Characters Per Second: `20` (reading pace)

---

## Next Steps

**You're ready to display text and images!** 🎉

Try these challenges:
1. Create a tutorial system with multiple text hints that advance on Space press
2. Make an achievement system that shows icons for 2 seconds
3. Add item pickup text with typewriter effect
4. Create a pause menu overlay with fade in/out
5. Build a dialogue system that shows NPC text with character-by-character reveal

**Remember:**
- Use `DisplayDefaultXxxTimed()` for auto-hide (achievements, pickups, notifications)
- Use `DisplayDefaultXxx()` + `HideXxx()` for manual control (tutorials, menus, dialogue)
- Use **Show Preview** to position content visually
- All methods respect your animation settings!
- Typewriter effect is unique to ActionDisplayText

**Need more control?** Call methods with specific text/sprites using `DisplayTextTimed(string)` or `DisplayImageTimed(Sprite)` from code!

**Want sequences?** Check out ActionDialogueSequence for complete dialogue system with character portraits and multiple lines!
