# PipeMuzzle World Map Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a three-world navigation shell with one authoritative major-screen state manager while preserving the current twelve-level puzzle flow.

**Architecture:** `ScreenManager` is attached to the existing Canvas and alone activates the World Map, Level Select, and Gameplay HUD panels. `WorldMapUI` presents world cards and raises selection events; `LevelSelectUI` delegates only major-screen transitions to the ScreenManager.

**Tech Stack:** Unity 6, C#, uGUI, TextMesh Pro, ScriptableObject, PlayerPrefs.

**Spec:** `docs/superpowers/specs/2026-09-17-world-map-phase-1-design.md`

## Global Constraints

- Keep the existing `Gameplay.unity` Canvas and its 1920x1080 `CanvasScaler`; do not create another Canvas.
- Do not modify `GameController`, pipe rotation, connectivity, solution checking, board generation, or completion behavior.
- Do not import or display the World Map reference PNG.
- Do not implement comics, world completion, themed pipes, themed level selects, or new puzzle layouts.
- Sakura Garden is the sole unlocked world; Bamboo Workshop and Moon Shrine remain visible but locked.
- Every world definition has `levelCount = 12`.

---

### Task 1: Add data and screen-state units

**Files:**
- Create: `Assets/Scripts/Data/WorldDefinition.cs`
- Create: `Assets/Scripts/Gameplay/WorldProgressService.cs`
- Create: `Assets/Scripts/UI/ScreenManager.cs`
- Test: `Assets/Tests/EditMode/WorldProgressServiceTests.cs`

**Interfaces:**
- Produces `WorldDefinition`, `WorldProgressService.IsWorldUnlocked(int)`, and `ScreenManager.ShowWorldMap()`, `ShowLevelSelect()`, `ShowGameplay()`.

- [ ] **Step 1: Write the failing world-state test**

```csharp
[Test]
public void OnlySakuraIsUnlockedAtFirstLaunch()
{
    var progress = new WorldProgressService();
    Assert.That(progress.IsWorldUnlocked(0), Is.True);
    Assert.That(progress.IsWorldUnlocked(1), Is.False);
    Assert.That(progress.IsWorldUnlocked(2), Is.False);
}
```

- [ ] **Step 2: Run the EditMode test and verify it fails because `WorldProgressService` is missing.**

- [ ] **Step 3: Implement minimal data/state classes.** `WorldDefinition` serializes `worldId`, `displayName`, `unlockOrder`, and `levelCount`; `WorldProgressService.IsWorldUnlocked` returns true only for `worldIndex == 0`; `ScreenManager` holds serialized panel references and each show method explicitly sets all three active states.

- [ ] **Step 4: Run the EditMode test and build `Assembly-CSharp.csproj --no-restore`.**

### Task 2: Build a single-canvas World Map panel

**Files:**
- Create: `Assets/Scripts/UI/WorldMapUI.cs`
- Modify: `Assets/Scenes/Gameplay.unity`
- Create: `Assets/ScriptableObjects/Worlds/SakuraGarden.asset`
- Create: `Assets/ScriptableObjects/Worlds/BambooWorkshop.asset`
- Create: `Assets/ScriptableObjects/Worlds/MoonShrine.asset`

**Interfaces:**
- Consumes `WorldDefinition`, `WorldProgressService`, and `ScreenManager`.
- Produces `WorldMapUI.WorldSelected` and an existing-Canvas `WorldMapPanel`.

- [ ] **Step 1: Add an initially inactive stretch-anchored `WorldMapPanel` under the existing Canvas.** Use the current Canvas Scaler and a soft plain background, route segments, and three destination cards; do not add a Canvas component or runtime reference art.

- [ ] **Step 2: Add `WorldMapUI` to the panel.** It renders names, applies the locked state to Bamboo/Moon, and invokes `WorldSelected` only for an unlocked card.

- [ ] **Step 3: Create the three `WorldDefinition` assets with `levelCount: 12`; assign them to `WorldMapUI` in scene serialization.**

- [ ] **Step 4: Bind `WorldMapUI.WorldSelected` to `ScreenManager.ShowLevelSelect()` for Sakura.**

- [ ] **Step 5: Enter Play mode at 16:9 and portrait sizes; confirm the map fits and only Sakura is interactable.**

### Task 3: Integrate existing Level Select transitions

**Files:**
- Modify: `Assets/Scripts/UI/LevelSelectUI.cs`
- Modify: `Assets/Scenes/Gameplay.unity`

**Interfaces:**
- Consumes `ScreenManager.ShowWorldMap()` and `ScreenManager.ShowGameplay()`.
- Preserves `GameController.LoadLevelByIndex(int)`.

- [ ] **Step 1: Add failing test coverage or a disposable PlayMode assertion that `ShowWorldMap` leaves only the map panel active.**

- [ ] **Step 2: Add serialized `ScreenManager` and Back-button references to `LevelSelectUI`.**

- [ ] **Step 3: Change only major-panel calls:** `ShowLevelSelect()` delegates to `ScreenManager.ShowLevelSelect()`, level selection calls `ScreenManager.ShowGameplay()` before the existing level load, and Back delegates to `ScreenManager.ShowWorldMap()`.

- [ ] **Step 4: Wire the existing Level Select Back button in `Gameplay.unity`; do not change level buttons or GameController bindings.**

- [ ] **Step 5: Verify the flow:** launch → World Map → Sakura → Level Select → Level 1 → puzzle → Levels → Level Select → Back → World Map. Confirm no panel overlap and no Console errors.

### Task 4: Final validation

**Files:**
- Test: `Assets/Tests/EditMode/WorldProgressServiceTests.cs`
- Test: `Assets/Scenes/Gameplay.unity`

- [ ] **Step 1: Run EditMode tests.**

- [ ] **Step 2: Run `dotnet build Assembly-CSharp.csproj --no-restore`.**

- [ ] **Step 3: Run `git diff --check` and inspect `git status --short`.**

- [ ] **Step 4: Check Unity Console after the full navigation path at two Game View aspect ratios.**
