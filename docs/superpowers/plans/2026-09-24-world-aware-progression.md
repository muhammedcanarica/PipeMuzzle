# PipeMuzzle World-Aware Progression Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make world content and progression independent so Sakura's twelve levels remain playable, Bamboo becomes `COMING SOON` after Sakura completion, and Moon unlocks after future Bamboo completion.

**Architecture:** `WorldDefinition` owns each ordered level list and derives readiness. `ProgressService` and `WorldProgressService` persist separate facts by stable `WorldId`; `LevelSelectUI.ConfigureForWorld` passes the selected world to `GameController` before a level can load. Existing board and screen systems remain in place.

**Tech Stack:** Unity 6000.3.9f1, C#, ScriptableObject, PlayerPrefs, uGUI, TextMesh Pro, Unity Test Framework EditMode tests.

**Spec:** `docs/superpowers/specs/2026-09-24-world-aware-progression-design.md`

## Global Constraints

- Finished worlds require exactly `12` distinct, non-null `LevelDefinition` references; actual `LevelCount` comes only from `Levels.Count`.
- Reuse `Assets/Scripts/Data/Level_001.asset` through `Level_012.asset` in order for Sakura. Bamboo and Moon level lists stay empty.
- Save identity uses the explicit stable `WorldId` segments `SakuraGarden`, `BambooWorkshop`, and `MoonShrine`, never an index, display name, or `Resources.LoadAll` order.
- Legacy `PipeMuzzle.HighestUnlockedLevel` migrates once only into Sakura; retain the legacy key.
- Preserve the 3x4 Level Select grid. Do not add route lines, placeholder levels, new pipe art, puzzle mechanics, or an ending flow.
- Preserve unrelated worktree edits. Before each commit inspect staged paths; do not push.
- Test in Unity EditMode after each code task. `Window > General > Test Runner > EditMode > Run All` is the project test entry point; use a selected test/class during red-green cycles. Inspect the Console for compile errors after script changes.

## File map

| File | Responsibility |
|---|---|
| `Assets/Scripts/Data/WorldDefinition.cs` | Ordered level ownership, derived count/readiness, existing theme/story data |
| `Assets/Scripts/Data/WorldIdPersistence.cs` + `.meta` | One explicit mapping from supported `WorldId` values to stable key segments |
| `Assets/Resources/Worlds/{SakuraGarden,BambooWorkshop,MoonShrine}.asset` | Serialized world content; Sakura references the existing levels |
| `Assets/Scripts/Gameplay/ProgressService.cs` | Bounds-checked level unlocks and Sakura-only legacy migration |
| `Assets/Scripts/Gameplay/WorldProgressService.cs` | World unlock/completion persistence and derived access state |
| `Assets/Scripts/Gameplay/GameController.cs` | Configured world, board load, and final-level completion dispatch |
| `Assets/Scripts/UI/LevelSelectUI.cs` | Controller handoff and bounds-checked 3x4 buttons |
| `Assets/Scripts/UI/WorldMapUI.cs` | Cards for locked, coming soon, playable; refresh on entry |
| `Assets/Scripts/UI/ScreenManager.cs` | Existing map-entry transition, if needed to trigger a card refresh |
| `Assets/Scenes/Gameplay.unity`, `Assets/_Recovery/0.unity` | Remove obsolete serialized controller list only |
| `Assets/Tests/EditMode/*.cs` + new `.meta` files | Focused data, save, controller, and UI behavior coverage |

## Review Focus

1. A saved unlock index above 11 must never expose a nonexistent button; Task 2 tests clamping.
2. An existing Sakura new-key save higher than the legacy value must survive migration; Task 2 tests the max merge.
3. An unlocked Bamboo with zero levels must display `COMING SOON` and emit no selection event; Tasks 3 and 5 test it.
4. Reconfiguring the controller from one world to another must clear the previous board and save namespace; Task 4 tests it.
5. A duplicate or unknown `WorldId` asset must not inherit another card's progress; Tasks 3 and 5 test fail-closed behavior.

---

### Task 1: Move ordered content into WorldDefinition

