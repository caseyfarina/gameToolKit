=======================================================
PUZZLE SWITCH SYSTEM - TUTORIAL & USAGE GUIDE
=======================================================

This guide explains how to create switch-based puzzles using the PuzzleSwitch and PuzzleSwitchChecker components.

=======================================================
COMPONENT OVERVIEW
=======================================================

**PuzzleSwitch** - Individual configurable multi-state switch
- Can have 2+ states (binary on/off, 3-position dial, 4-way selector, etc.)
- Trigger-based or key-press activation
- Visual feedback via materials, colors, and rotation
- Audio feedback per state change
- Fires UnityEvents for integration with other systems

**PuzzleSwitchChecker** - Validates switch configurations
- Monitors multiple switches for correct configuration
- Two checking modes: Automatic or Manual
- Flexible validation: all correct, threshold count, or custom
- Progress tracking and events
- Integration with existing game systems

=======================================================
BASIC SETUP - SIMPLE 2-SWITCH PUZZLE
=======================================================

**SCENARIO**: Two binary switches must both be "ON" to open a door

STEP 1: Create the Switches
----------------------------
1. Create two cube GameObjects named "Switch1" and "Switch2"
2. Add BoxCollider to each, check "Is Trigger"
3. Add PuzzleSwitch component to each

Configure Switch1:
- Switch ID: "Switch1"
- Number of States: 2
- Current State: 0 (starts OFF)
- Use Trigger Activation: true
- Required Tag: "Player"

Configure Switch2:
- Switch ID: "Switch2"
- Number of States: 2
- Current State: 0 (starts OFF)
- Use Trigger Activation: true
- Required Tag: "Player"

STEP 2: Create the Checker
---------------------------
1. Create empty GameObject named "SwitchPuzzleChecker"
2. Add PuzzleSwitchChecker component

Configure Checker:
- Automatic Checking: true (checks immediately when switches change)
- Require All Correct: true
- Switch Targets (Size: 2):
  - Element 0:
    - Target Switch: Drag Switch1 here
    - Required State: 1 (ON)
  - Element 1:
    - Target Switch: Drag Switch2 here
    - Required State: 1 (ON)

STEP 3: Connect to Door
------------------------
1. Select your door GameObject (or create one)
2. Make sure it has a component with an Open() method (like PuzzleDoor or ActionDisplayImage)
3. On the SwitchPuzzleChecker component:
   - Find "onPuzzleSolved" event
   - Click + to add event
   - Drag door GameObject into object field
   - Select Door component → Open() method

DONE! When both switches are activated, the door opens.

=======================================================
ADVANCED SETUP - 3-STATE COMBINATION LOCK
=======================================================

**SCENARIO**: Three dials must be set to specific positions (like a combination lock)

STEP 1: Create the Dials
-------------------------
1. Create three cylinder GameObjects (Dial1, Dial2, Dial3)
2. Add PuzzleSwitch to each

Configure each dial:
- Number of States: 4 (four positions: 0, 90, 180, 270 degrees)
- Cycle States: true
- State Rotations:
  - Element 0: 0
  - Element 1: 90
  - Element 2: 180
  - Element 3: 270

Visual feedback (optional):
- State Colors: Assign 4 different colors (red, blue, green, yellow)

STEP 2: Create the Checker
---------------------------
1. Add PuzzleSwitchChecker component

Configure for combination "2-1-3":
- Switch Targets (Size: 3):
  - Element 0: Dial1, Required State: 2
  - Element 1: Dial2, Required State: 1
  - Element 2: Dial3, Required State: 3

STEP 3: Add Progress Feedback (Optional)
-----------------------------------------
1. Create UI Text (TextMeshProUGUI)
2. On PuzzleSwitchChecker:
   - Find "onProgressChanged" event
   - Connect to custom script that updates text: "2/3 Correct"

=======================================================
MANUAL CHECKING MODE - QUIZ/SUBMIT BUTTON
=======================================================

**SCENARIO**: Player sets switches, then presses a "Submit" button to check answer

STEP 1: Setup Switches (same as before)
----------------------------------------
Create your switches as usual with required states

STEP 2: Configure Checker for Manual Mode
------------------------------------------
1. Add PuzzleSwitchChecker
2. **IMPORTANT**: Set "Automatic Checking: false"
3. Configure switch targets as normal

STEP 3: Create Submit Button
-----------------------------
1. Create a button GameObject or UI button
2. Add component that can trigger on player interaction
3. Wire button to call PuzzleSwitchChecker.CheckPuzzle()

Example with UI Button:
- Button.onClick → SwitchPuzzleChecker.CheckPuzzle()

Example with 3D object:
- Add InputTriggerZone component
- InputTriggerZone.onTriggerEnter → SwitchPuzzleChecker.CheckPuzzle()

STEP 4: Add Feedback for Wrong Answer
--------------------------------------
- On PuzzleSwitchChecker, find "onCheckFailed" event
- Connect to audio/visual feedback:
  - Play "buzzer" sound
  - Display "Try Again!" message
  - Shake camera, etc.

=======================================================
THRESHOLD CHECKING - PARTIAL CREDIT
=======================================================

