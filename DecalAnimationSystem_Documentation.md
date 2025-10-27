# Decal Animation System - Complete Guide

**Last Updated:** October 2025
**Unity Version:** Unity 6+ with URP (Universal Render Pipeline)
**Script Location:** `Assets/Scripts/Actions/ActionDecal*.cs`

---

## Overview

The Decal Animation System provides **texture-based animation** using Unity's URP Decal Projector. Perfect for animated facial expressions, animated signs, texture cycling, and any visual that needs frame-by-frame animation without skeletal animation. Students can create complex animations by simply swapping materials or textures in sequence.

### What Are Decals?

Decals are **projected textures** that overlay onto surfaces in your scene. Think of them like stickers that can be placed on walls, floors, characters, or any 3D object. Unity's DecalProjector (URP) projects these textures from a specific direction.

### Why Use Decal Animation?

✅ **No rigging required** - Animate without bones or blend shapes
✅ **Texture-based** - Easy for artists to create frame sequences
✅ **Lightweight** - More efficient than full 3D animation for simple effects
✅ **Perfect for 2D-in-3D** - Facial expressions, signs, billboards, effects
✅ **Student-friendly** - Just swap images in the Inspector!

---

## System Components

The system includes **4 scripts** (can be used independently or together):

### Core Animation Scripts

1. **ActionDecalSequence** ⭐ *Core Script*
   - Plays a sequence of materials with custom timing
   - **Standalone** - Works without any other script
   - Custom Inspector with playback controls
   - Perfect for: Facial expressions, animated signs, texture loops

2. **ActionDecalSequenceLibrary** 📚 *Optional Manager*
   - Manages multiple ActionDecalSequence components
   - Switch between different animation sequences
   - Requires ActionDecalSequence to work
   - Perfect for: Multiple expressions, different animation states

### Specialized Blink Scripts

3. **ActionBlinkDecal** 👁️ *Simple Blink*
   - Automatic eye blinking with randomized timing
   - Switches between two materials (open/closed eyes)
   - Simplest option for basic blinking

4. **ActionBlinkDecalOptimized** 👁️⚡ *Optimized Blink*
   - More efficient - switches textures instead of materials
   - Better performance for multiple characters
   - Recommended for production use

---

## 🚀 Quick Start (5 Minutes - For Overwhelmed Students!)

**Feeling overwhelmed? Don't worry! Let's make your first decal animation in 3 easy steps.** We'll create a simple two-frame animation (like a blinking light or flashing sign).

### Your First Decal Animation (3 Easy Steps)

**Step 1: Create the Decal Projector**
1. Right-click in Hierarchy → **Rendering → Decal Projector**
2. Name it "MyFirstAnimation"
3. Position it to face a wall or floor (the blue arrow shows projection direction)
4. You should see a default decal appear on the surface!

**Step 2: Add the Animation Script**
1. With the DecalProjector selected, click **Add Component**
2. Search for "ActionDecalSequence" and add it
3. That's it! The script is now attached ✨

**Step 3: Add Your Animation Frames**
1. In the ActionDecalSequence component, find **Material Frames**
2. Click the **+** button twice to add 2 frames
3. For each frame:
   - Drag a **Material** into the "Material" slot
   - Set **Duration** to how long to show it (try 0.5 seconds)
4. Check **Loop** to make it repeat
5. Check **Play On Start** to auto-play
6. **Press Play!** Your animation should be running! 🎉

### It's Not Working? Try These:
- ✅ Make sure the DecalProjector is pointing at a surface (blue arrow in Scene view)
- ✅ Check that your materials have textures assigned
- ✅ Verify the materials are URP-compatible (use Decal Shader Graph or URP/Lit)
- ✅ Make sure the surface has a renderer (not just a collider)

