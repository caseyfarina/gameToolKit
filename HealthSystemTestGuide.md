# Health System Testing Guide

## Manual Testing Checklist

### 1. Basic Health Events

#### onHealthChanged
- **Test**: Take any damage or heal any amount
- **Expected**: Event fires every time health value changes
- **Setup**: Connect to debug log or UI text update
- **Pass/Fail**: ⬜

#### onDamageReceived
- **Test**: Take damage (any amount)
- **Expected**: Event fires only when taking damage, not when healing
- **Setup**: Connect to damage effect (red screen flash, sound)
- **Pass/Fail**: ⬜

#### onHealthGained
- **Test**: Use heal method or health pickup
- **Expected**: Event fires only when gaining health, not when taking damage
- **Setup**: Connect to healing effect (green particles, sound)
- **Pass/Fail**: ⬜

### 2. Threshold Events

#### onLowHealthReached
- **Test**: Start with high health (50+), take damage to cross threshold (25)
- **Expected**: Fires exactly once when crossing FROM above TO below threshold
- **Setup**: Connect to warning UI (red screen tint, warning sound)
- **Pass/Fail**: ⬜

#### onLowHealthRecovered
- **Test**: Start in low health (<25), heal above threshold (>25)
- **Expected**: Fires exactly once when crossing FROM below TO above threshold
- **Setup**: Connect to remove warning effects
- **Pass/Fail**: ⬜

### 3. Death/Revival Events

#### onDeath
- **Test**: Take enough damage to reach 0 health
- **Expected**: Fires exactly once when reaching 0 health
- **Setup**: Connect to game over screen, mesh disable, death sound
- **Pass/Fail**: ⬜ (We know this works!)

#### onRevived
- **Test**: Set health to >0 when dead, or use SetHealth() method
- **Expected**: Fires when going from 0 health to any positive value
- **Setup**: Connect to re-enable mesh, revival effects
- **Pass/Fail**: ⬜

## Test Scenarios

### Scenario 1: Normal Damage Sequence
1. Start: 100 health
2. Take 30 damage → 70 health
3. Take 50 damage → 20 health (crosses low health threshold)
4. Take 25 damage → 0 health (death)

**Expected Events**:
- onHealthChanged: 3 times
- onDamageReceived: 3 times
- onLowHealthReached: 1 time (at 20 health)
- onDeath: 1 time (at 0 health)

### Scenario 2: Healing Sequence
1. Start: 20 health (low health)
2. Heal 10 → 30 health (crosses threshold)
3. Heal 20 → 50 health
4. Heal 60 → 100 health (capped at max)

**Expected Events**:
- onHealthChanged: 3 times
- onHealthGained: 3 times
- onLowHealthRecovered: 1 time (at 30 health)

### Scenario 3: Death and Revival
1. Start: 30 health
2. Take 35 damage → 0 health (death)
3. SetHealth(50) → 50 health (revival)
4. Take 60 damage → 0 health (death again)

**Expected Events**:
- onDeath: 2 times
- onRevived: 1 time
- Multiple health change events

### Scenario 4: Edge Cases
1. Take 0 damage (should do nothing)
2. Take negative damage (should do nothing)
3. Heal when at max health (should do nothing)
4. Multiple damage while dead (should be blocked)

## Debugging Helper Methods

Add these temporary methods to GameHealthManager for testing: