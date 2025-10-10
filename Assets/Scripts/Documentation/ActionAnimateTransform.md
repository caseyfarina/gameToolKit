# ActionAnimateTransform - Tutorial & Usage Guide

This guide explains how to create procedural animations using the ActionAnimateTransform component. This is a lightweight alternative to Unity's Animator system for simple transform-based animations.

---

## Component Overview

**ActionAnimateTransform** - Procedural animation using AnimationCurves
- Animate position, rotation, and scale independently on any axis
- Multiple curve mappings per object (animate Y position AND Z rotation simultaneously)
- Offset mode (add to current values) or Absolute mode (replace values)
- Loop, ping-pong, and one-shot playback options
- Randomization for duration and start delays
- Event system for triggering other actions
- High performance - can run hundreds simultaneously

---

## Parameter Reference

### Animation Configuration

#### Target Transform
- The transform to animate (defaults to this GameObject if empty)
- Drag any GameObject here to animate it remotely
- Example: Animate a child object, door, etc.

#### Duration
- How long the animation takes to complete (in seconds)
- Range: Must be greater than 0
- Example: `1.0` = one second, `0.5` = half second, `2.0` = two seconds
- **TIP:** Shorter durations = faster animations

#### Duration Randomness
- Randomizes the duration by a percentage (0 = no randomization, 1 = ±100%)
- Slider range: 0.0 to 1.0
- Example: Duration=1.0, Randomness=0.2 → actual duration between 0.8-1.2 seconds
- **USE CASE:** Create natural variation when animating multiple objects
- **TIP:** 0.1-0.3 is good for subtle variation, 0.5+ for dramatic differences

#### Curve Mappings (Expandable Array)
Each element defines one animation curve applied to one transform property. Can have multiple mappings (e.g., bounce Y position + spin Z rotation).

**Property** (dropdown)
- Which transform property to animate
- Options: PositionX, PositionY, PositionZ, RotationX, RotationY, RotationZ, ScaleX, ScaleY, ScaleZ
- **TIP:** Most common are PositionY (vertical motion) and RotationY (spin)

**Curve** (AnimationCurve editor)
- The animation curve defining the motion over time (0-1 normalized time)
- Click to open curve editor with visual keyframe editing
- Default: Bounce curve (0→1→0 for smooth up-down motion)
- **TIP:** Right-click keyframes for tangent options (Auto, Free, Linear, Constant)

**Min Value**
- Minimum output value (what curve value 0 maps to)
- In Offset mode: added to initial value when curve = 0
- In Absolute mode: replaces value when curve = 0
- Example: For 2-unit bounce, set Min=0, Max=2

**Max Value**
- Maximum output value (what curve value 1 maps to)
- In Offset mode: added to initial value when curve = 1
- In Absolute mode: replaces value when curve = 1
- **TIP:** Negative values work! Min=0, Max=-2 creates downward motion

**Mode** (dropdown)
- **Offset:** Adds curve value to current transform value (DEFAULT, most common)
- **Absolute:** Replaces transform value with curve value
- **USE CASE:** Use Offset for bouncing on current position, Absolute for moving to exact coordinates

**Enabled** (checkbox)
- Is this curve mapping active?
- Uncheck to temporarily disable without deleting
- **TIP:** Use to test different curve combinations

### Playback Settings

#### Play On Start
- If checked, animation plays automatically when scene starts
- Default: true (auto-play)
- Uncheck if you want to trigger via UnityEvent instead
- **USE CASE:** Auto-play for ambient animations, manual for triggered effects

#### Loop
- If checked, animation repeats continuously
- Default: true (infinite loop)
- Uncheck for one-shot animations
- **TIP:** Combine with pingPong for back-and-forth motion

#### Ping Pong
- If checked, animation reverses direction on each loop
- Only works when Loop is enabled
- Creates back-and-forth motion (0→1→0→1...)
- **USE CASE:** Pendulum, breathing effects
- **TIP:** Turn off for continuous rotation/movement in one direction

#### Start Delay
- Delay before animation starts (in seconds)
- Default: 0 (starts immediately)
- Example: 2.0 = wait 2 seconds before starting
- **USE CASE:** Stagger animations across multiple objects

#### Start Delay Randomness
- Randomizes the start delay by a percentage (0 = no randomization, 1 = ±100%)
- Slider range: 0.0 to 1.0
- Example: Delay=1.0, Randomness=0.5 → actual delay between 0.5-1.5 seconds
- **USE CASE:** Prevent synchronized objects from looking artificial
- **TIP:** Higher values create more chaotic/natural feeling