### What to Do Next (When You're Ready):
- 📖 **Want automatic blinking?** → See "ActionBlinkDecal - Quick Setup" below
- 🎮 **Want to switch between animations?** → See "ActionDecalSequenceLibrary Setup"
- 🎨 **Want custom timing per frame?** → See "Material Sequence Settings"
- 🎪 **Want to control with events?** → See "UnityEvents Reference"

**Remember:** Start simple! Make a 2-frame loop first, then add complexity when comfortable. The full documentation below is here when you need it. You've got this! 💪

---

## Can I Use ActionDecalSequence Without the Library?

**YES! Absolutely.**

- **ActionDecalSequence** = Standalone script, works perfectly on its own
- **ActionDecalSequenceLibrary** = Optional helper for managing multiple sequences

**When to use each:**

| Use Case | Script Needed |
|----------|---------------|
| Single animation (e.g., flashing sign) | ActionDecalSequence only |
| Multiple expressions on same object | ActionDecalSequence + Library |
| Eye blinking only | ActionBlinkDecal only |
| Complex character with multiple states | ActionDecalSequence + Library |

**Example:** A blinking traffic light only needs ActionDecalSequence. A character with multiple facial expressions would benefit from the Library to switch between them.

---

## ActionDecalSequence - Complete Reference

### Required Components

**Automatically Required:**
- **DecalProjector** (URP) - Added automatically via `[RequireComponent]`

**You Must Add:**
- Materials with textures for each animation frame

### Setup Guide

#### Step 1: Create DecalProjector

1. Create a **DecalProjector** in your scene:
   - Right-click Hierarchy → **Rendering → Decal Projector**
   - Or add DecalProjector component to existing GameObject

2. Position the projector:
   - The **blue arrow** (Z-axis) points in projection direction
   - Decal projects onto surfaces in front of this arrow
   - Adjust **Size** to control projection area

#### Step 2: Add ActionDecalSequence

1. Select the DecalProjector GameObject
2. Add Component → **ActionDecalSequence**
3. The script automatically finds the DecalProjector

#### Step 3: Configure Material Sequence

1. In the Inspector, find **Material Frames** array
2. Set **Size** to how many frames you want (e.g., 5 for a 5-frame animation)
3. For each frame:
   - **Material**: Drag your material into this slot
   - **Duration**: How long to display this frame (in seconds)

**Example: Facial Expression Animation**
```
Frame 0: Neutral_Face material, Duration: 1.0
Frame 1: Smile_Face material, Duration: 0.5
Frame 2: Surprised_Face material, Duration: 0.3
Frame 3: Smile_Face material, Duration: 0.5
Frame 4: Neutral_Face material, Duration: 1.0
```

#### Step 4: Configure Playback Settings

- **Play On Start**: Check to auto-play when scene starts
- **Loop**: Check to repeat the sequence continuously
- **Playback Speed**: 1.0 = normal, 0.5 = half speed, 2.0 = double speed

---

### Parameter Reference

#### Material Sequence Settings

| Parameter | Type | Description |
|-----------|------|-------------|
| **Material Frames** | Array | List of materials and their durations for the animation |
| **Material Frames[i].material** | Material | Material to display for this frame |
| **Material Frames[i].duration** | float | How long to show this material (seconds) |

**Custom Inspector Features:**
- Shows **Total Frames** count
- Calculates **Total Duration** automatically
- Shows **Adjusted Duration** when playback speed ≠ 1.0
- Displays frame-by-frame list

---

#### Playback Settings

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Play On Start** | bool | false | Auto-play when scene starts |
| **Loop** | bool | false | Restart from beginning when sequence completes |
| **Playback Speed** | float | 1.0 | Speed multiplier (0.1 to 5.0) - affects all frame durations |

**Playback Speed Examples:**
- `0.5` = Slow motion (twice as long)
- `1.0` = Normal speed
- `2.0` = Fast forward (half the time)

---

### UnityEvents (Output Events)

