# Checkpoint System - Quick Start Guide

**5-minute setup for saving player progress and respawning**

---

## What is the Checkpoint System?

The checkpoint system lets you save the player's position (and optionally their score and health) so they can respawn there when they die or when you reload the scene.

**Perfect for**:
- Platformer checkpoints
- Racing game lap markers
- Save points in adventure games
- Respawn locations after death

---

## Setup (3 Simple Steps)

### Step 1: Add the Checkpoint Manager

1. Create an empty GameObject in your scene
2. Name it "CheckpointManager"
3. Add the **GameCheckpointManager** component
4. Check "Persist Across Scenes" (usually you want this on)

**Optional**: Link your score and health managers if you want to save game data too.

---

### Step 2: Add Checkpoint Zones

1. Create a 3D object where you want the checkpoint (Cube or Cylinder works well)
2. Add the **InputCheckpointZone** component
3. Make sure the Collider is set to "Is Trigger" (should happen automatically)
4. Configure these settings:
   - **Trigger Object Tag**: "Player" (the tag on your player)
   - **One Time Use**: Check if checkpoint should only activate once
   - **Save Full State**: Check if you want to save score/health too

**Visual Feedback** (optional):
- Assign a **Visual Effect** GameObject to disable when activated
- Assign a **Checkpoint Renderer** and **Activated Material** to change color

---

### Step 3: Use CharacterControllerCC (Automatic!)

If you're using **CharacterControllerCC** for your player:
- ✅ **Nothing else to do!** It automatically finds checkpoints and spawns there.
- Make sure "Use Spawn Point Providers" is checked (it is by default)

**That's it!** Your checkpoints will work automatically when you:
- Die and restart the scene
- Reload the level
- Fall off the map

---

## How It Works

### The Magic Behind the Scenes

When your player starts (or restarts), the **CharacterControllerCC** does this:

1. **In Awake()**: "Is there a GameCheckpointManager with a saved checkpoint?"
2. **If yes**: "Spawn me at that position BEFORE physics starts!"
3. **If no**: "Stay at my current position in the scene"

This happens **before physics runs**, so there's no flickering or "teleporting" feel.

---

## Testing Your Checkpoints

### Quick Test Steps:

1. **Play** your scene
2. Walk into a checkpoint zone
3. Look for visual feedback (color change, effects disable)
4. Press **Restart** (or however you restart your scene)
5. **You should spawn at the checkpoint!**

### Debugging:

- Look in the **Console** for checkpoint messages:
  - "GameCheckpointManager: Checkpoint saved at (x, y, z)"
  - "CharacterControllerCC: Found spawn point at (x, y, z)"
  - "GameCheckpointManager: Player spawned at checkpoint (x, y, z)"

- Check the **Scene Gizmos**:
  - Yellow/Green wireframe = checkpoint trigger zone
  - Cyan flag icon = where player will spawn
  - Magenta line = spawn offset (if configured)
  - Blue arrow = spawn rotation (if using checkpoint rotation)

---

## Common Configurations

### Basic Checkpoint (Most Common)

```
InputCheckpointZone:
  - Trigger Object Tag: "Player"
  - One Time Use: ✓ (checked)
  - Save Full State: ✗ (unchecked)
  - Spawn Offset: (0, 0, 0)
  - Use Checkpoint Rotation: ✗ (unchecked)
```

**Use for**: Platformer checkpoints, progress markers

---

### Checkpoint with Spawn Offset

```
InputCheckpointZone:
  - Spawn Offset: (0, 1, 0)  ← Player spawns 1 meter above checkpoint
```

**Use when**: Checkpoint is on the ground but you want player to spawn slightly above

---

### Checkpoint with Rotation

```
InputCheckpointZone:
  - Use Checkpoint Rotation: ✓ (checked)
  - Rotate the checkpoint GameObject to face desired direction
```

**Use for**: Racing checkpoints (face forward), teleporters (face specific direction)

---

### Checkpoint with Game State

```
GameCheckpointManager:
  - Save Score: ✓ (checked)
  - Score Manager: [Link to GameCollectionManager]
  - Save Health: ✓ (checked)
  - Health Manager: [Link to GameHealthManager]

InputCheckpointZone:
  - Save Full State: ✓ (checked)
```

**Use for**: Adventure games, RPGs where you want to save progress AND game data

---

## Wiring Events (Optional)

You can connect checkpoint events to sounds, UI, animations, etc.

### On Checkpoint Activated:

```
InputCheckpointZone → onCheckpointActivated
  → ActionPlaySound (play "checkpoint_saved.wav")
  → ActionDisplayText (show "Checkpoint Saved!")
```

### On Checkpoint Restored:

```
GameCheckpointManager → onCheckpointRestored
  → ActionPlaySound (play "respawn.wav")
  → FadeInFromBlackOnRestart (already happens automatically)
```

---

## Multiple Checkpoints

You can have as many InputCheckpointZone components as you want!

**How it works**:
- Each checkpoint **overwrites** the previous saved checkpoint
- The most recent checkpoint is where you'll respawn
- Use "One Time Use" to prevent players from re-activating old checkpoints

---

## Advanced: Manual Checkpoint Saving

You can also save checkpoints via events (no trigger zone needed):

### Save Current Player Position:
```
[Your Event] → GameCheckpointManager.SaveCheckpointPosition()
```

### Save Specific Position:
```
[Your Event] → GameCheckpointManager.SaveCheckpointAtPosition(Vector3)
```

### Teleport Player to Last Checkpoint (Same Scene):
```
[Your Event] → GameCheckpointManager.TeleportPlayerToCheckpoint()
```

**Use for**: Manual save points, death triggers, debug commands

---

## Troubleshooting

### ❌ Player doesn't spawn at checkpoint after restart

**Check**:
1. Is "Persist Across Scenes" checked on GameCheckpointManager?
2. Did you actually activate a checkpoint first?
3. Is "Use Spawn Point Providers" checked on CharacterControllerCC?
4. Are you restarting the scene (not stopping/starting play mode)?

---

### ❌ Checkpoint doesn't activate when I walk into it

**Check**:
1. Is the Collider set to "Is Trigger"?
2. Does your player have the correct tag ("Player")?
3. Is "One Time Use" checked and you already activated it?

---

### ❌ Player spawns at wrong position

**Check**:
1. Look at the cyan flag gizmo - that shows exact spawn position
2. Adjust "Spawn Offset" if needed
3. Make sure you activated the correct checkpoint last

---

### ❌ Score/Health doesn't restore

**Check**:
1. Is "Save Full State" checked on InputCheckpointZone?
2. Are Score Manager and Health Manager linked on GameCheckpointManager?
3. Is "Save Score" and "Save Health" checked on GameCheckpointManager?

---

## Summary

✅ **Step 1**: Add GameCheckpointManager to scene
✅ **Step 2**: Add InputCheckpointZone where you want checkpoints
✅ **Step 3**: Use CharacterControllerCC (automatic!)

**That's it!** Your checkpoints work automatically - no code required!

---

## Next Steps

- Add sound effects to onCheckpointActivated
- Add visual feedback (particle effects, UI messages)
- Wire onCheckpointRestored to FadeInFromBlackOnRestart
- Experiment with spawn offsets and rotation

**You've got this!** 🎮
