---
name: sheepdemo-game-structure
description: Analyze, summarize, and maintain the architecture record for `SheepDemo/Assets/Game` in this Unity project. Use when changing gameplay flow, level logic, config or save data, ads or SDK integration, hot reload, or UI panels under `Assets/Game`, and when Codex needs a fast map of where related code and assets live.
---

# Sheepdemo Game Structure

## Overview

Use this skill to rebuild context for `SheepDemo/Assets/Game` before changing gameplay code or content.
Read [references/assets-game-structure.md](references/assets-game-structure.md) first, then open the smallest set of owner files needed for the task.

## Quick Start

1. Read [references/assets-game-structure.md](references/assets-game-structure.md).
2. Classify the task into one of these areas: startup or hot reload, play loop, level runtime, config or save data, ads or SDK, or UI.
3. Open the owner entry points from the matching section before editing.
4. After structural changes, update the reference file in the same task so the recorded map stays current.

## Owner Map

- Startup and package flow: `SheepDemo/Assets/Game/Scripts/HotReload/*`
- Global runtime bootstrap: `SheepDemo/Assets/Game/Scripts/GameManager.cs`
- Main play state machine: `SheepDemo/Assets/Game/Scripts/Play/PlayCtrl.cs`
- Level progression and table mapping: `SheepDemo/Assets/Game/Scripts/Play/LevelCtrl.cs`
- Level prefab contract: `SheepDemo/Assets/Game/Scripts/Level/BaseLevelCtrl.cs`
- Config tables and remote config: `SheepDemo/Assets/Game/Scripts/Config/*`
- Save data and persistent state: `SheepDemo/Assets/Game/DB/*`
- Ads and SDK wrapping: `SheepDemo/Assets/Game/Scripts/Runtime/Ads/*`, `SheepDemo/Assets/Game/Scripts/SDK/*`
- Runtime UI panels: `SheepDemo/Assets/Game/UI/*`
- Editor automation for this area: `SheepDemo/Assets/Game/Editor/*`

## Update Rules

- Treat [references/assets-game-structure.md](references/assets-game-structure.md) as the persistent memory for this code area.
- When adding, moving, or deleting folders, panels, state nodes, config tables, or ownership boundaries under `Assets/Game`, update the reference file in the same change.
- Prefer recording ownership, data flow, and entry points over line-by-line implementation detail.
- Refresh counts or inventory notes only after re-checking them in the repo.
- If a task changes behavior but not structure, update the relevant subsection summary instead of adding noise.

## Reference File

Read [references/assets-game-structure.md](references/assets-game-structure.md) for the current snapshot, subsystem map, and maintenance checklist.
