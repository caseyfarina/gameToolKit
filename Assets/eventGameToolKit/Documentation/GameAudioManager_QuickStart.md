# GameAudioManager Quick Start Guide

**Centralized audio control for music, ambient sounds, and sound effects**

Get background music playing in 3 minutes, then explore crossfading, ambient audio, and gradual volume control!

---

## Table of Contents

1. [3-Minute Quick Start](#3-minute-quick-start)
2. [Three Audio Channels](#three-audio-channels)
3. [Music System](#music-system)
4. [Ambient System](#ambient-system)
5. [Sound Effects](#sound-effects)
6. [Volume Control](#volume-control)
7. [Audio Mixer Integration](#audio-mixer-integration)
8. [Common Scenarios](#common-scenarios)
9. [Troubleshooting](#troubleshooting)

---

## 3-Minute Quick Start

**Goal:** Get background music playing with smooth crossfading.

### Step 1: Add the Component (30 seconds)

1. Create an empty GameObject (`GameObject > Create Empty`)
2. Name it "AudioManager"
3. Click **Add Component**
4. Search for **"GameAudioManager"**

**That's it!** The script will create its own AudioSources at runtime.

### Step 2: Import Your Audio (1 minute)

1. Drag your music files into your Unity project (MP3, WAV, or OGG)
2. Create a folder called **Resources** in your Assets folder
3. Drag your audio files into the **Resources** folder

**Example structure:**
```
Assets/
  Resources/
    Music/
      MainTheme.mp3
      BattleMusic.mp3
    Ambient/
      ForestAmbience.wav
```

### Step 3: Play Music via Event (1 minute)

1. Add an **InputKeyPress** component to any GameObject
2. Set **Key To Detect** to `M`
3. In **On Key Pressed** event:
   - Click **+** to add event
   - Drag your **AudioManager** GameObject
   - Select: **GameAudioManager > PlayMusicByName**
   - Enter: `Music/MainTheme` (path inside Resources folder, no extension)

### Step 4: Test It (30 seconds)

1. Press **Play** in Unity
2. Press **M** key
3. Music fades in and plays!

**Press M again with different music to hear crossfading!**

---

## Three Audio Channels

GameAudioManager provides **three independent audio channels**:

| Channel | Purpose | Loop | Crossfade |
|---------|---------|------|-----------|
| **Music** | Background music tracks | Yes | Yes |
| **Ambient** | Environmental sounds (forest, rain, city) | Yes | Yes |
| **SFX** | One-shot sound effects (jump, pickup, hit) | No | No (layers) |

Each channel has its own volume control and can be played/stopped independently.

---

## Music System

### Playing Music

**From UnityEvent (No Code):**

```
Wire: InputKeyPress → onKeyPressed → GameAudioManager → PlayMusicByName
Parameter: "Music/MainTheme"
```

**Available Methods:**

| Method | Description |
|--------|-------------|
| `PlayMusicByName(string)` | Load and play from Resources folder |
| `PlayMusic(AudioClip)` | Play an assigned AudioClip |
| `PlayMusic(AudioClip, bool)` | Play with optional fade (true = fade in) |
| `StopMusic()` | Stop with fade out |
| `StopMusic(bool)` | Stop with optional fade (false = instant) |
| `PauseMusic()` | Pause current track |
| `ResumeMusic()` | Resume paused track |

### Crossfading

**Automatic!** When music is already playing and you call `PlayMusic()` again:

1. Current track fades out
2. New track fades in simultaneously
3. Smooth transition with no gap

**Crossfade Duration:** Set via `Default Fade Duration` in Inspector (default: 2 seconds)

### Music Events

Wire these to respond to music changes:

- **onMusicStarted** - Fires when music begins playing
- **onMusicStopped** - Fires when music stops

**Example: Disable player during music transitions**
```
onMusicStarted → PlayerController.enabled = false
onMusicStopped → PlayerController.enabled = true
```

---

## Ambient System

Ambient audio works exactly like music - loops and crossfades - but on a separate channel.

### Playing Ambient

**From UnityEvent:**

```
Wire: InputTriggerZone → onTriggerEnter → GameAudioManager → PlayAmbientByName
Parameter: "Ambient/ForestAmbience"
```

**Available Methods:**

| Method | Description |
|--------|-------------|
| `PlayAmbientByName(string)` | Load and play from Resources folder |
| `PlayAmbient(AudioClip)` | Play an assigned AudioClip |
| `PlayAmbient(AudioClip, bool)` | Play with optional fade |
| `StopAmbient()` | Stop with fade out |
| `StopAmbient(bool)` | Stop with optional fade |
| `PauseAmbient()` | Pause ambient audio |
| `ResumeAmbient()` | Resume paused ambient |

### Ambient Crossfading

**Automatic!** When ambient is already playing and you call `PlayAmbient()` again:

- Forest ambience crossfades to cave ambience when entering a cave
- Rain sounds crossfade to indoor ambience when entering a building

### Ambient Events

- **onAmbientStarted** - Fires when ambient begins
- **onAmbientStopped** - Fires when ambient stops

---

## Sound Effects

Sound effects are **one-shot** sounds that layer on top of each other - no crossfading.

### Playing SFX

**Method 1: By Index (Inspector Array)**

1. In GameAudioManager Inspector, find **Sound Effects** array
2. Add your AudioClips to the array
3. Wire: `InputKeyPress → onKeyPressed → PlaySoundEffect` with index (0, 1, 2...)

**Method 2: By Resource Path**

```
Wire: InputTriggerZone → onTriggerEnter → GameAudioManager → PlaySFXByName
Parameter: "SFX/Pickup"
```

**Method 3: From Code**

```csharp
public AudioClip jumpSound;
audioManager.PlaySoundEffect(jumpSound);
audioManager.PlaySoundEffect(jumpSound, 0.5f); // With volume
```

### SFX Methods

| Method | Description |
|--------|-------------|
| `PlaySoundEffect(int)` | Play by index from Sound Effects array |
| `PlaySoundEffect(AudioClip)` | Play a specific clip |
| `PlaySoundEffect(AudioClip, float)` | Play with volume (0-1) |
| `PlaySFXByName(string)` | Load and play from Resources folder |

### SFX Events

- **onSoundEffectPlayed** - Fires each time an SFX plays

---

## Volume Control

### Instant Volume Change

Set volume immediately (0-1 range):

| Method | Description |
|--------|-------------|
| `SetMasterVolume(float)` | Master volume (requires AudioMixer) |
| `SetMusicVolume(float)` | Music channel volume |
| `SetSFXVolume(float)` | SFX channel volume |

**Wire to UI Slider:**
```
UI Slider → On Value Changed → GameAudioManager → SetMusicVolume
```

### Gradual Volume Change

Smoothly transition volume over time - great for cinematic moments!

| Method | Description |
|--------|-------------|
| `SetMusicVolumeGradual(float)` | Fade music volume (uses default duration) |
| `SetMusicVolumeGradual(float, float)` | Fade with custom duration |
| `SetAmbientVolumeGradual(float)` | Fade ambient volume |
| `SetAmbientVolumeGradual(float, float)` | Fade with custom duration |
| `SetSFXVolumeGradual(float)` | Fade SFX volume |
| `SetSFXVolumeGradual(float, float)` | Fade with custom duration |
| `SetMasterVolumeGradual(float)` | Fade master volume (requires AudioMixer) |
| `SetMasterVolumeGradual(float, float)` | Fade with custom duration |

**Example: Fade music to half volume over 3 seconds**
```
Wire: Button → OnClick → GameAudioManager → SetMusicVolumeGradual
Parameters: 0.5, 3
```

### Volume Change Event

- **onVolumeChangeComplete** - Fires when any gradual volume change finishes

**Example: Trigger something after music fades out**
```
onVolumeChangeComplete → LoadNextScene
```

---

## Audio Mixer Integration

For advanced audio control, use Unity's AudioMixer system.

### Setup

1. Create AudioMixer: `Assets > Create > Audio Mixer`
2. In AudioMixer window:
   - Right-click **Master** → Add child group → Name it **Music**
   - Right-click **Master** → Add child group → Name it **SFX**
   - Right-click **Master** → Add child group → Name it **Ambient**
3. For each group, right-click the Volume parameter → **Expose to script**
4. Name exposed parameters: `MasterVolume`, `MusicVolume`, `SFXVolume`
5. Assign AudioMixer to GameAudioManager's **Audio Mixer** field

### Auto-Assignment

GameAudioManager automatically assigns AudioSources to mixer groups if groups named "Music", "SFX", and "Ambient" exist.

### Mixer Volume Parameters

Default parameter names (customizable in Inspector):
- `MasterVolume`
- `MusicVolume`
- `SFXVolume`

Change these in the Inspector if your mixer uses different names.

---

## Common Scenarios

### Scenario 1: Background Music on Scene Load

**Goal:** Play music automatically when scene starts.

**Setup:**
1. Add GameAudioManager to scene
2. Add an **InputTriggerZone** to an invisible trigger at player spawn
3. Wire: `onTriggerEnter → PlayMusicByName("Music/LevelTheme")`

**Alternative: Use Start event from any script**

---

### Scenario 2: Zone-Based Ambient Audio

**Goal:** Different ambient sounds in different areas.

**Setup:**
1. Create trigger zones for each area (forest, cave, village)
2. Add InputTriggerZone to each
3. Wire each zone:
   - Forest Zone: `onTriggerEnter → PlayAmbientByName("Ambient/Forest")`
   - Cave Zone: `onTriggerEnter → PlayAmbientByName("Ambient/Cave")`
   - Village Zone: `onTriggerEnter → PlayAmbientByName("Ambient/Village")`

**Result:** Ambient audio crossfades as player moves between zones!

---

### Scenario 3: Battle Music Transition

**Goal:** Crossfade to battle music when enemy appears.

**Setup:**
1. Enemy has a trigger zone
2. Wire: `onTriggerEnter → PlayMusicByName("Music/BattleTheme")`
3. When battle ends: `onBattleComplete → PlayMusicByName("Music/ExploreTheme")`

**Result:** Smooth crossfade between exploration and battle music.

---

### Scenario 4: Collectible Pickup Sound

**Goal:** Play sound when collecting items.

**Setup:**
1. Add GameAudioManager to scene
2. In Inspector, add your pickup sound to **Sound Effects** array (index 0)
3. On collectible prefab, add InputTriggerZone
4. Wire: `onTriggerEnter → PlaySoundEffect(0)`

**Alternative:** Use `PlaySFXByName("SFX/Pickup")` if audio is in Resources folder.

---

### Scenario 5: Cinematic Music Fade

**Goal:** Gradually fade music during cutscene.

**Setup:**
1. Cutscene starts: `SetMusicVolumeGradual(0.2f, 2f)` - fade to 20% over 2 seconds
2. Cutscene ends: `SetMusicVolumeGradual(1f, 1f)` - fade back to 100% over 1 second

**Result:** Music ducks during dialogue, returns after.

---

### Scenario 6: Pause Menu Audio

**Goal:** Mute SFX but keep music during pause.

**Setup:**
1. On pause:
   - `PauseAmbient()`
   - `SetSFXVolumeGradual(0f, 0.3f)` - fade out SFX

2. On unpause:
   - `ResumeAmbient()`
   - `SetSFXVolumeGradual(1f, 0.3f)` - fade in SFX

---

### Scenario 7: Stop All Audio

**Goal:** Silence everything immediately (scene transition, game over).

**Setup:**
```
Wire: GameStateManager → onGameOver → GameAudioManager → StopAllAudio
```

**Result:** Music, ambient, and SFX all stop immediately (no fade).

---

## Inspector Reference

### Audio Mixer Section
- **Audio Mixer** - Assign your AudioMixer asset
- **Master Volume Parameter** - Exposed parameter name for master volume
- **Music Volume Parameter** - Exposed parameter name for music volume
- **SFX Volume Parameter** - Exposed parameter name for SFX volume

### Music Settings
- **Music Source** - Optional pre-assigned AudioSource (auto-created if empty)
- **Default Fade Duration** - Duration for crossfades and volume transitions (default: 2s)

### Sound Effects
- **SFX Source** - Optional pre-assigned AudioSource (auto-created if empty)
- **Sound Effects** - Array of AudioClips accessible by index

### Ambient Settings
- **Ambient Source** - Optional pre-assigned AudioSource (auto-created if empty)

### Events
- **onMusicStarted** - Music begins playing
- **onMusicStopped** - Music stops
- **onSoundEffectPlayed** - SFX plays
- **onAmbientStarted** - Ambient begins
- **onAmbientStopped** - Ambient stops
- **onVolumeChangeComplete** - Gradual volume change finishes

---

## Public Properties

Check audio state from code:

```csharp
if (audioManager.IsMusicPlaying) { }
if (audioManager.IsAmbientPlaying) { }
if (audioManager.IsFading) { }
```

---

## Troubleshooting

### "Music doesn't play"

**Check:**
- Is the AudioClip assigned or path correct?
- For `PlayMusicByName()`: Is file in **Resources** folder?
- Path should NOT include "Resources/" prefix or file extension

**Example:**
- File: `Assets/Resources/Music/Theme.mp3`
- Path: `"Music/Theme"` (correct)
- Path: `"Resources/Music/Theme.mp3"` (wrong)

---

### "Crossfade doesn't work"

**Check:**
- Is music already playing when you call `PlayMusic()` again?
- Is `fadeIn` parameter true? (default is true)
- Check **Default Fade Duration** isn't set to 0

---

### "Volume control doesn't work"

**Check:**
- For mixer volume: Is AudioMixer assigned?
- Are exposed parameter names correct?
- Did you expose the Volume parameter in the mixer? (Right-click → Expose)

---

### "Gradual volume change is too fast/slow"

**Fix:**
- Use the two-parameter version: `SetMusicVolumeGradual(0.5f, 3f)` for 3-second fade
- Default duration comes from **Default Fade Duration** field

---

### "SFX doesn't play"

**Check:**
- For index method: Is index valid? (0, 1, 2... within array bounds)
- For `PlaySFXByName()`: Is file in **Resources** folder?
- Is AudioClip imported correctly? (Check import settings)

---

### "Ambient and music overlap weirdly"

**Expected!** Music and ambient are independent channels. They're designed to play simultaneously:
- Music = soundtrack
- Ambient = environmental sounds (forest, rain, machinery)

If you want only one, call `StopMusic()` or `StopAmbient()` first.

---

## Quick Reference

### Minimum Setup
```
1. Add GameAudioManager to scene
2. Put audio files in Assets/Resources folder
3. Wire events to PlayMusicByName/PlayAmbientByName/PlaySFXByName
```

### Common Method Calls

**Music:**
- `PlayMusicByName("Music/Theme")` - Play from Resources
- `StopMusic()` - Fade out and stop
- `SetMusicVolumeGradual(0.5f)` - Fade to 50%

**Ambient:**
- `PlayAmbientByName("Ambient/Forest")` - Play from Resources
- `StopAmbient()` - Fade out and stop
- `SetAmbientVolumeGradual(0.3f)` - Fade to 30%

**SFX:**
- `PlaySoundEffect(0)` - Play by index
- `PlaySFXByName("SFX/Jump")` - Play from Resources

**Control:**
- `StopAllAudio()` - Stop everything immediately
- `PauseMusic()` / `ResumeMusic()` - Pause/resume music
- `PauseAmbient()` / `ResumeAmbient()` - Pause/resume ambient

---

## You're Ready!

Start with the **3-Minute Quick Start**, get music playing, then explore:

1. Add ambient sounds for different zones
2. Wire up SFX for player actions
3. Use gradual volume for cinematic moments
4. Set up AudioMixer for advanced control

**Three channels, smooth crossfading, gradual volume - all from UnityEvents!**