| Event Name | When It Fires | Parameters |
|------------|---------------|------------|
| **onSequenceStart** | When Play() is called | None |
| **onSequenceComplete** | When sequence finishes (not if looping) | None |
| **onSequencePause** | When Pause() is called | None |
| **onSequenceResume** | When Resume() is called | None |
| **onSequenceStop** | When Stop() is called | None |
| **onFrameChanged** | Each time a new frame displays | int frameIndex |

**Event Usage Examples:**
```
onSequenceStart → Play sound effect, show UI indicator
onSequenceComplete → Trigger next animation, unlock achievement
onFrameChanged → Update subtitle text based on frame index
```

---

### Public Methods (Callable from UnityEvents)

| Method | Parameters | Description |
|--------|------------|-------------|
| **Play()** | None | Start sequence from beginning (stops current if playing) |
| **Stop()** | None | Stop sequence and reset to beginning |
| **Pause()** | None | Pause at current frame |
| **Resume()** | None | Resume from paused frame |
| **JumpToFrame(int)** | frameIndex | Jump to specific frame (0-indexed) |
| **SetPlaybackSpeed(float)** | speed | Change playback speed (0.1 to 5.0) |
| **SetLoop(bool)** | shouldLoop | Enable/disable looping at runtime |
| **AddFrame(Material, float)** | material, duration | Add new frame to end of sequence |
| **ClearFrames()** | None | Remove all frames (stops if playing) |

**Example Usage:**
```
Button Click → ActionDecalSequence.Play()
Trigger Zone Enter → ActionDecalSequence.JumpToFrame(3)
Timer Complete → ActionDecalSequence.SetPlaybackSpeed(2.0)
```

---

### Public Properties (Read-Only)

| Property | Type | Description |
|----------|------|-------------|
| **IsPlaying** | bool | Is sequence currently playing? |
| **IsPaused** | bool | Is sequence currently paused? |
| **CurrentFrameIndex** | int | Current frame index (-1 if not started) |
| **TotalFrames** | int | Total number of frames in sequence |

---

### Custom Inspector Features

When you select an ActionDecalSequence in the editor, you'll see:

**In Edit Mode:**
- Total frame count
- Total sequence duration
- Adjusted duration (if playback speed ≠ 1.0)

**In Play Mode:**
- **Play/Pause/Resume/Stop buttons** - Control playback directly in Inspector
- **Current status** - Shows Playing/Paused/Stopped
- **Current frame** - Shows which frame is displaying (e.g., "3 / 5")

This makes testing and debugging super easy! No need to wire up buttons just to test your animation.

---

## ActionDecalSequenceLibrary - Complete Reference

### Purpose

Manages multiple ActionDecalSequence components on the same GameObject, allowing you to switch between different animations. Perfect for characters with multiple expressions or objects with multiple animation states.

### Setup Guide

#### Step 1: Create Multiple Sequences

First, create several ActionDecalSequence components on the same GameObject:

```
GameObject: CharacterFace (with DecalProjector)
├─ ActionDecalSequence #1 (rename to "Idle Expression")
├─ ActionDecalSequence #2 (rename to "Happy Expression")
├─ ActionDecalSequence #3 (rename to "Sad Expression")
└─ ActionDecalSequence #4 (rename to "Surprised Expression")
```

**How to add multiple:**
1. Add ActionDecalSequence component
2. Rename it in the component header (click the three dots → Rename)
3. Configure its material frames
4. Repeat for each animation state

#### Step 2: Add the Library

1. Add Component → **ActionDecalSequenceLibrary** to the same GameObject
2. The library will manage all the sequences

#### Step 3: Assign Sequences to Library

1. In the Library component, find **Sequences** array
2. Set **Size** to match your number of sequences (e.g., 4)
3. Drag each ActionDecalSequence component into the slots:
   - Sequences[0] → Idle Expression
   - Sequences[1] → Happy Expression
   - Sequences[2] → Sad Expression
   - Sequences[3] → Surprised Expression

#### Step 4: Configure Playback