#### Loop Delay
- Delay between loop iterations (in seconds)
- Default: 0 (loops immediately)
- Example: 1.0 = wait 1 second between each loop
- **USE CASE:** Pulsing effects, timed sequences, breathing animations

#### Use Unscaled Time
- If checked, animation ignores Time.timeScale (continues during pause)
- Default: false (respects game pause)
- **USE CASE:** UI animations that should play during pause menus
- **TIP:** Keep false for gameplay animations, true for UI/menu animations

### State (Read-Only)

#### Is Playing
- Shows if animation is currently playing
- Read-only (set via Play/Stop methods or events)
- Visible in Inspector during Play Mode for debugging

#### Current Time
- Current normalized time (0-1) in the animation
- 0 = start, 0.5 = halfway, 1 = end
- Read-only, updates every frame during playback

### Events

#### Debug Events
- If checked, logs all event invocations to Console
- Default: false
- **USE CASE:** Troubleshooting event connections and timing
- **TIP:** Enable to see exactly when events fire

#### onAnimationStart
- Fires when animation starts (after start delay)
- Fires each time Play() is called
- **USE CASE:** Play sound effect, trigger other animations, spawn particles

#### onAnimationComplete
- Fires when animation completes
- Does NOT fire on loop (only when animation fully ends)
- **USE CASE:** Open door after bounce completes, trigger next sequence

#### onAnimationLoop
- Fires each time animation loops
- Only fires if Loop is enabled
- **USE CASE:** Play loop sound, increment counter, spawn repeated effects

#### onAnimationUpdate (float)
- Fires every frame during animation
- Passes normalized time (0-1) as parameter
- **USE CASE:** Update UI progress bar, sync other systems
- **WARNING:** Fires very frequently, avoid heavy operations

---

## Public Methods (For UnityEvents & Scripting)

#### Play()
- Starts or restarts the animation from beginning
- Stops any currently playing animation first
- Captures current transform values as new initial state
- **USE CASE:** Wire to button click, trigger zone, etc.

#### Stop()
- Stops animation at current position
- Does not reset to initial values
- **USE CASE:** Freeze animation mid-motion

#### Pause()
- Pauses animation (can be resumed with Resume)
- Maintains current position and progress
- **USE CASE:** Pause menu, conditional pausing

#### Resume()
- Resumes a paused animation from where it stopped
- Only works if animation was paused (not stopped)
- **USE CASE:** Unpause menu, resume after event

#### ResetToInitial()
- Resets transform to initial values captured at Start
- Does not start playing
- **USE CASE:** Reset puzzle element

#### SetNormalizedTime(float)
- Sets animation to specific point in time (0-1)
- 0 = beginning, 0.5 = middle, 1 = end
- **USE CASE:** Scrubbing, state-based positioning

#### SetDuration(float)
- Changes animation duration at runtime
- **USE CASE:** Speed up/slow down based on game state

#### SetLoop(bool)
- Enables or disables looping at runtime
- **USE CASE:** Switch from looping to one-shot based on condition

#### PlayReverse()
- Plays animation in reverse direction
- Useful for reversible animations (open/close door)
- **USE CASE:** Toggle mechanisms, rewind effects

---

## Basic Setup - Simple Bounce Animation

**SCENARIO:** Make an object bounce up and down continuously

### Step 1: Add Component
1. Select your GameObject
2. Add Component → ActionAnimateTransform
3. Component comes with default bounce animation already configured

### Step 2: Verify Default Settings
- **Duration:** 1 (one second per bounce)
- **Curve Mappings (Size: 1):**
  - Property: PositionY (vertical motion)
  - Curve: Bounce (0→1→0 curve shape)
  - Min Value: 0
  - Max Value: 1 (bounces 1 unit up)
  - Mode: Offset (adds to current position)
- **Play On Start:** true (auto-plays)
- **Loop:** true (repeats forever)

### Step 3: Adjust Bounce Height
Change Max Value to control bounce height:
- `0.5` = half-unit bounce (subtle)
- `2.0` = two-unit bounce (dramatic)
- `-1.0` = downward bounce

**DONE!** Press Play to see the object bouncing.

---

## Advanced Setup - Rotating Coin with Bounce

**SCENARIO:** Create a collectable coin that spins and bounces

### Step 1: Add Component
1. Select coin GameObject
2. Add ActionAnimateTransform

### Step 2: Configure Two Animations
**Curve Mappings (Size: 2):**

