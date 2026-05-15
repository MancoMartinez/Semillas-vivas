# Semillas Vivas

A 2D action-platformer for Android built with Unity 6 LTS. The player explores side-scrolling levels, collects seeds, defeats enemies, and faces a multi-phase boss fight. The project targets Android (API 26+) and uses the Universal Render Pipeline (URP).

---

## Requirements

| Tool | Version |
|------|---------|
| Unity | 6 LTS (6000.x) |
| Android Build Support module | included in Unity Hub |
| Android SDK / NDK | installed via Unity Hub |
| JDK | bundled with Unity or custom path |

---

## Opening the Project

1. Clone the repository.
   ```
   git clone https://github.com/MancoMartinez/Semillas_Vivas.git
   ```
2. Open **Unity Hub**, click **Add**, and select the cloned folder.
3. Let Unity import assets and compile scripts (first open may take several minutes).
4. In **File → Build Settings**, switch the platform to **Android** and click **Switch Platform**.

---

## Scene Structure

```
Assets/Scenes/
├── MainMenu          — title screen, settings, level select, seed collection gallery
├── Lvl1 – Lvl5      — side-scrolling gameplay levels (progressive difficulty)
└── Boss              — three-phase boss fight arena
```

Each gameplay scene uses `DemoGameplayInstaller` as its composition root (see Architecture below).

---

## Architecture & Code Patterns

### 1. Composition Root / Installer

**`DemoGameplayInstaller`** (`Scripts/Gameplay/Demo/`) is a `MonoBehaviour` that runs in `Awake` and wires all gameplay systems for the current scene. It creates, finds, or configures every controller, then calls `Initialize()` on each one in dependency order. No controller discovers its own dependencies; they are handed in from the outside.

```
DemoGameplayInstaller.Awake()
  ├── GameAudioService.EnsureInstance()
  ├── new DemoPlayerController(…).Initialize(…)
  ├── new DemoPlayerHealth(…).Initialize(…)
  ├── new DemoPlayerCombatController(…).Initialize(…)
  ├── new DemoGameplayUiController(…).Initialize(…)
  └── new DemoLevelFlowController(…).Initialize(…)
```

For the **Main Menu**, `MainMenuCompositionRoot` plays the same role, wiring `SettingsScreenController`, `LevelSelectScreenController`, and `CollectionScreenController`.

### 2. Dependency Injection via `Initialize()`

Controllers do not use `FindObjectOfType` or public Inspector fields to locate each other. They receive references through an `Initialize(…)` method called by the installer. This keeps each class independently testable and avoids hidden coupling.

### 3. Observer Pattern — C# Events

Loose coupling between systems is achieved with `System.Action` and `System.Action<T>` events.

| Publisher | Event | Subscriber |
|-----------|-------|------------|
| `DemoPlayerHealth` | `OnDied` | `DemoLevelFlowController` (triggers game-over) |
| `RangedMeleeEnemy` | `OnDied`, `OnHealthChanged` | `BossFightController` (phase transitions, health bar) |
| `DemoSeedCollector` | `OnSeedCollected` | `DemoGameplayUiController` (shows seed message) |

No subscriber holds a direct reference to the event publisher's internals; it only knows the delegate signature.

### 4. Singleton — `GameAudioService`

`GameAudioService` (`Scripts/Systems/Audio/`) is a persistent `MonoBehaviour` singleton (`DontDestroyOnLoad`). It owns three internal `AudioSource` buses (UI, SFX, Music) and a tracked list of world `AudioSource` instances created via `CreateWorldSource()`. Volume changes propagate to all live sources immediately. Settings are persisted through `PlayerPrefs` using the keys defined in `SettingsWirer`.

```
GameAudioService.Instance
  ├── _uiSource      — one-shot UI clicks
  ├── _sfxSource     — gameplay sound effects
  ├── _musicSource   — looping background music (scene-driven)
  └── _worldSources  — any AudioSource created by gameplay objects
```

`GameAudioCatalog` is a `ScriptableObject` (`Resources/GameAudioCatalog`) that maps `GameAudioCue` enum values to `AudioClip` assets. If the catalog is absent the service falls back to searching all loaded clips by name.

### 5. Object Pool — Boss Projectiles