**Files:**
- Modify: `Assets/Scripts/Data/WorldDefinition.cs`
- Modify: `Assets/Resources/Worlds/SakuraGarden.asset`
- Modify: `Assets/Resources/Worlds/BambooWorkshop.asset`
- Modify: `Assets/Resources/Worlds/MoonShrine.asset`
- Modify: `Assets/Tests/EditMode/ComicStoryDefinitionTests.cs`
- Create: `Assets/Tests/EditMode/WorldDefinitionTests.cs` and `.meta`

**Interfaces:**
- Produces: `public const int ExpectedLevelCount = 12`, `IReadOnlyList<LevelDefinition> Levels`, `int LevelCount`, `bool IsContentReady`.
- Consumes: existing `LevelDefinition` assets. No new asset instances are created.

- [ ] **Step 1: Write failing EditMode tests.** In `WorldDefinitionTests`, load world assets with `AssetDatabase.LoadAssetAtPath<WorldDefinition>`. Assert `Sakura.Levels.Select(x => x.name)` equals `Enumerable.Range(1, 12).Select(i => $"Level_{i:000}")`; assert all entries are non-null and distinct; assert `LevelCount == 12 && IsContentReady`. Assert Bamboo/Moon `LevelCount == 0 && !IsContentReady`. For temporary ScriptableObjects, set private `levels` via `SerializedObject.FindProperty("levels")`, then test empty, 11 entries, 13 entries, one null, and one duplicated reference all return false. Change the story asset test's `world.LevelCount == 12` assertion to `world.LevelCount == (world.WorldId == WorldId.SakuraGarden ? 12 : 0)` while keeping story assertions.
- [ ] **Step 2: Run selected `WorldDefinitionTests` and changed story test in EditMode.** Expected red: `Levels`/`IsContentReady` absent and Bamboo/Moon still report 12; check that the failures are about the new contract, not test setup.
- [ ] **Step 3: Implement the minimal data API.** Replace `[SerializeField, Min(1)] int levelCount` with `[SerializeField] List<LevelDefinition> levels = new();`. Add `public IReadOnlyList<LevelDefinition> Levels => levels;`, `public int LevelCount => levels?.Count ?? 0;`, and `public bool IsContentReady => LevelCount == ExpectedLevelCount && levels.All(level => level != null) && levels.Distinct().Count() == ExpectedLevelCount;` with `using System.Collections.Generic; using System.Linq;`. Use `UnityEngine.Object` identity for distinct assets. Do not change story/theme fields.
- [ ] **Step 4: Migrate world YAML.** Add `levels:` to `SakuraGarden.asset` using these existing GUIDs, in order: `064af6107d3a84146bedbb6301ecb4a6`, `2451c317cd645a241ad9595acd5fb27f`, `a3b1fd79a2e391a43af0c7881f097188`, `41ed2a1e852b4f9386a0d9d3e04d9f11`, `b384d1c0dc0f4f2b9f7b410ed0f9f7c2`, `c6f7ab42e5314f2b90a1f8f6d4474f90`, `7a4e1c6825c14ac39ef06f7a9e1d4207`, `8b5f2d7936d25bd4a0f17e8b0f2e5318`, `9c6a3e8a47e36ce5b1f28f9c103f6429`, `a7b4f9b958f47df6c2a390ad2140753a`, `b8c50aca69a58e07d3b4a1be3251864b`, `c9d61bdb7ab69f18e4c5b2cf4362975c`. Each YAML reference uses `{fileID: 11400000, guid: <guid>, type: 2}`. Set `levels: []` in Bamboo/Moon and remove `levelCount:` from all three. Check every GUID against its `.meta` before editing.
- [ ] **Step 5: Run selected tests, then all EditMode tests.** Expect the new data tests to pass; the old index-based world service test remains unchanged for now. Inspect Console and `git diff --check`.
- [ ] **Step 6: Commit only Task 1 files** with `git commit -m "feat: make world definitions own ordered levels"` after reviewing `git diff --cached --stat`.

Implementation and test anchors:

```csharp
public const int ExpectedLevelCount = 12;
[SerializeField] private List<LevelDefinition> levels = new();
public IReadOnlyList<LevelDefinition> Levels => levels;
public int LevelCount => levels?.Count ?? 0;
public bool IsContentReady => LevelCount == ExpectedLevelCount &&
    levels.All(level => level != null) &&
    levels.Distinct().Count() == ExpectedLevelCount;

// WorldDefinitionTests: assert the serialized order, not just the count.
Assert.That(sakura.Levels.Select(level => level.name),
    Is.EqualTo(Enumerable.Range(1, 12).Select(i => $"Level_{i:000}")));
```

