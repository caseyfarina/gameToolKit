# Changelog

All notable changes to the Event Game Toolkit will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-28

### Initial Release

Complete educational toolkit with 46 fully-documented components across 8 categories.

### Added

#### Input Components (6)
- InputTriggerZone - 3D collision detection with tag filtering
- InputKeyPress - Simple key press event system
- InputKeyCountdown - Key-based countdown with TextMeshPro display
- InputCheckpointZone - Checkpoint trigger system
- InputMouseInteraction - Mouse-based interaction
- InputQuitGame - Application quit on ESC key

#### Action Components (12 + 4 editors)
- ActionSpawnObject - Manual single object spawning
- ActionAutoSpawner - Automatic object spawner with random intervals
- ActionDisplayText - TextMeshPro text with DOTween animations
- ActionDisplayImage - UI image display with DOTween fade/scale
- ActionDialogueSequence - Complete dialogue system for visual novels
- DialogueUIController - Dialogue UI management (companion)
- ActionDecalSequence - Frame-by-frame URP decal material animation
- ActionDecalSequenceLibrary - Manager for multiple decal sequences
- ActionBlinkDecal - Automatic eye blinking (material-based)
- ActionBlinkDecalOptimized - Optimized blinking (texture-based)
- ActionRestartScene - Scene restart functionality
- ActionRespawnPlayer - Player respawn system
- ActionPlaySound - Audio playback
- ActionEventSequencer - Sequential event execution
- ActionPlatformAnimator - Physics-based platform animation

#### Physics Components (10 + 1 editor)
- CharacterControllerCC - Advanced humanoid controller with moving platforms
- PhysicsBallPlayerController - Simple ball-based physics controller
- PhysicsCharacterController - Rigidbody-based character controller
- PhysicsEnemyController - AI enemy with player detection and chase
- EnemyControllerCC - CharacterController-based AI enemy
- PhysicsBumper - Advanced bumper with DOTween animations
- PhysicsBumperTag - Tag-based bumper variant
- PhysicsPlatformStick - Moving platform attachment system
- PhysicsPlatformAnimator - Physics-based platform movement

#### Game Management (9)
- GameStateManager - Pause and victory state management
- GameTimerManager - Comprehensive timer system (count-up/countdown)
- GameHealthManager - Health system with damage, healing, thresholds
- GameCollectionManager - Score/collection tracking with thresholds
- GameInventorySlot - Inventory management with capacity limits
- GameUIManager - UI data display with DOTween animations
- GameAudioManager - Audio system with mixer integration
- GameCameraManager - Cinemachine camera switching
- GameCheckpointManager - Persistent checkpoint system

#### Puzzle System (2 + 2 editors)
- PuzzleSwitch - Multi-state switch component
- PuzzleSwitchChecker - Verify switch state combinations

#### Animation (1 + 1 editor)
- ActionAnimateTransform - DOTween transform animation with AnimationCurves

#### UI Components (1)
- FadeInFromBlackOnRestart - Automatic scene transition fade

#### Documentation & Tools
- Script Documentation Generator - Auto-generates visual documentation
- 11 Example Scene Generators - One-click example scene creation
- CharacterControllerCC_Documentation.md (940 lines)
- DecalAnimationSystem_Documentation.md (830 lines)
- CLAUDE_STUDENT.md - Comprehensive student reference guide

### Technical Details

- **Unity Version:** Unity 6 (6000.0.0f1+)
- **Render Pipeline:** URP 17.0.3+
- **Dependencies:** Input System 1.11.2, TextMeshPro 3.0.6, Cinemachine 3.1.2
- **Animation Engine:** DOTween FREE (included)
- **XML Documentation:** 100% coverage (46/46 scripts)

### DOTween Integration

Replaced 405+ lines of manual coroutine code with DOTween:
- ActionDisplayImage - Fade and scale animations
- ActionDisplayText - Typewriter effect
- FadeInFromBlackOnRestart - Scene transitions (4 lines!)
- GameUIManager - Punch scale and value tweening
- PhysicsBumper - Scale and emission animations with AnimationCurve support
- ActionAnimateTransform - Multi-property animations

### Educational Features

- **No-Code Architecture** - Visual UnityEvent-driven design
- **Modular Components** - Each script serves a specific purpose
- **Comprehensive Examples** - 11 example scene generators
- **Auto-Documentation** - Script Documentation Generator tool
- **Student-Friendly** - Encouraging documentation with Quick Start guides

---

## [Unreleased]

### Planned Features

- Additional puzzle mechanics
- More character controller variants
- Expanded dialogue system with branching
- Save/load system integration
- More example scenes

---

## Version Numbering

We use [Semantic Versioning](https://semver.org/):
- **MAJOR** version: Incompatible API changes
- **MINOR** version: New functionality (backwards-compatible)
- **PATCH** version: Bug fixes (backwards-compatible)

---

[1.0.0]: https://github.com/caseyfarina/eventGameToolKit/releases/tag/v1.0.0
