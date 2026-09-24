# PipeMuzzle World-Aware Progression Design

## 1. Current-state findings

This design is based on commit `7d698fe` on `main`. The worktree was clean before this document was added.

The requested architectural problems are present:

- `Assets/Scripts/Data/WorldDefinition.cs` serializes an independent `levelCount` value. All three world assets currently store `levelCount: 12`; none owns level assets.
- `Assets/Scripts/Gameplay/GameController.cs` serializes one global `List<LevelDefinition> levels`, creates one parameterless `ProgressService`, loads level 0 in `Start`, and resolves all loading and unlocking against that global list.
- `Assets/Scripts/Gameplay/ProgressService.cs` reads and writes one global key, `PipeMuzzle.HighestUnlockedLevel`.
- `Assets/Scripts/Gameplay/WorldProgressService.cs` is not persistent and returns true only for `worldIndex == 0`.
- `Assets/Scripts/UI/WorldMapUI.cs` loads all world assets, sorts them by enum value, passes the loop index to `WorldProgressService`, and renders only locked/unlocked cards. Every unlocked card displays `12 LEVELS` and is interactable regardless of content.
- `Assets/Scripts/UI/StoryNavigationCoordinator.cs` already keeps the selected `WorldDefinition` from World Map through the comic and passes it to `LevelSelectUI.ConfigureForWorld`. `LevelSelectUI` keeps `currentWorld`, but it does not configure `GameController`; it intersects `currentWorld.LevelCount` with the unrelated global `gameController.LevelCount`.

Additional migration findings:

- `Assets/Scenes/Gameplay.unity` contains the global `GameController.levels` list with ordered references to `Level_001` through `Level_012`.
- The tracked recovery scene `Assets/_Recovery/0.unity` contains the same stale serialized list. It is not in `EditorBuildSettings`, but it must not retain obsolete serialized data if kept in source control.
- No prefab, utility, or other production script refers to the serialized `GameController.levels` field. Existing direct service consumers are `GameController`, `LevelSelectUI`, and `WorldMapUI`.
- The only existing world-progress test asserts the index-based first-launch rule. Existing story tests also assume every world reports 12 levels and must be updated because Bamboo and Moon will intentionally report zero actual levels.
- Sakura's twelve assets already exist once, with deterministic filenames and distinct GUIDs. They must be referenced, not copied.
- The current level-layout assets describe the same 3x4 coordinate grid. `Gameplay.unity` has no route-segment template assigned. This work must preserve the clean 3x4 grid and must not activate route-line rendering.

## 2. Goals and non-goals

### Goals

- Make each `WorldDefinition` the only owner and source of truth for its ordered playable levels.
- Keep Sakura's existing twelve level assets in order and leave Bamboo and Moon explicitly content-incomplete.
- Separate progression unlock from content readiness and derive playability from both.
- Isolate level progress and world progress by stable `WorldId` identity.
- Migrate the old global level progress once, only into Sakura, without losing valid progress.
- Pass one selected `WorldDefinition` through the existing World Map -> Story -> Level Select -> Gameplay flow.
- Persist final-world and inter-world progression without changing puzzle mechanics.
- Supply focused EditMode coverage for data, migration, progression, controller context, and derived world state.

### Non-goals

This phase does not create Bamboo or Moon levels, duplicate Sakura levels as placeholders, change any Sakura puzzle or pipe art, alter `BoardBuilder`, `ConnectionChecker`, `TileState`, `TileView`, `BoardView.cellSize`, add route lines, redesign World Map, create an ending cinematic, or add scoring, stars, achievements, profiles, save slots, cloud saves, or unrelated refactors.

## 3. Data model

`WorldDefinition` remains the content aggregate:

```csharp
WorldDefinition
- WorldId WorldId
- string DisplayName
- ComicStoryDefinition Story
- WorldLevelSelectTheme LevelSelectTheme
- LevelPathLayoutDefinition LevelPathLayout
- WorldGameplayTheme GameplayTheme
- IReadOnlyList<LevelDefinition> Levels
- int LevelCount                 // derived from Levels
- bool IsContentReady            // derived from Levels and the 12-level contract
```