### Task 2: Isolate level saves and migrate Sakura progress

**Files:**
- Create: `Assets/Scripts/Data/WorldIdPersistence.cs` and `.meta`
- Modify: `Assets/Scripts/Gameplay/ProgressService.cs`
- Create: `Assets/Tests/EditMode/ProgressServiceTests.cs` and `.meta`

**Interfaces:**
- Produces: `WorldIdPersistence.Segment(WorldId id) : string`; `ProgressService(WorldId worldId, int levelCount)`; `HighestUnlockedLevelIndex`, `IsLevelUnlocked(int)`, `UnlockLevel(int)`.
- Consumes: `WorldId` from `WorldDefinition.cs`. `GameController` adopts the new constructor in Task 4; keep intermediate compilation green by updating its temporary `new ProgressService()` call to `new ProgressService(WorldId.SakuraGarden, 12)` only as a temporary compatibility bridge, then remove that bridge in Task 4. Do not introduce a persistent second constructor.

- [ ] **Step 1: Write failing tests.** Assert `Segment(SakuraGarden) == "SakuraGarden"`, likewise for Bamboo/Moon, and unknown `(WorldId)999` throws `ArgumentOutOfRangeException`. For `ProgressService`, clean only `PipeMuzzle.HighestUnlockedLevel`, all three new highest-level keys, and `PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1` in `[SetUp]`/`[TearDown]`. Test Sakura unlock to 3 leaves Bamboo/Moon at 0; Bamboo unlock to 4 leaves Moon at 0; unlock 3 then 2 remains 3 after service recreation; `-1` and `12` are invalid for a 12-level service; stored 99 reads as 11; a zero-count service reports -1 and unlocks nothing.
- [ ] **Step 2: Add migration tests.** With legacy key absent, constructing Sakura sets marker to 1 and starts at 0. With legacy 7, Sakura starts at 7 and Bamboo/Moon stay at 0. With legacy -9 and 99, Sakura reads 0 and 11 respectively. With Sakura new key 9 and legacy 4, result remains 9. With legacy 4, construct Sakura, then change legacy to 10 and reconstruct Sakura; result stays 4 because marker is set. Assert the legacy key remains present. Recreate services to prove persistence.
- [ ] **Step 3: Run selected EditMode tests red.** Expected failures: missing mapping/constructor or old global-key behavior. Do not accept failures caused by leaked PlayerPrefs from other tests.
- [ ] **Step 4: Implement stable mapping and world-scoped service.** Use an exhaustive switch in `WorldIdPersistence.Segment`; the default throws `ArgumentOutOfRangeException(nameof(id))`. Construct keys as `"PipeMuzzle.Progress.World." + Segment(id) + ".HighestUnlockedLevel"`. Reject negative or `>= levelCount` indexes. Read stored values with default 0, clamp to `0..levelCount - 1`, or use -1 for empty content. Write only monotonic valid unlocks; call `PlayerPrefs.Save()` after writes.
- [ ] **Step 5: Implement Sakura-only migration.** In the Sakura constructor path, check marker first. If unset, read legacy only when `PlayerPrefs.HasKey`; clamp to Sakura's actual count; merge with a present new key using `Mathf.Max`; write the merged new key if needed, then marker `1`, then `PlayerPrefs.Save()`. If legacy absent, write only the marker. Leave legacy key untouched. No Bamboo/Moon path reads legacy. For zero count, write marker without a level index as the approved spec states.
- [ ] **Step 6: Replace the old `GameController.Awake` construction with the temporary Sakura compatibility bridge stated above.** This is only to keep the project compiling until Task 4; do not change board behavior yet.
- [ ] **Step 7: Run `ProgressServiceTests` and all EditMode tests; inspect Console and `git diff --check`.** Commit Task 2 files, including the one-line controller bridge, with `git commit -m "feat: scope level progress and migrate Sakura saves"`.

Implementation and test anchors:

```csharp
public static string Segment(WorldId id) => id switch
{
    WorldId.SakuraGarden => "SakuraGarden",
    WorldId.BambooWorkshop => "BambooWorkshop",
    WorldId.MoonShrine => "MoonShrine",
    _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
};

var sakura = new ProgressService(WorldId.SakuraGarden, 12);
var bamboo = new ProgressService(WorldId.BambooWorkshop, 12);
sakura.UnlockLevel(3);
Assert.That(new ProgressService(WorldId.SakuraGarden, 12)
    .HighestUnlockedLevelIndex, Is.EqualTo(3));
Assert.That(bamboo.IsLevelUnlocked(3), Is.False);
```

