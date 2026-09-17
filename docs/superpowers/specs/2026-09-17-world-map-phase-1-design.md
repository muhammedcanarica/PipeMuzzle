# PipeMuzzle World Map Phase 1 Design

## Scope

This phase creates only the world-data foundation and navigation shell for Sakura Garden, Bamboo Workshop, and Moon Shrine. It does not add comics, themed pipes, new puzzle layouts, or redesigned level-select visuals.

## Existing System

`Gameplay.unity` has one screen-space Canvas with a 1920x1080 `CanvasScaler`, `LevelSelectUI`, `GameUI`, and the working `GameController`. `LevelSelectUI` owns the existing level-select and gameplay-HUD visibility. `GameController` owns the current 12-level board sequence and must not be changed.

## Data

`WorldDefinition` is a ScriptableObject with a stable `WorldId`, display name, unlock order, and level count fixed at 12. It also contains nullable placeholder fields for the future story, level-select theme, and puzzle theme. Three assets are created: Sakura Garden (order 0), Bamboo Workshop (order 1), and Moon Shrine (order 2).

`WorldProgressService` exposes only the initial unlocked-world check for Phase 1. It starts at 0, so Sakura is unlocked and Bamboo/Moon remain visible but locked. It has no world-completion write path in this phase.

## Screen Ownership

One `ScreenManager` component is attached to the existing Canvas. It is the sole owner of the three major panels: `worldMapPanel`, the existing `levelSelectPanel`, and the existing `gameplayHud`. It exposes `ShowWorldMap()`, `ShowLevelSelect()`, and `ShowGameplay()`. Each transition sets all three panel states explicitly; no other component activates or deactivates a major panel.

`WorldMapUI` owns only destination cards, lock visuals, and `WorldDefinition` display data. It raises a Sakura-selection event. `ScreenManager` subscribes to that event and calls `ShowLevelSelect()`.

`LevelSelectUI` keeps its existing level-button and unlock behavior. Its Back button delegates to `ScreenManager.ShowWorldMap()`. Selecting a level delegates the major panel transition to `ScreenManager.ShowGameplay()` before retaining the existing `GameController.LoadLevelByIndex()` call.

At every transition, the state is explicit:

| State | World Map | Level Select | Gameplay HUD |
|---|---:|---:|---:|
| World Map | active | inactive | inactive |
| Existing Level Select | inactive | active | inactive |
| Existing Puzzle | inactive | inactive | active |

## World Map Layout

The map is built as a child panel of the existing Canvas using stretch anchors and the existing 1920x1080 scaler. It uses simple Unity UI shapes and text: a soft background, a vertical curved-looking route made from anchored segments, and three destination cards. The reference image is not imported, assigned, or displayed at runtime.

Sakura Garden appears at the bottom and is interactable. Bamboo Workshop and Moon Shrine appear above it with disabled buttons and a subtle lock label/icon. No new large art assets are introduced in this phase.

## Validation

1. Enter Play mode at 16:9 and a portrait Game View size.
2. Confirm only World Map is visible at launch.
3. Confirm Bamboo and Moon are visible but cannot be selected.
4. Select Sakura and confirm only the current Level Select is visible.
5. Select Level 1 and confirm the current puzzle opens and rotates normally.
6. Use the level-select Back button and confirm only World Map is visible.
7. Confirm the Unity Console has no errors and build `Assembly-CSharp.csproj` without restore.