The serialized `levelCount` field is removed. The replacement serialized field is a single ordered `List<LevelDefinition> levels`. The public API returns it as `IReadOnlyList<LevelDefinition>` and computes `LevelCount` as `levels?.Count ?? 0`.

The product-wide completeness contract is a code constant, `WorldDefinition.ExpectedLevelCount = 12`. It is not serialized per world and cannot drift independently from a world's list. The constant describes the game design contract; `LevelCount` describes actual content.

`WorldId` remains the domain identity. Its persistence segment must come from one explicit mapping rather than an array index, resource order, numeric enum value, or display name:

| `WorldId` | Stable persistence segment |
|---|---|
| `SakuraGarden` | `SakuraGarden` |
| `BambooWorkshop` | `BambooWorkshop` |
| `MoonShrine` | `MoonShrine` |

A single `WorldId` mapping helper should own these strings and reject unknown enum values. The enum values and stable string segments are compatibility identifiers: do not rename or reuse them. Resource ordering may still be chosen for presentation, but never for a save key or progression decision.

## 4. WorldDefinition ownership

`WorldDefinition.Levels` is the only source used by gameplay and level-select logic.

`SakuraGarden.asset` receives references to the existing assets in exactly this order:

1. `Level_001.asset`
2. `Level_002.asset`
3. `Level_003.asset`
4. `Level_004.asset`
5. `Level_005.asset`
6. `Level_006.asset`
7. `Level_007.asset`
8. `Level_008.asset`
9. `Level_009.asset`
10. `Level_010.asset`
11. `Level_011.asset`
12. `Level_012.asset`

No asset is copied or recreated. `BambooWorkshop.asset` and `MoonShrine.asset` receive empty level lists until their real puzzles are authored. Their `LevelCount` is therefore zero, even though the product contract expects twelve completed levels eventually.

No fallback may substitute Sakura levels when a world list is empty or invalid. A world with incomplete content fails closed and never reaches gameplay.

## 5. Content readiness contract

Content readiness is derived every time from `WorldDefinition`; it is never stored in `PlayerPrefs`.

A world is content-ready only when all of the following hold:

1. `Levels.Count == WorldDefinition.ExpectedLevelCount` (currently 12).
2. Every entry is non-null.
3. Every entry refers to a distinct `LevelDefinition` asset; repeating one level does not satisfy the complete-set contract.

The state model is computed from two independent facts:

```text
progressionUnlocked = WorldProgressService.IsWorldUnlocked(world.WorldId)
contentReady         = world.IsContentReady
playable             = progressionUnlocked && contentReady
```

Expose a small derived state such as `WorldAccessState.Locked`, `WorldAccessState.ComingSoon`, and `WorldAccessState.Playable` from `WorldProgressService.GetAccessState(WorldDefinition)`. The enum is a view of the two facts, not another persisted value.

| progressionUnlocked | contentReady | State | Playable |
|---:|---:|---|---:|
| false | false or true | `Locked` | no |
| true | false | `ComingSoon` | no |
| true | true | `Playable` | yes |

For the current content after Sakura's final completion, Sakura is `Playable`, Bamboo is `ComingSoon`, and Moon remains `Locked`.

## 6. GameController world handoff

`GameController` gains `ConfigureWorld(WorldDefinition world)` and loses its serialized global level list. Its level-facing members resolve only through `currentWorld`:

```text
LevelCount        -> currentWorld?.LevelCount ?? 0
LoadLevel(i)      -> currentWorld.Levels[i]
LoadNextLevel()   -> currentWorld.Levels[currentLevelIndex + 1]
IsLevelUnlocked() -> the ProgressService for currentWorld.WorldId
CompleteLevel()   -> level progress and final-world progress for currentWorld
```

The exact handoff point is `LevelSelectUI.ConfigureForWorld(selectedWorld)`. After validating the non-null world, it must call `gameController.ConfigureWorld(selectedWorld)` before refreshing buttons. This keeps the existing flow explicit:

```text
WorldMapUI.WorldSelected(WorldDefinition)
-> StoryNavigationCoordinator.selectedWorld
-> comic story
-> LevelSelectUI.ConfigureForWorld(selectedWorld)
-> GameController.ConfigureWorld(selectedWorld)
-> selectedWorld.Levels[selectedIndex]
```