- **Default Sequence Index**: Which sequence to select on start (-1 = none, 0 = first, etc.)
- **Play On Start**: Auto-play the default sequence when scene starts

---

### Parameter Reference

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Sequences** | Array | Empty | List of ActionDecalSequence components to manage |
| **Default Sequence Index** | int | -1 | Which sequence to play on start (-1 = none) |
| **Play On Start** | bool | false | Auto-play default sequence when scene starts |

---

### UnityEvents (Output Events)

| Event Name | When It Fires | Parameters |
|------------|---------------|------------|
| **onSequenceChanged** | When switching to new sequence | int sequenceIndex |
| **onLibraryStopped** | When StopCurrentSequence() is called | None |

---

### Public Methods (Callable from UnityEvents)

| Method | Parameters | Description |
|--------|------------|-------------|
| **PlaySequence(int)** | index | Play sequence by index (0-based), stops current |
| **PlaySequenceByName(string)** | sequenceName | Play sequence by GameObject name |
| **StopCurrentSequence()** | None | Stop the currently playing sequence |
| **PauseCurrentSequence()** | None | Pause the current sequence |
| **ResumeCurrentSequence()** | None | Resume the paused sequence |
| **PlayNextSequence()** | None | Play next sequence in list (wraps around) |
| **PlayPreviousSequence()** | None | Play previous sequence (wraps around) |
| **GetCurrentSequence()** | None | Returns currently playing sequence (or null) |
| **GetSequence(int)** | index | Get sequence by index |
| **SetDefaultSequenceIndex(int)** | index | Change default sequence at runtime |

**Example Usage:**
```
Collectible Pickup → Library.PlaySequence(1)  // Switch to "Happy"
Damage Taken → Library.PlaySequence(2)  // Switch to "Sad"
Next Button → Library.PlayNextSequence()
Previous Button → Library.PlayPreviousSequence()
```

---

### Public Properties (Read-Only)

| Property | Type | Description |
|----------|------|-------------|
| **CurrentSequenceIndex** | int | Index of current sequence (-1 if none) |
| **TotalSequences** | int | Total sequences in library |
| **IsPlaying** | bool | Is any sequence currently playing? |

---

### Custom Inspector Features

**Edit Mode:**
- Shows total sequence count
- Lists all sequences by name with index numbers
- Validates for null sequences

**Play Mode:**
- Shows current sequence index and status
- **Number buttons (0-9)** - Click to play that sequence instantly
- **Previous/Next buttons** - Navigate through sequences
- **Pause/Resume/Stop buttons** - Control playback
- Live status updates showing which sequence is playing

---

## ActionBlinkDecal - Complete Reference

### Purpose

Specialized script for automatic eye blinking using material switching. Simple two-state animation (open eyes ↔ closed eyes) with randomized timing.

### Setup Guide

#### Quick Setup

1. Create a DecalProjector and position it over character's eyes
2. Add Component → **ActionBlinkDecal**
3. Assign materials:
   - **Open Eyes Material**: Material with open eyes texture
   - **Closed Eyes Material**: Material with closed eyes texture
4. Adjust timing:
   - **Time Between Blinks**: Average time between blinks (e.g., 3 seconds)
   - **Random Percentage**: Variation (0.3 = ±30% randomness)
   - **Blink Duration**: How long eyes stay closed (e.g., 0.15 seconds)
5. Check **Blink On Start** to auto-start

---

### Parameter Reference

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Open Eyes Material** | Material | null | Material showing open eyes |
| **Closed Eyes Material** | Material | null | Material showing closed eyes |
| **Time Between Blinks** | float | 3.0 | Base time between blinks (seconds) |
| **Random Percentage** | float | 0.3 | Timing variation (0-1, where 0.5 = ±50%) |
| **Blink Duration** | float | 0.15 | How long eyes stay closed (seconds) |
| **Blink On Start** | bool | true | Auto-start blinking when scene begins |

