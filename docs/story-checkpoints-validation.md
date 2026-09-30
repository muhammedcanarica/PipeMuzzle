# Story checkpoints and restored World Map

The World Map presentation was restored from `1a1b209876745b5ddbb20cafb493db2df1ea0009`, before `17e273c` introduced destination illustrations. Only `Assets/Scripts/UI/WorldMapUI.cs` and its presentation assertions in `Assets/Tests/EditMode/Editor/WorldNavigationTests.cs` were restored. Current text raycast protection remains. Experimental art files are retained but no longer used by this map. No scene, puzzle, level, or progression-service code was reset.

## Model and persistence

`Assets/Scripts/Data/StoryCheckpoint.cs` defines a serializable checkpoint with `completedLevelNumber` and `ComicStoryDefinition story`. Each `WorldDefinition` owns a `storyCheckpoints` list. Zero means entry, followed by 3, 6, 9 and 12. Legacy `story` references remain for older/custom definitions.

`Assets/Scripts/Gameplay/StoryCheckpointProgress.cs` records a checkpoint when its presentation begins, including skip/back exits. Keys are `PipeMuzzle.Story.World.{WorldIdPersistence.Segment(world)}.Checkpoint.{completedLevelNumber}.Viewed`. They are separate from existing progression keys. A new service instance reads the same saved state. Display names never form keys.

`GameUI` reads prior unlock/world-completion state on `LevelLoaded`, before `GameController` updates progression. This suppresses stories for both replayed checkpoint levels and levels completed in older saves. `GameController`, `WorldProgressService`, board generation and pipe behavior are unchanged.

`StoryNavigationCoordinator` plays the unviewed Intro on entry. Intermediate completion stories return directly to that world's Level Select. Final stories return to the existing World Complete controls, including Next World/Back To Map; Moon has no successor. Intro Back returns to the map; checkpoint Back/Skip completes the checkpoint's contextual return.

## Created stories and grouping

Panels remain in the supplied sheets' reading order. No dialogue or replacement art was generated.

| World | Intro | After03 | After06 | After09 | Final |
|---|---|---|---|---|---|
| Sakura | 01–04 | 05–06 | 07 | 08 | 09–11 |
| Bamboo | 01–04 | 05–06 | 07 | 08 | 09–11 |
| Moon | 01–03 | 04 | 05 | 06–07 | 08 |

Created `ComicStoryDefinition` assets, each with a matching `.meta`:

- `Assets/Data/Stories/Sakura_Intro.asset`
- `Assets/Data/Stories/Sakura_After03.asset`
- `Assets/Data/Stories/Sakura_After06.asset`
- `Assets/Data/Stories/Sakura_After09.asset`
- `Assets/Data/Stories/Sakura_Final.asset`
- `Assets/Data/Stories/Bamboo_Intro.asset`
- `Assets/Data/Stories/Bamboo_After03.asset`
- `Assets/Data/Stories/Bamboo_After06.asset`
- `Assets/Data/Stories/Bamboo_After09.asset`
- `Assets/Data/Stories/Bamboo_Final.asset`
- `Assets/Data/Stories/Moon_Intro.asset`
- `Assets/Data/Stories/Moon_After03.asset`
- `Assets/Data/Stories/Moon_After06.asset`
- `Assets/Data/Stories/Moon_After09.asset`
- `Assets/Data/Stories/Moon_Final.asset`

## Cropped panel assets

The PNGs are byte-identical copies of the supplied images; crops are imported sub-sprites, rather than separate generated PNGs:

- `Assets/Art/Stories/SakuraCheckpointSheet.png`: `Sakura_Checkpoint_01` through `Sakura_Checkpoint_11`.
- `Assets/Art/Stories/BambooCheckpointSheet.png`: `Bamboo_Checkpoint_01` through `Bamboo_Checkpoint_11`.
- `Assets/Art/Stories/MoonCheckpointSheet.png`: `Moon_Checkpoint_01` through `Moon_Checkpoint_08`.

All 30 crops were visually inspected. Actual unequal panel boundaries were determined from source pixels, with an eight-pixel inset removing page gutters and rounded white corners. PNGs retain native resolution; imports use bilinear filtering and uncompressed textures. The existing one-Image, preserve-aspect viewer remains in use. Bamboo panel 08 is naturally panoramic.

## Verification

- Focused checkpoint data/persistence/integration EditMode tests: **86/86 passed**. The 66 integration cases use real `GameController.LoadLevelByIndex`, its completion event, `GameUI`, the coordinator, viewer and screen manager; completion is invoked to simulate a solved board, without changing puzzle logic.
- Sakura, Bamboo and Moon 3/6/9/12: passed for both Skip and story end. Existing next-world unlock behavior and Moon's terminal state passed.
- Replay, old progress, migrated Sakura progress: no repeated checkpoint story.
- All three Intro → Level Select → reentry cases passed.
- Intermediate → same-world Level Select, Final → World Complete, and checkpoint Back paths passed.
- `dotnet build Assembly-CSharp.csproj`: passed, 0 warnings / 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj`: passed, 0 warnings / 0 errors.
- Complete EditMode suite: **292/292 passed**, 0 failed / 0 skipped.
- `git diff --check`: exit 0; Git only reported line-ending normalization warnings.
- Restored map was inspected in Play Mode/Game View.
- Play Mode/Game View: Sakura Intro panel 01 was displayed, then the Advance button was invoked and panel 02 appeared in the same active Image after its fade. The frame contained exactly one Image, with preserve-aspect enabled; neither adjacent panels nor white page framing appeared. Bamboo and Moon Intro panel 01 were also displayed and inspected with one active Image each. Screenshots are under `Temp/StoryCheckpointValidation/`. Unity Console returned 0 errors after these checks.

Live first-completion validation requires temporary changes to the current player's save state. Automatic approval review rejected that bulk operation before it ran. Approval to snapshot, temporarily configure, and restore those values is pending; EditMode harnesses restore their own preference snapshots.

Remaining manual checks: first-completion navigation transitions in the live save session, physical tap/fade timing on a device, portrait/landscape presentation, and readability of the panoramic Bamboo panel. No push was requested.

The passing automated checks and the safe Game View inspection support one local commit. Live first-completion navigation remains a manual check because the current save session cannot be reset by the rejected bulk PlayerPrefs operation. Existing unrelated local artwork, project settings and recovery files are preserved outside the commit.