No static/global selected-world holder is added. `StoryNavigationCoordinator` owns the selection during navigation, `LevelSelectUI` owns the currently displayed selection, and `GameController` owns the world currently configured for gameplay; the same object reference is passed at each boundary.

`ConfigureWorld` must:

- reject null or non-content-ready worlds with an error and leave gameplay unavailable;
- stop transient board visuals and clear current-level/completion context so state from a previous world cannot leak;
- create or select the world-scoped `ProgressService` using the stable world identity and actual level count;
- not load a level automatically.

`GameController.Start` continues to validate `BoardView`/camera references and subscribe to tile clicks, but it no longer loads global level 0. The first board is built only after a playable world is configured and a level button calls `LoadLevelByIndex`. This prevents a hidden Sakura board from being used for Bamboo or Moon.

Changing the world while gameplay is visible is not a supported navigation path; even so, `ConfigureWorld` must reset context safely. Puzzle construction, rotation, solved-path evaluation, move counting, visuals, and camera fitting remain unchanged.

## 7. Per-world level progress

`ProgressService` becomes explicitly world-scoped. Its constructor receives a `WorldId` and actual `levelCount`; it does not infer identity from display text or list position.

Level keys are:

```text
PipeMuzzle.Progress.World.SakuraGarden.HighestUnlockedLevel
PipeMuzzle.Progress.World.BambooWorkshop.HighestUnlockedLevel
PipeMuzzle.Progress.World.MoonShrine.HighestUnlockedLevel
```

Values remain zero-based highest-unlocked indexes. For a non-empty new world the default is 0, so Level 1 is available. For an empty world the in-memory value is -1 and every index is invalid/unlocked=false; normally such a service is not created because World Map blocks entry.

Rules:

- `IsLevelUnlocked(index)` returns false for negative indexes or `index >= levelCount`.
- `UnlockLevel(index)` ignores invalid indexes.
- A valid unlock writes only when `index` is greater than the stored value; progress is monotonic and `PlayerPrefs.Save()` follows a write.
- Loaded values are clamped to the current actual range before use. A value above `levelCount - 1` cannot make a nonexistent button playable.
- Progress from one `WorldId` is never read while another world is configured.

## 8. Legacy save migration

The legacy source key is exactly:

```text
PipeMuzzle.HighestUnlockedLevel
```

The one-time marker is:

```text
PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1
```

Migration runs before Sakura's new progress is read for gameplay. It never runs for Bamboo or Moon.

Algorithm:

1. If the V1 marker is 1, do nothing.
2. If the legacy key does not exist, write the V1 marker and save. Do not create progress for Bamboo or Moon.
3. If Sakura has no actual levels, write the marker and save without creating a valid level index.
4. Otherwise read the legacy value and clamp it to `0..Sakura.LevelCount - 1`.
5. Read Sakura's new-key value if present. Clamp it to the same range and persist the greater of the existing new value and the clamped legacy value. This prevents an old key from reducing progress made under the new architecture.
6. Write the V1 marker and call `PlayerPrefs.Save()` in the same migration operation.
7. Leave the legacy key intact. It is ignored after the marker is written; not deleting it makes rollback/recovery safer and avoids silently destroying old progress.

This is idempotent because the marker prevents reapplication, and the merge is monotonic even if execution is repeated before the marker write completes. Tests must control and clean only these explicit keys rather than calling `PlayerPrefs.DeleteAll()`.

## 9. Persistent world unlock progression

`WorldProgressService` accepts and returns `WorldId`, never an integer index. World unlock keys are:

```text
PipeMuzzle.Progress.World.SakuraGarden.Unlocked
PipeMuzzle.Progress.World.BambooWorkshop.Unlocked
PipeMuzzle.Progress.World.MoonShrine.Unlocked
```

World completion keys are:

```text
PipeMuzzle.Progress.World.SakuraGarden.Completed
PipeMuzzle.Progress.World.BambooWorkshop.Completed
PipeMuzzle.Progress.World.MoonShrine.Completed
```

On first use, Sakura is treated as unlocked and its key is seeded to 1; Bamboo and Moon default to locked. Unlock and completion writes are monotonic: there is no normal relock or uncomplete operation.

The progression chain is an explicit `WorldId` switch:

```text
SakuraGarden    -> BambooWorkshop
BambooWorkshop  -> MoonShrine
MoonShrine      -> no next world
```

It is not derived from enum arithmetic, a resource array, card order, or display name. `MarkWorldCompleted(worldId)` persists that world's `Completed` flag and unlocks the mapped next world if one exists. It is invoked only when the configured world's final actual level completes. Ordinary level completion only advances that world's level progress.

The completion flag is not duplicated highest-unlocked state: reaching the final level and actually solving the final level are different facts. The flag records the latter and gives Moon completion a small, persistent representation without introducing an ending subsystem.

## 10. World Map state model

`WorldMapUI` continues using the current card style. It queries each asset by `world.WorldId` and derives its access state:

- `Locked`: use the current locked visual and `LOCKED`; button disabled.
- `ComingSoon`: show the world's normal identity/theme treatment plus `COMING SOON`; button disabled, so neither story nor gameplay opens.
- `Playable`: show the normal card and actual level count; button enabled and raises `WorldSelected(world)`.

The card list may have an explicit presentation order, but the service call and every persistent lookup use `WorldId`. `Resources.LoadAll` order has no semantic meaning. Duplicate or missing `WorldId` assets are logged as configuration errors and skipped/fail closed rather than silently binding the wrong progress.

World Map state must refresh when the map is shown again so completing Sakura 12 immediately changes Bamboo from `Locked` to `ComingSoon`. Rebuild or update the existing cards; do not redesign them. The current route/card visual styling is outside this architecture pass.

## 11. Level Select behavior

The existing twelve-button 3x4 grid remains. No route segments, roadmap flow, or replacement layout is introduced.

After `ConfigureForWorld` successfully configures `GameController`, button `i` is interactable only when:

```text
i < currentWorld.LevelCount
&& gameController.IsLevelUnlocked(i)
```

The redundant comparison against an unrelated global controller list disappears because the controller now uses the same `currentWorld`. Every button at or beyond actual content count is disabled. A click revalidates the same bounds/unlock conditions before showing gameplay and loading the level.

Bamboo and Moon cannot currently reach Level Select because World Map reports `ComingSoon` or `Locked`. If navigation is invoked incorrectly, `ConfigureWorld` rejects their incomplete content and Level Select remains non-playable. No placeholder list is generated.

`LevelPathLayout` remains part of `WorldDefinition` because it is existing world presentation data, but this implementation must preserve the current grid positions and must not enable `RebuildRoute` or assign a route template. Any later removal of obsolete route APIs is a separate cleanup.

## 12. Final-level completion flow

For a non-final level, `GameController.CompleteLevel` unlocks only `currentLevelIndex + 1` in the current world's `ProgressService` and reports that a next level exists.

For the final actual level (`currentLevelIndex == currentWorld.LevelCount - 1`), it does not attempt to create or unlock Level 13. It calls `WorldProgressService.MarkWorldCompleted(currentWorld.WorldId)`:

- Sakura Level 12: persist Sakura completed; persist Bamboo unlocked. Because Bamboo content is incomplete, its derived state becomes `ComingSoon`, not playable.
- Bamboo Level 12, after real Bamboo content exists: persist Bamboo completed; persist Moon unlocked. Moon remains `ComingSoon` until its real twelve-level set exists.
- Moon Level 12, after real Moon content exists: persist Moon completed; there is no next world and no Level 13. Existing completion UI may continue showing its all-levels-complete result; no cinematic or ending flow is added.

Repeated final-level completion is safe because both completion and unlock writes are idempotent.

## 13. Serialized Unity asset and scene migration

Migration order matters:

1. Add `WorldDefinition.levels` and derived APIs while temporarily reading no data from it.
2. Populate `SakuraGarden.asset` with the existing twelve GUID references in filename order. Serialize empty `levels` arrays for Bamboo and Moon. Remove `levelCount` from all three assets.
3. Add asset validation tests and verify Sakura is complete while Bamboo/Moon are incomplete.
4. Update `GameController` to use `currentWorld`, then remove its serialized `levels` field. Do not use `[FormerlySerializedAs]`: this is a transfer of ownership to another object, not a field rename on the same object.
5. Open and resave `Assets/Scenes/Gameplay.unity` so the obsolete `levels:` YAML block is removed while `BoardView`, `BoardCameraFitter`, UI, and all unrelated references remain unchanged.
6. Also open/resave the tracked `Assets/_Recovery/0.unity` or narrowly remove its obsolete `GameController.levels` block. It is not a build scene, but leaving tracked stale data would make future recovery misleading.
7. Confirm no prefab, test fixture, recovery scene, or utility still serializes or writes the old list.

