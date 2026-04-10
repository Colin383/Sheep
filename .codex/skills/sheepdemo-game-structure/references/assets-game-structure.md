# SheepDemo Assets/Game Structure Record

Updated: 2026-04-10
Scope: `D:\GitStore\SheepDemo\SheepDemo\Assets\Game`

## Snapshot

`Assets/Game` currently has 11 top-level folders. By file count, the largest areas are `UI` (656 files), `Scripts` (207 files, including 90 `.cs` files), and `Editor` (22 files).

Top-level layout:

| Folder | File Count | Notes |
| --- | ---: | --- |
| `ArtSrc` | 21 | Art source assets. |
| `Configs` | 12 | Luban-generated JSON config tables and meta files. |
| `DB` | 12 | Save data and settings ScriptableObjects plus partial classes. |
| `Editor` | 22 | Unity editor tooling for config generation, UI generation, sprite import, and atlas packing. |
| `LevelConfig` | 4 | Standalone level JSON files. |
| `Prefabs` | 10 | Shared gameplay prefabs, currently animal prefabs. |
| `Resources` | 1 | Minimal root resource folder at the moment. |
| `Scenes` | 4 | `Loading.unity` and `Main.unity` plus meta files. |
| `Scripts` | 207 | Runtime gameplay, SDK, config, and helper code. |
| `Settings` | 4 | Generated DB manager code and DB setting asset. |
| `UI` | 656 | Panel prefabs, generated scripts, sprites, and shared UI resources. |

## Runtime Ownership

### Startup and hot reload

- `Scripts/HotReload/HotReloadCtrl.cs` owns the hot-update state machine.
- `Scripts/HotReload/HotReloadCtrl_Nodes/*` splits the package workflow into small state nodes: initialize package, request version, update manifest, create downloader, download files, clear cache, start game.
- `Scripts/HotReload/HotReloadCtrl_Nodes/HotReloadCtrl_StartGame.cs` marks the hot-update flow complete by calling `SetFinish()`.

### Global runtime bootstrap

- `Scripts/GameManager.cs` is the persistent `MonoSingleton<GameManager>`.
- It sets frame rate and sleep policy in `Awake()`, creates `PurchaseManager` in `Init()`, captures `PlayCtrl.Instance` in `ReadyToPlay()`, and forwards `Update()` into `PlayCtrl.Update()`.
- It also owns the startup camera toggle used when gameplay levels are created or destroyed.

### Main play loop

- `Scripts/Play/PlayCtrl.cs` is the central runtime coordinator.
- It owns:
  - the state machine with `START`, `PLAYING`, `PAUSE`, `SUCCESS`, `FAILED`
  - the current `LevelCtrl`
  - the active level prefab instance (`BaseLevelCtrl LevelCtrl`)
  - the current gameplay UI panel (`GamePlayPanel`)
  - bag state (`SimpleBag`)
  - interstitial gating policy (`InterstitialAdPolicy`)
- It responds to events for entering a level, entering the next level, resetting, using tips, and switching state.
- It instantiates gameplay prefabs through `Resources.Load<BaseLevelCtrl>($"Level/{path}")`.
- It swaps the gameplay panel by opening `GamePlayPanel_<id>` resources based on the active level prefab's `GamePlayPanelName`.

### Level progression and level data

- `Scripts/Play/LevelCtrl.cs` is the progression model and the table-to-runtime bridge.
- It loads:
  - `TbLevelData` into `leveldatas`
  - `TbLevelSort` into `levelsort`
- It maps `DB.GameData.CurrentLevel` to the active `LevelSort` and `LevelData`.
- It owns `CurrentLevelState` (`LevelRuntimeState`) for per-run counters.
- It updates unlock and pass progress in `Victory()`, persists to `DB.GameData`, and recalculates availability in `RefreshLevel()`.
- It appends a synthetic waiting level in `AddWaitingForNextLevel()`.
- It can rewrite `levelsort` order from remote config when `level_config.enabled == true`.