### Task 3: Persist world unlocks and derive access state

**Files:**
- Modify: `Assets/Scripts/Gameplay/WorldProgressService.cs`
- Modify: `Assets/Tests/EditMode/WorldProgressServiceTests.cs`
- Create: `Assets/Tests/EditMode/WorldAccessStateTests.cs` and `.meta` if state cases do not fit cleanly in the service tests

**Interfaces:**
- Produces: `enum WorldAccessState { Locked, ComingSoon, Playable }`; `bool IsWorldUnlocked(WorldId id)`; `bool IsWorldCompleted(WorldId id)`; `void MarkWorldCompleted(WorldId id)`; `WorldAccessState GetAccessState(WorldDefinition world)`.
- Consumes: `WorldIdPersistence.Segment` and `WorldDefinition.IsContentReady`.
- `WorldMapUI` still calls `IsWorldUnlocked(int)` until Task 5; keep a temporary index overload mapping 0/1/2 to explicit IDs solely for intermediate compilation, then remove it in Task 5. This overload must never build a save key from the index.

- [ ] **Step 1: Replace the old index test with clean-key tests.** Clean only the six explicit `.Unlocked`/`.Completed` keys for three worlds. Assert first launch unlocks only Sakura. Assert Sakura completion unlocks Bamboo but not Moon, Bamboo completion unlocks Moon, Moon completion sets its completion flag and unlocks no extra world, and repeated completion is idempotent. Reconstruct service and assert state survives. Assert `(WorldId)999` fails closed. Exercise completion only through `MarkWorldCompleted`; Task 4 tests that ordinary level solves do not call it.
- [ ] **Step 2: Add derived-state tests.** Create temporary complete and incomplete `WorldDefinition` objects by `SerializedObject` assignment. With Bamboo locked, even a complete list returns `Locked`. After Sakura completion, empty Bamboo returns `ComingSoon`; a complete Bamboo returns `Playable`. Null world returns `Locked`. Assert access decisions are unchanged if the input world objects are presented in reverse order.
- [ ] **Step 3: Run selected tests red.** Expected failures: absent `WorldId` API/state and persistence.
- [ ] **Step 4: Implement keys and transitions.** Build `"PipeMuzzle.Progress.World." + Segment(id) + ".Unlocked"` and `.Completed`; first use treats Sakura as unlocked and writes its key to 1. `MarkWorldCompleted` writes current completion, then uses explicit switch `SakuraGarden => BambooWorkshop`, `BambooWorkshop => MoonShrine`, `MoonShrine => none`. Save only after a changed write. Unknown IDs throw/fail closed. `GetAccessState` checks unlock before content readiness and returns the three enum values. Place `WorldAccessState` in the same file unless it grows.
- [ ] **Step 5: Run selected and full EditMode tests; inspect Console and `git diff --check`.** Commit Task 3 files with `git commit -m "feat: persist world completion and access state"`.

Implementation and test anchors:

```csharp
public WorldAccessState GetAccessState(WorldDefinition world)
{
    if (world == null) return WorldAccessState.Locked;
    if (!IsWorldUnlocked(world.WorldId)) return WorldAccessState.Locked;
    return world.IsContentReady
        ? WorldAccessState.Playable
        : WorldAccessState.ComingSoon;
}

var progress = new WorldProgressService();
progress.MarkWorldCompleted(WorldId.SakuraGarden);
Assert.That(new WorldProgressService().IsWorldUnlocked(WorldId.BambooWorkshop),
    Is.True);
Assert.That(progress.IsWorldUnlocked(WorldId.MoonShrine), Is.False);
```

### Task 4: Make GameController use the configured world

**Files:**
- Modify: `Assets/Scripts/Gameplay/GameController.cs`
- Create: `Assets/Tests/EditMode/GameControllerWorldTests.cs` and `.meta`