**Element 0 (Bounce):**
- Property: PositionY
- Curve: Default bounce (0→1→0)
- Min Value: 0
- Max Value: 0.5 (gentle bounce)
- Mode: Offset
- Enabled: true

**Element 1 (Spin):**
- Property: RotationY
- Curve: Linear (0→1 straight line)
- Min Value: 0
- Max Value: 360 (full rotation)
- Mode: Offset
- Enabled: true

### Step 3: Set Timing
- Duration: 2.0 (slower, more visible)
- Loop: true
- Ping Pong: false (continuous rotation, not back-and-forth)

**TIP:** For continuous spin, use Offset mode with 0→360. Each loop adds another 360° rotation.

---

## Advanced Setup - Staggered Object Group

**SCENARIO:** Create 5 objects that bounce with natural variation

### Step 1: Create Objects
1. Create 5 GameObjects
2. Position them in a row
3. Add ActionAnimateTransform to each

### Step 2: Configure Randomization
On ALL objects, set:
- Duration: 1.5
- Duration Randomness: 0.3 (±30% variation)
- Start Delay: 0.5
- Start Delay Randomness: 1.0 (±100% variation, 0-1 second range)
- Curve: Default bounce
- Min Value: 0
- Max Value: 1

**RESULT:** Each object bounces at slightly different speeds and start times, creating a natural, non-synchronized appearance.

---

## Pattern: Open/Close Door

**SCENARIO:** Door slides open on trigger, stays open

### Step 1: Setup Animation
1. Add ActionAnimateTransform to door
2. Configure:
   - Property: PositionX (or Z, depending on door orientation)
   - Min Value: 0
   - Max Value: 2 (slides 2 units)
   - Mode: Offset
   - Play On Start: false (trigger-based)
   - Loop: false (one-shot animation)

### Step 2: Wire Trigger
1. Create trigger zone with InputTriggerZone component
2. InputTriggerZone.onTriggerEnter → ActionAnimateTransform.Play()

### Step 3: Add Sound (Optional)
1. ActionAnimateTransform.onAnimationStart → AudioSource.Play()

---

## Pattern: Scaling Pulse Effect

**SCENARIO:** Object pulses larger and smaller (breathing effect)

### Step 1: Setup Scale Animation
1. Add ActionAnimateTransform to object
2. Curve Mappings (Size: 3):
   - Element 0: ScaleX, Min=1, Max=1.2, Mode=Absolute
   - Element 1: ScaleY, Min=1, Max=1.2, Mode=Absolute
   - Element 2: ScaleZ, Min=1, Max=1.2, Mode=Absolute

### Step 2: Configure Timing
- Duration: 1.0
- Curve: Smooth in-out (use Auto tangents on keyframes)
- Loop: true
- Ping Pong: true

**TIP:** Use Absolute mode for scale to ensure it returns to 1.0 (normal size). Offset mode would continuously grow larger each loop!

---

## Pattern: Victory Sequence

**SCENARIO:** When player wins, object spins and bounces upward

### Step 1: Setup Multi-Property Animation
Curve Mappings (Size: 2):
- Element 0: PositionY, Min=0, Max=5, Curve=ease-out
- Element 1: RotationY, Min=0, Max=720, Curve=linear

### Step 2: Configure for One-Shot
- Duration: 2.0
- Play On Start: false
- Loop: false

### Step 3: Wire to Victory Event
- GameStateManager.onVictory → ActionAnimateTransform.Play()

---

## Curve Editor Tips

### Opening Curve Editor
- Click the curve thumbnail in Inspector
- Opens visual keyframe editor

### Adding Keyframes
- Double-click on curve to add keyframe
- Drag keyframes to reposition

### Tangent Types (Right-click keyframe)
- **Auto:** Smooth automatic tangents (best for organic motion)
- **Free:** Manual control of in/out tangents
- **Linear:** Straight lines between keyframes (robotic motion)
- **Constant:** Holds value until next keyframe (stepped motion)

### Common Curve Shapes

**Bounce (Default):**
- 0.0s: value 0
- 0.5s: value 1 (peak)
- 1.0s: value 0
- **Use:** Bounce, pulse, jump

**Linear:**
- 0.0s: value 0
- 1.0s: value 1
- Straight diagonal line
- **Use:** Constant speed, rotation

**Ease In/Out:**
- Slow start, fast middle, slow end
- Use Auto tangents on all keyframes
- **Use:** Smooth natural motion, door slides