`BossProjectile` (`Scripts/Gameplay/Boss/`) implements a simple static pool. Instances are recycled with `BossProjectile.Get(prefab, position, rotation)` and returned with `ReturnToPool()` rather than being destroyed, avoiding per-frame GC pressure during the boss fight.

### 6. State Machine (implicit) — Enemy AI

Each enemy type drives its behaviour through an explicit local state checked every `Update()`:

- `MeleeOnlyEnemy` — Idle → Walk → Attack1 → Defeat
- `SniperOnlyEnemy` — Idle → Attack1 → Defeat
- `RangedMeleeEnemy` — Idle → Attack1 (ranged) / Walk → Attack1 (melee) → Defeat

Animation state changes go through a single `ChangeAnimationState(string)` guard that skips redundant `Animator.Play` calls.

---

## Boss Fight Flow

The boss scene (`Boss`) runs entirely through `BossFightController`:

```
Phase 1  — RangedMeleeEnemy (boss character, 8 hits)
           On death → 3 s defeat animation
Phase 2  — Three guardian enemy types spawn under _phaseTwoRoot
           BossFightController monitors all guardians via OnDied events
           All guardians dead → Phase 3
Phase 3  — FinalBoss (BossEnemyActor) activates in idle mode
           UI message shown: "¡Elimina a todos los guardianes…"
           Player defeats remaining enemies → boss becomes vulnerable
           FinalBoss defeated → CompleteLevel() → next scene loads
```

`BossEnemyActor` exposes `SetPhase3IdleMode(bool)` to freeze its X position while guardians remain alive, preventing it from being displaced by enemy collisions.

---

## Settings & Audio Persistence

Volume levels are written to `PlayerPrefs` with the following keys:

| Key | Default |
|-----|---------|
| `settings.musicVolume` | `0.75` |
| `settings.effectsVolume` | `0.75` |

`SettingsWirer.WireSliders(Transform root)` finds the sliders under a given UI root by name heuristics (keywords: `music`, `musica`, `bgm`, `effect`, `sfx`, `sonido`, etc.) and registers `onValueChanged` listeners that persist and apply the value immediately.

---

## Mobile Controls

`MobileControlsOverlay` manages a virtual joystick (`MobileJoystick`) and action buttons (`MobileButton`) shown only on non-desktop platforms. Input state is aggregated in the static `MobileInputState` and consumed by `DemoPlayerController` the same way keyboard input would be, keeping the controller platform-agnostic.

---

## Building for Android

1. **File → Build Settings → Android**, ensure your scene order matches:
   `MainMenu` at index 0, levels in order, `Boss` last.
2. **Player Settings → Other Settings**:
   - Package Name: `com.yourcompany.semillasvivas`
   - Minimum API Level: **26 (Android 8.0)**
   - Target API Level: latest stable
   - Scripting Backend: **IL2CPP**
   - Target Architectures: **ARM64** (add ARMv7 for wider compatibility)
3. **Player Settings → Adaptive Icons** (API 26+): assign foreground and background layers under *Android → Adaptive Icons*. The Texture Type for icon textures must be **Default** (not Sprite).
4. Connect a device or start an emulator, then click **Build and Run**.

---

## Deprecated Scripts

The following scripts remain in the repository for historical reference. They are not attached to any active GameObject in any scene and can be safely ignored. They are kept to preserve git history and because some assets (prefabs, animations) originally built for them may still reference them indirectly.

| Script | Why deprecated | Replaced by |
|--------|---------------|-------------|
| `Enemy2D.cs` | Early combined ranged+melee enemy with hard-coded patrol and shoot logic. Was the only enemy type during prototyping. | `MeleeOnlyEnemy`, `SniperOnlyEnemy`, `RangedMeleeEnemy` |
| `EnemyProjectile2D.cs` | Projectile companion to `Enemy2D`. Used a simple forward-velocity approach with no target tracking. | `EnemyShootProjectile` (tracks the player transform) |
| `DemoEnemyController.cs` | Patrol + hover enemy controller with waypoint-based movement. Predates the specialized enemy types. | `SimpleEnemyPatrol` + the three specialized enemy scripts |
| `DemoEnemyPatrolPath.cs` | Waypoint path helper used exclusively by `DemoEnemyController`. Has no consumers in the current scenes. | Removed from scenes; `SimpleEnemyPatrol` handles patrol inline |
| `CharacterSelectController.cs` | UI controller for a character-selection screen that was cut before release. The screen is not present in any scene. | Feature removed; selection screen does not exist |