The scene must not gain a replacement selected-world reference. Runtime navigation supplies it explicitly. Review scene diffs narrowly; Unity resaves must not introduce unrelated object, layout, or art changes.

## 14. Error handling and invalid data behavior

- Null selected world: log an error, do not configure or load gameplay.
- Incomplete selected world: log a content/configuration error, clear prior world gameplay context, and keep entry disabled.
- Null, duplicate, too few, or too many level references: `IsContentReady == false`; no fallback or partial play.
- Invalid level index: `IsLevelUnlocked == false`; unlock/load calls return without changing state and may log a concise warning at the controller boundary.
- Unknown `WorldId`: persistence-key mapping throws/logs a configuration error and fails closed rather than sharing another world's key.
- Duplicate/missing world assets from `Resources`: log and skip/disable the invalid card; never infer identity from load order.
- Corrupt stored progress below or above range: clamp for the configured world's actual count; never unlock nonexistent content.
- Missing legacy key: mark migration handled without creating cross-world progress.
- Missing theme/story/layout on an otherwise complete world: report the existing presentation validation error. Content readiness describes the level set only; presentation validity is checked separately before navigation.

## 15. Test strategy

All new automated coverage is EditMode unless a focused existing integration seam makes a PlayMode test clearly necessary. Use temporary `ScriptableObject` instances or known project assets and destroy temporary Unity objects after each test. PlayerPrefs tests delete only the named test keys in setup/teardown.

### WorldDefinition and assets

- `LevelCount` changes with the serialized list and has no independent count field.
- `SakuraGarden.asset` has exactly 12 non-null, distinct entries in `Level_001` through `Level_012` order.
- Sakura is content-ready.
- Empty, short, overfull, null-containing, and duplicate-containing lists are not content-ready.
- Bamboo and Moon project assets have zero levels and are not content-ready.

### ProgressService

- Sakura and Bamboo keys do not collide; Bamboo and Moon keys do not collide.
- Unlock is monotonic and persists across service recreation.
- Negative and out-of-range indexes are rejected safely.
- Stored out-of-range progress is clamped to actual content.
- Legacy progress migrates only to Sakura.
- Constructing Bamboo/Moon services never consumes the legacy key.
- Migration with no legacy key is safe.
- Recreating the Sakura service after migration is idempotent.
- Negative and oversized legacy values clamp to Sakura's `0..11` range.
- Existing higher new-key Sakura progress is not reduced by legacy migration.

### WorldProgressService

- Fresh keys yield only Sakura unlocked.
- Completing an ordinary Sakura or Bamboo level does not unlock a world; use the controller completion seam or a service API that distinguishes final completion.
- Marking Sakura complete unlocks Bamboo and does not unlock Moon.
- Marking Bamboo complete unlocks Moon.
- Marking Moon complete records completion and unlocks nothing else.
- Unlock/completion persists after service recreation and repeated writes remain safe.
- Queries are keyed by `WorldId`, independent of resource/card order.

### Derived world/content state

- Locked complete or incomplete world -> `Locked`, not playable.
- Unlocked incomplete world -> `ComingSoon`, not playable.
- Unlocked complete world -> `Playable`.

### GameController and handoff

- With Sakura configured, `LevelCount` is 12 and each requested index resolves to the matching Sakura `LevelDefinition`.
- Switching between two complete test worlds changes `LevelCount`, loaded definition/board, and progress namespace.
- Progress and current-level state from the previous world do not leak after `ConfigureWorld`.
- Null/incomplete worlds and invalid indexes cannot load a board.
- Non-final completion unlocks only the next level in the same world.
- Final completion calls world completion and does not create Level 13.

### UI integration