**Ease In (Accelerate):**
- Slow start, fast end
- **Use:** Gravity fall, speed up

**Ease Out (Decelerate):**
- Fast start, slow end
- **Use:** Landing, slow stop

---

## Performance Notes

### Optimized for Scale
- Can run 100+ concurrent animations smoothly
- Caches property flags at startup
- Only reads/writes modified transform properties
- Minimal garbage collection allocations

### Best Practices
- Disable curve mappings you're not using (uncheck Enabled)
- Use Offset mode when possible (slightly faster)
- Avoid onAnimationUpdate for heavy operations (fires every frame)
- Use unscaled time sparingly (small performance cost)

---

## Troubleshooting

### Animation doesn't play
- Check "Play On Start" is enabled (or wire Play() to event)
- Verify at least one curve mapping is enabled
- Check Duration > 0
- If using trigger, verify event connection in Inspector

### Animation plays but nothing moves
- Check Target Transform is assigned (defaults to self)
- Verify curve has keyframes (not flat line)
- Check Min/Max values are different
- Ensure curve mapping is enabled

### Animation stutters or jerks
- Avoid very short durations (< 0.1s can stutter)
- Check curve tangents (use Auto for smooth motion)
- Verify frame rate is stable (Performance Profiler)

### Animation moves wrong amount
- Offset mode: adds to current position (doubles each loop if not using ping-pong)
- Absolute mode: replaces position (use for exact coordinates)
- Check Min/Max values match intended units
- For rotation, remember 360 = full circle

### Warnings in Console
- "Duration must be greater than 0": Set duration to positive value
- "All curve mappings are disabled": Enable at least one mapping
- "Min value > Max value": Swap your min/max (or use negative max intentionally)

### Multiple animations conflict
- Don't animate the same property on the same object with multiple scripts
- Use curve mappings array instead of multiple components
- Only one script should control each transform property

---

## Integration with Other Systems

### With InputTriggerZone
- InputTriggerZone.onTriggerEnter → ActionAnimateTransform.Play()
- **USE CASE:** Pressure plates, proximity triggers

### With PuzzleSwitchChecker
- PuzzleSwitchChecker.onPuzzleSolved → ActionAnimateTransform.Play()
- **USE CASE:** Door opens when puzzle solved

### With GameCollectionManager
- GameCollectionManager.onThresholdReached → ActionAnimateTransform.Play()
- **USE CASE:** Victory animation when score reached

### With GameHealthManager
- GameHealthManager.onDeath → ActionAnimateTransform.Play()
- **USE CASE:** Collapse animation on death

### With GameAudioManager
- ActionAnimateTransform.onAnimationStart → GameAudioManager.PlaySFX()
- **USE CASE:** Synchronized audio feedback

### Chaining Animations
- Animation1.onAnimationComplete → Animation2.Play()
- **USE CASE:** Sequential animation sequences

---

## Student Workflow Summary

For students creating their first procedural animation:

1. Add ActionAnimateTransform component to GameObject
2. Choose which property to animate (PositionY is default)
3. Adjust Min/Max values to control range of motion
4. Test default bounce curve (or edit curve for custom motion)
5. Set Duration to control speed
6. Enable Loop for continuous animation
7. Add randomness for natural variation (optional)
8. Wire events to other components for integration
9. Press Play to test

**No code required!** Everything connects via Inspector.

---

## Common Student Mistakes

### Mistake 1: Using Offset mode for scale animations
- ❌ **WRONG:** Scale animation with Offset mode grows infinitely
- ✅ **RIGHT:** Use Absolute mode for scale (always returns to base size)

### Mistake 2: Forgetting to enable Loop
- ❌ **WRONG:** Animation plays once and stops (if that's not intended)
- ✅ **RIGHT:** Enable Loop for continuous ambient animations

### Mistake 3: Duration too short
- ❌ **WRONG:** Duration = 0.1 creates fast, hard-to-see motion
- ✅ **RIGHT:** Start with Duration = 1.0, adjust from there

### Mistake 4: Min/Max swapped
- ❌ **WRONG:** Min=5, Max=0 (creates inverted motion)
- ✅ **RIGHT:** Min=0, Max=5 (or intentionally invert for downward motion)

### Mistake 5: Multiple scripts animating same property
- ❌ **WRONG:** Two scripts both animate PositionY → conflict/jitter
- ✅ **RIGHT:** Use one script with multiple curve mappings

---

This component is designed to make procedural animation accessible to students without requiring any code. Experiment with different curve shapes, properties, and combinations to create unique motion!