---

## Project Structure (Scripts)

```
Assets/Scripts/
├── Gameplay/
│   ├── Boss/
│   │   ├── BossEnemyActor.cs          — final boss movement, attacks, phase 3 idle lock
│   │   ├── BossFightController.cs     — phase orchestration, guardian monitoring
│   │   └── BossProjectile.cs          — pooled projectile for boss attacks
│   ├── Demo/
│   │   ├── DemoGameplayInstaller.cs   — composition root for all gameplay scenes
│   │   ├── DemoLevelFlowController.cs — win/lose conditions, scene transitions
│   │   ├── DemoPlayerController.cs    — movement, jump, platform detection
│   │   ├── DemoPlayerHealth.cs        — HP, knockback, death event
│   │   ├── DemoPlayerCombatController.cs — attack hitbox, combo logic
│   │   ├── DemoPlayerAnimationController.cs — animation state bridge
│   │   ├── DemoPlayerPowerUpController.cs   — power-up state management
│   │   ├── DemoSeedCollector.cs       — seed pickup, OnSeedCollected event
│   │   ├── DemoPowerUpPickup.cs       — power-up item behaviour
│   │   ├── DemoLevelEndTrigger.cs     — trigger zone that ends the level
│   │   ├── DemoBackgroundFollower.cs  — parallax background scroll
│   │   ├── DemoCameraFollow2D.cs      — smooth camera follow with bounds
│   │   ├── DemoCharacterAudioController.cs — per-character SFX wiring
│   │   ├── CopoEnemy.cs               — flying snowflake enemy (Lvl3)
│   │   ├── CopoProjectile.cs          — projectile for CopoEnemy
│   │   ├── MurcielagoEnemy.cs         — bat enemy with dive attack
│   │   ├── SimpleEnemyPatrol.cs       — basic left-right patrol enemy
│   │   ├── MovingPlatform.cs          — platform with waypoint movement
│   │   ├── MobileControlsOverlay.cs   — shows/hides touch controls
│   │   ├── MobileJoystick.cs          — virtual joystick input
│   │   ├── MobileButton.cs            — virtual action button input
│   │   ├── MobileInputState.cs        — shared static input state
│   │   ├── EmptyGraphic.cs            — transparent raycast-blocking UI graphic
│   │   ├── ISeedPickupEffect.cs       — interface for seed pickup side-effects
│   │   └── UI/
│   │       └── DemoGameplayUiController.cs — HUD: health, seeds, messages
│   ├── Enemy2D.cs              ← DEPRECATED
│   ├── EnemyProjectile2D.cs    ← DEPRECATED
│   ├── EnemyShootProjectile.cs — active homing enemy projectile
│   ├── MeleeOnlyEnemy.cs       — melee guardian (phase 2/3)
│   ├── RangedMeleeEnemy.cs     — ranged+melee hybrid (phase 1 boss body)
│   └── SniperOnlyEnemy.cs      — long-range shoot-only guardian
├── Systems/
│   ├── Audio/
│   │   ├── GameAudioService.cs  — singleton audio manager
│   │   ├── GameAudioCatalog.cs  — ScriptableObject: cue → clip mapping
│   │   ├── GameAudioCue.cs      — enum of all named audio events
│   │   └── GameAudioSetup.cs    — scene bootstrap that calls EnsureInstance
│   ├── LevelProgressService.cs  — PlayerPrefs wrapper for level unlock state
│   ├── LevelSeedCatalog.cs      — ScriptableObject: seeds per level
│   └── SettingsWirer.cs         — slider ↔ PlayerPrefs ↔ AudioService bridge
└── UI/
    └── MainMenu/
        ├── MainMenuCompositionRoot.cs      — composition root for main menu
        ├── SettingsScreenController.cs     — settings panel logic
        ├── LevelSelectScreenController.cs  — level select panel logic
        ├── CollectionScreenController.cs   — seed gallery panel logic
        └── CharacterSelectController.cs    ← DEPRECATED
    └── Navigation/
        └── UIScreenManager.cs              — show/hide screen panels by name
```

---

## Contributing

Pull requests are welcome. Keep each class focused on a single responsibility, pass dependencies through `Initialize()` rather than `FindObjectOfType`, and route audio through `GameAudioService` rather than attaching `AudioSource` components directly to prefabs.