**Timing Example:**
```
Time Between Blinks = 3.0 seconds
Random Percentage = 0.3 (30%)

Actual blink intervals will vary between:
3.0 - (3.0 × 0.3) = 2.1 seconds
3.0 + (3.0 × 0.3) = 3.9 seconds

This creates natural, non-robotic blinking!
```

---

### Public Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| **StartBlinking()** | None | Start automatic blink loop |
| **StopBlinking()** | None | Stop blinking (eyes remain open) |
| **BlinkOnce()** | None | Trigger single blink manually |
| **SetBlinkMaterials(Material, Material)** | open, closed | Change materials at runtime |
| **SetTimeBetweenBlinks(float)** | time | Adjust blink frequency |
| **SetBlinkDuration(float)** | duration | Adjust how long blink lasts |
| **SetRandomPercentage(float)** | percentage | Adjust timing randomness |

---

### UnityEvents

| Event Name | When It Fires |
|------------|---------------|
| **onBlinkStart** | Eyes close (blink begins) |
| **onBlinkComplete** | Eyes open (blink ends) |

---

## ActionBlinkDecalOptimized - Complete Reference

### Purpose

**Optimized version** of ActionBlinkDecal. Instead of swapping entire materials, it only swaps the texture on a single material. This is more efficient and recommended for scenes with multiple characters.

### Key Differences from ActionBlinkDecal

| Feature | ActionBlinkDecal | ActionBlinkDecalOptimized |
|---------|------------------|---------------------------|
| **Switches** | Entire materials | Only textures |
| **Performance** | Good | Better (less overhead) |
| **Setup** | Assign 2 materials | Assign 2 textures + 1 material |
| **Use Case** | Single character | Multiple characters |

---

### Setup Guide

#### Quick Setup

1. Create a DecalProjector with a material assigned
2. Add Component → **ActionBlinkDecalOptimized**
3. Assign textures:
   - **Open Eyes Texture**: Texture showing open eyes
   - **Closed Eyes Texture**: Texture showing closed eyes
4. Set **Texture Property Name**:
   - For URP: Usually `_BaseMap`
   - For Built-in: Usually `_MainTex`
   - Check your shader to confirm
5. Adjust timing (same as ActionBlinkDecal)
6. Check **Blink On Start**

---

### Parameter Reference

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| **Open Eyes Texture** | Texture | null | Texture showing open eyes |
| **Closed Eyes Texture** | Texture | null | Texture showing closed eyes |
| **Texture Property Name** | string | "_BaseMap" | Shader property name (URP: "_BaseMap", Built-in: "_MainTex") |
| **Time Between Blinks** | float | 3.0 | Base time between blinks (seconds) |
| **Random Percentage** | float | 0.3 | Timing variation (0-1) |
| **Blink Duration** | float | 0.15 | How long eyes stay closed (seconds) |
| **Blink On Start** | bool | true | Auto-start blinking |

**Important:** This script creates a **material instance** automatically to avoid modifying the original material asset. The instance is destroyed when the GameObject is destroyed.

---

### Public Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| **StartBlinking()** | None | Start automatic blink loop |
| **StopBlinking()** | None | Stop blinking (eyes remain open) |
| **BlinkOnce()** | None | Trigger single blink manually |
| **SetBlinkTextures(Texture, Texture)** | open, closed | Change textures at runtime |
| **SetTimeBetweenBlinks(float)** | time | Adjust blink frequency |
| **SetBlinkDuration(float)** | duration | Adjust how long blink lasts |
| **SetRandomPercentage(float)** | percentage | Adjust timing randomness |

---

## Common Setup Scenarios

### Scenario 1: Flashing Neon Sign

**Goal:** Create a looping 3-frame neon sign animation

**Setup:**
1. Create DecalProjector pointing at wall
2. Add ActionDecalSequence
3. Create 3 material frames:
   - Frame 0: Sign_Off material, 0.5s
   - Frame 1: Sign_On material, 0.3s
   - Frame 2: Sign_Flicker material, 0.1s
