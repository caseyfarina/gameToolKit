# Multi-Scene Setup Quick Start

This guide explains how to create games with multiple scenes while keeping health, score, and other data consistent across levels.

## Two Approaches

| Approach | Best For | Complexity |
|----------|----------|------------|
| **Manager Prefabs + SO Variables** | Most projects | Simple |
| **Bootstrap Scene** | Advanced projects with persistent player | Complex |

This guide focuses on the **Manager Prefabs approach** - it's simpler and works great for most student projects.

---

## Manager Prefabs Approach

### How It Works

Each scene contains its own copy of the managers (as prefab instances). Data is stored in **ScriptableObject Variable assets** that persist across scenes.

```
Project Assets:
├── Prefabs/
│   └── GameManagers.prefab
└── Variables/
    ├── PlayerHealth.asset (IntVariable)
    └── PlayerScore.asset (IntVariable)

Level1 Scene:
├── GameManagers (prefab instance)
│   ├── GameHealthManager → uses PlayerHealth.asset
│   └── GameCollectionManager → uses PlayerScore.asset
├── Player (prefab instance)
├── Enemy → wired to GameHealthManager.TakeDamage()
└── Coin → wired to GameCollectionManager.Increment()

Level2 Scene:
├── GameManagers (prefab instance) ← same prefab
├── Player (prefab instance) ← same prefab
└── ... level content
```

When Level1 unloads and Level2 loads:
- The manager instances are new, but they read from the **same** variable assets
- Health and score persist because they're stored in the assets, not the managers

### Benefits

- All Inspector wiring works normally (drag managers directly)
- Each scene is self-contained and testable
- No DontDestroyOnLoad complexity
- Familiar workflow for students

---

## Setup Instructions

### Step 1: Create Variable Assets

1. In Project window, right-click → **Create > eventGameToolKit > Variables > Int Variable**
2. Name it `PlayerHealth`
3. Set **Default Value** to `100`
4. Optionally enable **Use Min Value** (0) and **Use Max Value** (100)

Repeat for score:
1. Create another Int Variable named `PlayerScore`
2. Set **Default Value** to `0`

### Step 2: Configure Managers

1. Select your **GameHealthManager**
2. In the **Multi-Scene Persistence** section, assign `PlayerHealth.asset`
3. Select your **GameCollectionManager**
4. In the **Multi-Scene Persistence** section, assign `PlayerScore.asset`

### Step 3: Create Manager Prefab

1. Create an empty GameObject called `GameManagers`
2. Add GameHealthManager, GameCollectionManager, and other managers as children
3. Drag the GameObject to your Prefabs folder to create a prefab
4. Delete the scene instance (you'll use the prefab instead)

### Step 4: Set Up Each Scene

For each level scene:
1. Drag the `GameManagers` prefab into the scene
2. Drag your `Player` prefab into the scene
3. Add a **SpawnPoint** where the player should appear
4. Wire enemies, collectibles, etc. to the manager instances

### Step 5: Scene Transitions

Add scene loading triggers:

**Example: Door to Level 2**
1. Add **InputTriggerZone** to a door
2. Add a script or use UnityEvents to call `SceneManager.LoadScene("Level2")`

Or use **GameSceneManager** for advanced transitions with fade effects.

---

## Variable Assets Reference

### IntVariable

| Setting | Description |
|---------|-------------|
| Default Value | Starting value when game begins |
| Use Min Value | Enable to clamp at minimum |
| Min Value | Lowest allowed value |
| Use Max Value | Enable to clamp at maximum |
| Max Value | Highest allowed value |

### FloatVariable

Same as IntVariable, but for decimal values (timers, percentages, etc.)

---

## How Values Reset

**In Editor:**
- Values automatically reset to Default Value when you enter Play mode
- This prevents "dirty" data from previous play sessions

**In Builds:**
- Values reset when the game launches
- Values persist across scene loads during gameplay
- Values reset when the player quits and restarts the game

**Manual Reset:**
- Call `healthVariable.ResetToDefault()` for "New Game" functionality
- Wire this to a menu button if needed

---

## Example: Complete Level Setup

```
Level1 Scene:
├── GameManagers (prefab)
│   ├── GameHealthManager
│   │   └── Health Variable: PlayerHealth.asset
│   ├── GameCollectionManager
│   │   └── Value Variable: PlayerScore.asset
│   └── GameCheckpointManager
├── Player (prefab)
│   └── CharacterControllerCC
├── SpawnPoint (Is Default: true)
├── UI Canvas
│   ├── Health Bar → reads from GameHealthManager
│   └── Score Text → reads from GameCollectionManager
├── Enemy
│   └── OnPlayerHit → GameHealthManager.TakeDamage(10)
├── Coin
│   └── OnCollected → GameCollectionManager.Increment(1)
└── DoorToLevel2
    └── OnEnter → LoadScene("Level2")
```

---

## Spawn Points

Each scene needs at least one **SpawnPoint** to mark where the player appears:

1. Create an empty GameObject
2. Add the **SpawnPoint** component
3. Position it where the player should spawn
4. Check **Is Default Spawn Point**

For scenes with multiple entry points, use **Spawn IDs**:
- SpawnPoint "from_level1" with Spawn Id: `from_level1`
- SpawnPoint "from_level3" with Spawn Id: `from_level3`

---

## Checkpoints

Checkpoints work automatically across scenes:

1. Add **GameCheckpointManager** to your managers prefab
2. Add **InputCheckpointZone** triggers in your levels
3. When player touches checkpoint, position is saved
4. If player dies and respawns, they return to the checkpoint

Checkpoints take priority over SpawnPoints - players always respawn at their last checkpoint.

---

## Tips

- **Test each scene independently** - they should work on their own
- **One Default SpawnPoint per scene** - mark only one as default
- **Variable assets are shared** - changes in one scene affect all scenes
- **Prefab changes propagate** - update the manager prefab to update all scenes

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Health/score resets between scenes | Assign IntVariable assets to managers |
| Can't drag manager to enemy event | Manager must be in same scene (use prefabs) |
| Player position wrong in new scene | Add SpawnPoint and mark as default |
| Values don't reset on restart | Call `ResetToDefault()` on variable assets |
| Editor shows old values | Values reset on Play - this is normal |

---

## Advanced: Bootstrap Scene Pattern

For complex games needing a persistent player (same player instance across all scenes), see the Bootstrap Scene documentation. This approach uses DontDestroyOnLoad and additive scene loading, but requires more setup.