- `WorldMapUI` maps the three derived states to label/interactability without asserting fragile colors or pixel layout.
- `LevelSelectUI.ConfigureForWorld` hands the same world reference to `GameController` before button refresh.
- Buttons beyond `currentWorld.LevelCount` are disabled.
- A `ComingSoon` world does not raise gameplay/story selection.
- Existing 12-node grid data remains 3x4 and no route segment is generated.

After implementation, run the relevant EditMode suite, inspect Unity console compilation errors, verify `Gameplay.unity` in Play mode for Sakura selection/loading, and run `git diff --check`.

## 16. Ordered implementation phases

1. **Lock tests around data ownership.** Add failing `WorldDefinition`/asset-order/content-readiness tests and update the old story assertion that all worlds have 12 actual levels.
2. **Move content ownership.** Add the derived `WorldDefinition` APIs, populate Sakura's ordered references, and leave Bamboo/Moon empty.
3. **Make level progress world-aware.** Add stable identity mapping, world-specific keys, bounds handling, and Sakura-only legacy migration with focused tests.
4. **Persist world progression.** Replace index-based `WorldProgressService` with stable-ID unlock/completion keys, explicit next-world mapping, and derived access state.
5. **Configure gameplay by world.** Add `GameController.ConfigureWorld`, remove automatic/global loading, keep puzzle mechanics untouched, and add context-leak/final-completion tests.
6. **Complete the handoff and UI state.** Configure the controller from `LevelSelectUI`, render `Locked`/`ComingSoon`/`Playable` in World Map, refresh on map entry, and preserve the 3x4 grid without route lines.
7. **Migrate serialized scenes.** Remove the obsolete global list from `Gameplay.unity` and tracked `_Recovery/0.unity`; review diffs for unrelated changes.
8. **Verify end to end.** Run EditMode tests, compile/check console, exercise Sakura -> story -> level grid -> gameplay, simulate Sakura final completion to confirm Bamboo `COMING SOON`, and run `git diff --check`.

Each phase should remain independently reviewable. Do not create Bamboo/Moon content to make tests pass.

## 17. Expected files affected

Expected production/data changes during implementation:

- `Assets/Scripts/Data/WorldDefinition.cs`
- a small stable-`WorldId` persistence mapping file under `Assets/Scripts/Data/` (or the equivalent single mapping colocated with `WorldDefinition`)
- `Assets/Resources/Worlds/SakuraGarden.asset`
- `Assets/Resources/Worlds/BambooWorkshop.asset`
- `Assets/Resources/Worlds/MoonShrine.asset`
- `Assets/Scripts/Gameplay/ProgressService.cs`
- `Assets/Scripts/Gameplay/WorldProgressService.cs`
- `Assets/Scripts/Gameplay/GameController.cs`
- `Assets/Scripts/UI/WorldMapUI.cs`
- `Assets/Scripts/UI/LevelSelectUI.cs`
- `Assets/Scripts/UI/StoryNavigationCoordinator.cs` only if a small explicit refresh/handoff call is cleaner there; do not add global state
- `Assets/Scenes/Gameplay.unity`
- `Assets/_Recovery/0.unity`

Expected test changes/additions under `Assets/Tests/EditMode/`:

- update `ComicStoryDefinitionTests.cs` to stop treating configured target count as actual content count
- replace/expand `WorldProgressServiceTests.cs`
- add focused `WorldDefinition`, `ProgressService`, `GameController`, and world-access/UI integration tests as needed
- add Unity `.meta` files for any new scripts/tests

No board, tile, pipe art, Sakura level definition, gameplay-theme art, or README file should change as part of this architecture implementation.

## Self-review result

- There is one source of actual level count: `WorldDefinition.Levels`.
- Content readiness and progression unlock are separate derived/persisted facts; playability is derived, never saved.
- Save identity uses explicit stable `WorldId` segments, never indices, enum arithmetic, resource order, or display names.
- Legacy progress migrates once and only to Sakura, is clamped, monotonic, and retained at the source key.
- Final-level behavior is explicit for all three worlds and never creates Level 13.
- The selected world is passed through existing instance references and reaches `GameController` at `LevelSelectUI.ConfigureForWorld`.
- Bamboo and Moon remain empty and cannot fall back to Sakura.
- The only new abstractions are the shared stable-ID mapping and derived access-state representation required to prevent duplicated/ambiguous state.