4. Enable **Loop** and **Play On Start**

**Result:** Sign flashes continuously with flicker effect

---

### Scenario 2: Character Facial Expressions

**Goal:** Character with 4 different expression animations controlled by events

**Setup:**
1. Create DecalProjector on character's face
2. Add 4 ActionDecalSequence components:
   - Sequence 0: Idle (3 frames, subtle breathing animation)
   - Sequence 1: Happy (5 frames, smile animation)
   - Sequence 2: Sad (4 frames, frown animation)
   - Sequence 3: Surprised (2 frames, quick reaction)
3. Add ActionDecalSequenceLibrary
4. Assign all 4 sequences to library
5. Wire up events:
   - Victory → Library.PlaySequence(1) // Happy
   - Damage → Library.PlaySequence(2) // Sad
   - Jump Scare → Library.PlaySequence(3) // Surprised

**Result:** Character shows different expressions based on game events

---

### Scenario 3: Realistic Eye Blinking

**Goal:** Natural eye blinking for multiple NPCs

**Setup:**
1. For each NPC:
   - Create DecalProjector over eyes
   - Add ActionBlinkDecalOptimized (more efficient for multiple characters)
   - Assign open/closed eye textures
2. Vary timing per character for natural feel:
   - NPC 1: Time = 2.5s, Random = 0.4
   - NPC 2: Time = 3.2s, Random = 0.3
   - NPC 3: Time = 2.8s, Random = 0.5
3. Enable **Blink On Start** for all

**Result:** Each NPC blinks independently with natural timing

---

### Scenario 4: Interactive Animated Poster

**Goal:** Poster that plays animation when player approaches

**Setup:**
1. Create DecalProjector on poster location
2. Add ActionDecalSequence with poster animation frames
3. **Disable** Play On Start
4. Create trigger zone in front of poster
5. Wire InputTriggerZone events:
   - onTriggerEnter → ActionDecalSequence.Play()
   - onTriggerExit → ActionDecalSequence.Stop()

**Result:** Animation plays only when player is nearby

---

## Troubleshooting

### Decal Not Visible
**Symptoms:** DecalProjector exists but nothing shows on surfaces
**Solutions:**
- ✅ Check DecalProjector **Size** is large enough to cover the surface
- ✅ Verify the **blue arrow** (Z-axis) points toward the surface
- ✅ Ensure surface has a **Renderer** component (MeshRenderer, SkinnedMeshRenderer)
- ✅ Confirm material uses a **URP-compatible shader** (Decal Shader Graph or URP/Lit)
- ✅ Check **URP Renderer** has Decal Renderer Feature enabled (Project Settings → Graphics → URP Renderer → Add Renderer Feature → Decal)
- ✅ Verify **Decal Layer** matches surface's Rendering Layer Mask

### Animation Not Playing
**Symptoms:** Sequence added but frames don't change
**Solutions:**
- ✅ Check **Material Frames** array has at least 1 frame with material assigned
- ✅ Verify **Play On Start** is checked OR call Play() via UnityEvent
- ✅ Ensure all materials in frames are not null
- ✅ Check **Total Duration** in Inspector (if 0, frames have no duration)
- ✅ Look for errors in Console - script logs warnings for missing materials

### Blink Not Working
**Symptoms:** ActionBlinkDecal added but eyes don't blink
**Solutions:**
- ✅ Check both **Open Eyes Material** and **Closed Eyes Material** are assigned
- ✅ Verify **Blink On Start** is checked OR call StartBlinking()
- ✅ Ensure **Time Between Blinks** and **Blink Duration** are > 0
- ✅ Check Console for warnings (script disables itself if materials missing)