### Level prefab contract

- `Scripts/Level/BaseLevelCtrl.cs` is the shared MonoBehaviour base class for level prefabs.
- It handles:
  - pause and resume event subscription
  - success and fail detection
  - success animation dispatch
  - retry trigger after failure
  - destroying the level root on unload
- Concrete or level-specific behavior is expected to extend this base.
- Level-specific scripts currently live under `Scripts/Level/LevelItem/Level_17` and `Scripts/Level/LevelItem/Level_2`.

### Gameplay UI input

- `UI/GamePanel/Scripts/GamePlayPanel.cs` is the runtime control surface.
- It converts button interaction into gameplay events such as:
  - move left and right
  - jump
  - reset
  - pause
  - open tips
  - open shop
- Its `Update()` loop continuously dispatches left and right movement while the corresponding button is held.
- It also owns fade transitions and panel-intro sequencing.

## Scripts Map

`Assets/Game/Scripts` contains 90 `.cs` files.

| Area | C# Count | Responsibility |
| --- | ---: | --- |
| `(root)` | 2 | Global entry files such as `GameManager.cs` and `Booster.cs`. |
| `Common` | 33 | Cross-cutting helpers, managers, UI button helpers, pooling, singleton base classes, screen fit, triggers, and utilities. |
| `Config` | 3 | Config table loading and remote-config accessors. |
| `HotReload` | 11 | Package bootstrap and hot-update FSM. |
| `Level` | 5 | Base level logic, success animation, and level-specific handlers. |
| `Play` | 17 | Main play controller, level progression, animals, level generation, and play-state nodes. |
| `Runtime` | 3 | Runtime helpers, currently focused on ads policy and ad helpers. |
| `SDK` | 16 | SDK adapters and services for ads, events, purchase, remote config, and analytics. |

Detailed notes:

- `Common`
  - `AudioManager`, `VibrationManager`, `JsonUtils`, `EventsUtils`, `InputUtils`, `LocalizatioinUtils`
  - UI button stack under `Common/Buttons`
  - object pooling under `Common/ObjectPool`
  - singleton base classes under `Common/Singleton`
  - camera and canvas fit helpers under `Common/ResolutionFit` and `Common/Others`
- `Config`
  - `ConfigManager.cs` loads Luban tables from `Resources` first, then `Assets/Game/Configs/*.json` via YooAsset
  - `ConfigManager.RemoteConfig.cs` stores raw remote JSON into `DB.GameData.RemoteConfigCache` and exposes typed getters
- `HotReload`
  - state names and events live next to controller and nodes
- `Play`
  - `PlayCtrl.cs` is the owner
  - `LevelCtrl.cs` owns progression
  - `LevelRuntimeState.cs` tracks per-level runtime state
  - `Animals/*` holds player-character related enums and classes
  - `LevelGeneration/*` looks like generation-time config and helper code
- `Runtime/Ads`
  - `InterstitialAdPolicy.cs` derives ad gating from remote config and persisted counters
  - helper classes wrap interstitial and reward ad usage
- `SDK`
  - `SDKImps/*` defines integration-facing types like `GameProtocol`, `GameRoot`, `GameAnalytics`, and `GameAdsSpec`
  - `Services/Core/*` holds the singleton service shell
  - `Services/Events/*` wraps event and analytics reporting
  - `Services/Purchase/*` owns IAP manager logic
  - `Services/Remote/*` stores remote data service abstractions

## Config, Save, and Data Sources

### Static config

- `Configs/tbleveldata.json`
- `Configs/tblevelsort.json`
- `Configs/tblanguage.json`
- `Configs/tbproducts.json`
- `Configs/tbshop.json`
- `Configs/lubanconfig_tbglobalconst.json`

These are the main table inputs consumed by `ConfigManager`.

### Save data