**Interfaces:**
- Produces: `bool ConfigureWorld(WorldDefinition world)` (true only for unlocked, content-ready worlds); `WorldDefinition CurrentWorld { get; }`; `LevelDefinition CurrentLevelDefinition { get; }` (read-only loaded-level identity); existing `LevelCount`, `IsLevelUnlocked`, `LoadLevelByIndex`, `LoadNextLevel`, `RestartLevel` now use current world.
- Consumes: `ProgressService(WorldId, int)`, `WorldProgressService.MarkWorldCompleted(WorldId)`, `WorldDefinition.Levels`.
- `LevelSelectUI` calls `ConfigureWorld` in Task 5.

- [ ] **Step 1: Write focused controller tests.** Create a controller GameObject with configured `BoardView`/camera dependencies; if EditMode lifecycle prevents this, load `Gameplay.unity` additively via `EditorSceneManager.OpenScene` and restore the prior scene in teardown. Use Sakura and a second complete temporary Bamboo world containing the twelve existing level assets in reverse order, with isolated PlayerPrefs keys; mark Sakura complete in the fixture to unlock Bamboo before configuring it. Assert configuring Sakura sets `CurrentWorld` and `LevelCount == 12`; loading index 0 raises `LevelLoaded(1,12)` and sets `CurrentLevelDefinition == Sakura.Levels[0]`. Configure Bamboo and verify its first level and progress namespace change. Assert null, incomplete, or locked worlds clear `CurrentWorld`, `LevelCount`, `CurrentLevelNumber`, and `CurrentLevelDefinition`. Assert invalid indexes do not raise `LevelLoaded`.
- [ ] **Step 2: Add completion tests through the actual controller completion path.** Use twelve existing valid assets in a complete temporary world and isolate PlayerPrefs keys. Load an unlocked non-final or final index; invoke private `CompleteLevel` via reflection in the test after a board exists, without exposing a public cheat API. Seed the chosen final index through its world-specific highest-unlocked key before controller construction. Assert ordinary completion only unlocks the next level in that world and no next world. Assert Sakura final completion unlocks Bamboo, Bamboo final completion unlocks Moon, and Moon final completion writes `Completed` without Level 13. Assert `LevelCompleted(false)` and `HasNextLevel == false` on final completion.
- [ ] **Step 3: Run selected tests red.** Expected failure is missing `ConfigureWorld`/world-owned loading or leaked global state.
- [ ] **Step 4: Implement configuration.** Remove serialized `levels` and the temporary Sakura constructor bridge. Add `currentWorld`, `currentLevelDefinition`, and `worldProgressService`. On any call, stop transient effects, clear the displayed board with `boardView.Clear()` when present, then clear `board`, `currentLevelDefinition`, `currentLevelIndex`, `isCompleted`, and `solvedPath`; clear `progressService`. Reject null or any world whose `worldProgressService.GetAccessState(world) != WorldAccessState.Playable` before assignment. On success, set `currentWorld`, instantiate `ProgressService(world.WorldId, world.LevelCount)`, and return true. Do not call `LoadLevel` in `ConfigureWorld`.
- [ ] **Step 5: Change controller reads.** `LevelCount` comes from `currentWorld`; `LoadLevel` resolves `currentWorld.Levels[levelIndex]` and assigns `currentLevelDefinition`; all bounds and unlock checks remain. `Start` validates view/camera, adds the raycaster if needed, subscribes to `TileClicked`, and does not auto-load level 0. `RestartLevel` is a no-op when no board is loaded. `CompleteLevel` unlocks the next level for non-final solves, otherwise calls `MarkWorldCompleted(currentWorld.WorldId)` and raises existing `LevelCompleted(false)`. Retain board construction, rotation, feedback, evaluation, camera fitting, and events.
- [ ] **Step 6: Run controller tests and full EditMode suite; inspect Console and `git diff --check`.** Commit Task 4 files with `git commit -m "feat: configure gameplay from selected world"`.

Implementation and test anchors:

```csharp
public int LevelCount => currentWorld?.LevelCount ?? 0;
public WorldDefinition CurrentWorld => currentWorld;
public LevelDefinition CurrentLevelDefinition => currentLevelDefinition;
// In LoadLevel, after validating the index:
LevelDefinition level = currentWorld.Levels[levelIndex];
currentLevelDefinition = level;
// In CompleteLevel, after setting isCompleted:
if (HasNextLevel) progressService.UnlockLevel(currentLevelIndex + 1);
else worldProgressService.MarkWorldCompleted(currentWorld.WorldId);

// GameControllerWorldTests: invoke the existing completion path only after loading a board.
typeof(GameController).GetMethod("CompleteLevel",
    BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(controller, null);
```