### Sequence Plays Too Fast/Slow
**Symptoms:** Animation speed doesn't feel right
**Solutions:**
- ✅ Adjust **Playback Speed** (1.0 = normal, 0.5 = slower, 2.0 = faster)
- ✅ Check individual frame **durations** - may be set too low/high
- ✅ Verify **Total Duration** in Custom Inspector matches expected time

### Materials Switch But Look Wrong
**Symptoms:** Animation plays but textures look incorrect
**Solutions:**
- ✅ Ensure all materials use the **same shader** for consistency
- ✅ Check **Tiling** and **Offset** settings match across materials
- ✅ Verify textures are imported correctly (not rotated/flipped)
- ✅ For Optimized blink: Confirm **Texture Property Name** is correct ("_BaseMap" for URP)

### Library Not Switching Sequences
**Symptoms:** PlaySequence() called but animation doesn't change
**Solutions:**
- ✅ Verify all sequences in **Sequences** array are assigned (not null)
- ✅ Check index is valid (0 to TotalSequences-1)
- ✅ Ensure each ActionDecalSequence has frames configured
- ✅ Try using **PlaySequenceByName()** if index numbers are confusing

### Decal Appears on Wrong Surface
**Symptoms:** Decal projects onto unintended objects
**Solutions:**
- ✅ Adjust **DecalProjector Fade Factor** to limit projection distance
- ✅ Use **Decal Layers** to filter which objects receive decals
- ✅ Rotate DecalProjector to aim blue arrow at correct surface
- ✅ Reduce **Size** to limit projection area

---

## URP Decal Setup (One-Time Project Setup)

### Enabling Decals in Your URP Project

If decals aren't showing at all, you may need to enable the Decal Renderer Feature:

1. Open **Edit → Project Settings → Graphics**
2. Select your **URP Renderer** asset (usually "UniversalRenderer" or similar)
3. Click **Add Renderer Feature** at the bottom
4. Select **Decal**
5. Configure Decal settings:
   - **Technique**: Automatic (or DBuffer for best quality)
   - **Max Draw Distance**: 1000 (or your scene size)
   - **Surface Data**: Albedo + Normal + Emissive (for full features)

### Creating Decal Materials

**Option 1: Use Existing URP/Lit Material**
1. Create Material → Set shader to **Universal Render Pipeline/Lit**
2. Assign your texture to **Base Map**
3. Works out of the box with DecalProjector!

**Option 2: Create Decal-Specific Shader Graph (Advanced)**
1. Right-click → Create → Shader Graph → URP → Decal Shader Graph
2. Open and configure properties (albedo, normal, emissive, etc.)
3. Create material from shader graph
4. More control but requires Shader Graph knowledge

---

## Best Practices

### Performance
- ✅ Use **ActionBlinkDecalOptimized** instead of ActionBlinkDecal for multiple characters
- ✅ Keep **Material Frames** count reasonable (< 50 frames per sequence)
- ✅ Use texture atlases when possible (multiple expressions in one texture)
- ✅ Limit **Max Draw Distance** in Decal Renderer Feature to avoid distant rendering

### Organization
- 📁 Name your ActionDecalSequence components descriptively ("Happy_Expression", not "ActionDecalSequence (3)")
- 📁 Keep materials organized in folders: `Materials/Decals/Expressions/`
- 📁 Use prefabs for characters with multiple sequences for easy reuse

### Animation Design
- 🎨 Keep frame durations consistent within a sequence (e.g., all 0.1s) for smooth motion
- 🎨 Add 1-frame "holds" at beginning/end for cleaner transitions
- 🎨 Use odd numbers of frames (3, 5, 7) for more natural-feeling loops
- 🎨 Test at different playback speeds to find the right feel

### Events
- 📢 Use **onFrameChanged** to sync sound effects with specific frames
- 📢 Wire **onSequenceComplete** to chain animations together
- 📢 Use **onBlinkStart** to briefly disable eye tracking or other eye behaviors

---

## Technical Notes

### Material vs Texture Switching