**SCENARIO**: Door opens when at least 2 out of 3 switches are correct

Configure Checker:
- Require All Correct: false
- Required Correct Count: 2

Now the puzzle solves when any 2 switches match their required states.

=======================================================
INTEGRATION WITH EXISTING SYSTEMS
=======================================================

**With GameStateManager:**
- PuzzleSwitchChecker.onPuzzleSolved → GameStateManager.SetVictory()

**With GameCollectionManager:**
- PuzzleSwitchChecker.onFirstTimeSolved → GameCollectionManager.AddScore(10)

**With GameAudioManager:**
- PuzzleSwitch.onStateChanged → GameAudioManager.PlaySFX("switch_click")
- PuzzleSwitchChecker.onPuzzleSolved → GameAudioManager.PlaySFX("puzzle_solved")
- PuzzleSwitchChecker.onCheckFailed → GameAudioManager.PlaySFX("wrong_answer")

**With GameUIManager:**
- PuzzleSwitchChecker.onProgressChanged → Custom UI update method

**Multi-Stage Puzzles:**
- Puzzle1Checker.onPuzzleSolved → Puzzle2Checker.SetActivatable(true)
- Puzzle1Checker.onPuzzleSolved → Door1.Open()
- Puzzle2Checker.onPuzzleSolved → Door2.Open()

=======================================================
VISUAL FEEDBACK TIPS
=======================================================

**Material-Based:**
1. Create materials for each state (e.g., Red_Off, Green_On)
2. Assign to State Materials array on PuzzleSwitch
3. Switch automatically changes material when state changes

**Color-Based (Simpler):**
1. Assign colors to State Colors array
2. Works with any material that supports color tinting

**Rotation-Based:**
1. Use State Rotations array for physical rotation
2. Great for dials, levers, directional switches

**Animation-Based:**
1. Create animation clips for each state
2. Assign to State Animations array (future enhancement)

**Audio Feedback:**
1. Add AudioSource to switch GameObject
2. Assign clips to State Sounds array (different sound per state)
3. Or use single State Change Sound for all transitions

=======================================================
EDITOR TOOLS - RUNTIME TESTING
=======================================================

**During Play Mode:**

On PuzzleSwitch inspector:
- See current state highlighted
- Buttons to cycle through states
- Quick set to specific state
- Test activation

On PuzzleSwitchChecker inspector:
- See solve status (SOLVED/UNSOLVED)
- Progress bar showing correct switches
- "Check Puzzle Now" - manually trigger check
- "Reset All Switches" - set all to state 0
- "Reveal Solution" - auto-set correct configuration

=======================================================
COMMON PATTERNS
=======================================================

**Pattern 1: Zelda-Style Switches**
- Automatic checking: true
- All switches light up different colors when activated
- Door opens instantly when all correct
- Can be "broken" by deactivating a switch

**Pattern 2: Escape Room Combination Lock**
- Automatic checking: true
- 3-4 dials with multiple states
- Visual indicator shows progress
- Cannot be broken once solved

**Pattern 3: Quiz/Test Mode**
- Automatic checking: false
- Player sets answers then hits "Submit"
- Wrong answer feedback
- Limited attempts counter

**Pattern 4: Progressive Unlock**
- Multiple checkers in sequence
- Checker1.onSolved enables Checker2
- Creates multi-stage puzzle progression

=======================================================
TROUBLESHOOTING
=======================================================

**Switches don't activate:**
- Check collider "Is Trigger" is enabled
- Verify Required Tag matches player tag
- For key activation, check player is in trigger range

**Checker doesn't fire events:**
- In automatic mode: verify switches are connected to checker
- In manual mode: ensure CheckPuzzle() is being called
- Check switch Required States match intended solution

**Visual feedback not working:**
- Verify Target Renderer is assigned (auto-finds on Start)
- Check State Materials/Colors array size matches Number of States
- Materials must be in correct order (index 0 = state 0)

**Events fire multiple times:**
- If unwanted: set Can Be Unsolved: false
- Use onFirstTimeSolved for one-time rewards
- Use onPuzzleSolved for repeatable events

=======================================================
EXAMPLE CONFIGURATIONS
=======================================================

**2-Switch AND Gate (Both On):**
- 2 binary switches (2 states each)
- Require All Correct: true
- Both required state: 1

**3-Switch OR Gate (Any One):**
- 3 binary switches
- Require All Correct: false
- Required Correct Count: 1

**Combination Lock (3-2-1):**
- 3 switches with 4 states each
- Required states: [3, 2, 1]
- Automatic checking: true

**Quiz with Submit Button:**
- 5 switches with 3 states each (A/B/C)
- Automatic checking: false
- Submit button calls CheckPuzzle()
- onCheckFailed shows feedback

=======================================================
STUDENT WORKFLOW SUMMARY
=======================================================

For students creating their first switch puzzle:

1. Add PuzzleSwitch components to objects
2. Set number of states for each switch
3. Add PuzzleSwitchChecker component
4. Drag switches into checker's array
5. Set required state for each switch
6. Choose automatic or manual checking
7. Wire onPuzzleSolved event to desired action
8. Test in Play Mode using editor buttons

No code required! Everything connects via Inspector.

=======================================================