### Task 5: Complete selected-world handoff and World Map states

**Files:**
- Modify: `Assets/Scripts/UI/LevelSelectUI.cs`
- Modify: `Assets/Scripts/UI/WorldMapUI.cs`
- Modify: `Assets/Scripts/UI/ScreenManager.cs` only to trigger a map refresh on entry if no smaller existing transition seam works
- Modify: `Assets/Scripts/UI/StoryNavigationCoordinator.cs` only if a small return-to-map refresh call is needed
- Create: `Assets/Tests/EditMode/WorldNavigationTests.cs` and `.meta`

**Interfaces:**
- Consumes: `GameController.ConfigureWorld(WorldDefinition)`, `WorldProgressService.GetAccessState(WorldDefinition)`.
- Produces: `WorldMapUI.Refresh()`; `LevelSelectUI.ConfigureForWorld(WorldDefinition)` keeps its current signature and calls the controller before button refresh.

- [ ] **Step 1: Write UI behavior tests.** Instantiate `WorldMapUI` under a temporary Canvas and call its existing `Initialize()` to use the three project assets. Assert Sakura card is enabled and clicking it emits that exact `WorldDefinition`; locked Moon says `LOCKED` and is disabled. After Sakura completion Bamboo says `COMING SOON`, retains world identity, is disabled, and never raises `WorldSelected`. Extract a private static `ValidateWorlds(IEnumerable<WorldDefinition>)` for the production resource path and call it via reflection in tests with duplicate/unknown IDs; assert each invalid fixture is skipped with an error. Assert `Refresh()` updates existing card state without duplicating cards.
- [ ] **Step 2: Write Level Select handoff tests.** With the existing twelve buttons or a small temporary UI fixture, call `ConfigureForWorld(Sakura)` and assert the controller's `CurrentWorld` is the same reference before button interactability is computed. Assert buttons beyond actual content count remain disabled. Call with null/incomplete world and assert no gameplay entry. Keep label/color assertions limited to required state text and interactability.
- [ ] **Step 3: Run selected tests red.** Expected failures: no controller handoff, binary map state, no refresh.
- [ ] **Step 4: Update `LevelSelectUI`.** Validate `world`; call `gameController.ConfigureWorld(world)` before assigning `currentWorld` and `RefreshButtons`; if false, keep `currentWorld = null` and return. In `RefreshButtons`, use `i < currentWorld.LevelCount && gameController.IsLevelUnlocked(i)`; remove the independent global count comparison. In `SelectLevel`, recheck `currentWorld`, bounds and unlock before `ShowGameplay()` and `LoadLevelByIndex`.
- [ ] **Step 5: Update `WorldMapUI`.** Continue loading world assets from `Resources/Worlds`, but validate non-null/known unique IDs. Keep only one asset for each known `WorldId`, log duplicates, and skip unknown IDs; cap rendering to three known worlds so the fixed `positions` array cannot overflow. Sort for visual order only. Replace `IsWorldUnlocked(index)` with `GetAccessState(world)`; store references to each card's `Button`, background `Image`, and status `TMP_Text` for `Refresh()`. State labels are `LOCKED`, `COMING SOON`, or `$"{world.LevelCount} LEVELS"`; only playable buttons are interactable. `Refresh()` changes existing cards without rebuilding routes/layout. Remove the temporary index overload from `WorldProgressService`.
- [ ] **Step 6: Refresh on map entry.** Use the smallest central seam: `ScreenManager.ShowWorldMap` may call `worldMapPanel.GetComponent<WorldMapUI>()?.Refresh()` after making the panel active. Check existing `LevelSelectUI.Start`, back button, and comic back paths all call `ShowWorldMap`; avoid a second refresh path. Do not enable `LevelSelectUI.RebuildRoute` or set a route template.
- [ ] **Step 7: Run UI tests and full EditMode suite, inspect Console and `git diff --check`.** In Play mode, verify Sakura map -> comic -> 3x4 grid -> Level 1 -> map. Commit Task 5 files with `git commit -m "feat: render playable and coming-soon worlds"`.

Implementation and test anchors:

```csharp
WorldAccessState state = progress.GetAccessState(world);
button.interactable = state == WorldAccessState.Playable;
status.text = state switch
{
    WorldAccessState.Locked => "LOCKED",
    WorldAccessState.ComingSoon => "COMING SOON",
    _ => $"{world.LevelCount} LEVELS"
};
// After Sakura final completion:
Assert.That(bambooButton.interactable, Is.False);
Assert.That(bambooStatus.text, Is.EqualTo("COMING SOON"));
```

### Task 6: Remove obsolete scene serialization

**Files:**
- Modify: `Assets/Scenes/Gameplay.unity`
- Modify: `Assets/_Recovery/0.unity`

**Interfaces:**
- Removes the obsolete `GameController.levels` serialized data; no replacement scene-level selected world field is added.

- [ ] **Step 1: Capture exact current YAML.** Confirm both scenes contain one `GameController` component with `boardView`, `boardCameraFitter`, and twelve `levels` references; record surrounding file IDs. Confirm `Gameplay.unity` is the enabled build scene and `_Recovery/0.unity` is tracked but not in build settings.
- [ ] **Step 2: Remove only each controller's obsolete `levels:` block.** Prefer a narrow text patch over an uncontrolled whole-scene reserialization. Keep all other component references, `LevelSelectUI` button list, Canvas, and grid coordinates unchanged. Do not add a serialized `currentWorld` field.
- [ ] **Step 3: Inspect both scene diffs and search all tracked scene/prefab/asset YAML for stale `GameController.levels`.** `rg -n -C 2 'levels:' Assets/Scenes Assets/_Recovery -g '*.unity'` should find no old controller block; unrelated `levels` fields elsewhere must be inspected by context rather than deleted.
- [ ] **Step 4: Open `Gameplay.unity` in Unity, verify no Missing Script or broken references, and run relevant EditMode tests.** Run `git diff --check`; stage only these two scenes and commit `git commit -m "chore: remove global level list from scenes"`.

### Task 7: End-to-end verification and review

**Files:**
- Verify: `Assets/Scenes/Gameplay.unity`
- Verify: `Assets/Tests/EditMode/*.cs`
- Verify: all touched files from Tasks 1–6

**Interfaces:** None new.

- [ ] **Step 1: Run all EditMode tests from Unity Test Runner.** Record total tests, passed, failed, and skipped. Inspect Unity Console for compile/errors. If Unity cannot be launched, report that limitation explicitly; do not claim tests passed.
- [ ] **Step 2: Exercise first-launch and migrated saves with isolated named PlayerPrefs keys.** Confirm Sakura Level 1 entry; legacy Sakura progress retains its unlocked level; Bamboo/Moon level keys remain separate; restore only test keys afterward.
- [ ] **Step 3: Exercise the runtime flow.** From World Map, select Sakura, view/skip story, confirm twelve 3x4 buttons, open Level 1, restart/return, and verify map cards. Complete Sakura Level 12 using a controlled test save or test seam and confirm Bamboo shows `COMING SOON` without opening story/gameplay; Moon remains `LOCKED`. For future complete test fixtures, verify Bamboo 12 unlocks Moon and Moon 12 records completion with existing `ALL LEVELS COMPLETE!` UI and no Level 13.
- [ ] **Step 4: Inspect asset and scene diffs.** Verify no Sakura levels or pipe art changed, no Bamboo/Moon placeholder level references, no route template or new route lines, and no unrelated scene edits. Search save-key construction for any index/display-name/resource-order identity.
- [ ] **Step 5: Run `git diff --check` and `git status --short`; review the commits and summarize results.** State exact tests run, any unrun validation, affected files, and remaining risks. Do not push.

## Plan self-review

- The seventeen spec sections map to Tasks 1–7: ownership/readiness (1), per-world save and migration (2), world unlock/state (3), controller/final completion (4), map/level-select/handoff (5), serialized scenes (6), and integration/error validation (7).
- No task introduces a second serialized level count or a content-ready save key.
- The only temporary compatibility APIs are the Task 2 controller bridge and Task 3 index overload; Tasks 4 and 5 remove them.
- The save segments, migration marker, and completion rules match the approved spec.
- `GameUI` already handles `LevelCompleted(false)` with `ALL LEVELS COMPLETE!`; no UI ending work is required.
- All five Review Focus conditions have a corresponding task test.