**ActionDecalSequence** and **ActionBlinkDecal**:
- Switch entire **Material** references
- Easier to set up (drag materials into Inspector)
- Slightly more overhead (material switching)

**ActionBlinkDecalOptimized**:
- Switches **Texture** property on a single material
- Creates material instance automatically (safe, won't modify original asset)
- Better performance for multiple instances
- Requires knowing shader property name ("_BaseMap" for URP)

### Coroutine-Based Timing

All scripts use Unity Coroutines for timing:
- Accurate frame-by-frame timing
- Respects pause state
- Handles playback speed adjustments
- Automatically cleaned up on destroy/disable

### Custom Inspector Implementation

Both ActionDecalSequence and ActionDecalSequenceLibrary have custom editors:
- **Edit Mode**: Shows statistics, duration calculations
- **Play Mode**: Live playback controls, status updates
- Makes testing easy without wiring up UI buttons

---

## Related Scripts

**Recommended Companion Scripts:**
- `InputTriggerZone.cs` - Trigger animations when player enters area
- `InputKeyPress.cs` - Trigger animations with keyboard input
- `GameTimerManager.cs` - Sync animations to game timer events
- `ActionDisplayText.cs` - Show subtitles in sync with facial expressions

**Alternative Animation Approaches:**
- `ActionAnimateTransform.cs` - Transform-based procedural animation
- `Animator` component - Skeletal/blend shape animation for 3D models

---

## Example: Complete Facial Expression System

Here's a full setup for a character with animated facial expressions:

**GameObject Hierarchy:**
```
Character
├─ Body (MeshRenderer)
├─ FaceDecal (DecalProjector + Sequences + Library)
│   ├─ ActionDecalSequence - "Idle"
│   ├─ ActionDecalSequence - "Happy"
│   ├─ ActionDecalSequence - "Sad"
│   ├─ ActionDecalSequence - "Surprised"
│   ├─ ActionDecalSequence - "Talking"
│   └─ ActionDecalSequenceLibrary
└─ EyesDecal (DecalProjector + ActionBlinkDecalOptimized)
```

**Material Setup:**
- Idle: 3 frames (subtle breathing, 1s each)
- Happy: 5 frames (smile transition, 0.2s each)
- Sad: 4 frames (frown transition, 0.3s each)
- Surprised: 2 frames (quick reaction, 0.1s each)
- Talking: 8 frames (mouth shapes, 0.15s each, looping)

**Event Wiring:**
```
GameStateManager.onVictory → FaceDecal Library.PlaySequence(1) // Happy
GameHealthManager.onDamage → FaceDecal Library.PlaySequence(2) // Sad
InputTriggerZone.onTriggerEnter → FaceDecal Library.PlaySequence(3) // Surprised
DialogueSystem.onDialogueStart → FaceDecal Library.PlaySequence(4) // Talking
DialogueSystem.onDialogueEnd → FaceDecal Library.PlaySequence(0) // Idle
```

**Result:** Fully reactive character face that responds to all game events!

---

## Version History

### October 2025 - Initial Release
- ✅ ActionDecalSequence with custom Inspector and playback controls
- ✅ ActionDecalSequenceLibrary for managing multiple sequences
- ✅ ActionBlinkDecal for simple automatic blinking
- ✅ ActionBlinkDecalOptimized for efficient texture-based blinking
- ✅ Full UnityEvent integration for no-code interactions
- ✅ Custom editors with live playback controls in Inspector

---

## License & Credits

**Part of:** Unity Educational Toolkit for "Animation and Interactivity" class
**Design Philosophy:** UnityEvent-driven, texture-based animation accessible to non-programmers
**Requirements:** Unity 6+ with Universal Render Pipeline (URP)

---

**Questions or Issues?**
Check the Custom Inspector statistics for debugging, and use Play Mode controls to test sequences directly in the Inspector. The system logs helpful warnings to the Console when materials or textures are missing!
