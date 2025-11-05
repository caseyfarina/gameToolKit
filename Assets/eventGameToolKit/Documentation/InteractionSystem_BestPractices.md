# Interaction System Best Practices

Complete guide for setting up location-based interactions with different animations using the Event Game Toolkit.

---

## Overview

This guide shows you how to create interactive objects (doors, levers, chests, etc.) that trigger different animations based on the player's location. Students can set up complex interaction systems without writing any code.

**Key Pattern**: Trigger Zones + Input Detection + Animation Control

---

## Core Components

### 1. InputTriggerZone
**Purpose**: Detect when the player enters/exits an interaction area
**Location**: Runtime/Input/InputTriggerZone.cs

**Key Settings**:
- `targetTag`: "Player" (must match player's tag)
- `onTriggerEnter`: Enable the interaction input listener
- `onTriggerExit`: Disable the interaction input listener

### 2. InputKeyPress
**Purpose**: Listen for the North button (Y/Triangle) press
**Location**: Runtime/Input/InputKeyPress.cs

**Key Settings**:
- `keyCode`: JoystickButton3 (North button on controller)
- `onKeyPress`: Trigger the animation
- Start with component **disabled** (only enable when in trigger zone)

### 3. ActionTriggerAnimatorParameter
**Purpose**: Play specific animations by setting animator parameters
**Location**: Runtime/Actions/Events/ActionTriggerAnimatorParameter.cs

**Key Settings**:
- `targetAnimator`: Reference to player's Animator component
- `parameterType`: Trigger (for one-shot animations)
- `parameterName`: Name of the animator trigger (e.g., "OpenDoor", "PullLever")

---

## Setup Pattern 1: Single Interaction Point

**Example**: A door that plays an "OpenDoor" animation when player presses North button nearby.

### Hierarchy:
```
DoorInteractionZone (GameObject)
├─ TriggerZone (Collider, set to Trigger)
│  └─ InputTriggerZone component
├─ InteractionInput (GameObject, disabled)
│  └─ InputKeyPress component
└─ AnimationTrigger (GameObject)
   └─ ActionTriggerAnimatorParameter component
```

### Configuration:

**TriggerZone Object:**
- Add BoxCollider (or SphereCollider)
- Check "Is Trigger"
- Size to cover interaction area

**InputTriggerZone Component:**
```
Target Tag: "Player"
Events:
  onTriggerEnter → InteractionInput.GameObject.SetActive(true)
  onTriggerExit → InteractionInput.GameObject.SetActive(false)
```

**InputKeyPress Component** (on disabled GameObject):
```
Key Code: JoystickButton3
Events:
  onKeyPress → AnimationTrigger.ActionTriggerAnimatorParameter.TriggerParameter()
```

**ActionTriggerAnimatorParameter Component:**
```
Target Animator: PlayerCharacter/Animator
Parameter Type: Trigger
Parameter Name: "OpenDoor"
Events:
  onParameterSet → (Optional) Play sound, visual feedback, etc.
```

### Animator Setup:
1. Open player's Animator Controller
2. Create a **Trigger** parameter named "OpenDoor"
3. Create transition: Any State → OpenDoorAnimation
4. Condition: "OpenDoor" trigger

---

## Setup Pattern 2: Multiple Interaction Points

**Example**: Three different interactions (door, lever, chest) each with unique animations.

### Hierarchy:
```
InteractionPoints (Parent)
├─ DoorInteraction
│  ├─ TriggerZone (InputTriggerZone)
│  ├─ DoorInput (InputKeyPress, disabled)
│  └─ DoorAnimation (ActionTriggerAnimatorParameter → "OpenDoor")
│
├─ LeverInteraction
│  ├─ TriggerZone (InputTriggerZone)
│  ├─ LeverInput (InputKeyPress, disabled)
│  └─ LeverAnimation (ActionTriggerAnimatorParameter → "PullLever")
│
└─ ChestInteraction
   ├─ TriggerZone (InputTriggerZone)
   ├─ ChestInput (InputKeyPress, disabled)
   └─ ChestAnimation (ActionTriggerAnimatorParameter → "OpenChest")
```

### Key Rules:
✅ Each interaction has its **own InputKeyPress component** (disabled by default)
✅ Only **one** InputKeyPress enabled at a time (ensured by trigger zones)
✅ Each ActionTriggerAnimatorParameter has **unique parameter name**
✅ All reference the **same player Animator**

### Animator Setup:
1. Create three trigger parameters: "OpenDoor", "PullLever", "OpenChest"
2. Create three animation clips for each action
3. Create transitions from Any State to each animation
4. Set conditions using the trigger parameters

---

## Setup Pattern 3: Context-Sensitive Interactions

**Example**: One interaction point that does different things based on game state (locked vs unlocked door).

### Hierarchy:
```
ContextSensitiveDoor
├─ TriggerZone (InputTriggerZone)
├─ InteractionInput (InputKeyPress, disabled)
├─ OpenDoorAnimation (ActionTriggerAnimatorParameter → "OpenDoor", disabled)
└─ LockedDoorAnimation (ActionTriggerAnimatorParameter → "TryLockedDoor", enabled)
```

### Configuration:

**When Door is Locked:**
```
InteractionInput.onKeyPress → LockedDoorAnimation.TriggerParameter()
                            → Play "door locked" sound
                            → Show "Need key" UI message
```

**When Player Gets Key:**
```
CollectionManager.onThresholdReached(1) → LockedDoorAnimation.GameObject.SetActive(false)
                                        → OpenDoorAnimation.GameObject.SetActive(true)
```

**Result**: Same button press, different animation based on state!

---

## Controller Input Configuration

The Event Game Toolkit uses Unity's Input System. The North button is already configured.

### Input Action Asset:
**Path**: `Assets/InputSystem_Actions.inputactions`

**Action Map**: "Player"
**Action Name**: "Interact" (North button binding)

**Bindings**:
- Gamepad: Button North (Y on Xbox, Triangle on PlayStation)
- Keyboard: E key (common interact key)

### Using Input System Instead of KeyCode:

For students who want to use the Input System directly:

**Alternative Component**: ActionPlayCharacterEmoteAnimation
**Setup**:
```
1. Add ActionPlayCharacterEmoteAnimation to player
2. In Inspector, create Emote Mapping:
   - Action Reference: Player/Interact
   - Animator Trigger Name: "OpenDoor"
```

**Limitation**: This approach listens **globally** (works anywhere, not location-based).
**Recommendation**: Use InputKeyPress + InputTriggerZone for location-based interactions.

---

## Advanced: Visual Feedback

Add UI prompts when player enters interaction zones.

### Setup:
1. Create Canvas with TextMeshPro: "Press Y to Open Door"
2. Disable the text by default
3. Wire events:

```
InputTriggerZone (Door):
  onTriggerEnter → PromptText.GameObject.SetActive(true)
  onTriggerExit → PromptText.GameObject.SetActive(false)
```

### Multi-Location Prompts:
Each interaction zone updates the same prompt text:

```
DoorZone:
  onTriggerEnter → PromptText.SetText("Press Y to Open Door")
  onTriggerEnter → PromptText.GameObject.SetActive(true)

LeverZone:
  onTriggerEnter → PromptText.SetText("Press Y to Pull Lever")
  onTriggerEnter → PromptText.GameObject.SetActive(true)
```

**Note**: Use ActionDisplayText component for animated text prompts!

---

## Common Mistakes & Solutions

### ❌ Problem: Button works everywhere, not just near objects
**Cause**: InputKeyPress component is always enabled
**Solution**: Disable InputKeyPress by default, enable only in trigger zones

### ❌ Problem: Animation plays at wrong location
**Cause**: Multiple ActionTriggerAnimatorParameter components enabled
**Solution**: Each interaction should have separate components with unique parameters

### ❌ Problem: Animation doesn't play
**Causes**:
1. Animator parameter name doesn't match (case-sensitive!)
2. Parameter is wrong type (needs to be Trigger, not Bool/Int/Float)
3. Target Animator reference is null

**Solution**: Check Inspector warnings, validate animator setup

### ❌ Problem: Multiple interactions trigger at once
**Cause**: Trigger zones are overlapping
**Solution**: Space out interaction points or resize trigger zones

### ❌ Problem: Player tag not recognized
**Cause**: Tag not created or misspelled
**Solution**: Edit > Project Settings > Tags and Layers > Add "Player" tag, assign to player

---

## Component Reference

### ActionTriggerAnimatorParameter

**Public Methods** (UnityEvent compatible):
- `TriggerParameter()` - Trigger using configured type and value
- `SetTrigger()` - Fire a trigger parameter
- `SetBoolTrue()` - Set bool parameter to true
- `SetBoolFalse()` - Set bool parameter to false
- `SetInt(int value)` - Set int parameter
- `SetFloat(float value)` - Set float parameter
- `SetParameterName(string name)` - Change parameter at runtime
- `SetBoolValue(bool value)` - Update stored bool value
- `SetIntValue(int value)` - Update stored int value
- `SetFloatValue(float value)` - Update stored float value

**Events**:
- `onParameterSet` - Fires when parameter successfully set
- `onParameterFailed` - Fires when parameter cannot be set

**Properties** (read-only):
- `ParameterName` - Current parameter name
- `ParameterType` - Current parameter type
- `IsInitialized` - Whether component is ready

---

## Example: Complete Door Setup (Step-by-Step)

### Step 1: Create the Interaction Zone
1. Create empty GameObject: "DoorInteractionZone"
2. Position it in front of the door
3. Add Component → Physics → Box Collider
4. Check "Is Trigger"
5. Set size: X=3, Y=3, Z=3

### Step 2: Add Detection Component
1. Add Component → Input Trigger Zone
2. Set Target Tag: "Player"
3. Leave events empty for now

### Step 3: Create Input Listener
1. Right-click DoorInteractionZone → Create Empty
2. Rename to "DoorInput"
3. Disable the GameObject (uncheck in Inspector)
4. Add Component → Input Key Press
5. Set Key Code: JoystickButton3

### Step 4: Create Animation Trigger
1. Right-click DoorInteractionZone → Create Empty
2. Rename to "DoorAnimation"
3. Add Component → Action Trigger Animator Parameter
4. Drag player's Animator to Target Animator field
5. Set Parameter Type: Trigger
6. Set Parameter Name: "OpenDoor"

### Step 5: Wire Events
**InputTriggerZone**:
- onTriggerEnter → DoorInput (GameObject) → SetActive(true)
- onTriggerExit → DoorInput (GameObject) → SetActive(false)

**InputKeyPress**:
- onKeyPress → DoorAnimation (ActionTriggerAnimatorParameter) → TriggerParameter()

### Step 6: Setup Animator
1. Select player character
2. Open Window → Animation → Animator
3. Click "Parameters" tab
4. Click "+" → Trigger
5. Name it: "OpenDoor"
6. Right-click in graph → Create State → From New Blend Tree (or import animation)
7. Right-click "Any State" → Make Transition → OpenDoorAnimation
8. Click transition arrow
9. Conditions → Add → "OpenDoor"

### Step 7: Test
1. Press Play
2. Walk character near door (inside trigger zone)
3. Press Y/Triangle button
4. Animation should play!

---

## Educational Benefits

This pattern teaches:
- **Event-driven programming**: Cause and effect relationships
- **State machines**: Enabling/disabling components based on state
- **Spatial design**: Positioning trigger zones for good UX
- **Animation control**: Understanding animator parameters
- **Modular design**: Reusable components in different contexts
- **Input systems**: Gamepad button mapping
- **Tag-based detection**: Filtering what triggers interactions

Students learn complex game programming concepts without writing code!

---

## Performance Notes

✅ **Efficient**: Components use cached StringToHash for animator parameters
✅ **Optimized**: Only one InputKeyPress active at a time
✅ **Safe**: Null checking and validation prevent runtime errors
✅ **Scalable**: Pattern works for 100+ interaction points

---

## Related Components

- **InputTriggerZone**: Spatial detection
- **InputKeyPress**: Button input detection
- **ActionTriggerAnimatorParameter**: Animation control
- **ActionPlayCharacterEmoteAnimation**: Global input-based animations
- **ActionDisplayText**: UI prompt animations
- **GameCollectionManager**: Track keys/items for context-sensitive interactions
- **GameInventorySlot**: Gate interactions behind required items

---

## Version History

### November 2025 - Initial Documentation
- Created ActionTriggerAnimatorParameter component
- Documented location-based interaction best practices
- Added complete door interaction example
- Provided troubleshooting guide

---

**Event Game Toolkit for Unity 6**
Educational package for Animation and Interactivity class
