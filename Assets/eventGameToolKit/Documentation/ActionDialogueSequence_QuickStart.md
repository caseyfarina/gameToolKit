# ActionDialogueSequence Quick Start Guide

**Complete dialogue and visual novel system for Unity**

Don't worry if this seems complex at first! Start with the [5-Minute Quick Start](#5-minute-quick-start) below, then explore advanced features when you're ready.

---

## Table of Contents

1. [5-Minute Quick Start](#5-minute-quick-start)
2. [Basic Dialogue Setup](#basic-dialogue-setup)
3. [Adding Character Portraits](#adding-character-portraits)
4. [Animation Options](#animation-options)
5. [Decision System](#decision-system)
6. [Click-Through & Controls](#click-through--controls)
7. [Visual Customization](#visual-customization)
8. [Common Scenarios](#common-scenarios)
9. [Troubleshooting](#troubleshooting)

---

## 5-Minute Quick Start

**Goal:** Get a simple dialogue working in under 5 minutes.

### Step 1: Create the Dialogue Object (30 seconds)

1. **Right-click in Hierarchy** → Create Empty
2. **Name it:** "MyDialogue"
3. **Add Component** → Search "ActionDialogueSequence"
4. **Add Component** → Search "DialogueUIController" (companion script)

### Step 2: Add Your First Dialogue Line (2 minutes)

1. Find **"Dialogue Content"** section in Inspector
2. Click **"Dialogue Lines"** to expand
3. Change **Size** to `1`
4. Set **Element 0:**
   - **Orientation:** Left
   - **Dialogue Text:** "Welcome to my game!"
   - **Display Time:** 3

### Step 3: Preview It (1 minute)

1. Scroll down to **"Editor Preview"** section
2. Click **"Show Canvas Preview"**
3. Look at your **Scene view** - you'll see the dialogue!
4. Adjust **Text Position** and **Text Box Size** in **Visual Settings** to reposition

### Step 4: Make It Play (1 minute)

1. In **"Playback Settings"** section:
   - ✅ Check **"Play On Start"**

2. **Press Play** in Unity - your dialogue appears!

**🎉 You're done!** You have a working dialogue system. Now let's make it better...

---

## Basic Dialogue Setup

### Adding Multiple Dialogue Lines

Each dialogue line is independent and can have different text, timing, and portraits.

**Example: 3-line conversation**

```
Dialogue Lines: Size = 3

Element 0:
  Orientation: Left
  Dialogue Text: "Hello there!"
  Display Time: 2
  Character Image: (none for now)

Element 1:
  Orientation: Right
  Dialogue Text: "Hi! How are you?"
  Display Time: 2
  Character Image: (none for now)

Element 2:
  Orientation: Left
  Dialogue Text: "I'm doing great, thanks!"
  Display Time: 3
  Character Image: (none for now)
```

### Playback Settings Explained

- **Play On Start**: Dialogue starts automatically when scene loads
- **Loop**: When dialogue ends, restart from beginning (useful for ambient NPC dialogue)
- **Enable Click Through**: Player can click or press any key to advance dialogue

**Tip:** Turn off "Play On Start" and call `StartDialogue()` from a button or trigger instead!

---

## Adding Character Portraits

Character portraits bring your dialogue to life with visual representation of who's speaking.

### Step 1: Prepare Your Images

1. **Drag your character images** into Unity (PNG with transparency works best)
2. **Change Texture Type** to "Sprite (2D and UI)" in Inspector
3. Click **Apply**

### Step 2: Assign to Dialogue Lines

```
Element 0:
  Orientation: Left          ← Image appears on left side
  Dialogue Text: "Hello!"
  Display Time: 2
  Character Image: Hero_Portrait    ← Drag sprite here

Element 1:
  Orientation: Right         ← Image appears on right side
  Dialogue Text: "Greetings!"
  Display Time: 2
  Character Image: NPC_Portrait     ← Drag sprite here
```

### Step 3: Adjust Portrait Positions

In **"Visual Settings" > "Character Images"**:

- **Left Image Position:** `(-400, -100)` - left side of screen
- **Right Image Position:** `(400, -100)` - right side of screen
- **Image Size:** `(300, 300)` - adjust for your sprites

**Preview tip:** Use "Show Canvas Preview" and scrub the slider to see different lines!

---

## Animation Options

Make your dialogue more dynamic with built-in animations.

### Image Animations

**Animation Settings > Image Animation:**

- **None**: Images appear instantly (fastest)
- **FadeIn**: Smooth fade from transparent to visible
- **SlideUpFromBottom**: Image slides up into position (cinematic)
- **SlideInFromSide**: Slides from left/right (dynamic)
- **PopIn**: Quick scale-up effect (playful)

**Settings to adjust:**
- **Slide Duration / Fade In Duration**: How long animation takes (0.2s = snappy, 0.5s = smooth)
- **Fade Out Duration**: How long to fade out when switching lines
- **Slide Distance**: How far images slide (500 = half screen)
- **Slide Easing**: Animation curve (OutQuad = smooth, OutBack = bouncy)

### Text Animations

**Animation Settings > Text Animation:**

- **None**: Text appears instantly
- **TypeOn**: Classic typewriter effect (immersive!)
- **FadeIn**: Text fades in smoothly
- **SlideUpFromBottom**: Text slides up (dramatic reveals)

**Settings to adjust:**
- **Characters Per Second**: Typewriter speed (30 = readable, 60 = fast)
- **Fade In Duration**: Text fade speed
- **Fade Out Duration**: Text fade out speed

**Pro Tip:** Combine `SlideUpFromBottom` for images with `TypeOn` for text - looks amazing!

---

## Decision System

Give players choices that affect your game!

### Basic Decision Setup

1. Enable **"Decision System (Optional)"**
2. ✅ Check **"Enable Decision"**
3. Set **"Decision Choices"** Size to `2` (for two choices)

**Example: Simple yes/no choice**

```
Decision Choice - Element 0:
  Choice Text: "Yes, I'll help you!"
  Choice Image: (optional icon)
  On Choice Selected:
    ↳ Wire to your event (e.g., start quest)

Decision Choice - Element 1:
  Choice Text: "No, sorry."
  Choice Image: (optional icon)
  On Choice Selected:
    ↳ Wire to your event (e.g., end conversation)
```

### Wiring Decision Events

**Example: Two doors choice**

```
Decision Choice 0: "Go through the red door"
  On Choice Selected:
    ↳ RedDoor GameObject → SetActive(true)
    ↳ Player → Transform.SetPosition(to red room)

Decision Choice 1: "Go through the blue door"
  On Choice Selected:
    ↳ BlueDoor GameObject → SetActive(true)
    ↳ Player → Transform.SetPosition(to blue room)
```

### Decision Panel Settings

Customize the look of your decision buttons:

- **Panel Position**: `(0, -200)` puts buttons at bottom-center
- **Button Size**: `(400, 100)` - width x height of each button
- **Button Spacing**: `20` - gap between stacked buttons
- **Choice Image Size**: `(80, 80)` - size of optional icons
- **Font Size**: `36` - text size on buttons
- **Button Opacity**: `0.9` - transparency (0 = invisible, 1 = solid)

### Keyboard/Gamepad Navigation

Decisions automatically support:
- **Keyboard**: Up/Down arrows or W/S to select, Enter/Space to confirm
- **Gamepad**: D-pad or Left Stick to select, South Button (A/X) to confirm
- **Mouse**: Click any button

**Visual Feedback:** Selected button highlights brighter automatically!

---

## Click-Through & Controls

### Enable Click-Through

In **"Playback Settings"**:
- ✅ Check **"Enable Click Through"**

Now players can advance dialogue by:
- Clicking mouse
- Pressing any keyboard key
- Pressing gamepad A button

**Smart behavior:**
- If typewriter is active: first click completes typing, second click advances
- If text is fully shown: click advances to next line

### Manual Control (No Click-Through)

Wire a button to control dialogue manually:

1. Create UI Button: GameObject → UI → Button
2. In Button Inspector, find **"On Click ()"** event
3. Drag your **MyDialogue** object to the event slot
4. Select function: **ActionDialogueSequence → NextDialogue()**

Now button clicks advance dialogue instead of automatic clicking!

### Public Methods for Events

Wire these to buttons or triggers:

- **StartDialogue()** - Begin playing from first line
- **NextDialogue()** - Advance to next line (or skip typewriter)
- **StopDialogue()** - Stop and hide dialogue immediately
- **PauseDialogue()** - Pause at current line
- **ResumeDialogue()** - Continue from paused state

---

## Visual Customization

### Background Image

Add a dialogue box background:

1. Import your dialogue box sprite (e.g., rounded rectangle)
2. In **"Visual Settings" > "Background Image"**:
   - **Image**: Drag your sprite
   - **Background Position**: `(0, -300)` centers at bottom
   - **Background Size**: `(1400, 300)` width x height

### Custom Fonts

Use your own fonts for dialogue:

1. Import TrueType font (.ttf) into Unity
2. Right-click font → Create → TextMeshPro → Font Asset
3. In **"Visual Settings" > "Dialogue Text"**:
   - **Custom Font**: Drag your TMP Font Asset

**Same font applies to dialogue text AND decision buttons!**

### Text Positioning

Fine-tune text placement:

- **Text Position**: `(0, -300)` for bottom-center
- **Text Box Size**: `(1200, 200)` - wider = more text fits
- **Font Size**: `48` - bigger = more readable, smaller = more text fits

**Alignment tip:** Text is center-aligned by default. Modify in code if needed.

---

## Common Scenarios

### Scenario 1: Tutorial Pop-Up

**Use Case:** Quick tutorial message on level start

```
Setup:
- Dialogue Lines: Size = 1
- Element 0:
  - Text: "Use WASD to move!"
  - Display Time: 3
  - No character image

Playback Settings:
- ✅ Play On Start
- ❌ Loop
- ✅ Enable Click Through

Result: Message shows for 3 seconds, player can skip with click
```

### Scenario 2: NPC Conversation

**Use Case:** Talk to shopkeeper

```
Setup:
- Dialogue Lines: Size = 4
- Line 0: Shopkeeper says "Welcome!" (Right, 2s)
- Line 1: Player says "What do you sell?" (Left, 2s)
- Line 2: Shopkeeper says "Potions and weapons!" (Right, 2s)
- Line 3: Player says "I'll take a look." (Left, 2s)

Playback Settings:
- ❌ Play On Start (triggered by talking to NPC)
- ❌ Loop
- ✅ Enable Click Through

Trigger:
- Attach InputTriggerZone to NPC
- Wire onTriggerEnter → MyDialogue.StartDialogue()
```

### Scenario 3: Visual Novel Scene

**Use Case:** Story cutscene with choices

```
Setup:
- Dialogue Lines: Size = 5
- Each line has character portrait
- Animations: SlideInFromSide (images), TypeOn (text)

Decision System:
- ✅ Enable Decision
- Choice 1: "Trust the stranger" → TrustPath.StartDialogue()
- Choice 2: "Walk away" → DistrustPath.StartDialogue()

Playback Settings:
- ✅ Play On Start (or trigger from previous scene)
- ❌ Loop
- ✅ Enable Click Through (let player read at own pace)

Result: Cinematic dialogue with branching story paths
```

### Scenario 4: Looping Ambient NPC

**Use Case:** Background character saying random things

```
Setup:
- Dialogue Lines: Size = 3
- Line 0: "Nice weather today!"
- Line 1: "I love this town."
- Line 2: "Hope you're having fun!"
- Each line: 5 second display

Playback Settings:
- ✅ Play On Start
- ✅ Loop (keeps repeating)
- ❌ Enable Click Through (not interactive)

Result: NPC cycles through lines endlessly
```

---

## Troubleshooting

### ❌ "Dialogue doesn't appear when I press Play"

**Check:**
- ✅ Is "Play On Start" checked in Playback Settings?
- ✅ Do you have at least one dialogue line added (Size = 1 or more)?
- ✅ Is DialogueUIController component attached to same GameObject?
- ✅ Did you save your scene after making changes?

**Fix:** Enable "Show Canvas Preview" in Editor to verify setup before playing.

---

### ❌ "I can't see the dialogue text in Scene view"

**Check:**
- ✅ Is "Show Canvas Preview" button pressed?
- ✅ Are you looking at **Scene view**, not Game view?
- ✅ Is your Main Camera's background blocking the canvas?

**Fix:** Canvas renders at high sorting order (100), but preview uses HideFlags.DontSave so it won't appear in Game view during edit mode.

---

### ❌ "Character portraits are too big/small"

**Check:**
- Go to **Visual Settings > Character Images**
- Adjust **"Image Size"** (default 300x300)

**Fix:**
- Larger sprites: Try (400, 400) or (500, 500)
- Smaller sprites: Try (200, 200) or (150, 150)
- Use "Show Canvas Preview" to see changes instantly!

---

### ❌ "Text is cut off or not centered correctly"

**Check:**
- Go to **Visual Settings > Dialogue Text**
- **Text Box Size** might be too small

**Fix:**
- Increase width: Change to (1400, 200) or wider
- Increase height: Change to (1200, 250) for more vertical space
- Adjust **Text Position** Y value to move up/down

---

### ❌ "Typewriter effect is too slow/fast"

**Check:**
- Go to **Animation Settings > Text Animation**
- If "TypeOn" is selected, find **"Characters Per Second"**

**Fix:**
- Too slow? Increase to 40-60 characters/second
- Too fast? Decrease to 15-25 characters/second
- Sweet spot: 30 characters/second (default)

---

### ❌ "Decision buttons are hard to see"

**Check:**
- Go to **Decision System > Decision Panel Settings**
- **Button Opacity** might be too low

**Fix:**
- Increase **Button Opacity** to 0.95 or 1.0 (fully opaque)
- Increase **Button Size** to make buttons bigger
- Increase **Font Size** to make text more readable

---

### ❌ "Click-through advances too quickly"

**Check:**
- Is **Enable Click Through** turned on?
- Are you clicking rapidly during typewriter effect?

**Fix:**
- First click completes typewriter, second click advances (working as intended)
- Increase **Display Time** per line to give more reading time
- Disable **Enable Click Through** and use manual button instead

---

### ❌ "Decision choices don't trigger my events"

**Check:**
- Did you wire **On Choice Selected** event for each choice?
- Is the target object active in the scene?
- Did you select the correct public method?

**Fix:**
1. Click **Decision Choices > Element 0 > On Choice Selected**
2. Click **+** to add event slot
3. Drag target GameObject into object field
4. Select function from dropdown (must be public method)
5. Test in Play mode and watch Console for errors

---

### ❌ "Preview slider doesn't show decision panel"

**Check:**
- Is **Enable Decision** checked?
- Are **Decision Choices** Size > 0?
- Is preview slider at the **rightmost position**?

**Fix:**
- Decision preview only shows when slider is at maximum value
- Example: If you have 3 dialogue lines, slider goes 0-3, position 3 = decision
- Look for "(Decision)" label next to slider when at right position

---

### ❌ "Custom font doesn't apply"

**Check:**
- Did you create a **TextMeshPro Font Asset**? (regular .ttf won't work)
- Is the font asset assigned in **Visual Settings > Custom Font**?

**Fix:**
1. Right-click your .ttf font file
2. Create → TextMeshPro → Font Asset
3. Wait for generation to complete
4. Drag the new Font Asset (not .ttf) into Custom Font field

---

### ❌ "Background image doesn't appear"

**Check:**
- Is sprite assigned in **Visual Settings > Background Image**?
- Is **Background Size** large enough to see? (try 1920x1080)
- Is sprite import set to "Sprite (2D and UI)"?

**Fix:**
- Select sprite in Project window
- Change **Texture Type** to "Sprite (2D and UI)"
- Click **Apply**
- Re-assign to Background Image field

---

## Events Reference

Wire these events to extend functionality:

### Dialogue Events

- **onDialogueStart** - Fires when dialogue begins playing
  - *Example use:* Disable player movement

- **onDialogueComplete** - Fires when all lines finish (or decision made)
  - *Example use:* Re-enable player movement, give reward

- **onLineChanged (int)** - Fires each time dialogue advances, passes line index
  - *Example use:* Change camera angle per line

- **onDecisionStart** - Fires when decision panel appears
  - *Example use:* Pause game, highlight important UI

### Decision Choice Events

- **onChoiceSelected** - Fires when player selects this specific choice
  - *Example use:* Start quest, open door, change relationship value

**Example: Disable player during dialogue**

```
MyDialogue Component:
  onDialogueStart:
    ↳ PlayerController.enabled = false

  onDialogueComplete:
    ↳ PlayerController.enabled = true
```

---

## Best Practices

### ✅ DO:

- **Use "Show Canvas Preview"** constantly while designing - it's your best friend!
- **Test with "Enable Click Through"** on - players will want to skip
- **Keep Display Time** at 2-3 seconds per line (player reads at own pace with click-through)
- **Use consistent Image Size** for all character portraits (looks professional)
- **Preview decisions** by sliding preview slider all the way right
- **Use TypeOn animation** for important/dramatic lines (immersive)
- **Add background image** for dialogue box (makes text more readable)

### ❌ DON'T:

- **Don't use tiny fonts** - 48pt minimum for readability
- **Don't make Display Time too short** - player needs time to read
- **Don't forget DialogueUIController** - companion script is required!
- **Don't put too much text** in one line - split into multiple lines
- **Don't use Loop + Decision together** - decision ends dialogue (incompatible)
- **Don't forget to wire decision events** - choices won't do anything otherwise

---

## Quick Reference Card

### Minimum Setup Checklist

```
□ GameObject with ActionDialogueSequence component
□ GameObject with DialogueUIController component
□ At least 1 dialogue line added (Dialogue Lines: Size = 1+)
□ Display Time set per line (2-3 seconds typical)
□ Play On Start enabled OR trigger wired to StartDialogue()
```

### Common Settings Quick Values

```
Text Position: (0, -300)         Bottom-center
Text Size: (1200, 200)           Wide dialogue box
Font Size: 48                    Readable size

Portrait Size: (300, 300)        Medium character images
Left Position: (-400, -100)      Left side, slightly down
Right Position: (400, -100)      Right side, slightly down

Decision Panel Position: (0, -200)   Bottom-center
Decision Button Size: (400, 100)      Medium-wide buttons
Button Opacity: 0.9                   Mostly opaque
```

---

## You've Got This! 🎓

Start with the **5-Minute Quick Start**, get comfortable with basic dialogue, then gradually add portraits, animations, and decisions as you get more confident.

The **"Show Canvas Preview"** button is your best tool - use it constantly while designing!

**Need more help?** Check the full component library in `CLAUDE_STUDENT.md` or use the Script Documentation Generator (Tools menu).

---

**Happy dialogue creating!** 🎭