- `DB/GameData.cs` declares persisted gameplay fields.
- `DB/GameData_Partial.cs` exposes properties and default initialization.
- Important stored state includes:
  - current and max level
  - unlocked and passed levels
  - tool inventory and purchase cache
  - interstitial and reward ad timestamps
  - fail and success counters used by ad policy
  - per-level play statistics
  - remote config raw JSON cache

### Settings

- `DB/GameSetting.cs` and `DB/GameSetting_Partial.cs` pair with `GameSetting.asset`.
- `Settings/DBManager_Generated.cs` and `Settings/DBSetting.asset` support the save-system wiring.

## UI Map

`Assets/Game/UI` is resource-heavy and mixes generated view code with runtime panel logic.

Panel groups and file counts:

| Area | File Count | C# Count |
| --- | ---: | ---: |
| `CommonPopup` | 18 | 2 |
| `GamePanel` | 72 | 3 |
| `GameTipsPopup` | 37 | 4 |
| `GameVictory` | 31 | 2 |
| `Global` | 135 | 18 |
| `GM` | 16 | 5 |
| `LevelChoicePanel` | 76 | 4 |
| `Loading` | 45 | 2 |
| `NoAds` | 23 | 2 |
| `RatingPopup` | 38 | 3 |
| `SettingPopup` | 68 | 8 |
| `ShopPanel` | 69 | 6 |
| `StartPanel` | 15 | 2 |

Patterns worth remembering:

- Each panel area usually contains `Resources`, `Scripts`, and `Sprites`.
- Many panels have paired handwritten and generated view files such as `Panel.cs` plus `Panel.Generated.cs`.
- `GamePanel/Resources` contains multiple `GamePlayPanel_###.prefab` variants that `PlayCtrl` selects by name.
- `Global` is the shared UI infrastructure bucket and has the most scripts and common prefabs.

## Editor Automation

`Assets/Game/Editor` is small but important because it affects generated assets and scripts.

Key files:

- `LubanGenerator.cs`
- `ConfigTest.cs`
- `ExportCsvCharsWindow.cs`
- `SpritesAutoImportProcessor.cs`
- `TMPFontBatchSwitcher.cs`
- `SpriteAtlasPacker/*`
- `UIGenerator/*`

This folder is the likely owner when generated UI bindings, sprite atlas behavior, or config regeneration change.

## Content Assets

- `Prefabs/Animals/*` contains animal prefabs including `Sheep.prefab`.
- `Scenes/*` contains `Loading.unity` and `Main.unity`.
- `LevelConfig/*` currently contains at least `level001.game (1).json` and `level002.game.json`.

## High-Value Entry Files

Open these first when the task is structural or cross-cutting:

- `Assets/Game/Scripts/GameManager.cs`
- `Assets/Game/Scripts/Play/PlayCtrl.cs`
- `Assets/Game/Scripts/Play/LevelCtrl.cs`
- `Assets/Game/Scripts/Level/BaseLevelCtrl.cs`
- `Assets/Game/Scripts/Config/ConfigManager.cs`
- `Assets/Game/Scripts/Config/ConfigManager.RemoteConfig.cs`
- `Assets/Game/DB/GameData.cs`
- `Assets/Game/UI/GamePanel/Scripts/GamePlayPanel.cs`
- `Assets/Game/Scripts/Runtime/Ads/InterstitialAdPolicy.cs`
- `Assets/Game/Scripts/HotReload/HotReloadCtrl.cs`

## Maintenance Checklist

Update this record when any of the following changes happen:

- a new top-level folder appears under `Assets/Game`
- a runtime owner moves between `GameManager`, `PlayCtrl`, `LevelCtrl`, or `BaseLevelCtrl`
- a new play-state node or hot-reload node is added
- config tables or save-data fields are added, removed, or renamed
- a UI panel group is added or its loading path changes
- `Resources.Load` or YooAsset loading conventions change

When updating:

1. Re-check the affected directory and counts in the repo.
2. Update the ownership summary first.
3. Update inventory tables only where they changed.
4. Keep the file focused on structure and responsibility, not implementation trivia.
